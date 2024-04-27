Shader "MyShader/WaterShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BaseColor("Base Color",Color) = (1,1,1,1)
        //What color the water will sample when the surface below is shallow  浅
        _DepthGradientShallow("Depth Gradient Shallow", Color) = (0.325, 0.807,0.971,0.725)
        //What color the water will sample when surface below is at its deepest. 深
        _DepthGradientDeep("Depth Gradient Deep", Color) = (0.086, 0.407, 1, 0.749)
        //Maximum distance the surface below the water will affect the color gradient
        _DepthMaxDistance("Depth Maximum Distance", Float) = 1

        _SurfaceNoise("Surface Noise", 2D) = "white" {}
        _SurfaceNoiseCutoff("Surface Noise Cutoff", Range(0,1)) = 0.777

        //Foam Control for what depth the shoreline is visible.
        _FoamDistance("Foam Distance", Float) = 0.4
        //animation
        _SurfaceNoiseScroll("Surface Noise Scroll Amount", Vector) = (0.03, 0.03, 0, 0)

        // distortion
        // two channel distortion texture
        _SurfaceDistortion("Surface Distortion", 2D) = "white" {}
        // Control to multiply the strength of the distortion.
        _SurfaceDistortionAmount("Surface Distortion Amount", Range(0,1)) = 0.27

        //Foam
        _Foam("Foam", Float) = 0.5
        _FoamColor("Foam Color", Color) = (1,1,1,1)
        
        // Wave
        // 天空盒反射的强度
        _WaveStrength("WaveStrength", Range(0,1)) = 0.1
        // Wave Height
        _WaveHeight("WaveHeight", Range(0.0,0.5)) = 0.05
    }
    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalRenderPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }
        Blend SrcAlpha OneMinusSrcAlpha
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
        #define REQUIRE_DEPTH_TEXTURE
        #define SMOOTHSTEP_AA 0.01

        CBUFFER_START(UnityPerMaterial)
        half4 _MainTex_ST;
        half4 _BaseColor;
        half4 _DepthGradientShallow;
        half4 _DepthGradientDeep;
        float _DepthMaxDistance;
        half4 _SurfaceNoise_ST;
        float _SurfaceNoiseCutoff;
        float _FoamDistance;
        float2 _SurfaceNoiseScroll;
        half4 _SurfaceDistortion_ST;
        float _SurfaceDistortionAmount;
        float _Foam;
        half4 _FoamColor;
        float _WaveStrength;
        float _WaveHeight;
        CBUFFER_END
        TEXTURE2D(_MainTex);
        SAMPLER(sampelr_MainTex);
        TEXTURE2D(_SurfaceNoise);
        SAMPLER(sampler_SurfaceNoise);
        // TEXTURE2D(_CameraDepthTexture);
        // SAMPLER(sampler_CameraDepthTexture);
        TEXTURE2D(_SurfaceDistortion);
        SAMPLER(sampler_SurfaceDistortion);

        struct Attributes
        {
            float4 vertex : POSITION;
            float2 uv : TEXCOORD0;
        };

        struct Varying
        {
            float4 pos : SV_POSITION;
            float2 uv : TEXCOORD0;
            float4 screenPosition : TEXCOORD1;
            float2 noiseUV : TEXCOORD2;
            float2 distortUV : TEXCOORD3;
            float3 worldPos : TEXCOORD4;
        };

        Varying vert(Attributes IN)
        {
            Varying OUT;
            OUT.pos = TransformObjectToHClip(IN.vertex.xyz);
            OUT.worldPos = TransformObjectToWorld(IN.vertex.xyz);
            OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
            OUT.screenPosition = ComputeScreenPos(OUT.pos);
            OUT.noiseUV = TRANSFORM_TEX(IN.uv, _SurfaceNoise);
            OUT.distortUV = TRANSFORM_TEX(IN.uv, _SurfaceDistortion);
            return OUT;
        }

        //噪声图生成
        float2 rand(float2 st, int seed)
        {
            float2 s = float2(dot(st, float2(127.1, 311.7)) + seed, dot(st, float2(269.5, 183.3)) + seed);
            return -1 + 2 * frac(sin(s) * 43758.5453123);
        }

        float noise(float2 st, int seed)
        {
            st.x += _Time.y;

            float2 p = floor(st);
            float2 f = frac(st);

            float w00 = dot(rand(p, seed), f);
            float w10 = dot(rand(p + float2(1, 0), seed), f - float2(1, 0));
            float w01 = dot(rand(p + float2(0, 1), seed), f - float2(0, 1));
            float w11 = dot(rand(p + float2(1, 1), seed), f - float2(1, 1));

            float2 u = f * f * (3 - 2 * f);

            return lerp(lerp(w00, w10, u.x), lerp(w01, w11, u.x), u.y);
        }

        // 海浪的涌起法线计算
        float3 swell(float3 pos, float anisotropy)
        {
            float3 normal;
            float height = noise(-pos.xz * _WaveHeight, 0);
            height *= anisotropy; //使距离地平线近的区域的海浪高度降低
            normal = normalize(cross(float3(0, ddy(height), 1), float3(1, ddx(height), 0)));
            return normal;
        }


        half4 frag(Varying IN) : SV_Target
        {
            //通过_CameraDepthTexture获取深度值
            // float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, IN.uv);
            // //使用linearEyeDepth得到视角空间下的线性深度值
            // float linearDepth = LinearEyeDepth(depth, _ZBufferParams);
            // float depthDifference = linearDepth - IN.screenPosition.w;
            //
            // float waterDepthDifference01 = saturate(depthDifference / _DepthMaxDistance);
            // half4 waterColor = lerp(_DepthGradientShallow, _DepthGradientDeep, waterDepthDifference01);
            // //return waterColor;
            // //return waterDepthDifference01;
            // return _BaseColor;
            //return depthDifference;
            float2 screenPos = IN.screenPosition.xy / IN.screenPosition.w;
            float depth = LinearEyeDepth(SampleSceneDepth(screenPos), _ZBufferParams);
            float depthDifference = depth - IN.screenPosition.w;
            float waterDepthDifference01 = saturate(depthDifference / _DepthMaxDistance);
            float4 waterColor = lerp(_DepthGradientShallow, _DepthGradientDeep, waterDepthDifference01);
            //distortSample
            //float2 distortSample = (tex2D(_SurfaceDistortion, IN.distortUV).xy * 2 - 1) * _SurfaceDistortionAmount;
            float2 distortSample = (SAMPLE_TEXTURE2D(_SurfaceDistortion, sampler_SurfaceDistortion, IN.distortUV).xy * 2
                - 1) * _SurfaceDistortionAmount;
            //Scroll noiseUV
            float2 noiseUV = float2((IN.noiseUV.x + _Time.y * _SurfaceNoiseScroll.x) + distortSample.x,
                                    (IN.noiseUV.y + _Time.y * _SurfaceNoiseScroll.y) + distortSample.y);
            // water perlin noise
            float surfaceNoiseSample = SAMPLE_TEXTURE2D(_SurfaceNoise, sampler_SurfaceNoise, noiseUV).r;
            //foam
            float foamDepthDifference01 = saturate(depthDifference / _FoamDistance);
            // float surfaceNoiseCutoff = foamDepthDifference01 * _SurfaceNoiseCutoff;
            //float surfaceNoise = surfaceNoiseSample > _SurfaceNoiseCutoff ? 1 : 0;
            half4 foamLine = 1 - saturate(_Foam * depthDifference * surfaceNoiseSample);
            //foamLine = smoothstep(foamLine - SMOOTHSTEP_AA,foamLine + SMOOTHSTEP_AA, foamLine);
            //half4 col = foamLine * _FoamColor;

            //水体波动
            half3 worldViewDir = normalize(_WorldSpaceCameraPos - IN.worldPos);
            float3 v = IN.worldPos - _WorldSpaceCameraPos;
            //通过临近像素间摄像机到片元位置差值来计算哪里是接近地平线的部分
            //地平线处波浪在屏幕上显得更窄，而摄像机附近的波浪在屏幕上显得更宽，因此减低远处波浪高度
            float anisotropy = saturate(1 / ddy(length(v.xz)) / 10); //
            float3 swelledNormal = swell(IN.worldPos, anisotropy);
            //只反射天空盒
            //反射向量
            half3 reflDir = reflect(-worldViewDir, swelledNormal);
            half4 reflectionColor = SAMPLE_TEXTURECUBE(unity_SpecCube0, samplerunity_SpecCube0, reflDir);

            half4 col = lerp(waterColor, _FoamColor, foamLine);
            half4 finalcol = lerp(col, reflectionColor, _WaveStrength);
            //return col;
            //return min(waterColor + surfaceNoise,1);
            //return col;
            return finalcol;
        }
        ENDHLSL


        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            ENDHLSL
        }
    }
    FallBack "Packages/com.unity.render-pipelines.universal/FallbackError"
}