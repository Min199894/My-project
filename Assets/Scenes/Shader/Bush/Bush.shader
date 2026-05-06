Shader "Unlit/Bush"
{
    Properties
    {
        [Enum(On, 0, Off, 2)] _DoubleSidedMode ("Double Sided", Float) = 2
        _Albedo ("Albedo", 2D) = "white" {}
        _AlphaTestThreshold("AlphaTestThreshold", Float) = 0.5
        _Normal ("Normal", 2D) = "white" {}
        _Mask ("Mask", 2D) = "white" {}
        [RemapSlider(0, 1)]_GlossRemap("Gloss Remap", Vector) = (0, 1, 0, 0)
        [RemapSlider(0, 1)]_OcclusionRemap("OcclusionRemap", Vector) = (0, 1, 0, 0)
        
        [Space(10)]
        [Enum(Add,0,Overlay,1)] _TranslucencyBlendMode ("Blend Mode", Float) = 0
        _TranslucencyStrength ("Translucency Strength", Range(0, 2)) = 1
        _TranslucencyScattering ("Translucency Scattering", Range(0, 3)) = 2
        _TranslucencyDistortion ("Translucency Distortion", Range(0, 1)) = 0.5
        _TranslucencyColor ("Translucency Color", Color) = (1, 1, 1, 1)
        _TranslucencyAmbient ("Translucency Ambient", Range(0, 1)) = 0.5
        _TranslucencyShadow ("Translucency Shadow", Range(0,1)) = 0.8
        
        _ThicknessMap("Thickness", 2D) = "white"{}
        [RemapSlider(0, 1)]_ThicknessRemap("Thickness Remap", Vector) = (0, 1, 0, 0)

        [Enum(Tint,0, HSL,1)] _ColorCorrection ("Color Variation", Float) = 0
        [RemapSlider(0, 0.5, 0.5)]_HueRange("HueVariation", Vector) = (-0.5, 0.5, 0, 0)
        [RemapSlider(0, 0.5, 0.5)]_SaturationRange("Saturationtion", Vector) = (-0.5, 0.5, 0, 0)
        [RemapSlider(0, 0.5, 0.5)]_LightnessRange("LightnessVariation", Vector) = (-0.5, 0.5, 0, 0)
        _Tint ("Tint", Color) = (1, 1, 1, 1)
        _TintVariation ("Tint Variation", Color) = (1, 1, 1, 1)
        
        [Header(Bender)]
        [Space]
        _BendTint("Bending tint", Color) = (0.8, 0.8, 0.8, 1.0)
        _GustTint("Gust Tint tint", Float) = 0.04
        
        [Header(Wind)]
        [Space]
        _Wind ("Wind", Float) = 1
        _WindVariation ("Wind Variation", Range(0, 1)) = 0.3
        _WindStrength ("Wind Strength", Range(0, 2)) = 1
        _TurbulenceStrength ("Turbulence Strength", Range(0, 2)) = 1
        _RecalculateWindNormals ("Recalculate Normals", Range(0,1)) = 0.5
        _WindFade ("Wind Fade", Vector) = (50, 20, 0, 0)
        
        _ScaleFade ("Scale Fade", Vector) = (50, 20, 0, 0)
        
        [Toggle]_DEBUG("DebugMode", Float) = 1
    }
    SubShader
    {
        Pass
        {
            Name "ForwardLit"
            Tags
            {
                "LightMode" = "UniversalForward"
            }
            Cull [_DoubleSidedMode]
            ZTest LEqual
            
            HLSLPROGRAM
            #define _TYPE_PLANT
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #include "BushPass.hlsl"
            
            #pragma multi_compile_fog
            
            #pragma vertex vert
            #pragma fragment frag
            
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags
            {
                "LightMode" = "ShadowCaster"
            }
            
            Blend One Zero, One Zero
            ZWrite On
            
            Cull [_DoubleSidedMode]
            
            ZTest LEqual
            
            AlphaToMask Off
            
            ZWrite On
            ColorMask 0
            
            HLSLPROGRAM
            #define _TYPE_PLANT
            #include "BushPass.hlsl"
            #pragma vertex vert
            #pragma fragment frag
            ENDHLSL
        }
    }
}
