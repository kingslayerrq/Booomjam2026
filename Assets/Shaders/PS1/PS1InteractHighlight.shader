Shader "PS1/InteractHighlight"
{
    Properties
    {
        [HDR] _HighlightColor ("Highlight Color", Color) = (0.2, 1.0, 0.8, 1.0)
        _FillStrength ("Fill Strength", Range(0, 1)) = 0.35
        _RimStrength ("Rim Strength", Range(0, 4)) = 1.8
        _RimPower ("Rim Power", Range(0.25, 8)) = 2.2
        _PulseStrength ("Pulse Strength", Range(0, 1)) = 0.25
        _PulseSpeed ("Pulse Speed", Range(0, 12)) = 3.0
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

            CBUFFER_START(UnityPerMaterial)
                half4 _HighlightColor;
                half _FillStrength;
                half _RimStrength;
                half _RimPower;
                half _PulseStrength;
                half _PulseSpeed;
                half _VertexSnap;
                float _VertexSnapResolution;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 viewDirWS : TEXCOORD1;
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
                output.normalWS = normalInputs.normalWS;
                output.viewDirWS = GetWorldSpaceViewDir(positionInputs.positionWS);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half3 normalWS = normalize(input.normalWS);
                half3 viewDirWS = normalize(input.viewDirWS);
                half rim = pow(saturate(half(1.0) - dot(normalWS, viewDirWS)), _RimPower);
                half pulse = half(1.0) + sin(_Time.y * _PulseSpeed) * _PulseStrength;
                half strength = saturate(_FillStrength + rim * _RimStrength) * pulse;

                half3 color = _HighlightColor.rgb * strength;
                return half4(color, half(1.0));
            }
            ENDHLSL
        }
    }
}
