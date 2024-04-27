Shader "YaoDao/TooShader2"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
        _MainTex("Main Texture", 2D) = "white" {}
        // Ambient light is applied uniformly to all surfaces on the object
        [HDR]
        _AmbientColor("Ambient Color", Color) = (0.4,0.4,0.4,1)
        [HDR]
        _SpecularColor("Specular Color", Color) = (0.9,0.9,0.9,1)
        //Controls the size of the specular reflection
        _Glossiness("Glossiness", Float) = 16
        [HDR]
        _RimColor("Rim Color", Color) = (1,1,1,1)
        _RimAmount("Rim Amount", Range(0,1)) = 0.716
        // Control how smoothly the rim blends when approaching unlit
        //parts of the surface
        _RimThreshold("Rim Threshold", Range(0,1)) = 0.1
        
        //outline
        _OutlineWidth("OutlineWidth", Float) = 0.1
        _OutlineColor("OutlineColor", Color) = (0,0,0,1)
        
    }
    SubShader
    {
        Pass
        {
            // setup out pass to use Forwrad rendering and only 
            // receive data on the main directional light and ambient light
            Tags
            {
                "LightMode" = "ForwardBase"
                "PassFlags" = "OnlyDirectional"
            }
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            //Compile multiple versions of this shader depending on lighting setting
            #pragma multi_compile_fwdbase

            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldNormal : NORMAL;
                float2 uv : TEXCOORD0;
                float3 viewDir : TEXCOORD1;
                SHADOW_COORDS(2)
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.viewDir = WorldSpaceViewDir(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                TRANSFER_SHADOW(o)
                return o;
            }
            float4 _Color;
            float4 _AmbientColor;

            float _Glossiness;
            float4 _SpecularColor;

            float4 _RimColor;
            float _RimAmount;
            float _RimThreshold;

            float4 frag(v2f i) : SV_Target
            {
                //diffuse reflection
                float3 normal = normalize(i.worldNormal);
                float3 viewDir = normalize(i.viewDir);
                float NdotL = dot(_WorldSpaceLightPos0, normal);

                //float lightIntensity = NdotL > 0 ? 1 : 0;
                // shadow
                float shadow = SHADOW_ATTENUATION(i);
                float lightIntensity = smoothstep(0, 0.1, NdotL * shadow);
                float light = lightIntensity * _LightColor0;
                
                //Specular reflection
                float3 halfVector = normalize(_WorldSpaceLightPos0 + viewDir);
                float3 NdotH = dot(normal, halfVector);
                float specularIntensity = pow(NdotH*lightIntensity, _Glossiness * _Glossiness);
                float specularIntensitySmooth = smoothstep(0.005, 0.01, specularIntensity);
                float4 specular = specularIntensitySmooth * _SpecularColor;

                // rim
                float4 rimDot = 1 - dot(viewDir, normal);
                float rimIntensity = rimDot * pow(NdotL, _RimThreshold);
                rimIntensity = smoothstep(_RimAmount - 0.01, _RimAmount + 0.01, rimIntensity);
                float4 rim = rimIntensity * _RimColor;
                

                //Rim lighting
                
                return _Color *  (_AmbientColor + light + specular + rim);
            }
            
            ENDCG
            
        }
        
        Pass
        {
            cull front
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 normal  : NORMAL;
            };

            float _OutlineWidth;

            // vertex
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                float3 clipNormal =  normalize(mul((float3x3)UNITY_MATRIX_MVP, v.normal));
                o.vertex.xy += clipNormal.xy * _OutlineWidth / _ScreenParams.xy * pow(1.0f / o.vertex.w, 0.7f)  * o.vertex.w *_OutlineWidth * 2.0;
                return o;
            }
            float4 _OutlineColor;

            fixed frag(v2f i) : SV_Target
            {
                //fixed4 col = fixed4(1,1,1,1);
                return _OutlineColor;
            }
            ENDCG
        }
    }
    
    /*Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }*/
}
