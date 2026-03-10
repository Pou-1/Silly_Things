Shader "Custom/PhotoCameraFX_Surveillance"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Contrast ("Contrast", Range(0.8,2)) = 1.3
        _Scan ("Scanline", Range(0,0.3)) = 0.08
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
            float _Scan;

            struct appdata{float4 vertex:POSITION;float2 uv:TEXCOORD0;};
            struct v2f{float2 uv:TEXCOORD0;float4 vertex:SV_POSITION;};

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex=UnityObjectToClipPos(v.vertex);
                o.uv=v.uv;
                return o;
            }

            fixed4 frag(v2f i):SV_Target
            {
                float2 uv=i.uv;
                float3 col=tex2D(_MainTex,uv).rgb;

                col=(col-0.5)*_Contrast+0.5;

                col*=float3(0.7,0.85,1.2);

                float scan=sin(uv.y*800)*_Scan;
                col-=scan;

                return float4(saturate(col),1);
            }
            ENDCG
        }
    }
}