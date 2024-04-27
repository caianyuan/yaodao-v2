using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class FogWithDepthTexture : ScriptableRendererFeature
{
    [Serializable, VolumeComponentMenu("FogVolume")]
    public class CustomVolumeComponent : FogVolume
    {
        public ClampedFloatParameter FogDensity = new ClampedFloatParameter(1.0f, 0, 3.0f);
        public MinFloatParameter FogStart = new MinFloatParameter(0f, 0f);
        public MinFloatParameter FogEnd = new MinFloatParameter(2.0f, 0f);

        public ColorParameter FogColor = new ColorParameter(Color.white, false, false, true);
        public Texture2DParameter textureNoise2d = new Texture2DParameter(null);
        public MinFloatParameter textureNoiseAmount = new MinFloatParameter(1, 0);
        public FloatParameter fogXSpeed = new FloatParameter(1);
        public FloatParameter fogYSpeed = new FloatParameter(1);

        public override bool IsActive() => isRender.value;
        public override bool IsTileCompatible() => false;
    }

    class CustomRenderPass : ScriptableRenderPass
    {
        // This method is called before executing the render pass.
        // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
        // When empty this render pass will render to the active camera render target.
        // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
        // The render pipeline will ensure target setup and clearing happens in a performant manner.

        public Material material; //后处理使用材质

        private RenderTargetIdentifier source { get; set; }

        //private RenderTargetHandle destination {get; set;}
        //辅助RT
        private RenderTargetHandle tempTexture;


        private Matrix4x4 frustumCorners;

        //RT的滤波模式
        public FilterMode filterMode { get; set; }

        //当前渲染阶段的colorRT
        //RenderTargetIdentifier、RenderTargetHandle都可以理解为RT,Identifier为camera提供的需要被应用的texture

        string m_ProfilerTag;

        //Profiling上显示
        public CustomVolumeComponent volume; // 提供一个Volume传递位置
        private ProfilingSampler m_ProfilingSampler = new ProfilingSampler("UPRDing");

        public CustomRenderPass(RenderPassEvent renderPassEvent, Shader shader, CustomVolumeComponent volume,
            string tag)
        {
            //确定在哪个阶段插入渲染
            this.renderPassEvent = renderPassEvent;
            this.volume = volume;
            if (shader == null)
            {
                return;
            }

            this.material = CoreUtils.CreateEngineMaterial(shader); //新建材质
            m_ProfilerTag = tag;
            //初始化辅助RT的名字
            tempTexture.Init("_TempRTexture");
        }

        //初始化
        public void Setup(RenderTargetIdentifier source)
        {
            this.source = source;
        }


        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
        }

        // Here you can implement the rendering logic.
        // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
        // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
        // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (!volume.IsActive())
            {
                return;
            }

            CommandBuffer cmd = CommandBufferPool.Get("m_ProfilerTag"); //从存储好的命令缓冲区中读取命令

            //using 方法可以实现在FrameDebug查看渲染过程
            // using (new ProfilingScope(cmd, m_ProfilingSampler))
            // {
            //获取摄像机
            Camera camera = renderingData.cameraData.camera;
            //获取摄像机transform组件
            Transform cameraTransform = camera.transform;

            float fov = camera.fieldOfView;
            float near = camera.nearClipPlane;
            float far = camera.farClipPlane;
            float aspect = camera.aspect;

            float halfHeight = near * Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);
            Vector3 toTop = cameraTransform.up * halfHeight;
            Vector3 toRight = cameraTransform.right * halfHeight * aspect;

            //topLeft
            Vector3 topLeft = cameraTransform.forward * near + toTop - toRight;
            float scale = topLeft.magnitude / near;
            topLeft.Normalize();
            topLeft *= scale;
            // toptRight
            Vector3 topRight = cameraTransform.forward * near + toTop + toRight;
            topRight.Normalize();
            topRight *= scale;
            //bottomLeft
            Vector3 bottomLeft = cameraTransform.forward * near - toTop - toRight;
            bottomLeft.Normalize();
            bottomLeft *= scale;
            //bottomRight
            Vector3 bottomRight = cameraTransform.forward * near - toTop + toRight;
            bottomRight.Normalize();
            bottomRight *= scale;

            frustumCorners.SetRow(0, bottomLeft);
            frustumCorners.SetRow(1, bottomRight);
            frustumCorners.SetRow(2, topRight);
            frustumCorners.SetRow(3, topLeft);

            material.SetMatrix("_FrustumCornersRay", frustumCorners);
            material.SetMatrix("_ViewProjectionInverseMatrix",
                (camera.projectionMatrix * camera.worldToCameraMatrix).inverse);

            material.SetFloat("_FogDensity", volume.FogDensity.value);
            material.SetFloat("_FogStart", volume.FogStart.value);
            material.SetFloat("_FogEnd", volume.FogEnd.value);

            material.SetColor("_FogColor", volume.FogColor.value);
            material.SetTexture("_textureNoise2D", volume.textureNoise2d.value);
            material.SetFloat("_textureNoiseAmount", volume.textureNoiseAmount.value);

            material.SetFloat("_fogXSpeed", volume.fogXSpeed.value);
            material.SetFloat("_fogYSpeed", volume.fogYSpeed.value);

            /////////////////////////////////////////////////////////////////////////////////
            //创建一张RT
            RenderTextureDescriptor cameraTextureDesc = renderingData.cameraData.cameraTargetDescriptor;
            cameraTextureDesc.depthBufferBits = 0;
            //取消抗锯齿
            //cameraTextureDesc.msaaSamples = 1;
            cmd.GetTemporaryRT(tempTexture.id, cameraTextureDesc, filterMode);

            //将当前帧的colorRT用着色器(shader in material)渲染输出到之前创建的贴图（辅助RT）上
            Blit(cmd, source, tempTexture.Identifier(), material, 0); //运行自己的着色器
            //将处理后的辅助RT重新渲染到当前帧的ColorRT上
            Blit(cmd, tempTexture.Identifier(), source);
            //}

            //执行渲染
            context.ExecuteCommandBuffer(cmd);
            //释放回收
            CommandBufferPool.Release(cmd);
        }

        // Cleanup any allocated resources that were created during the execution of this render pass.
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
        }

        // public override void FrameCleanup(CommandBuffer cmd)
        // {
        //     base.FrameCleanup(cmd);
        //     cmd.ReleaseTemporaryRT(tempTexture.id);
        // }
    }

    [System.Serializable]
    public class Settings
    {
        public RenderPassEvent Event = RenderPassEvent.AfterRenderingTransparents; //在后处理后执行
        public Shader shader; //汇入shader
    }

    public Settings settings = new Settings(); //开放设置
    CustomRenderPass m_ScriptablePass;
    CustomVolumeComponent volume;

    /// <inheritdoc/>
    public override void Create()
    {
        //feature
        var stack = VolumeManager.instance.stack;
        volume = stack.GetComponent<CustomVolumeComponent>();
        if (volume == null)
        {
            CoreUtils.Destroy(m_ScriptablePass.material);
            return;
        }

        m_ScriptablePass = new CustomRenderPass(settings.Event, settings.shader, volume, name);

        // Configures where the render pass should be injected.
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.

    //每一帧都会调用
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        var src = renderer.cameraColorTarget;
        //var dst = RenderTargetHandle.CameraTarget

        if (settings.shader == null)
        {
            Debug.LogWarningFormat("shader丢失", GetType().Name);
            return;
        }

        //将当前渲染的colorRT传到Pass中
        m_ScriptablePass.Setup(src);

        //将Pass添加到渲染队列中
        renderer.EnqueuePass(m_ScriptablePass);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
    }
}