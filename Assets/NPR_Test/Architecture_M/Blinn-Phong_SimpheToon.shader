Shader "MyShader/Blinn-Phong_SimpheToon"
{
    Properties
    {
        /*_MainTex ("Texture", 2D) = "white" {}*/
        _BaseMap("MainTex", 2D) = "White" { }
        _BaseColor("Base Color", Color) = (1,0,0,1)
        _Gloss("Gloss", Range(8,20)) = 8.0
        _LevelCount("LevelCount", Range(1,10)) = 5
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"
        }
        LOD 100

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

        CBUFFER_START(UnityPerMaterial)
        half4 _BaseColor;
        half4 _BaseMap_ST;
        half _Gloss;
        half _LevelCount;
        CBUFFER_END


        struct Attributes
        {
            // The positionOS variable contains the vertex position in object space
            float4 positionOS : POSITION;
            float4 normalOS : NORMAL;
            float2 uv : TEXCOORD0;
        };

        struct Varyings
        {
            // The output position in this struct must have the SV_Position semantic
            float4 positionHCS : SV_POSITION;
            //uv coordinate
            float2 uv: TEXCOORD0;
            //world space position
            float3 positionWS : TEXCOORD1;
            half3 normalWS : TEXCOORD2;
        };
        ENDHLSL

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // TEXTURE2D(_BaseMap);
            // SAMPLER(SAMPLER_BaseMap);


            Varyings vert(Attributes IN)
            {
                //Declaring the output object(OUT) with the Varying struct
                Varyings OUT;
                //The TransformObjectToHClip function transforms vertex positions from
                //object space to homogenous clip space //齐次裁剪空间
                //OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                VertexPositionInputs vertexInput = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionHCS = vertexInput.positionCS;
                OUT.positionWS = vertexInput.positionWS;
                //uv
                //OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);

                VertexNormalInputs normalInput = GetVertexNormalInputs(IN.normalOS.xyz);
                OUT.normalWS = normalInput.normalWS;
                return OUT;
            }


            half4 frag(Varyings IN) : SV_Target
            {
                // half3 color = half3((sin(frac(_Time.y)) + 1.0) / 2.0, 0.0, (cos(frac((_Time.y)) + 1.0) / 2.0));
                // return half4(color, 1.0);
                Light mainLight = GetMainLight(); // main light
                half3 lightDir = mainLight.direction;

                // ambient
                //half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, SAMPLER_BaseMap, IN.uv);
                half3 ambient = half4(unity_SHAr.w, unity_SHAg.w, unity_SHAb.w, 1) * _BaseColor;
                //diffuse
                float NdotL = pow(dot(normalize(IN.normalWS), normalize(lightDir)) * 0.5 + 0.5, 1);
                float ramp = max(1.0 / _LevelCount, floor(NdotL * _LevelCount) / _LevelCount);
                half3 diffuse = _BaseColor * ramp * mainLight.color;

                //specular
                half3 viewDir = normalize(_WorldSpaceCameraPos - IN.positionWS);
                half3 halfWayDir = normalize(lightDir + viewDir);
                float NdotH = dot(IN.normalWS, halfWayDir);
                float spec = pow(saturate(NdotH), _Gloss);
                half4 specular = _BaseColor * spec;

                //return ambient + diffuse + specular;
                //return difffuse;

                /////////计算其他光源------------additional light 附加光源////////////////
                //获取外灯的数量
                int addlightCount = GetAdditionalLightsCount();
                for (int lightIndex = 0; lightIndex < addlightCount; lightIndex++)
                {
                    Light additional_light = GetAdditionalLight(lightIndex, IN.positionWS);
                    float additional_light_NdotL = pow(
                        dot(normalize(IN.normalWS), normalize(additional_light.direction)) * 0.5 + 0.5, 1);

                    float ramp2 = max(1.0 / _LevelCount, floor(additional_light_NdotL * _LevelCount) / _LevelCount);
                    //diffuse += _BaseColor.rgb * ramp2 * additional_light.color * additional_light.shadowAttenuation;
                }
                return half4(ambient + diffuse, 1.0);
            }
            ENDHLSL
        }
    }
}