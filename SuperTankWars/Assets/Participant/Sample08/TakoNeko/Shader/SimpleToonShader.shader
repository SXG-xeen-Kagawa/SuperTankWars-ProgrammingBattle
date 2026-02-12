Shader "Xeen/SimpleToonURP"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [MainColor]_BaseColor("Base Color", Color) = (1,1,1,1)

        _ShadeColor("Shade Color", Color) = (0.65,0.65,0.65,1)

            _Step("Toon Step", Range(-1, 1)) = 0.0

             _Feather("Toon Feather", Range(0.0001, 1)) = 0.05

             _ShadowStrength("Shadow Strength", Range(0, 1)) = 1.0
    }

        SubShader
        {
            Tags
            {
                "RenderPipeline" = "UniversalPipeline"
                "RenderType" = "Opaque"
                "Queue" = "Geometry"
            }

            Pass
            {
                Name "Forward"
                Tags { "LightMode" = "UniversalForward" }

                Cull Back
                ZWrite On
                ZTest LEqual

                HLSLPROGRAM
                #pragma vertex vert
                #pragma fragment frag

            // URP lighting variants
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float4 _ShadeColor;
                float _Step;
                float _Feather;
                float _ShadowStrength;
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
                float4 shadowCoord : TEXCOORD3;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;

                VertexPositionInputs pos = GetVertexPositionInputs(v.positionOS.xyz);
                VertexNormalInputs nrm = GetVertexNormalInputs(v.normalOS);

                o.positionHCS = pos.positionCS;
                o.positionWS = pos.positionWS;
                o.normalWS = NormalizeNormalPerVertex(nrm.normalWS);
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                o.shadowCoord = GetShadowCoord(pos);
                return o;
            }

            float ToonRamp(float ndotl, float stepParam, float feather)
            {
                // threshold �� [0..1] �Ɋ񂹂�
                float threshold = saturate(0.5 + stepParam * 0.5);
                return smoothstep(threshold - feather, threshold + feather, ndotl);
            }

            float3 ShadeMix(float3 baseCol, float toon)
            {
                // toon=0 -> shade, toon=1 -> base
                return lerp(_ShadeColor.rgb * baseCol, baseCol, toon);
            }

            float4 frag(Varyings i) : SV_Target
            {
                float3 N = normalize(i.normalWS);

                float4 baseSample = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv);
                float3 baseCol = baseSample.rgb * _BaseColor.rgb;

                // Main light (URP)
                Light mainLight = GetMainLight(i.shadowCoord);
                float3 L = normalize(mainLight.direction);
                float ndotl = saturate(dot(N, L));

                // Toon shading
                float toon = ToonRamp(ndotl, _Step, _Feather);

                // Shadow attenuation
                float shadowAtten = mainLight.shadowAttenuation;
                float shadowBlend = lerp(1.0, shadowAtten, _ShadowStrength);

                float3 lit = ShadeMix(baseCol, toon);
                float3 col = lit * mainLight.color.rgb * (mainLight.distanceAttenuation * shadowBlend);

                #if defined(_ADDITIONAL_LIGHTS)
                uint addCount = GetAdditionalLightsCount();
                for (uint li = 0u; li < addCount; li++)
                {
                    Light add = GetAdditionalLight(li, i.positionWS);
                    float3 La = normalize(add.direction);
                    float ndotla = saturate(dot(N, La));
                    float toonA = ToonRamp(ndotla, _Step, _Feather);
                    float3 litA = ShadeMix(baseCol, toonA);
                    col += litA * add.color.rgb * (add.distanceAttenuation * add.shadowAttenuation);
                }
                #endif

                return float4(col, 1);
            }
            ENDHLSL
        }

            // ShadowCaster pass (casts shadows)
            Pass
            {
                Name "ShadowCaster"
                Tags { "LightMode" = "ShadowCaster" }

                Cull Back
                ZWrite On
                ZTest LEqual

                HLSLPROGRAM
                #pragma vertex vert
                #pragma fragment frag

                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

                struct Attributes
                {
                    float4 positionOS : POSITION;
                    float3 normalOS   : NORMAL;
                };

                struct Varyings
                {
                    float4 positionHCS : SV_POSITION;
                };

                Varyings vert(Attributes v)
                {
                    Varyings o;

                    float3 positionWS = TransformObjectToWorld(v.positionOS.xyz);
                    float3 normalWS = TransformObjectToWorldNormal(v.normalOS);

                    // URP shadow bias
                    o.positionHCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _MainLightPosition.xyz));
                    return o;
                }

                float4 frag(Varyings i) : SV_Target
                {
                    return 0;
                }
                ENDHLSL
            }
        }
}