using System.Runtime.CompilerServices;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class TargetOutline : ScriptableRendererFeature
{
    [System.Serializable]
    [VolumeComponentMenuForRenderPipeline("Custom/TargetOutlineVolume", typeof(UniversalRenderPipeline))]
    public class VolumeTargetOutline : TargetOutlineVolume
    {
        [Tooltip("EdgeOnly")] public ClampedFloatParameter edgeOnly = new ClampedFloatParameter(0.0f, 0, 1.0f);
        [Tooltip("Outline Color")] public ColorParameter outlineColor = new ColorParameter(new Color(0, 0, 0, 1));
        [Tooltip("Background Color")] public ColorParameter backgroundColor = new ColorParameter(new Color(1, 1, 1, 1));
        [Tooltip("Sample Distance")] public MinFloatParameter sampleDistance = new MinFloatParameter(1.0f, 0f);
        [Tooltip("Sensitivity")] public Vector4Parameter sensitivity = new Vector4Parameter(Vector4.one);
        [Tooltip("Layer")] public LayerMaskParameter layer = new LayerMaskParameter(-1);


        public override bool IsActive() => isRender.value;
        public override bool IsTileCompatible() => false;
    }

    class CustomRenderPass : ScriptableRenderPass
    {
        private VolumeTargetOutline _volume;

        private int _rt;
        private FilteringSettings _filtering;
        private const string ShaderName = "Hidden/ScreenSpaceOutline";
        private Material _material;

        private readonly int EdgeOnly = Shader.PropertyToID("_EdgeOnly");
        private readonly int OutlineColor = Shader.PropertyToID("_OutlineColor");
        private readonly int BackgroundColor = Shader.PropertyToID("_BackgroundColor");
        private readonly int SampleDistance = Shader.PropertyToID("_SampleDistance");
        private readonly int Sensitivity = Shader.PropertyToID("_Sensitivity");

        private readonly List<ShaderTagId> _shaderTag = new List<ShaderTagId>()
        {
            new ShaderTagId("UniversalForward"),
            new ShaderTagId("SRPDefaultUnlit"),
            new ShaderTagId("UniversalForwardOnly")
        };

        public CustomRenderPass()
        {
            _volume = VolumeManager.instance.stack.GetComponent<VolumeTargetOutline>();
            _material = CoreUtils.CreateEngineMaterial(ShaderName);
        }


        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            _rt = Shader.PropertyToID("TargetRT"); //目标层RT
            _filtering = new FilteringSettings(RenderQueueRange.all, _volume.layer.value);
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            cmd.GetTemporaryRT(_rt, cameraTextureDescriptor); //创建目标层RT
            ConfigureTarget(_rt); //设置RT为渲染目标
            ConfigureClear(ClearFlag.All, UnityEngine.Color.clear); //清空
        }


        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (!_volume.IsActive())
                return;

            _material.SetFloat(EdgeOnly, _volume.edgeOnly.value);
            _material.SetColor(OutlineColor, _volume.outlineColor.value);
            _material.SetColor(BackgroundColor, _volume.backgroundColor.value);
            _material.SetFloat(SampleDistance, _volume.sampleDistance.value);
            _material.SetVector(Sensitivity, _volume.sensitivity.value);

            var draw = CreateDrawingSettings(_shaderTag, ref renderingData,
                renderingData.cameraData.defaultOpaqueSortFlags);

            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, new ProfilingSampler("DrawTargetLayerOutline")))
            {
                cmd.BeginSample("GetLayerTex");
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                context.DrawRenderers(renderingData.cullResults, ref draw, ref _filtering); //过滤未选中的物体将置于当前渲染纹理_rt上
                cmd.EndSample("GetLayerTex");

                //外描边
                cmd.BeginSample("Outline");
                RenderTextureDescriptor cameraTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;
                int rtId = Shader.PropertyToID("_OutlineTex");
                cmd.GetTemporaryRT(rtId, cameraTargetDescriptor);
                cmd.Blit(_rt, rtId, _material, 0);
                cmd.EndSample("Outline");

                //混合选中外描边和屏幕显示图像
                cmd.BeginSample("MixedTex");
                cmd.SetGlobalTexture("_SourceTex",
                    renderingData.cameraData.renderer.cameraColorTarget); //_SourceTex显示摄像机原始场景渲染纹理，_MainTex显示外描边纹理
                int rtId2 = Shader.PropertyToID("_OutlineTex2");
                cmd.GetTemporaryRT(rtId2, cameraTargetDescriptor);
                cmd.Blit(rtId, rtId2, _material, 1);
                cmd.EndSample("MixedTex");

                cmd.Blit(rtId2, renderingData.cameraData.renderer.cameraColorTarget);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        // Cleanup any allocated resources that were created during the execution of this render pass.
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            cmd.Clear();
        }
    }

    CustomRenderPass m_ScriptablePass;
    public RenderPassEvent _event;

    /// <inheritdoc/>
    public override void Create()
    {
        m_ScriptablePass = new CustomRenderPass();


        m_ScriptablePass.renderPassEvent = _event;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(m_ScriptablePass);
    }
}