Shader "UI/AddBlend"
{
    Properties{
        _MainTex("Texture", 2D) = "white" {}
        _Color("Color", Color) = (1,1,1,1)
    }
        SubShader{
            Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
            Cull Off
            ZWrite Off
            Blend SrcAlpha One    // ← 加算（アルファで強さを制御）
            Pass {
                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #include "UnityCG.cginc"

                struct appdata_t {
                    float4 vertex : POSITION;
                    float4 color  : COLOR;
                    float2 texcoord : TEXCOORD0;
                };
                struct v2f {
                    float4 position : SV_POSITION;
                    fixed4 color : COLOR;
                    float2 uv : TEXCOORD0;
                };

                sampler2D _MainTex;
                fixed4 _Color;

                v2f vert(appdata_t v) {
                    v2f o;
                    o.position = UnityObjectToClipPos(v.vertex);
                    o.uv = v.texcoord;
                    o.color = v.color;
                    return o;
                }

                fixed4 frag(v2f i) : SV_Target {
                    fixed4 tex = tex2D(_MainTex, i.uv) * _Color * i.color;
                    return tex;
                }
                ENDCG
            }
        }
}