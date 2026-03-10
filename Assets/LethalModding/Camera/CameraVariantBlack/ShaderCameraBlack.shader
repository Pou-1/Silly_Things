Shader "Custom/PhotoCameraFX_Vintage"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Contrast ("Contrast", Range(0.8,1.5)) = 1.15
        _Saturation ("Saturation", Range(0,1)) = 0.75
        _Vignette ("Vignette", Range(0,1)) = 0.7
        _Grain ("Film Grain", Range(0,0.2)) = 0.05
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
            float _Saturation;
            float _Vignette;
            float _Grain;

            struct appdata
            {
                float4 vertex:POSITION;
                float2 uv:TEXCOORD0;
            };

            struct v2f
            {
                float2 uv:TEXCOORD0;
                float4 vertex:SV_POSITION;
            };

            float rand(float2 p)
            {
                return frac(sin(dot(p,float2(12.9898,78.233))) * 43758.5453);
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i):SV_Target
            {
                float2 uv=i.uv;
                float3 col=tex2D(_MainTex,uv).rgb;

                col=(col-0.5)*_Contrast+0.5;

                float lum=dot(col,float3(0.299,0.587,0.114));
                col=lerp(float3(lum,lum,lum),col,_Saturation);

                col*=float3(1.1,1.0,0.85);

                float2 center=uv-0.5;
                float dist=length(center)*1.6;
                float vignette=smoothstep(1.0,0.25,dist);
                col*=lerp(1.0,vignette,_Vignette);

                float grain=rand(uv*_Time.y)*_Grain;
                col+=grain;

                return float4(saturate(col),1);
            }
            ENDCG
        }
    }
}