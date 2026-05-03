Shader "Hidden/PS1/PostProcess"
{
    Properties
    {
        _PixelHeight ("Pixel Height", Float) = 180
        _ColorLevels ("Color Levels", Float) = 32
        _DitherStrength ("Dither Strength", Range(0, 1)) = 0.25
        _ScanlineStrength ("Scanline Strength", Range(0, 1)) = 0.08
        _VignetteStrength ("Vignette Strength", Range(0, 1)) = 0.12
        _JitterStrength ("Jitter Strength", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
        }

        ZWrite Off
        Cull Off
        ZTest Always

        Pass
        {
            Name "PS1PostProcess"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _PixelHeight;
            float _ColorLevels;
            float _DitherStrength;
            float _ScanlineStrength;
            float _VignetteStrength;
            float _JitterStrength;

            float Bayer4(float2 pixel)
            {
                uint x = (uint)pixel.x & 3u;
                uint y = (uint)pixel.y & 3u;
                uint index = y * 4u + x;

                const float values[16] =
                {
                    0.0, 8.0, 2.0, 10.0,
                    12.0, 4.0, 14.0, 6.0,
                    3.0, 11.0, 1.0, 9.0,
                    15.0, 7.0, 13.0, 5.0
                };

                return (values[index] + 0.5) / 16.0;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 screenSize = max(_ScreenParams.xy, float2(1.0, 1.0));
                float aspect = screenSize.x / screenSize.y;
                float pixelHeight = max(_PixelHeight, 1.0);
                float2 virtualResolution = float2(max(1.0, floor(pixelHeight * aspect)), pixelHeight);

                float2 uv = input.texcoord;
                float jitter = sin((_Time.y * 19.0) + uv.y * 47.0) * (_JitterStrength / screenSize.x);
                uv.x += jitter;

                float2 pixelUv = (floor(uv * virtualResolution) + 0.5) / virtualResolution;
                half4 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, pixelUv);

                float2 pixelCoord = floor(input.positionCS.xy);
                float levels = max(_ColorLevels, 2.0);
                float dither = (Bayer4(pixelCoord) - 0.5) * _DitherStrength / levels;
                color.rgb = saturate(color.rgb + dither);
                color.rgb = floor(color.rgb * (levels - 1.0) + 0.5) / (levels - 1.0);

                float scanline = 1.0 - _ScanlineStrength * (0.5 + 0.5 * sin(pixelCoord.y * 3.14159265));
                color.rgb *= scanline;

                float2 centered = input.texcoord * 2.0 - 1.0;
                float vignette = smoothstep(1.35, 0.35, length(centered));
                color.rgb *= lerp(1.0, vignette, _VignetteStrength);

                return color;
            }
            ENDHLSL
        }
    }
}
