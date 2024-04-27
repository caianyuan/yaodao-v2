Shader "Hidden/ScreenSpaceOutline"
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
            "RenderPipeline" = "UniversalRenderPipeline"
            "RenderType"="Opaque"
        }
        LOD 100

        Pass
        {
            Name "Outline"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include"Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"


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

            sampler sampler_LinearClamp;
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
                float2 uv[9] : TEXCOORD0;
            };

            //顶点着色器
            vertexOutput vert(vertexInput v)
            {
                vertexOutput OUT;
                OUT.pos = TransformObjectToHClip(v.vertex);
                half2 uv = v.texcoord;

                // OUT.uv[0] = uv + _MainTex_TexelSize.xy * half2(-1, -1);
                // OUT.uv[1] = uv + _MainTex_TexelSize.xy * half2(0, -1);
                // OUT.uv[2] = uv + _MainTex_TexelSize.xy * half2(1, -1);
                //
                // OUT.uv[3] = uv + _MainTex_TexelSize.xy * half2(-1, 0);
                // OUT.uv[4] = uv + _MainTex_TexelSize.xy * half2(0, 0);
                // OUT.uv[5] = uv + _MainTex_TexelSize.xy * half2(1, 0);
                //
                // OUT.uv[6] = uv + _MainTex_TexelSize.xy * half2(-1, 1);
                // OUT.uv[7] = uv + _MainTex_TexelSize.xy * half2(0, 1);
                // OUT.uv[8] = uv + _MainTex_TexelSize.xy * half2(1, 1);
                return OUT;
                // OUT.uv[0] = uv;
                // #if UNITY_UV_STARTS_AT_TOP
                // if (_MainTex_TexelSize.y < 0)
                //     uv.y = 1 - uv.y;
                // #endif
                //
                // //Robert 算子
                // OUT.uv[1] = uv + _MainTex_TexelSize.xy * half2(1, 1) * _SampleDistance; //右上[1,1]
                // OUT.uv[2] = uv + _MainTex_TexelSize.xy * half2(-1, -1) * _SampleDistance; //左下[-1,-1]
                // OUT.uv[3] = uv + _MainTex_TexelSize.xy * half2(-1, 1) * _SampleDistance; //左上[-1,1]
                // OUT.uv[4] = uv + _MainTex_TexelSize.xy * half2(1, -1) * _SampleDistance; //右下[1,-1]
            }

            // 检测边缘
            // 返回0表明这两点之间存在一条边界，反之则返回1
            // half CheckSame(half2 sampleNor1, half2 sampleNor2, half2 sampleDep1, half2 sampleDep2)
            // {
            //     //first : detect difference in normals
            //     half2 diffNormal = abs(sampleNor1 - sampleNor2) * _Sensitivity.x;
            //     // normals是否差异大，差异大则表明此两点之间有一条直线
            //     int isSameNormal = (diffNormal.x + diffNormal.y) < 0.1;
            //
            //     //difference in depth
            //     float diffDepth = abs(sampleDep1 - sampleDep2) * _Sensitivity.y;
            //     int isSameDepth = diffDepth < 0.1 * sampleDep1;
            //
            //     //return:
            //     // 1 - if normal and depth are simialr enough
            //     // 0 - different
            //     return isSameNormal * isSameDepth ? 1.0 : 0.0;
            // }
            /////////////////////////////////////////////////////
            //灰度图化
            half luminance(half4 color)
            {
                return 0.2125 * color.r + 0.7154 * color.g + 0.0721 * color.b;
            }

            half Sobel(vertexOutput IN)
            {
                const half Gx[9] = {
                    -1, -2, -1,
                    0, 0, 0,
                    1, 2, 1
                };
                const half Gy[9] = {
                    -1, 0, 1,
                    -2, 0, 2,
                    -1, 0, 1
                };
                half texColor;
                half edgeX = 0;
                half edgeY = 0;
                for (int it = 0; it < 9; it++)
                {
                    //texColor = luminance(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv[it]));
                    texColor = luminance(SAMPLE_TEXTURE2D(_MainTex, sampler_LinearClamp, IN.uv[it]));
                    edgeX += texColor * Gx[it];
                    edgeY += texColor * Gy[it];
                }
                half edge = 1 - abs(edgeX) - abs(edgeY);
                return edge;
            }

            ////////////////////////////////////////////////////////////////////////////////////////

            half intensity(half4 color)
            {
                return sqrt((color.x * color.x) + (color.y * color.y) + (color.z * color.z));
            }

            float Outline(float stepX, float stepY, float2 center)
            {
                float topLeft = intensity(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, center + float2(-stepX, stepY)));
                float midLeft = intensity(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, center + float2(-stepX, 0)));
                float bottomLeft = intensity(
                    SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, center + float2(-stepX, -stepY)));
                float midTop = intensity(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, center + float2(0, stepY)));
                float midBottom = intensity(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, center + float2(0, -stepY)));
                float topRight = intensity(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, center + float2(stepX, stepY)));
                float midRight = intensity(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, center + float2(stepX, 0)));
                float bottomRight = intensity(
                    SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, center + float2(stepX, -stepY)));

                //        3  0 -3        3  10   3
                //    X = 10 0 -10  Y =  0   0   0
                //        3  0 -3       -3 -10 -3

                float Gx = 3.0 * topLeft + 10.0 * midLeft + 3.0 * bottomLeft - 3.0 * topRight - 10.0 * midRight - 3.0 *
                    bottomRight;
                float Gy = 3.0 * topLeft + 10.0 * midTop + 3.0 * topRight - 3.0 * bottomLeft - 10.0 * midBottom - 3.0 *
                    bottomRight;

                return sqrt(Gx * Gx + Gy * Gy);
            }

            /////////////////////////////////////////////////////////////////////////////////////////////////////

            //片元着色器
            half4 frag(vertexOutput IN) : SV_TARGET
            {
                // half edge = Sobel(IN);
                // edge = smoothstep(0.1 / max(length(_WorldSpaceCameraPos), 1), 1, edge);
                // return edge;

                float2 screenUV = IN.pos.xy / _ScreenParams.xy;
                half4 sceneColor = SAMPLE_TEXTURE2D(_MainTex, sampler_LinearClamp, screenUV);
                
                float outlineGradient = Outline(_SampleDistance / _ScreenParams.x, _SampleDistance / _ScreenParams.y,
                                                screenUV);
                outlineGradient = smoothstep(0.1 / max(length(_WorldSpaceCameraPos), 1), 1, outlineGradient);
                return outlineGradient;


                /////////////////////////////////////////////////////////////////////////////////////////
                // half4 col = _MainTex.Sample(sampler_MainTex, IN.uv);
                // return col * _OutlineColor;

                //法线
                // half2 sampleNormal1 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv[1]);
                // //右上
                // half2 sampleNormal2 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv[2]);
                // //左下
                //
                // half2 sampleNormal3 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv[3]);
                // //左上
                // half2 sampleNormal4 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv[4]);
                //右下

                //深度
                //获取view空间下的线性深度值
                // half2 sampleDepth1 = LinearEyeDepth(
                //     SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, IN.uv[1]), _ZBufferParams);
                // half2 sampleDepth2 = LinearEyeDepth(
                //     SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, IN.uv[2]), _ZBufferParams);
                // half2 sampleDepth3 = LinearEyeDepth(
                //     SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, IN.uv[3]), _ZBufferParams);
                // half2 sampleDepth4 = LinearEyeDepth(
                //     SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, IN.uv[4]), _ZBufferParams);

                // half edge = 1.0;
                // edge *= CheckSame(sampleNormal1, sampleNormal2, sampleDepth1, sampleDepth2);
                // edge *= CheckSame(sampleNormal3, sampleNormal4, sampleDepth3, sampleDepth4);

                //边缘直接为outlineColor，其他非边缘照旧为原有屏幕颜色
                //half4 withedgeColor = lerp(_OutlineColor,SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv[0]), edge);

                //只显示边缘或者只显示背景
                //half4 onlyedgeColor = lerp(_OutlineColor, _BackgroundColor, edge);

                //插值withedgeColor onlyedgeColor
                //half4
                //return half4(1, 0, 0, 0);
                //return withedgeColor;
                //return onlyedgeColor;
                //return lerp(withedgeColor, onlyedgeColor, _EdgeOnly);
            }
            ENDHLSL
        }
        Pass
        {
            Name "Mixed"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
            float _EdgeOnly;
            half4 _OutlineColor;
            half4 _BackgroundColor;
            CBUFFER_END

            sampler sampler_LinearClamp;
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_SourceTex);
            SAMPLER(sampler_SourceTex);

            struct vertexInput
            {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct vertexOutput
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            vertexOutput vert(vertexInput v)
            {
                vertexOutput OUT;
                OUT.pos = TransformObjectToHClip(v.vertex);
                OUT.uv = v.texcoord;
                return OUT;
            }

            half4 frag(vertexOutput IN): SV_TARGET
            {
                // ///////////////////////////////////////////////////////////////////////////
                // half4 sceneColor = SAMPLE_TEXTURE2D(_SourceTex, sampler_SourceTex, IN.uv);
                // half lineColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv).r;
                //
                // half4 outlineColor = lerp(_OutlineColor, sceneColor, lineColor);
                // return outlineColor;
                ////////////////////////////////////////////////////////////////////////////////
                float2 screenUV = IN.pos.xy / _ScreenParams.xy;
                half4 sceneColor = SAMPLE_TEXTURE2D(_SourceTex, sampler_LinearClamp, screenUV);
                half lineColor = SAMPLE_TEXTURE2D(_MainTex, sampler_LinearClamp, screenUV).r;
                float4 outlineColor = lerp(sceneColor, _OutlineColor, lineColor);
                return outlineColor;
            }
            ENDHLSL

        }


    }
}