Shader "MyShader/Outline"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _EdgeOnly ("Edge Only", Float) = 1.0
        _OutlineColor("OutlineColor",Color) = (1,1,1,1)
        _BackgroundColor("Background Color", Color) = (1,1,1,1)
        _SampleDistance ("Sample Distance", Float) = 1.0
        _Sensitivity("Sensitivity", Vector) = (1,1,1,1)
    }
    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalRenderPipeline"
        }
        LOD 100
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        //#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

        CBUFFER_START(UnityPerMaterial)
        // Texture2D _MainTex;
        // SamplerState sampler_MainTex;
        half4 _MainTex_TexelSize; //提供访问纹理对应的每个纹素大小，一张512x512大小的纹理,该值为1/512,为0.001953

        float _EdgeOnly;
        half4 _OutlineColor;
        half4 _BackgroundColor;
        float _SampleDistance;
        half4 _Sensitivity;
        CBUFFER_END

        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);
        TEXTURE2D(_CameraDepthTexture);
        SAMPLER(sampler_CameraDepthTexture);
        TEXTURE2D(_CameraNormalsTexture);
        SAMPLER(sampler_CameraNormalsTexture);

        struct vertexInput
        {
            float4 vertex: POSITION;
            float2 texcoord: TEXCOORD0;
        };

        struct vertexOutput
        {
            float4 pos: SV_POSITION;
            float2 uv[5] : TEXCOORD0;
        };

        //顶点着色器
        vertexOutput vert(vertexInput v)
        {
            vertexOutput OUT;
            OUT.pos = TransformObjectToHClip(v.vertex);
            half2 uv = v.texcoord;
            OUT.uv[0] = uv;
            #if UNITY_UV_STARTS_AT_TOP
            if (_MainTex_TexelSize.y < 0)
                uv.y = 1 - uv.y;
            #endif

            //Robert 算子
            OUT.uv[1] = uv + _MainTex_TexelSize.xy * half2(1, 1) * _SampleDistance; //右上[1,1]
            OUT.uv[2] = uv + _MainTex_TexelSize.xy * half2(-1, -1) * _SampleDistance; //左下[-1,-1]
            OUT.uv[3] = uv + _MainTex_TexelSize.xy * half2(-1, 1) * _SampleDistance; //左上[-1,1]
            OUT.uv[4] = uv + _MainTex_TexelSize.xy * half2(1, -1) * _SampleDistance; //右下[1,-1]
            return OUT;
        }

        // 检测边缘
        // 返回0表明这两点之间存在一条边界，反之则返回1
        half CheckSame(half2 sampleNor1, half2 sampleNor2, half2 sampleDep1, half2 sampleDep2)
        {
            //first : detect difference in normals
            half2 diffNormal = abs(sampleNor1 - sampleNor2) * _Sensitivity.x;
            // normals是否差异大，差异大则表明此两点之间有一条直线
            int isSameNormal = (diffNormal.x + diffNormal.y) < 0.1;

            //difference in depth
            float diffDepth = abs(sampleDep1 - sampleDep2) * _Sensitivity.y;
            int isSameDepth = diffDepth < 0.1 * sampleDep1;

            //return:
            // 1 - if normal and depth are simialr enough
            // 0 - different
            return isSameNormal * isSameDepth ? 1.0 : 0.0;
        }

        //片元着色器
        half4 frag(vertexOutput IN) : SV_TARGET
        {
            // half4 col = _MainTex.Sample(sampler_MainTex, IN.uv);
            // return col * _OutlineColor;

            //法线
            half2 sampleNormal1 = SAMPLE_TEXTURE2D(_CameraNormalsTexture, sampler_CameraNormalsTexture, IN.uv[1]); //右上
            half2 sampleNormal2 = SAMPLE_TEXTURE2D(_CameraNormalsTexture, sampler_CameraNormalsTexture, IN.uv[2]); //左下

            half2 sampleNormal3 = SAMPLE_TEXTURE2D(_CameraNormalsTexture, sampler_CameraNormalsTexture, IN.uv[3]); //左上
            half2 sampleNormal4 = SAMPLE_TEXTURE2D(_CameraNormalsTexture, sampler_CameraNormalsTexture, IN.uv[4]); //右下

            //深度
            //获取view空间下的线性深度值
            half2 sampleDepth1 = LinearEyeDepth(
                SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, IN.uv[1]), _ZBufferParams);
            half2 sampleDepth2 = LinearEyeDepth(
                SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, IN.uv[2]), _ZBufferParams);
            half2 sampleDepth3 = LinearEyeDepth(
                SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, IN.uv[3]), _ZBufferParams);
            half2 sampleDepth4 = LinearEyeDepth(
                SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, IN.uv[4]), _ZBufferParams);

            half edge = 1.0;
            edge *= CheckSame(sampleNormal1, sampleNormal2, sampleDepth1, sampleDepth2);
            edge *= CheckSame(sampleNormal3, sampleNormal4, sampleDepth3, sampleDepth4);

            //边缘直接为outlineColor，其他非边缘照旧为原有屏幕颜色
            half4 withedgeColor = lerp(_OutlineColor,SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv[0]), edge);

            //只显示边缘或者只显示背景
            half4 onlyedgeColor = lerp(_OutlineColor, _BackgroundColor, edge);

            //插值withedgeColor onlyedgeColor
            //half4
            //return edge;
            //return withedgeColor;
            //return onlyedgeColor;
            return lerp(withedgeColor,onlyedgeColor,_EdgeOnly);
        }
        ENDHLSL

        Pass
        {
            Cull Off
            ZTest Always
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma  fragment frag
            ENDHLSL
        }

    }
}