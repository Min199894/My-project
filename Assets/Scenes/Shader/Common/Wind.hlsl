#include "LitInput.hlsl"

struct WindInput
{
    // Global
    float3 direction;
    float speed;
                
    // Per-Object
    float3 objectPivot;
    float fade;
                
    // Per-Vertex
    float phaseOffset;
    float3 normalWS;
    float mask;
    float flutter;
};

float3 GetObjectPivot()
{
    return GetAbsolutePositionWS( float3(UNITY_MATRIX_M[0].w, UNITY_MATRIX_M[1].w, UNITY_MATRIX_M[2].w) );
}

float4 SmoothCurve( float4 x )
{
    return x * x *( 3.0 - 2.0 * x );
}

float4 TriangleWave( float4 x )
{
    return abs( frac( x + 0.5 ) * 2.0 - 1.0 );
}

float4 SmoothTriangleWave( float4 x )
{
    return SmoothCurve( TriangleWave( x ) );
}

float4 FastSin( float4 x )
{
    #ifndef PI
    #define PI 3.14159265
    #endif
    #define DIVIDE_BY_PI 1.0 / (2.0 * PI)
    return (SmoothTriangleWave( x * DIVIDE_BY_PI ) - 0.5) * 2;
}

float3 GetWindDirection()
{
    return g_WindDirection != 0
    ? normalize(float3(g_WindDirection.x, 0, g_WindDirection.z))
    : float3(1, 0, 0);
}

float GetWindSpeed()
{
    return g_Wind.x;
}

float GetWindStrength()
{
    return g_Wind.y * _WindStrength;
}

float GetWindVariation(float3 objectPivot ) // The object pivot in world space.
{
    return 1.0 - frac( objectPivot.x * objectPivot.z * 10.0 ) * _WindVariation;
}

void PerlinNoise( float2 uv, float scale, out float noise )
{
    noise =
        tex2Dlod(
            g_PerlinNoise,
            float4(uv.xy, 0, 0) * g_PerlinNoiseScale).r;
}

float PerVertexPerlinNoise( float3 objectPivot )
{
               
    float noise;
    PerlinNoise( objectPivot.xz, g_PerlinNoiseScale, noise );
    return noise;
}

float3 SampleGust(
                float3 objectPivot,
                float3 vertexWorldPosition,
                float3 windDirection,
                float phaseOffset,
                float edgeFlutter,
                float lod,
                float2 windOffset )
{
    #if defined(_TYPE_TREE_LEAVES) || defined(_TYPE_TREE_BARK)
        lod = 5;
    #endif
    
    windOffset -= phaseOffset.xx * 0.05;
    #if defined(_TYPE_TREE_LEAVES)
    float3 vertexOffset = vertexWorldPosition - objectPivot;
    float2 offset = (objectPivot.xz + g_FloatingOriginOffset_Gust.xy) * 0.02 - windOffset.xy + vertexOffset.xz * 0.0075 * edgeFlutter;
    #else
    float2 offset = (objectPivot.xz + g_FloatingOriginOffset_Gust.xy) * 0.02 - windOffset.xy;
    #endif

    float strength = tex2Dlod( g_GustNoise, float4(offset, 0, lod) ).r;
    return strength * windDirection;
}

void GetFade(float3 objectPivot, out float windFade,out float scaleFade )
{
    #if defined(_TYPE_TREE_LEAVES) || defined(_TYPE_TREE_BARK)
        windFade = 1.0;
        scaleFade = 1.0;
    #else
        float distanceToCamera = distance( objectPivot, _WorldSpaceCameraPos );
        windFade = 1.0 - saturate( (distanceToCamera - _WindFade.x) / _WindFade.y );
        scaleFade = 1.0 - saturate( (distanceToCamera - _ScaleFade.x) / _ScaleFade.y );
    #endif
    
}

float GetHeightMask(float4 vertexColor, float2 uv1) 
{
    #if defined(_TYPE_TREE_LEAVES) || defined(_TYPE_TREE_BARK)
        return uv1.y;
    #else
        return vertexColor.r;
    #endif
}
            
float GetPhaseOffset(float4 vertexColor) 
{
    #if defined(_TYPE_TREE_LEAVES) || defined(_TYPE_TREE_BARK)
        return vertexColor.r;
    #else
        return 1.0 - vertexColor.g;
    #endif
}

float GetVertexMask( float4 vertexColor )
{
    #if defined(_TYPE_TREE_LEAVES) || defined(_TYPE_TREE_BARK)
        return 1.0;
    #else
        return vertexColor.r;
    #endif
}

float GetEdgeFlutter( float4 vertexColor )
{
    #if defined(_TYPE_TREE_BARK)
        return 0;
    #else
        #if defined(_TYPE_TREE_LEAVES)
            return vertexColor.g;
        #else
            return 1;
        #endif
    #endif
}

float GetTurbulenceStrength()
{
    return g_Turbulence.y * _TurbulenceStrength;
}

float3 FixStretching( float3 vertex, float3 original, float3 center )
{
     return center + SafeNormalize(vertex - center) * length(original - center);
}

float3 ApplyScaleFade(float3 vertexWorldPosition, float3 objectPivot, float fade )
{
    vertexWorldPosition.y = lerp(objectPivot.y, vertexWorldPosition.y, max(fade, 0.2));
    return vertexWorldPosition;
}

float4 AmbientFrequency(
                float3 objectPivot, // The object pivot in world space.
                float3 vertexWorldPosition, // The vertex position in world space.
                float3 windDirection, // The wind direction in world space.
                float phaseOffset, // The wind phase offset. (Range: 0-1)
                float speed, // The wind speed.
                float time ) // The current time.
{
    float footprint = 3;
    time -= phaseOffset * footprint;
                
    #ifdef PER_OBJECT_VALUES_CALCULATED
    float pivotOffset = g_PivotOffset;
    #else
    float pivotOffset = length( float3(objectPivot.x + g_FloatingOriginOffset_Ambient.x, 0, objectPivot.z + g_FloatingOriginOffset_Ambient.y) );
    #endif
                
    float scale = 0.5;
    float frequency = pivotOffset * scale - time;
    return FastSin(
        float4(
            frequency,
            frequency*0.5,
            frequency*0.25,
            frequency*0.125) * speed );
}

float3 AmbientWind(
                float3 objectPivot, // The object pivot in world space.
                float3 vertexWorldPosition, // The vertex position in world space.
                float3 windDirection, // The wind direction in world space.
                float phaseOffset, // The wind phase offset. (Range: 0-1)
                float time )
{
    float4 sine = AmbientFrequency( objectPivot, vertexWorldPosition, windDirection, phaseOffset, 1, time );
    sine.w = abs(sine.w) + 0.5;
    float xz = 1.5 * sine.x * sine.z + sine.w + 1;
    float y = 1 * sine.y * sine.z + sine.w;
    return windDirection * float3(xz, 0, xz) + float3(0, y, 0);
}

float3 Turbulence(
                float3 objectPivot, // The object pivot in world space.
                float3 vertexWorldPosition, // The vertex position in world space.
                float3 worldNormal, // The direction of the turbulence in world space (usually vertex normal).
                float phaseOffset, // The wind phase offset.
                float edgeFlutter, // The amount of edge flutter for tree leaves. (Range: 0-1)
                float speed, // The wind speed.
                float time )
{
    #if defined(_TYPE_TREE_BARK)
    return float3(0, 0, 0);
    #else
    time -= phaseOffset;
    float frequency =
        ( (objectPivot.x + g_FloatingOriginOffset_Turbulence.x)
            + (objectPivot.y)
            + (objectPivot.z + g_FloatingOriginOffset_Turbulence.y)
            ) * 2.5 - time;
    
    float4 sine =
        FastSin(
            float4(
                (1.65 * frequency) * speed,
                (2 * 1.65 * frequency) * speed,
                0,
                0) );
                    
    float x = 1 * sine.x + 1;
    float z = 1 * sine.y + 1;
    float y = (x + z) * 0.5;
    #if defined(_TYPE_TREE_LEAVES)
        return worldNormal * float3(x, y, z) * float3(1, .6, 1) * edgeFlutter;
    #else
        return worldNormal * float3(x, y, z) * float3(1, 0.35, 1);
    #endif
    #endif
}

float3 RotateAroundAxis( float3 center, float3 original, float3 u, float angle )
{
    original -= center;
    float C = cos( angle );
    float S = sin( angle );
    float t = 1 - C;
    float m00 = t * u.x * u.x + C;
    float m01 = t * u.x * u.y - S * u.z;
    float m02 = t * u.x * u.z + S * u.y;
    float m10 = t * u.x * u.y + S * u.z;
    float m11 = t * u.y * u.y + C;
    float m12 = t * u.y * u.z - S * u.x;
    float m20 = t * u.x * u.z - S * u.y;
    float m21 = t * u.y * u.z + S * u.x;
    float m22 = t * u.z * u.z + C;
    float3x3 finalMatrix = float3x3( m00, m01, m02, m10, m11, m12, m20, m21, m22 );
    return mul( finalMatrix, original ) + center;
}

float3 CombineWind(float3 ambient, float3 gust, float3 turbulence, float3 shiver, float4 strength )
{
    ambient *= strength.x;
    gust *= strength.y;
    turbulence *= strength.z;
    shiver *= strength.w;
    
    #if defined(_TYPE_TREE_LEAVES) || defined(_TYPE_TREE_BARK)
    ambient *= 3;
    gust *= 1;
    turbulence *= 3;
    shiver *= 3;
    #endif
    
    float gustLength = length( gust );
    float increaseTurbelenceWithGust = smoothstep(0, 1, gustLength) + 1;
    
    ambient *= 0.1;
    gust *= 1.5;
    turbulence *= 0.15;
    shiver *= 0.15;
    
    return ambient + gust + lerp(turbulence, shiver, gustLength) * increaseTurbelenceWithGust;
}
float GetTrunkMask(float3 vertex, float2 uv1, float bendFactor, float baseBendFactor )
{
    float trunkMask = saturate( uv1.y * bendFactor );
                
    return saturate( trunkMask + saturate( vertex.y ) * baseBendFactor );
}

float3 ComputeWind( WindInput input, float3 positionWS, float timeOffset )
{
    #if defined(_TYPE_GRASS) || defined(_TYPE_PLANT)
        input.phaseOffset += dot( input.direction, (positionWS - input.objectPivot) );
        input.phaseOffset += input.mask * 0.3;
    #endif
    
    float3 ambient =
        AmbientWind(
            input.objectPivot,
            positionWS,
            input.direction,
            input.phaseOffset,
            g_SmoothTime.x );
    
    float3 gust =
        SampleGust(
            input.objectPivot,
            positionWS,
            input.direction,
            input.phaseOffset,
            input.flutter,
            0,
            g_WindOffset.xy );
    
    #if defined(_TYPE_TREE_LEAVES)
        input.phaseOffset +=
            dot( input.direction, (positionWS - input.objectPivot) ) * input.flutter;
    #endif
    
    float3 turbulence1 =
        Turbulence(
            input.objectPivot.xyz,
            positionWS.xyz,
            input.normalWS.xyz,
            input.phaseOffset,
            input.flutter,
            1,
            g_SmoothTime.z );
    
    float3 turbulence2 =
        Turbulence(
            input.objectPivot.xyz,
            positionWS.xyz,
            input.normalWS.xyz,
            input.phaseOffset,
            input.flutter,
            2,
            g_SmoothTime.z );
    
    return CombineWind(
        ambient,
        gust,
        turbulence1,
        turbulence2,
        float4(GetWindStrength().xx, GetTurbulenceStrength().xx) );
}

float3 ApplyWind(
                float3 positionWS, // Vertex position in world space.
                float3 objectPivot, // Object Pivot in world space.
                float3 combinedWind, // Combined Wind vector in world space.
                float mask, // Wind mask. (Range: 0-1)
                float distanceFade) // Wind distance fade. (Range: 0-1)
{
    #if defined(_TYPE_GRASS)
    return FixStretching(positionWS + combinedWind * mask * distanceFade,positionWS,
        float3( positionWS.x, objectPivot.y, positionWS.z ) );
    #elif defined(_TYPE_TREE_LEAVES) || defined(_TYPE_TREE_BARK)
    return FixStretching(
                        positionWS + combinedWind * mask * distanceFade * 4,
                        positionWS,
                        objectPivot);
    #else
        return FixStretching(
                        positionWS + combinedWind * mask * mask * distanceFade,
                        positionWS,
                        objectPivot);
    #endif
}

void Wind( WindInput input, inout float3 positionWS, inout float3 normalWS, float timeOffset )
{
    #ifdef _TYPE_GRASS
        input.objectPivot = float3(positionWS.x, input.objectPivot.y, positionWS.z);
    #endif
    // Compute wind.
    float3 wind = ComputeWind(input, positionWS, timeOffset);
    
    // Apply wind to vertex.
    float3 outputWS = ApplyWind(positionWS,input.objectPivot,wind,input.mask,input.fade );
                
    // Recalculate normals for grass
    
    float3 delta = outputWS - positionWS;
    normalWS =
        lerp(
            normalWS,
            normalWS + SafeNormalize( delta + float3(0, 0.1, 0) ),
            length(delta) * _RecalculateWindNormals * input.fade );
                
    positionWS = outputWS;
}

float3 Wind( WindInput input,  float3 positionWS,  float timeOffset )
{
    #ifdef _TYPE_GRASS
        input.objectPivot = float3(positionWS.x, input.objectPivot.y, positionWS.z);
    #endif
    // Compute wind.
    float3 wind = ComputeWind(input, positionWS, timeOffset);
    return wind * input.mask * input.fade;
}

void RecalculateWindNormals( WindInput input, inout float3 normalWS, float3 offset)
{
    float3 delta = offset;
    normalWS =
        lerp(
            normalWS,
            normalWS + SafeNormalize( delta + float3(0, 0.1, 0) ),
            length(delta) * _RecalculateWindNormals * input.fade );
}

WindInput GetWindInput(
                VertexAttributes vertex,
                Varyings surface,
                float windFade,
                float heightMask,
                float phaseOffset,
                float3 objectPivot)
{
    WindInput input;
                
    // Global
    input.direction = GetWindDirection();
    input.speed = GetWindSpeed();
                
    // Per-Object
    input.objectPivot = objectPivot;
    input.fade = windFade;
                
    // Per-Vertex
    input.phaseOffset = phaseOffset;
    input.normalWS = surface.normalWS;
    float windVariation = GetWindVariation( input.objectPivot );
    float vertexMask = GetVertexMask(vertex.color);
    input.mask = heightMask * vertexMask * windVariation;
    input.flutter = GetEdgeFlutter(vertex.color);
    
    return input;
}

void Wind(  WindInput input,
            inout Varyings surface,
            
            float timeOffset )
{
    float3 vertexOut = surface.positionWS;
    float3 normalOut = surface.normalWS;
    Wind(
        input,
        vertexOut,
        normalOut,
        timeOffset );
    
    surface.positionWS = vertexOut;
    surface.normalWS = normalOut;
}


#ifdef _TYPE_TREE_BARK
float2 GetTrunkBendFactor()
{
    return _TrunkBendFactor.xy;
}
#endif

void Wind_Trunk(
    VertexAttributes vertex,
    WindInput input,
    inout Varyings varyings)
{
    float2 bendFactor = _TrunkBendFactor.xy;
    float trunkMask = GetTrunkMask(vertex.positionOS, varyings.uv1, 
        bendFactor.x, bendFactor.y);
    float ambientStrength = GetWindStrength();
   
    
    float4 trunkAmbint = 
        AmbientFrequency(input.objectPivot,
            varyings.positionWS,
            input.direction,
            0, 0.75,
            g_SmoothTime.x) + ambientStrength;
    
    //trunkAmbint *= trunkMask;
    
    float3 trunkGust = 
        SampleGust(input.objectPivot,varyings.positionWS, input.direction
            ,0, 0, 7, g_WindOffset.xy);
    //trunkGust *= trunkMask;
    
    float gustFrequency = trunkAmbint.w + length( trunkGust );
    float baseFrequency1 = trunkAmbint.x;
    float baseFrequency2 = trunkAmbint.x + trunkAmbint.y;
    
    float baseFrequency = lerp(baseFrequency1, baseFrequency2, (_SinTime.x + 1) * 0.5 * ambientStrength);
    
    varyings.positionWS = RotateAroundAxis(input.objectPivot, varyings.positionWS,
                            normalize(cross(float3(0,1,0), input.direction)),
                            (baseFrequency*0.75 + gustFrequency) * ambientStrength *0.0375 * 2);
}

float GetAngle(float3 a, float3 b)
{
    // 1. Normalize both vectors to ensure they are unit length
    float3 nA = normalize(a);
    float3 nB = normalize(b);
    
    // 2. Get the dot product and clamp to avoid NaNs from floating point errors
    float dotP = clamp(dot(nA, nB), -1.0, 1.0);
    
    // 3. Return the arc-cosine
    return acos(dotP);
}

void Wind_TrunkBranch(
    VertexAttributes vertex,
    WindInput input,
    inout Varyings varyings)
{
    float2 bendFactor = _TrunkBendFactor.xy;
    float trunkMask = GetTrunkMask(vertex.positionOS, varyings.uv1, 
        bendFactor.x, bendFactor.y);
    float ambientStrength = GetWindStrength();
   
    
    float4 trunkAmbint = 
        AmbientFrequency(input.objectPivot,
            varyings.positionWS,
            input.direction,
            0, 0.75,
            g_SmoothTime.x) + ambientStrength;
    
    trunkAmbint *= trunkMask;
    
    float3 trunkGust = 
        SampleGust(input.objectPivot,varyings.positionWS, input.direction
            ,0, 0, 7, g_WindOffset.xy);
    trunkGust *= trunkMask;
    
    float gustFrequency = trunkAmbint.w + length( trunkGust );
    float baseFrequency1 = trunkAmbint.x;
    float baseFrequency2 = trunkAmbint.x + trunkAmbint.y;
    
    float baseFrequency = lerp(baseFrequency1, baseFrequency2, (_SinTime.x + 1) * 0.5 * ambientStrength);
    
    // varyings.positionWS = RotateAroundAxis(input.objectPivot, varyings.positionWS,
    //                         normalize(cross(float3(0,1,0), input.direction)),
    //                         (baseFrequency*0.75 + gustFrequency) * ambientStrength *0.0375 * 2);
    #ifdef _TYPE_TREE_BARK
    varyings.positionWS = RotateAroundAxis(TransformObjectToWorld(varyings.branchStart), varyings.positionWS,
                            normalize(cross(varyings.branchEnd - varyings.branchStart, input.direction)),
                             //(baseFrequency*0.75 + gustFrequency) * ambientStrength *0.0375 * 2);
                             min(GetAngle(varyings.branchEnd - varyings.branchStart, input.direction),(baseFrequency*0.75 + gustFrequency) * ambientStrength *0.0375 * 2));
    #endif
    
    #ifdef _TYPE_TREE_LEAVES
    varyings.positionWS = RotateAroundAxis(TransformObjectToWorld(varyings.branchStart), varyings.positionWS,
                           normalize(cross(float3(0,1,0), input.direction)),
                            (baseFrequency*0.75 + gustFrequency) * ambientStrength *0.0375 * 2);
    #endif
}

void Wind(
                inout VertexAttributes vertex,
                inout Varyings surface,
                inout float3 positionWS,
                float windFade,
                float scaleFade,
                float heightMask,
                float phaseOffset,
                float3 objectPivot,
                float timeOffset )
{
    WindInput input;
                
    // Global
    input.direction = GetWindDirection();
    input.speed = GetWindSpeed();
                
    // Per-Object
    input.objectPivot = objectPivot;
    input.fade = windFade;
                
    // Per-Vertex
    input.phaseOffset = phaseOffset;
    input.normalWS = surface.normalWS;
    float windVariation = GetWindVariation( input.objectPivot );
    float vertexMask = GetVertexMask( vertex.color );
    input.mask = heightMask * vertexMask * windVariation;
    input.flutter = GetEdgeFlutter( vertex.color );
                
    float3 vertexOut = positionWS;
    float3 normalOut = surface.normalWS;
    Wind(
        input,
        vertexOut,
        normalOut,
        timeOffset );
    
    positionWS = vertexOut;
    surface.normalWS = normalOut;
}

float BoundsEdgeMask(float2 position)
{
    const float blendDistance = 2;
    //Negate and center
    position = -position + _BendMapUV.z;
	
    const float2 boundsMin = _BendMapUV.xy;
    const float2 boundsMax = _BendMapUV.xy + _BendMapUV.z;
	
    float2 weightDir = min(position - boundsMin, boundsMax - position) / blendDistance;
	
    return saturate(min(weightDir.x, weightDir.y));
}

float HeightDistanceWeight(float3 obstaclePos, float3 surfacePos)
{
    const float grassHeight = obstaclePos.y;
    const float bendHeight = surfacePos.y;

    const float pixelDist = -(bendHeight - grassHeight);

    //Ensure the weight tapers off once the obstacle start to go lower than 3 units from the grass.
    const float falloff = 1 - saturate((pixelDist - 3.0) / (grassHeight));

    return saturate((grassHeight - bendHeight) * falloff);
}
float4 GetBendVector(float3 positionWS)
{
    float2 uv = _BendMapUV.xy / _BendMapUV.z + 
                    (_BendMapUV.z / (_BendMapUV.z * _BendMapUV.z)) * positionWS.xz;
    uv.y = 1 - uv.y;
    
    float4 v = SAMPLE_TEXTURE2D_LOD(_BendMap, sampler_BendMap, uv, 0).rgba;
    
    v.xz = v.xz * 2.0 - 1.0;
    v.a *= BoundsEdgeMask(positionWS.xz);
    return v;
}
float4 GetBendOffset(float3 posWS, float heightMask)
{
    float4 offset = 0;
    float4 vec = GetBendVector(posWS);
    
    const float weight = HeightDistanceWeight(posWS.y, vec.y);

    offset.xz = vec.xz * weight * heightMask;;
    offset.y = heightMask * (vec.a * 0.75) * weight;
	
    //Pass the mask, so it can be used to lerp between wind and bend offset vectors
    offset.a = vec.a * weight;

    //Apply mask
    offset.xyz *= offset.a;
    
    return offset;
}

float4 GetBendVector(float3 positionWS, float heightMask)
{
    float2 bendUV = _BendMapUV.xy / _BendMapUV.z + 
                    (_BendMapUV.z / (_BendMapUV.z * _BendMapUV.z)) * positionWS.xz;
    bendUV.y = 1 - bendUV.y;
    float4 v = SAMPLE_TEXTURE2D_LOD(_BendMap, sampler_BendMap, bendUV, 0).rgba;
    v.xz = v.xz * 2.0 - 1.0;
    v.a *= BoundsEdgeMask(positionWS.xz);
    
    const  float weight = HeightDistanceWeight(positionWS.y, v.y);
    float4 offset = 0;
    offset.xz = v.xz * heightMask * weight;
    offset.y = heightMask * v.a * 0.75 * weight;
    
    offset.w = v.a * weight;;
    
    return offset;
}

void ApplyBend(inout Varyings surface, float3 offset)
{
    surface.positionWS.xz += offset.xz;
    surface.positionWS.y -= offset.y;
}