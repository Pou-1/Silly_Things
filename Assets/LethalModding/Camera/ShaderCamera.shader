Shader "Custom/PhotoCameraFX_DreamBloom"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Saturation ("Saturation", Range(0,2)) = 1.6
        _Vignette ("Vignette", Range(0,1)) = 0.8
        _BloomIntensity ("Bloom Intensity", Range(0,2)) = 1.9
        _BloomThreshold ("Bloom Threshold", Range(0,1.5)) = 1.4
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
            float _Saturation;
            float _Vignette;
            float _BloomIntensity;
            float _BloomThreshold;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float2 uv : TEXCOORD0; float4 vertex : SV_POSITION; };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float3 ApplySaturation(float3 col, float sat)
            {
                float lum = dot(col,float3(0.299,0.587,0.114));
                return lerp(float3(lum,lum,lum), col, sat);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;

                float3 col = tex2D(_MainTex, uv).rgb;

                // Saturation
                col = ApplySaturation(col, _Saturation);

                // Bloom
                float brightness = max(col.r, max(col.g, col.b));
                float bloom = saturate((brightness - _BloomThreshold) * 3.0);
                col += bloom * _BloomIntensity;

                // Vignette
                float2 center = uv - 0.5;
                float dist = length(center) * 1.5;
                float vignette = smoothstep(1.0, 0.2, dist);
                col *= lerp(1.0, vignette, _Vignette);

                return float4(saturate(col),1);
            }
            ENDCG
        }
    }
}