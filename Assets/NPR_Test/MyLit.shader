Shader "MyShader/Ulit"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        _Shades("Shadee",Range(1,10)) = 3
        _OutlineColor("Outline Color", Color) = (1,0,0,1)
        _OutlineWidth("Outline Width", Range(0, 0.1)) = 0.01

    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"
        }

        HLSLINCLUDE
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
            float4 positionHCS : SV_POSITION;
            //用于存储法线的数据
            half3 normalWS :TEXCOORD2;
        };

        CBUFFER_START(UnityPerMaterial)
        half4 _BaseColor;
        float _Shades;
        half4 _OutlineColor;
        half _OutlineWidth;
        CBUFFER_END
        ENDHLSL
        Pass
        {
            Tags
            {
                "LightMode" = "UniversalForward"
            }
            HLSLPROGRAM
            //定义顶点shader的名字
            #pragma vertex vert
            // 定义片元shader的名字
            #pragma  fragment frag


            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                //OUT.postionHCS = TransformObjectToHClip(IN.positionOS.xyz); //局部空间->裁剪空间
                //利用UPR函数
                VertexPositionInputs positionInputs = GetVertexPositionInputs(IN.vertex);
                //clip space position
                OUT.positionHCS = positionInputs.positionCS;

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
                //cosineAngle = pow(cosineAngle * 0.5 + 0.5,2);
                cosineAngle = max(1 / _Shades, 0.5 * cosineAngle + 0.5);
                //cosineAngle = max(1 / _Shades, pow(0.5 * cosineAngle + 0.5,2)); // smooth function
                cosineAngle = floor(cosineAngle * _Shades) / _Shades;
                return _BaseColor * cosineAngle;
            }
            ENDHLSL
        }
        Pass
        {
            Cull Front
            Name "OUTLINE_PASS"
            Tags
            {
                "LightMode" = "SRPDefaultUnlit"
            }


            HLSLPROGRAM
            //定义顶点片元名字
            #pragma vertex OutlinePassVert
            #pragma fragment OutlinePassFrag


            Varyings OutlinePassVert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(IN.vertex);
                OUT.positionHCS = positionInputs.positionCS;

                float3 normalView = mul((float3x3)UNITY_MATRIX_IT_MV, IN.normalOS.xyz);
                float3 normalClip = TransformWViewToHClip(normalView);
                //截取屏幕xy值膨胀
                float2 offset = normalize(normalClip).xy * OUT.positionHCS.w * _OutlineWidth;
                OUT.positionHCS.xy += offset;
                //OUT.positionHCS.xy +=.5;
                return OUT;
            }

            half4 OutlinePassFrag(Varyings IN) : SV_Target
            {
                return _OutlineColor;
            }
            ENDHLSL

        }


    }
}