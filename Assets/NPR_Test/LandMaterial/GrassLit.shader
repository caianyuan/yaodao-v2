Shader "MyShader/Ulit2"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        _Shades("Shadee",Range(1,10)) = 3
        _OutlineColor("Outline Color", Color) = (1,0,0,0)
        _OutlineWidth("Outline Width", Range(0, 0.1)) = 0.01

    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"
        }
        Pass
        {
            Tags
            {
                "LightMode" = "UniversalForward"
            }
            Cull Off
            
            HLSLPROGRAM
            //定义顶点shader的名字
            #pragma vertex vert
            // 定义片元shader的名字
            #pragma  fragment frag

            //这个Core.hlsl文件包含了常用的HLSL宏定义以及函数，也包括了对其他常用HLSL文件的引用
            //例如Common.hlsl, SpaceTransform.hlsl
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            //下面结构体包含了顶点着色器的输入数据
            struct Attributes
            {
                float3 vertex : POSITION; // 输入顶点位置 OS- object space
                //声明需要法线数据
                half3 normalOS : NORMAL; //object space normal
            };

            //输出变量
            struct Varyings
            {
                //这个结构体必须包含SV_POSITION, Homogeneous Clipping Space
                float4 postionHCS : SV_POSITION;
                //用于存储法线的数据
                half3 normalWS :TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            float _Shades;
            CBUFFER_END


            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                //OUT.postionHCS = TransformObjectToHClip(IN.positionOS.xyz); //局部空间->裁剪空间
                //利用UPR函数
                VertexPositionInputs positionInputs = GetVertexPositionInputs(IN.vertex);
                //clip space position
                OUT.postionHCS = positionInputs.positionCS;

                // Normal
                //VertexNormalInputs normalInputs = GetVertexNormalInputs(IN.normalOS);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                //OUT.normalWS = normalInputs.normalWS;

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target //SV_Target 固定语义
            {
                //return _BaseColor;
                Light light = GetMainLight();
                float3 lightDirWs = light.direction;
                float cosineAngle = dot(IN.normalWS, normalize(lightDirWs));
                //cosineAngle = max(0.0, cosineAngle);
                cosineAngle = max(1 / _Shades, 0.5 * cosineAngle + 0.5);
                cosineAngle = floor(cosineAngle * _Shades) / _Shades;
                return _BaseColor * cosineAngle;
            }
            ENDHLSL
        }



    }
}