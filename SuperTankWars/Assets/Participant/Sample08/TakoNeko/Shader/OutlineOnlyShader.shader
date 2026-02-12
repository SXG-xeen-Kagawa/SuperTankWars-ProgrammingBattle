Shader "Xeen/OutlineOnlyURP"
{
    Properties
    {
        _OutlineColor("Outline Color", Color) = (0,0,0,1)
        _DepthOffset("Depth Offset", Range(-5, 5)) = -1
    }

        SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry-1"
        }

        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "UniversalForward" }

         //Cull Front
        Cull Back

        ZWrite On
        ZTest LEqual

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