Shader "PS1/LowPolyLit"
{
    Properties
    {
        [MainTexture] _BaseMap ("Texture", 2D) = "white" {}
        [MainColor] _BaseColor ("Color", Color) = (1, 1, 1, 1)
        _LightBands ("Light Bands", Range(1, 8)) = 4
        _AmbientStrength ("Ambient Strength", Range(0, 1)) = 0.28
        _VertexSnap ("Vertex Snap", Range(0, 1)) = 0
        _VertexSnapResolution ("Vertex Snap Resolution", Float) = 160
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }

        Cull Back
        ZWrite On
        ZTest LEqual

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _LightBands;
                half _AmbientStrength;
                half _VertexSnap;
                float _VertexSnapResolution;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);

                float4 positionCS = positionInputs.positionCS;
                float snapResolution = max(_VertexSnapResolution, 1.0);
                float3 snapped = floor(positionCS.xyz * snapResolution) / snapResolution;
                positionCS.xyz = lerp(positionCS.xyz, snapped, saturate(_VertexSnap));

                output.positionCS = positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = normalInputs.normalWS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                Light mainLight = GetMainLight();

                half3 normalWS = normalize(input.normalWS);
                half ndotl = saturate(dot(normalWS, mainLight.direction));
                half bands = max(_LightBands, half(1.0));
                half stepped = floor(ndotl * bands) / bands;
                half lighting = saturate(_AmbientStrength + stepped * (half(1.0) - _AmbientStrength));

                half3 rgb = albedo.rgb * (mainLight.color * lighting);
                return half4(rgb, albedo.a);
            }
            ENDHLSL
        }
    }
}
