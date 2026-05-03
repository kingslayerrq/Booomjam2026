using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

public sealed class PS1PostProcessFeature : ScriptableRendererFeature
{
    [Serializable]
    public sealed class Settings
    {
        public bool enabled = true;
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
        public Material material;

        [Min(1f)] public float pixelHeight = 180f;
        [Min(2f)] public float colorLevels = 32f;
        [Range(0f, 1f)] public float ditherStrength = 0.25f;
        [Range(0f, 1f)] public float scanlineStrength = 0.08f;
        [Range(0f, 1f)] public float vignetteStrength = 0.12f;
        [Range(0f, 1f)] public float jitterStrength = 0f;
    }

    public Settings settings = new Settings();

    private PS1PostProcessPass pass;

    public override void Create()
    {
        pass = new PS1PostProcessPass();
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (!settings.enabled || settings.material == null)
            return;

        CameraType cameraType = renderingData.cameraData.cameraType;
        if (cameraType == CameraType.Preview || cameraType == CameraType.Reflection)
            return;

        pass.Setup(settings);
        renderer.EnqueuePass(pass);
    }

    private sealed class PS1PostProcessPass : ScriptableRenderPass
    {
        private static readonly int PixelHeightId = Shader.PropertyToID("_PixelHeight");
        private static readonly int ColorLevelsId = Shader.PropertyToID("_ColorLevels");
        private static readonly int DitherStrengthId = Shader.PropertyToID("_DitherStrength");
        private static readonly int ScanlineStrengthId = Shader.PropertyToID("_ScanlineStrength");
        private static readonly int VignetteStrengthId = Shader.PropertyToID("_VignetteStrength");
        private static readonly int JitterStrengthId = Shader.PropertyToID("_JitterStrength");

        private Settings settings;

        public void Setup(Settings featureSettings)
        {
            settings = featureSettings;
            renderPassEvent = settings.renderPassEvent;
            requiresIntermediateTexture = true;

            Material material = settings.material;
            material.SetFloat(PixelHeightId, Mathf.Max(1f, settings.pixelHeight));
            material.SetFloat(ColorLevelsId, Mathf.Max(2f, settings.colorLevels));
            material.SetFloat(DitherStrengthId, settings.ditherStrength);
            material.SetFloat(ScanlineStrengthId, settings.scanlineStrength);
            material.SetFloat(VignetteStrengthId, settings.vignetteStrength);
            material.SetFloat(JitterStrengthId, settings.jitterStrength);
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            if (resourceData.isActiveTargetBackBuffer)
                return;

            TextureHandle source = resourceData.activeColorTexture;
            TextureDesc destinationDesc = renderGraph.GetTextureDesc(source);
            destinationDesc.name = "CameraColor-PS1PostProcess";
            destinationDesc.clearBuffer = false;

            TextureHandle destination = renderGraph.CreateTexture(destinationDesc);
            RenderGraphUtils.BlitMaterialParameters parameters = new(source, destination, settings.material, 0);
            renderGraph.AddBlitPass(parameters, "PS1 Post Process");

            resourceData.cameraColor = destination;
        }
    }
}
