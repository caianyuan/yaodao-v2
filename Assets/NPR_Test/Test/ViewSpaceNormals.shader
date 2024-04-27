Shader "MyShader/ViewSpaceNormals2"
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
        CBUFFER_START(UnityPerMaterial)
        CBUFFER_END


        struct vertexInput
        {
            half4 vertex : POSITION;
            float2 texcoord : TEXCOORD0;
            half3 normalOS : NORMAL;
        };

        struct vertexOutput
        {
            half4 pos : SV_POSITION;
            float2 uv : TEXCOORD0;
            half3 normalVS : TEXCOORD1;
        };

        vertexOutput vert(vertexInput v)
        {
            vertexOutput OUT;
            OUT.pos = TransformObjectToHClip(v.vertex);
            //OUT.uv = v.texcoord;
            //OUT.uv = UNITY_MATRIX_IT_MV(v.texcoord);
            OUT.normalVS = mul((real3x3)UNITY_MATRIX_IT_MV, v.normalOS);
            //OUT.normalVS  = v.normalOS;
            //OUT.normalVS = v.normalOS;
            return OUT;
        }

        //remap
        half remap(half x, half t1, half t2, half s1, half s2)
        {
            return (x - t1) / (t2 - t1) * (s2 - s1) + s1;
        }


        half4 frag(vertexOutput IN) : SV_TARGET
        {
            //return RangeRemap()
            //IN.normalVS = normalize(IN.normalVS);
            // half normal_x = remap(IN.normalVS.x, -1, 1, 0, 1);
            // half normal_y = remap(IN.normalVS.y, -1, 1, 0, 1);
            // half normal_z = remap(IN.normalVS.z, -1, 1, 0, 1);
            //return half4(normal_x,normal_y,normal_z,0);

            half normal_x = (IN.normalVS.x + 1) / 2.0;
            half normal_y = (IN.normalVS.y + 1) / 2.0;
            half normal_z = (IN.normalVS.z + 1) / 2.0;
            //return half4(1, 0, 0, 0);
            return half4(normal_x, normal_y, normal_z, 1);
        }
        ENDHLSL

        PASS
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