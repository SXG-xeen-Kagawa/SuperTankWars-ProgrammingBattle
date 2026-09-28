Shader "Hidden/TrackRT_StampMask"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Overlay" }
        Pass
        {
            Name "StampMask"
            ZTest Always
            ZWrite Off
            Cull Off
            Blend Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _SourceTex;   // ★入力RT（Out）
            sampler2D _StampTex;    // ブラシ

            float4 _StampUV;        // (centerU, centerV, widthUV, lengthUV)
            float _Angle;           // rad
            float _Strength;        // 強度

            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 baseC = tex2D(_SourceTex, i.uv);

                float2 center = _StampUV.xy;
                float2 sizeUV = _StampUV.zw;
                if (sizeUV.x <= 1e-7 || sizeUV.y <= 1e-7) return baseC;

                float2 p = i.uv - center;

                float s = sin(_Angle);
                float c = cos(_Angle);
                float2 pr = float2(p.x * c - p.y * s, p.x * s + p.y * c);

                float2 halfSize = sizeUV * 0.5;
                float2 suv = pr / (halfSize * 2.0) + 0.5;

                if (suv.x < 0.0 || suv.x > 1.0 || suv.y < 0.0 || suv.y > 1.0)
                    return baseC;

                fixed4 stamp = tex2D(_StampTex, suv);
                float a = saturate(stamp.a * _Strength);

                // Alphaにマスク蓄積（頭打ち）
                //baseC.a = max(baseC.a, a);
                baseC.a = saturate(baseC.a + a * 0.25f);

                // デバッグ可視化したいなら一時的にこれをON：
                // baseC.rgb = baseC.a;

                return baseC;
            }
            ENDHLSL
        }
    }
}