Shader "MyShader/FogWiithTexture"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _FogDensity("Fog Density", Float) = 1.0
        _FogColor("Fog Color", Color) = (1,1,1,1)
        _FogStart("Fog Start", Float) = 0.0
        _FogEnd("Fog End", Float) = 1.0
    }
    SubShader
    {
        Tags
        {
            "RenderType"="UniversalPipeline"
        }
        LOD 100
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

        CBUFFER_START(UnityPerMaterial)
        float4x4 _FrustumCornersRay;
        half4 _MainTex_TexelSize;
        half _FogDensity;
        half4 _FogColor;
        float _FogStart;
        float _FogEnd;
        sampler2D _textureNoise2D;
        float _textureNoiseAmount;
        float _fogXSpeed;
        float _fogYSpped;
        CBUFFER_END

        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);
        TEXTURE2D(_CameraDepthTexture);
        SAMPLER(sampler_CameraDepthTexture);

        struct appdata
        {
            float4 vertex : POSITION;
            float2 texcoord : TEXCOORD0;
        };

        struct v2f
        {
            float4 pos : SV_POSITION;
            half2 uv : TEXCOORD0;
            half2 uv_depth : TEXCOORD1;
            float4 interpolatedRay : TEXCOORD2;
        };

        v2f vert(appdata v)
        {
            v2f o;
            o.pos = TransformObjectToHClip(v.vertex.xyz);
            o.uv = v.texcoord;
            o.uv_depth = v.texcoord;

            #if UNITY_UV_STARTS_AT_TOP
            if (_MainTex_TexelSize.y < 0)
                o.uv_depth.y = 1 - o.uv_depth.y;
            #endif

            int index = 0;
            if (v.texcoord.x < 0.5 && v.texcoord.y < 0.5)
            {
                index = 0; //左下
            }
            else if (v.texcoord.x > 0.5 && v.texcoord.y < 0.5)
            {
                index = 1; //右下
            }
            else if (v.texcoord.x > 0.5 && v.texcoord.y > 0.5)
            {
                index = 2; //右上
            }
            else
            {
                index = 3;
            }
            #if UNITY_UV_STARTS_AT_TOP
            if (_MainTex_TexelSize.y < 0)
                index = 3 - index;
            #endif
            o.interpolatedRay = _FrustumCornersRay[index];

            return o;
        }

        half4 frag(v2f i) : SV_Target
        {
            //使用LinearEyeDepth得到视角空间下的线性深度值
            float linearDepth = LinearEyeDepth(
                SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, i.uv), _ZBufferParams);
            float3 worldPos = _WorldSpaceCameraPos + linearDepth * i.interpolatedRay.xyz;
            //距离雾
            float3 temp = worldPos - _WorldSpaceCameraPos;
            float fogDensity = (length(temp) - _FogStart) / _FogStart;
            //float fogDensity = min((length(temp) - _FogStart) / (_FogEnd - _FogStart), 1);
            //高度雾
            //float fogDensity = (_FogEnd - worldPos.y) / (_FogEnd - _FogStart);
            fogDensity = saturate(fogDensity * _FogDensity);


            float2 speed = _Time.y * float2(_fogXSpeed, _fogYSpped);
            float noise = tex2D(_textureNoise2D, i.uv + speed).r * _textureNoiseAmount;
            float finalDensity = saturate(fogDensity * (1 + noise));
            half4 finalColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
            finalColor.rgb = lerp(finalColor.rgb, _FogColor.rgb, finalDensity);
            return finalColor;
        }
        ENDHLSL

        Pass
        {
            ZTest Always Cull Off ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            ENDHLSL
        }
    }
    FallBack "Packages/com.unity.render-pipelines.universal/FallbackError"
}