#ifndef BARKPASS
#define BARKPASS
#endif

#include "..\Common/LitInput.hlsl"
#include "..\Common/Wind.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "..\Common/PlanetCommon.hlsl"

#ifdef BARKPASS
void VertexMethod(VertexAttributes vertex, inout Varyings varyings, float4 timeOffset)
{
    float3 objectPivot = (float3(UNITY_MATRIX_M[0].w, UNITY_MATRIX_M[1].w, UNITY_MATRIX_M[2].w) );
    varyings.noise = PerVertexPerlinNoise(objectPivot);
    
    float windFade = 1;
    float scaleFade = 1;
    
    GetFade(objectPivot, windFade, scaleFade);
               
    float heightMask = GetHeightMask(vertex.color, vertex.uv1);
                
    float phaseOffset = GetPhaseOffset(vertex.color);
    varyings.debug = heightMask;
   
    WindInput input = GetWindInput(vertex, varyings,windFade,
                                    heightMask,phaseOffset,objectPivot);
    float3 windOffset =  Wind(
       input,
       varyings.positionWS,
       timeOffset );
  
    float3 resultOffset = windOffset;
    varyings.positionWS =  FixStretching(  varyings.positionWS + resultOffset, varyings.positionWS,
    float3( varyings.positionWS.x, objectPivot.y, varyings.positionWS.z ) );
    Wind_Trunk(vertex,input,varyings);
   
    // float3 windOffset =  Wind(
    //   input,
    //   varyings.positionWS,
    //   timeOffset );
    //
    // float3 resultOffset = windOffset;
    // varyings.positionWS =  FixStretching(  varyings.positionWS + resultOffset, varyings.positionWS,
    // float3( varyings.positionWS.x, objectPivot.y, varyings.positionWS.z ) );
    
    
    // varyings.positionWS = ApplyScaleFade( varyings.positionWS, objectPivot, scaleFade);
}

Varyings vert( VertexAttributes input )
{
    float3 positionWS = TransformObjectToWorld(input.positionOS);
    float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
    float4 tangentWS = float4(TransformObjectToWorldDir(input.tangentOS.xyz), input.tangentOS.w);
    
    #if defined(SHADERPASS_SHADOWCASTER)
        positionWS = ApplyShadowBias( positionWS, normalWS, _LightDirection );
    #endif
    Varyings output = (Varyings)0;
    output.positionCS = TransformWorldToHClip(positionWS);
    output.positionWS = positionWS;
    output.normalWS = normalWS;			// normalized in TransformObjectToWorldNormal()
    output.tangentWS = tangentWS;		// normalized in TransformObjectToWorldDir()
    
    output.uv0 = input.uv0;
    
    output.uv1 = input.uv1;
    
    output.color = input.color;
    output.viewDirectionWS.xyz = normalize( _WorldSpaceCameraPos.xyz - positionWS );
    
    VertexMethod( input, output, float4(0,0,0,0) );
    
    input.positionOS = TransformWorldToObject(output.positionWS );
    output.positionCS = TransformWorldToHClip(output.positionWS);
    
    #ifdef _MAIN_LIGHT_SHADOWS
        output.shadowCoord = TransformWorldToShadowCoord( positionWS );
    #endif
    //
    // #ifdef VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
    //     half3 vertexLight = VertexLighting(positionWS, normalWS);
    //     half fogFactor = ComputeFogFactor(output.positionCS.z);
    //     output.fogFactorAndVertexLight = half4(fogFactor, vertexLight);
    // #endif
    
    return output;
}

void SurfaceMethod( Varyings input, inout Surface surface)
{
    float2 uv = input.uv0;
    
    float4 albedo = SAMPLE_TEXTURE2D(_Albedo, sampler_Albedo, uv);
    
    clip(albedo.a - _AlphaTestThreshold);
    float3 normalTS = UnpackNormalmapRGorAG(SAMPLE_TEXTURE2D(_Normal, sampler_Normal, uv));
    float4 mask = SAMPLE_TEXTURE2D(_Mask, sampler_Mask, uv);
    
    ApplyColorCorrection(albedo, input.noise);
    
    surface.Albedo = albedo.rgb;
    
    surface.Normal = normalTS;
    surface.Metallic = mask.r;
    surface.Smoothness = Remap(mask.a, _GlossRemap.xy);
    surface.Occlusion = Remap(mask.g, _OcclusionRemap.xy);
    surface.Alpha = albedo.a;
    
    #ifdef SHADER_STAGE_FRAGMENT
        FlipNormals( surface.Normal, input.cullFace );
    #endif
}

void InitializeInputData(Varyings input, half3 normalTS, out InputData inputData)
{
    inputData = (InputData)0;

    inputData.positionWS = input.positionWS;
    half3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);

    float sgn = input.tangentWS.w;      // should be either +1 or -1
    float3 bitangent = sgn * cross(input.normalWS.xyz, input.tangentWS.xyz);
    half3x3 tangentToWorld = half3x3(input.tangentWS.xyz, bitangent.xyz, input.normalWS.xyz);
    
    inputData.tangentToWorld = tangentToWorld;
    inputData.normalWS = TransformTangentToWorld(normalTS, tangentToWorld);

    inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS);
    inputData.viewDirectionWS = viewDirWS;

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    inputData.shadowCoord = input.shadowCoord;
#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
    inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);
#else
    inputData.shadowCoord = float4(0, 0, 0, 0);
#endif

#if defined(DYNAMICLIGHTMAP_ON)
    inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.dynamicLightmapUV, input.vertexSH, inputData.normalWS);
#else
    inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.ambientOrLightmapUV.rgb, inputData.normalWS).rgb;
#endif

    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
    inputData.shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV);
    
}

float4 frag (Varyings input) : SV_Target
{
    SurfaceData surfaceData = (SurfaceData)0;
   
    Surface surface = (Surface)0;
    SurfaceMethod(input, surface);
    //return input.debug;
    ConvertSurfaceToSurfaceData(surface, surfaceData);
    InputData inputData = (InputData)0;
    
    InitializeInputData(input, surfaceData.normalTS, inputData);
    
    float3 color = UniversalFragmentPBR(inputData, surfaceData).rgb;
    
    return float4(color, 1);
}
#endif
