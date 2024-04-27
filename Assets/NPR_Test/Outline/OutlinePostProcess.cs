using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;


public class OutlinePostProcess : ScriptableRendererFeature
{
    [System.Serializable]
    [VolumeComponentMenuForRenderPipeline("Custom/Outlinevolume", typeof(UniversalRenderPipeline))]
    public class VolumeOutline : OutlineVolume
    {
        [Tooltip("EdgeOnly")] public ClampedFloatParameter EdgeOnly = new ClampedFloatParameter(0.0f, 0, 1.0f);
        [Tooltip("Outline Color")] public ColorParameter outlineColor = new ColorParameter(new Color(0, 0, 0, 1));
        [Tooltip("Bakcground Color")] public ColorParameter BackgroundColor = new ColorParameter(new Color(1, 1, 1, 1));
        [Tooltip("Sample Distance")] public MinFloatParameter SampleDistance = new MinFloatParameter(1.0f, 0f);
        [Tooltip("Sensitivity")] public Vector4Parameter Sensitivity = new Vector4Parameter(Vector4.one);
        //[Tooltip("Layer Mask")] public LayerMaskParameter layer = new LayerMaskParameter(-1);

        public override bool IsActive() => isRender.value;
        public override bool IsTileCompatible() => false;

        public void load(Material material, ref RenderingData data)
        {
            //将所有参数载入目标材质
            material.SetFloat("_EdgeOnly", EdgeOnly.value);
            material.SetColor("_OutlineColor", outlineColor.value);
            material.SetColor("_BackgroundColor", BackgroundColor.value);
            material.SetFloat("_SampleDistance", SampleDistance.value);
            material.SetVector("_Sensitivity", Sensitivity.value);
        }
    }

    //手动指定该RenderFeature所用到的Shader
    [SerializeField] private Shader outlineShader; // outline shader 
    [SerializeField] private RenderPassEvent evt = RenderPassEvent.AfterRenderingTransparents;
    //[SerializeField] private LayerMask outlineLayerMask;

    private Material outlineMaterial; //创建一个该shader的材质对象

    class OutlineRenderPass : ScriptableRenderPass
    {
        // This method is called before executing the render pass.
        // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
        // When empty this render pass will render to the active camera render target.
        // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
        // The render pipeline will ensure target setup and clearing happens in a performant manner.

        private const string customPassTag = "Custom Render Pass";
        private VolumeOutline outlinePrameters;

        //对特定对象过滤
        private FilteringSettings _filtering;

        private Material mat;

        //RenderTargetIdentifier SourceRT 
        //用来标识CommandBuffer的RenderTexture
        //指向RenderTexture
        //源渲染纹理
        private RenderTargetIdentifier sourceRT;

        //中间渲染纹理
        //维护渲染目标的句柄
        //指向着色器变量
        //当shader被赋予纹理且RenderIdentifier已准备好才可以写入RenderTargetHandle,然后填充
        //至RenderTexture
        private RenderTargetHandle tempRT;
        
        //--------------------
        private readonly List<ShaderTagId> _shaderTag = new List<ShaderTagId>()
        {
            new ShaderTagId("UniversalForward"),
            new ShaderTagId("SRPDefaultUnlit"),
            new ShaderTagId("UniversalForwardOnly")
        };

        // public OutlineRenderPass(LayerMask layerMask)
        // {
        //     _filtering = new FilteringSettings(RenderQueueRange.all, layerMask);
        // }


        //初始化
        //RenderPass接收并保存之后的纹理
        public void Setup(RenderTargetIdentifier identifier, Material material)
        {
            this.sourceRT = identifier;
            this.mat = material;
            //this._filtering = new FilteringSettings(RenderQueueRange.all, outlinePrameters.layer.value);
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            //帮助Execure()提前准备它所需要的RenderTexture 或者 其他的变量
            //_filtering = new FilteringSettings(RenderQueueRange.opaque, outlinePrameters.layer.value); //设置渲染队列以及层级
            
            // _filtering = new FilteringSettings(RenderQueueRange.all, outlinePrameters.layer.value);
            // tempRT.id = Shader.PropertyToID("_Target");
        }

        // Here you can implement the rendering logic.
        // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
        // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
        // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            //执行
            //实现这个RenderPass做什么事情
            VolumeStack stack = VolumeManager.instance.stack;
            outlinePrameters = stack.GetComponent<VolumeOutline>();

            // var draw = CreateDrawingSettings(_shaderTag, ref renderingData,
            //     renderingData.cameraData.defaultOpaqueSortFlags);

            //取出“customPassTag" 命令
            CommandBuffer command = CommandBufferPool.Get(customPassTag);
            
            ///----------------------------------drawRenderers
            // command.BeginSample("GetLayerTex");
            // context.ExecuteCommandBuffer(command);
            // command.Clear();
            // context.DrawRenderers(renderingData.cullResults,ref draw,ref _filtering);
            // command.EndSample("GetLayerTex");
            //----------------------------------------------

            if (outlinePrameters.IsActive())
            {
                outlinePrameters.load(mat, ref renderingData);

                //创建一张RenderTexture
                RenderTextureDescriptor opaqueDesc = renderingData.cameraData.cameraTargetDescriptor;
                opaqueDesc.depthBufferBits = 0;
                command.GetTemporaryRT(tempRT.id, opaqueDesc);

                //将当前帧的colorRT用着色器(shader in material)渲染输出到之前创建的tempRT上
                //command.Blit(sourceRT, tempRT.Identifier(), mat);
                command.Blit(sourceRT, tempRT.Identifier(), mat);

                command.Blit(tempRT.Identifier(), sourceRT);

                //执行渲染
                context.ExecuteCommandBuffer(command);
                //释放回收
                CommandBufferPool.Release(command);
                command.ReleaseTemporaryRT(tempRT.id);
            }
        }

        // Cleanup any allocated resources that were created during the execution of this render pass.
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            //释放OnCameraSetup()里声明的变量
            // 尤其是TempRT
        }
    }

    private OutlineRenderPass m_ScriptablePass; //初始化RenderPass

    /// <inheritdoc/>
    public override void Create()
    {
        //最开始执行Create，并新建OutlineRenderPass()实例，然而并不是每一帧都执行
        m_ScriptablePass = new OutlineRenderPass();
        m_ScriptablePass.renderPassEvent = evt;


        // Configures where the render pass should be injected.
        //m_ScriptablePass.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        //每一帧调用实例化CustomRenderPass， 插入到渲染管线中
        //每一帧执行
        if (outlineShader == null)
            return;
        if (outlineMaterial == null)
        {
            outlineMaterial = CoreUtils.CreateEngineMaterial(outlineShader);
        }

        RenderTargetIdentifier currentRT = renderer.cameraColorTarget;
        m_ScriptablePass.Setup(currentRT, outlineMaterial);
        renderer.EnqueuePass(m_ScriptablePass);
    }

    protected override void Dispose(bool disposing)
    {
        CoreUtils.Destroy(outlineMaterial);
    }
}