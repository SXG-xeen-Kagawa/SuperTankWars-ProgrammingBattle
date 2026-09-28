Shader "Custom/TankBarrier"
{
    Properties
    {
        _PatternTex ("Pattern Texture (White on Black)", 2D) = "black" {}
        [HDR] _ShieldColor ("Shield Color", Color) = (0.1, 0.7, 1.0, 1.0)

        _BaseAlpha ("Base Transparency", Range(0, 0.3)) = 0.005

        _PatternAlpha ("Pattern Opacity", Range(0, 1)) = 0.45
        _PatternIntensity ("Pattern Brightness", Range(0, 10)) = 3.0
        _PatternThreshold ("Pattern Gray Cutoff", Range(0, 1)) = 0.18
        _PatternSoftness ("Pattern Edge Softness", Range(0.01, 1)) = 0.38

        _RimPower ("Rim Width (Higher = Thinner)", Range(0.5, 10)) = 5.0
        _RimAlpha ("Rim Opacity", Range(0, 1)) = 0.75
        _RimIntensity ("Rim Brightness", Range(0, 10)) = 5.0

        _SoftRimAlpha ("Soft Rim Opacity", Range(0, 1)) = 0.08
        _SoftRimIntensity ("Soft Rim Brightness", Range(0, 10)) = 0.7
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }

        Pass
        {
            Name "Barrier"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_PatternTex);
            SAMPLER(sampler_PatternTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _PatternTex_ST;
                half4 _ShieldColor;

                half _BaseAlpha;
                half _PatternAlpha;
                half _PatternIntensity;
                half _PatternThreshold;
                half _PatternSoftness;

                half _RimPower;
                half _RimAlpha;
                half _RimIntensity;
                half _SoftRimAlpha;
                half _SoftRimIntensity;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS  : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float2 uv          : TEXCOORD2;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;

                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _PatternTex);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half3 texColor =
                    SAMPLE_TEXTURE2D(_PatternTex, sampler_PatternTex, input.uv).rgb;

                // ìYïtÉeÉNÉXÉ`ÉÉÇÃïùçLÇ¢äDêFÇó}Ç¶ÅA
                // ñæÇÈÇ¢òZäpå`ÇÃê¸Çã≠í≤Ç∑ÇÈÅB
                half gray = dot(texColor, half3(0.299, 0.587, 0.114));
                half pattern = smoothstep(
                    _PatternThreshold,
                    _PatternThreshold + _PatternSoftness,
                    gray
                );

                half3 normalWS = normalize(input.normalWS);
                half3 viewDirWS =
                    normalize(GetCameraPositionWS() - input.positionWS);

                half edge = 1.0h - saturate(dot(normalWS, viewDirWS));

                // çLÇ≠îñÇ¢åıÇ∆ÅAç◊Ç≠ñæÇÈÇ¢ó÷äsÇï ÅXÇ…çÏÇÈÅB
                half softRim = pow(edge, 1.5h);
                half sharpRim = pow(edge, _RimPower);

                half alpha = saturate(
                    _BaseAlpha
                    + pattern * _PatternAlpha
                    + softRim * _SoftRimAlpha
                    + sharpRim * _RimAlpha
                );

                half brightness =
                    0.03h
                    + pattern * _PatternIntensity
                    + softRim * _SoftRimIntensity
                    + sharpRim * _RimIntensity;

                half3 color = _ShieldColor.rgb * brightness;

                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}