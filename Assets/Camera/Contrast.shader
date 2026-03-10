Shader "Custom/PhotoCameraFX"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Contrast ("Contrast", Range(0,3)) = 1.3
        _Vignette ("Vignette", Range(0,1)) = 0.3
        _Grain ("Grain", Range(0,1)) = 0.05
    }

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _Contrast;
            float _Vignette;
            float _Grain;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            float rand(float2 co)
            {
                return frac(sin(dot(co,float2(12.9898,78.233))) * 43758.5453);
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float4 col = tex2D(_MainTex, i.uv);

                col.rgb = (col.rgb - 0.5) * _Contrast + 0.5;

                float dist = distance(i.uv, float2(0.5,0.5));
                col.rgb *= 1.0 - dist * _Vignette;

                float noise = rand(i.uv + _Time.y);
                col.rgb += (noise - 0.5) * _Grain;

                return col;
            }
            ENDCG
        }
    }
}