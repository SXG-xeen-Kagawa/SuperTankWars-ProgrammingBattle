Shader "Xeen/HelmetGlass_ZWriteURP"
{
    Properties
    {
        _BaseColor("Base Color (RGBA)", Color) = (0.8, 0.95, 1.0, 0.25)
        _DepthOffset("Depth Offset", Range(-5, 5)) = 0
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
            Name "HelmetGlass"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
            ZTest LEqual

        // óvåèÅFDepthèëÇ´çûÇ›óL
        ZWrite On

        // îºìßñæ
        Blend SrcAlpha OneMinusSrcAlpha

        Offset[_DepthOffset],[_DepthOffset]

        HLSLPROGRAM
        #pragma vertex vert
        #pragma fragment frag
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;
        CBUFFER_END

        struct Attributes
        {
            float4 positionOS : POSITION;
        };

        struct Varyings
        {
            float4 positionHCS : SV_POSITION;
        };

        Varyings vert(Attributes v)
        {
            Varyings o;
            o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
            return o;
        }

        half4 frag(Varyings i) : SV_Target
        {
            return half4(_BaseColor.rgb, _BaseColor.a);
        }
        ENDHLSL
    }
    }
}