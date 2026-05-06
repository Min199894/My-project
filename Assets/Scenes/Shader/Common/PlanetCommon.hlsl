#include "LitInput.hlsl"

void FlipNormals( inout float3 normals, FRONT_FACE_TYPE cullFace )
{
    normals.z *= IS_FRONT_VFACE(cullFace, 1, -1);
}
            
float ObjectPosRand01() 
{
    return frac(UNITY_MATRIX_M[0][3] + UNITY_MATRIX_M[1][3] + UNITY_MATRIX_M[2][3]);
}

float Remap( float value, float2 remap )
            {
                return remap.x + value * (remap.y - remap.x);
            }
            
float3 Linear_to_HSV(float3 In)
{
    float3 sRGBLo = In * 12.92;
    float3 sRGBHi = (pow(max(abs(In), 1.192092896e-07), float3(1.0 / 2.4, 1.0 / 2.4, 1.0 / 2.4)) * 1.055) - 0.055;
    float3 Linear = float3(In <= 0.0031308) ? sRGBLo : sRGBHi;
    float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
    float4 P = lerp(float4(Linear.bg, K.wz), float4(Linear.gb, K.xy), step(Linear.b, Linear.g));
    float4 Q = lerp(float4(P.xyw, Linear.r), float4(Linear.r, P.yzx), step(P.x, Linear.r));
    float D = Q.x - min(Q.w, Q.y);
    float E = 1e-10;
    return float3(abs(Q.z + (Q.w - Q.y)/(6.0 * D + E)), D / (Q.x + E), Q.x);
}

float3 HSV_to_Linear(float3 In)
{
    float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    float3 P = abs(frac(In.xxx + K.xyz) * 6.0 - K.www);
    float3 RGB = In.z * lerp(K.xxx, saturate(P - K.xxx), In.y);
    float3 linearRGBLo = RGB / 12.92;
    float3 linearRGBHi = pow(max(abs((RGB + 0.055) / 1.055), 1.192092896e-07), float3(2.4, 2.4, 2.4));
    return float3(RGB <= 0.04045) ? linearRGBLo : linearRGBHi;
}

void HSL_float( float3 hsv, float3 hsl, out float3 colorOut )
{
    hsv.x += hsl.x;
    hsv.y = saturate(hsv.y + hsl.y * 0.5);
    hsv.z = saturate(hsv.z + hsl.z * 0.5);
    colorOut = HSV_to_Linear(hsv);
}

void ApplyColorCorrection( inout float4 albedo, float noise )
{
    float3 albedoHSV = Linear_to_HSV( albedo.rgb );
    float3 albedo1;
    float3 albedo2;
    float3 HSL = float3(_HueRange.x, _SaturationRange.x, _LightnessRange.x);
    float3 HSLVariation = float3(_HueRange.y, _SaturationRange.y, _LightnessRange.y);
    HSL_float( albedoHSV, HSL, albedo1 );
    HSL_float( albedoHSV, HSLVariation, albedo2 );
    albedo.rgb = lerp(albedo.rgb * lerp(_TintVariation, _Tint, noise).rgb, lerp(albedo2, albedo1, noise), _ColorCorrection);
}

void ConvertSurfaceToSurfaceData(Surface input, inout SurfaceData output)
{
    output.albedo = input.Albedo;
    output.emission = input.Emission;
    output.metallic = input.Metallic;;
    output.smoothness = input.Smoothness;
    output.occlusion = input.Occlusion;
    output.alpha = input.Alpha;
    output.normalTS = input.Normal;
}

float3 Overlay(float3 a, float3 b)
{
    return a < 0.5
    ? 2 * a * b
    : 1 - 2 * (1-a) * (1-b);
}

struct TranslucencyInput
{
    float Scale;
    float NormalDistortion;
    float Scattering;
    float Thickness;
    float Ambient;
    half3 Color;
    float Shadow;
};

half3 Translucency(
                float thickness,
                float3 surfaceAlbedo,
                float3 bakedGI,
                float3 surfaceNormal,
                float3 viewDirectionWS,
                Light light )
{
    TranslucencyInput input = (TranslucencyInput)0;
    input.Scale = _TranslucencyStrength;
    input.NormalDistortion = _TranslucencyDistortion;
    input.Scattering = _TranslucencyScattering;
    input.Thickness = thickness;
    input.Color = _TranslucencyColor.rgb;
    input.Ambient = _TranslucencyAmbient;
    
    input.Shadow = _TranslucencyShadow;
    
    half3 lightDir = light.direction + surfaceNormal * input.NormalDistortion;
    half transVdotL =
        pow( saturate( dot( viewDirectionWS, -lightDir ) ), input.Scattering ) * input.Scale;
    half3 translucency =
        (transVdotL + bakedGI * input.Ambient)
        * (1-input.Thickness)
        * lerp(1, light.shadowAttenuation, input.Shadow)
        * light.distanceAttenuation;
                
    return half3( surfaceAlbedo * light.color * translucency * input.Color );
}