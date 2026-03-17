Shader "Custom/PhotoCameraFX_Glitch"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Glitch ("Glitch", Range(0,0.02)) = 0.005
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
            float _Glitch;

            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; };
            struct v2f { float2 uv:TEXCOORD0; float4 vertex:SV_POSITION; };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i):SV_Target
            {
                float shift = sin(i.uv.y * 80 + _Time.y * 10) * _Glitch;

                float r = tex2D(_MainTex, i.uv + float2(shift,0)).r;
                float g = tex2D(_MainTex, i.uv).g;
                float b = tex2D(_MainTex, i.uv - float2(shift,0)).b;

                return float4(r,g,b,1);
            }
            ENDCG
        }
    }
}