Shader "Custom/PhotoCameraFX_BlackWhite"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Contrast ("Contrast", Range(0.5,2)) = 1.2
        _Vignette ("Vignette", Range(0,1)) = 0.4
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
                float3 col = tex2D(_MainTex,i.uv).rgb;

                float lum = dot(col,float3(0.299,0.587,0.114));
                col = float3(lum,lum,lum);

                col = (col - 0.5) * _Contrast + 0.5;

                float2 center = i.uv - 0.5;
                float dist = length(center)*1.5;
                float vignette = smoothstep(1.0,0.2,dist);
                col *= lerp(1.0,vignette,_Vignette);

                return float4(saturate(col),1);
            }
            ENDCG
        }
    }
}