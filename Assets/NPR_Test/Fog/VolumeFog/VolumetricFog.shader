Shader "MyShader/VolumetricFog"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags
        {
            "RenderType"="UniversalRenderPipeline"
        }
        LOD 100
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

        CBUFFER_START(UnityPerMaterial)
        Texture2D _MainTex;
        SamplerState sampler_MainTex;
        half4 _BaseColor;
        sampler3D _DensityNoiseTex;
        sampler2D _DensityNoise2D;
        float3 _DensityNoise_Scale;
        float3 _DensityNoise_Offset;
        float _Absorption;
        float _LightAbsorption;
        float _LightPower;
        float3 _boundMin;
        float3 _boundMax;
        float _FogXSpeed;
        float _FogYSpeed;
        CBUFFER_END
        // TEXTURE2D(_MainTex);
        // SAMPLER(sampler_MainTex);

        struct Attributes
        {
            float4 vertex : POSITION;
            float2 uv: TEXCOORD0;
        };

        struct Varyings
        {
            float4 pos : SV_POSITION;
            float2 uv : TEXCOORD0;
        };

        //重建世界坐标函数，利用DeclareDepthTexture库以及NDC坐标 、逆VP矩阵得到wortldspace position
        float3 GetWorldPosition(float3 positionHCS)
        {
            // get world space postion
            float2 ndc_pos_xy = positionHCS.xy / _ScaledScreenParams.xy;
            #if UNITY_REVERSED_Z
            real depth = SampleSceneDepth(ndc_pos_xy);
            #else
                real depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, SampleSceneDepth(ndc_pos_xy);
            #endif
            return ComputeWorldSpacePosition(ndc_pos_xy, depth,UNITY_MATRIX_I_VP);
        }

        // 射线与包围盒相交
        // 通过传入包围盒最小坐标和最大坐标以及射线的起始位置和方向，
        // 返回从射线起点到包围盒最近的距离
        float2 rayBoxDst(float3 boundsMin, float3 boundsMax, float3 rayOrigin, float3 rayDir)
        {
            //通过boundsMin和boundsMax锚定一个长方体包围盒，
            //从rayOrigin朝rayDir发射一条射线，计算出射线到包围盒的距离

            float3 t0 = (boundsMin - rayOrigin) / rayDir;
            float3 t1 = (boundsMax - rayOrigin) / rayDir;

            float3 tmin = min(t0, t1);
            float3 tmax = max(t0, t1);
            //射线到box两个相交点的距离 dstA为最近距离，dstB为最远距离 在三个维度上,
            //只要有一个维度不在包围盒内意味着射线已经离开包围盒
            //因此从大中取到最小的，从小中取到最大的
            float dstA = max(max(tmin.x, tmin.y), tmin.z);
            float dstB = min(min(tmax.x, tmax.y), tmax.z); //最大里面选最小的

            //射线起点到包围盒最近距离
            float dstToBox = max(0, dstA);
            //射线穿过包围盒的距离
            float dstInBox = max(0, dstB - dstToBox);

            return float2(dstToBox, dstInBox);
        }

        float RayMarching(float dstLimit)
        {
            float sum = 0;
            for (int i = 0; i < 32; i++)
            {
                if (dstLimit > 0)
                {
                    sum += 0.0003;
                    if (sum > 1)
                        break;
                }
            }
            return sum;
        }

        Varyings vert(Attributes IN)
        {
            Varyings OUT;
            OUT.pos = TransformObjectToHClip(IN.vertex.xyz);
            OUT.uv = IN.uv;
            return OUT;
        }

        half4 frag(Varyings IN) : SV_Target
        {
            //采样主纹理
            half4 albedo = _MainTex.Sample(sampler_MainTex, IN.uv);


            //重建世界坐标
            float3 worldPosition = GetWorldPosition(IN.pos.xyz);
            float3 ray = worldPosition - _WorldSpaceCameraPos;
            float3 rayDir = normalize(worldPosition - _WorldSpaceCameraPos);

            //调用rayBoxDst函数，计算碰撞体积
            float2 rayBoxInfo = rayBoxDst(_boundMin, _boundMax, _WorldSpaceCameraPos.xyz, rayDir);
            float dstToBox = rayBoxInfo.x;
            float dstInsideBox = rayBoxInfo.y;
            float depthLinear = length(ray);
            float dstLimit = min(depthLinear - dstToBox, dstInsideBox);

            float density = RayMarching(dstLimit);

            //噪声
            float2 spped = _Time.y * float2(_FogXSpeed, _FogYSpeed);
            float noise = tex2D(_DensityNoise2D, IN.uv + spped - 0.5).r * 10;
            float finalDesity = saturate(density * (1 + noise));

            half4 finalColor = lerp(albedo, _BaseColor, finalDesity);

            return finalColor;
        }
        ENDHLSL

        Pass
        {
            Cull Off
            ZTest Always
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            ENDHLSL
        }
    }

   
}