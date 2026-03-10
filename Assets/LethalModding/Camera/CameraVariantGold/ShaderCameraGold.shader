Shader "Custom/PhotoCameraFX_Dream"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Glow ("Glow", Range(0,1)) = 0.5
        _Saturation ("Saturation", Range(1,2)) = 1.25
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
            float _Glow;
            float _Saturation;

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

                float lum=dot(col,float3(0.299,0.587,0.114));
                col=lerp(float3(lum,lum,lum),col,_Saturation);

                float3 blur=tex2D(_MainTex,uv+0.002).rgb;
                col+=blur*_Glow;

                return float4(saturate(col),1);
            }
            ENDCG
        }
    }
}