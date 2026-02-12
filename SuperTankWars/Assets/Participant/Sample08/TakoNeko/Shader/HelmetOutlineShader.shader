Shader "Xeen/HelmetOutline_OpaqueURP"
{
    Properties
    {
        _OutlineColor("Outline Color", Color) = (0.05, 0.1, 0.15, 1.0)
        _DepthOffset("Depth Offset", Range(-5, 5)) = -1
    }

        SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"

        // 描画順は滝下さん運用どおり、後から描きたいので Transparent+1 に寄せます
        // 透明キューに入れることで、確実に本体の後ろ（後描画）になりやすいです
        "Queue" = "Transparent+1"
    }

    Pass
    {
        Name "HelmetOutline"
        Tags { "LightMode" = "UniversalForward" }

        // 反転法線＋拡大アウトライン用
        Cull Back

        // 要件：デプステスト有
        ZTest LEqual

        // 不透明（＝書き込みもONでOK）
        ZWrite On
        Blend Off

        Offset[_DepthOffset],[_DepthOffset]

        HLSLPROGRAM
        #pragma vertex vert
        #pragma fragment frag
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _OutlineColor;
        CBUFFER_END

        struct Attributes { float4 positionOS : POSITION; };
        struct Varyings { float4 positionHCS : SV_POSITION; };

        Varyings vert(Attributes v)
        {
            Varyings o;
            o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
            return o;
        }

        half4 frag(Varyings i) : SV_Target
        {
            return half4(_OutlineColor.rgb, 1);
        }
        ENDHLSL
    }
    }
}