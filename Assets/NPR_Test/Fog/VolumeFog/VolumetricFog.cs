using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class VolumetricFog : ScriptableRendererFeature
{
    [System.Serializable]
    [VolumeComponentMenuForRenderPipeline("Custom/VolumetricCloud", typeof(UniversalRenderPipeline))]
    public class VolumeticCloud : FogVolume
    {
        [Tooltip("Base Color")] public ColorParameter baseColor = new ColorParameter(new Color(1, 1, 1, 1));
        [Tooltip("Density Noise")] public Texture3DParameter densityNoise = new Texture3DParameter(null);
        [Tooltip("Density Noise Scale")] public Vector3Parameter densityNoiseScale = new Vector3Parameter(Vector3.one);

        [Tooltip("Density Noise Offset")]
        public Vector3Parameter densityNoiseOffset = new Vector3Parameter(Vector3.zero);

        [Tooltip("Molar Extinction Coefficient")]
        public MinFloatParameter absorption = new MinFloatParameter(1, 0);

        [Tooltip("Light Molar Extinction Coefficient")]
        public MinFloatParameter lightAbsorption = new MinFloatParameter(1, 0);

        [Tooltip("Light Absorption In Cloud")] public MinFloatParameter lightPower = new MinFloatParameter(1, 0);
        [Tooltip("Noise Texture2D")] public Texture2DParameter densityNoise2d = new Texture2DParameter(null);
        [Tooltip("Fog XSpeed")] public FloatParameter fogXSpeed = new FloatParameter(1);
        [Tooltip("Fog YSpeed")] public FloatParameter fogYSpeed = new FloatParameter(1);
        
        public override bool IsActive() => isRender.value;
        public override bool IsTileCompatible() => false;

        public void load(Material material, ref RenderingData data)
        {
            GameObject boxObj = GameObject.Find("RayMarchBox");
            if (boxObj != null)
            {
                BoxCollider box = boxObj.GetComponent<BoxCollider>();
                // if (boxObj != null)
                //     boxObj.TryGetComponent(out box);
                material.SetVector("_boundMin", box.transform.position + box.center - box.size / 2);
                material.SetVector("_boundMax", box.transform.position + box.center + box.size / 2);
            }


            //将所有参数载入目标材质
            material.SetColor("_BaseColor", baseColor.value);
            if (densityNoise != null)
            {
                material.SetTexture("_DensityNoiseTex", densityNoise.value);
            }

            if (densityNoise2d != null)
            {
                material.SetTexture("_DensityNoise2D",densityNoise2d.value);
            }

            material.SetVector("_DensityNoise_Scale", densityNoiseScale.value);
            material.SetVector("_DensityNoise_Offset", densityNoiseOffset.value);
            material.SetFloat("_Absorption", absorption.value);
            material.SetFloat("_LightAbsorption", lightAbsorption.value);
            material.SetFloat("_LightPower", lightPower.value);
            material.SetFloat("_FogXSpeed", fogXSpeed.value);
            material.SetFloat("_FogYSpeed", fogYSpeed.value);
            //material.SetFloat("_LightAbsorption",light);
        }
    }

    [SerializeField] private Shader shader; //手动指定该RenderFeature的所用到的Shader
    [SerializeField] private RenderPassEvent evt = RenderPassEvent.BeforeRenderingPostProcessing; //渲染之前还是之后

    private Material matInstance; //创建一个该Shader的材质对象

    class VolumetricFogRenderPass : ScriptableRenderPass
    {
        // This method is called before executing the render pass.
        // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
        // When empty this render pass will render to the active camera render target.
        // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
        // The render pipeline will ensure target setup and clearing happens in a performant manner.

        private const string customPassTag = "Custom Render Pass";
        private VolumeticCloud cloudPrameters;

        private Material mat;

        //RenderTargetIdentifier SourceRT // 用来标识CommandBuffer的RenderTexture
        //指向RenderTexture
        private RenderTargetIdentifier sourceRT;

        //维护渲染目标的句柄
        //指向着色器变量
        //当shader被赋予纹理且RenderIdentifier已准备好才可以写入RenderTargetHandle,然后填充至RenderTexture
        private RenderTargetHandle tempRT;

        //初始化
        //RenderPass接收冰保存之后的纹理
        public void Setup(RenderTargetIdentifier identifier, Material material)
        {
            this.sourceRT = identifier;
            this.mat = material;
        }


        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            // 帮助Execute()提前准备它所需要的RenderTexture或者其他的变量
        }

        // Here you can implement the rendering logic.
        // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
        // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
        // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            // 实现这个RenderPass做什么事情
            VolumeStack stack = VolumeManager.instance.stack;
            cloudPrameters = stack.GetComponent<VolumeticCloud>();

            //取出”customPassTag" 命令
            CommandBuffer command = CommandBufferPool.Get(customPassTag);

            if (cloudPrameters.IsActive())
            {
                cloudPrameters.load(mat, ref renderingData);

                //创建一张RenderTexture
                RenderTextureDescriptor opaqueDesc = renderingData.cameraData.cameraTargetDescriptor;
                opaqueDesc.depthBufferBits = 0;
                command.GetTemporaryRT(tempRT.id, opaqueDesc);

                // 将当前帧的colorRT用着色器（shader in material) 渲染输出到之前创建的tempRT上
                // command.Blit(sourceRT, tempRT.Identifier(), mat);
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
            // 释放在OnCameraSetup()里声明的变量，尤其是TemporaryRenderTexture
        }
    }

    private VolumetricFogRenderPass m_ScriptablePass; // 初始化RenderPass


    /// <inheritdoc/>
    public override void Create()
    {
        m_ScriptablePass = new VolumetricFogRenderPass();
        m_ScriptablePass.renderPassEvent = evt;
        // m_ScriptablePass = new VolumetricFogRenderPass(); //实例化m_ScrptablePass
        // Configures where the render pass should be injected.
        // m_ScriptablePass.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.\

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        //每一帧调用实例化的CustomRenderPass,插入到渲染管线当中
        if (shader == null)
            return;
        if (matInstance == null)
        {
            matInstance = CoreUtils.CreateEngineMaterial(shader);
        }

        RenderTargetIdentifier currentRT = renderer.cameraColorTarget;
        m_ScriptablePass.Setup(currentRT, matInstance); //初始化 将材质注入到纹理上
        renderer.EnqueuePass(m_ScriptablePass);
        // renderer.EnqueuePass(m_ScriptablePass);
    }
}