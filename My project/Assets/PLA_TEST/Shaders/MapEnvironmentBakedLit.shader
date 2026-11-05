Shader "Dicegeon/Map Environment Baked Lit"
{
    Properties
    {
        _MainTex ("Albedo", 2D) = "white" {}
        _Color ("Color", Color) = (1, 1, 1, 1)
        [Toggle(_ALPHATEST_ON)] _AlphaClip ("Alpha Clipping", Float) = 0
        _Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.5
    }

    SubShader
    {
        Tags { "Queue" = "Geometry" "RenderType" = "Opaque" }

        Pass
        {
            Tags { "LightMode" = "ForwardBase" }
            Cull Back
            ZWrite On
            ZTest LEqual

            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma shader_feature_local _ALPHATEST_ON
            #include "UnityCG.cginc"
            #include "Lighting.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            half _Cutoff;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
                float2 lightmapUV : TEXCOORD1;
            };

            struct v2f
            {
                float4 position : SV_POSITION;
                float2 uv : TEXCOORD0;
                half3 worldNormal : TEXCOORD1;
                float2 lightmapUV : TEXCOORD2;
            };

            v2f vert(appdata input)
            {
                v2f output;
                output.position = UnityObjectToClipPos(input.vertex);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.worldNormal = UnityObjectToWorldNormal(input.normal);
                output.lightmapUV = input.lightmapUV * unity_LightmapST.xy + unity_LightmapST.zw;
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                fixed4 albedo = tex2D(_MainTex, input.uv) * _Color;

                #if defined(_ALPHATEST_ON)
                    clip(albedo.a - _Cutoff);
                #endif

                #if defined(LIGHTMAP_ON)
                    half3 bakedGI = DecodeLightmap(UNITY_SAMPLE_TEX2D(unity_Lightmap, input.lightmapUV));
                    return fixed4(albedo.rgb * bakedGI, albedo.a);
                #else
                    half3 normalWS = normalize(input.worldNormal);
                    half3 bakedGI = max(ShadeSH9(half4(normalWS, 1.0h)), 0.0h);
                    half3 directLight = _LightColor0.rgb * saturate(dot(normalWS, _WorldSpaceLightPos0.xyz)) * 0.55h;
                    return fixed4(albedo.rgb * (bakedGI + directLight), albedo.a);
                #endif
            }
            ENDCG
        }

        Pass
        {
            Name "META"
            Tags { "LightMode" = "Meta" }
            Cull Off

            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert_meta
            #pragma fragment frag_meta
            #pragma shader_feature_local _ALPHATEST_ON
            #include "UnityCG.cginc"
            #include "UnityMetaPass.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            half _Cutoff;

            struct appdata_meta
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                float2 uv2 : TEXCOORD2;
            };

            struct v2f_meta
            {
                float4 position : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f_meta vert_meta(appdata_meta input)
            {
                v2f_meta output;
                output.position = UnityMetaVertexPosition(input.vertex, input.uv1, input.uv2, unity_LightmapST, unity_DynamicLightmapST);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }

            fixed4 frag_meta(v2f_meta input) : SV_Target
            {
                fixed4 albedo = tex2D(_MainTex, input.uv) * _Color;

                #if defined(_ALPHATEST_ON)
                    clip(albedo.a - _Cutoff);
                #endif

                UnityMetaInput meta;
                UNITY_INITIALIZE_OUTPUT(UnityMetaInput, meta);
                meta.Albedo = albedo.rgb;
                meta.Emission = 0;
                return UnityMetaFragment(meta);
            }
            ENDCG
        }
    }
}
