#ifndef  GrassLitInput
#define GrassLitInput
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ParallaxMapping.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"

struct VertexAttributes
{
    float3 positionOS   : POSITION;
    float3 normalOS     : NORMAL;
    float4 tangentOS    : TANGENT;
    float2 uv0     : TEXCOORD0;
    float2 uv1     : TEXCOORD1;

    float4 color : COLOR;
   
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS               : SV_POSITION;
    float3 positionWS               : TEXCOORD0;
   
    float3 normalWS                 : TEXCOORD1;
    half4 tangentWS                 : TEXCOORD2;    // xyz: tangent, w: sign
    float2 uv0                      : TEXCOORD3;
    float2 uv1                      : TEXCOORD4;
    float3 viewDirectionWS          : TEXCOORD5;
    half4 ambientOrLightmapUV       : TEXCOORD6;
    float4 fogFactorAndVertexLight  : TEXCOORD7;
    float4 shadowCoord              : TEXCOORD8;
    float4 color                    : COLOR;
    
    float noise                     : TEXCOORD10; 
    float4 debug                     : TEXCOORD11; 
    
    #if defined(SHADER_STAGE_FRAGMENT)
    FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
    #endif
};

struct Surface
{
    float3 Albedo; // base (diffuse or specular) color
    float3 Normal; // tangent space normal, if written
    half3 Emission;
    half Metallic; // 0=non-metal, 1=metal
    half Smoothness; // 0=rough, 1=smooth
    half Occlusion; // occlusion (default 1)
    float Alpha; // alpha for transparencies
    
    float Thickness;
   
                
};

TEXTURE2D(_Albedo);        SAMPLER(sampler_Albedo);
TEXTURE2D(_Normal);        SAMPLER(sampler_Normal);
TEXTURE2D(_Mask);        SAMPLER(sampler_Mask);
TEXTURE2D(_ThicknessMap);        SAMPLER(sampler_ThicknessMap);
float4 _BendMapUV;
TEXTURE2D(_BendMap); SAMPLER(sampler_BendMap);
float4 _BendMap_TexelSize;

CBUFFER_START(UnityPerMaterial)
float4 _BaseColor;
float _AlphaTestThreshold;
float4 _BendTint;
float4 _Tint;
float4 _TintVariation;
float2 _ScaleFade;
float _GustTint;

float _ColorCorrection;
float4 _HSL;
float4 _HSLVariation;
float4 _HueRange;
float4 _SaturationRange;
float4 _LightnessRange;

float4 _GlossRemap;
float4 _OcclusionRemap;

float _TranslucencyBlendMode;
float _TranslucencyStrength;
float _TranslucencyDistortion;
float _TranslucencyScattering;
float4 _TranslucencyColor;
float _TranslucencyAmbient;
float _TranslucencyShadow;
float4 _ThicknessRemap;

// Wind
float _ObjectHeight;
float _ObjectRadius;
            
float _Wind;
float _WindVariation;
float _WindStrength;
float _TurbulenceStrength;
float _RecalculateWindNormals;
float4 _TrunkBendFactor;

float2 _WindFade;
CBUFFER_END

uniform sampler2D g_PerlinNoise;
float g_PerlinNoiseScale;
float4x4 _GrassCameraVPMatrix;
float4 _GrassProjectionParams;
float _Strength;

uniform float4 g_SmoothTime;
uniform float4 g_PrevSmoothTime;
uniform float3 g_WindDirection;
uniform float4 g_WindOffset;
uniform float2 g_Wind;
uniform float2 g_Turbulence;
uniform sampler2D g_GustNoise;

uniform float2 g_FloatingOriginOffset_Gust;
uniform float2 g_FloatingOriginOffset_Ambient;
uniform float2 g_FloatingOriginOffset_Turbulence;
#endif