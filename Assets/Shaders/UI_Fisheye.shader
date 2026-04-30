Shader "UI/FisheyeRawImage"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Distortion ("Distortion", Range(-1, 1)) = 0.25
        _Zoom ("Zoom", Range(0.5, 2.0)) = 1.0
        _Vignette ("Vignette", Range(0, 1)) = 0.25
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Distortion;
            float _Zoom;
            float _Vignette;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 centered = i.uv * 2.0 - 1.0;

                float radiusSquared = dot(centered, centered);
                float distortionFactor = 1.0 + _Distortion * radiusSquared;

                float2 distorted = centered * distortionFactor;
                distorted /= _Zoom;

                float2 uv = distorted * 0.5 + 0.5;

                if (uv.x < 0.0 || uv.x > 1.0 || uv.y < 0.0 || uv.y > 1.0)
                {
                    return fixed4(0, 0, 0, 1);
                }

                fixed4 col = tex2D(_MainTex, uv) * i.color;

                float distanceFromCenter = length(centered);
                float vignette = smoothstep(1.0, 1.0 - _Vignette, distanceFromCenter);
                col.rgb *= vignette;

                return col;
            }
            ENDCG
        }
    }
}