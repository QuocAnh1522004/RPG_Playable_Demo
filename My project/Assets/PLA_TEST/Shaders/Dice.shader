Shader "Dicegeon/Dice"
{
    Properties
    {
        [MainTexture] _MainTex ("Base Map (RGB) Alpha (A)", 2D) = "white" {}
        [MainColor] _BaseColor ("Base Tint", Color) = (1, 1, 1, 1)
        _LightDir ("Light Direction", Vector) = (0.5, 1, -0.5, 0.4)

        [Header(Outline)]
        _OutlineWidth ("Outline Width", Range(0, 20)) = 2.5
        _OutlineZOffset ("Outline Z Offset", Range(-1, 1)) = 0.1
        [HDR] _OutlineColor ("Outline Color", Color) = (1, 1, 1, 1)
        _IceRate ("Ice Rate", Range(0, 1)) = 0

        [Header(Alpha Settings)]
        [Toggle(_ALPHATEST_ON)] _AlphaTest ("Enable Alpha Clipping", Float) = 0
        _Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.5

        [Header(Blend Mode)]
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 0
        [Enum(Off, 0, On, 1)] _ZWrite ("ZWrite", Float) = 1
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
    }

    SubShader
    {
        Tags { "Queue" = "Geometry" "RenderType" = "Opaque" "IgnoreProjector" = "True" }

        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode" = "ForwardBase" }
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            Cull [_Cull]

            CGPROGRAM
            #pragma target 2.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _BaseColor;
            half4 _LightDir;
            half _Cutoff;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 position : SV_POSITION;
                float2 uv : TEXCOORD0;
                half3 worldNormal : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            v2f vert(appdata input)
            {
                v2f output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                output.position = UnityObjectToClipPos(input.vertex);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.worldNormal = UnityObjectToWorldNormal(input.normal);
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                fixed4 texColor = tex2D(_MainTex, input.uv);
                fixed4 color = texColor * _BaseColor;

                #if defined(_ALPHATEST_ON)
                    clip(color.a - _Cutoff);
                #endif

                half nDotL = saturate(dot(input.worldNormal, normalize(_LightDir.xyz)));
                color.rgb *= lerp(1.0h, nDotL, _LightDir.w);
                return color;
            }
            ENDCG
        }

        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "Always" }
            Cull [_Cull]
            ZWrite On
            ZTest LEqual

            CGPROGRAM
            #pragma target 2.0
            #pragma vertex outlineVert
            #pragma fragment outlineFrag
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _BaseColor;
            fixed4 _OutlineColor;
            half _OutlineWidth;
            half _OutlineZOffset;
            half _IceRate;
            half _Cutoff;

            struct outlineAppdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct outlineV2f
            {
                float4 position : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float4 ApplyViewZOffset(float4 positionCS, float viewSpaceZOffset)
            {
                if (unity_OrthoParams.w == 0)
                {
                    float2 projectionZRow = UNITY_MATRIX_P[2].zw;
                    float modifiedPositionVSZ = -positionCS.w - viewSpaceZOffset;
                    float modifiedPositionCSZ = modifiedPositionVSZ * projectionZRow.x + projectionZRow.y;
                    positionCS.z = modifiedPositionCSZ * positionCS.w / (-modifiedPositionVSZ);
                }
                else
                {
                    #if UNITY_REVERSED_Z
                        positionCS.z -= viewSpaceZOffset / _ProjectionParams.z;
                    #else
                        positionCS.z += viewSpaceZOffset / _ProjectionParams.z;
                    #endif
                }

                return positionCS;
            }

            outlineV2f outlineVert(outlineAppdata input)
            {
                outlineV2f output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float4 positionCS = UnityObjectToClipPos(input.vertex);
                float3 normalWS = UnityObjectToWorldNormal(input.normal);
                float3 normalCS = mul((float3x3)UNITY_MATRIX_VP, normalWS);
                float2 offset = normalize(normalCS.xy) * (_OutlineWidth * 0.001) * positionCS.w;
                offset.x *= _ScreenParams.y / _ScreenParams.x;
                positionCS.xy += offset;

                output.position = ApplyViewZOffset(positionCS, _OutlineZOffset);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }

            fixed4 outlineFrag(outlineV2f input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                #if defined(_ALPHATEST_ON)
                    fixed alpha = tex2D(_MainTex, input.uv).a * _BaseColor.a;
                    clip(alpha - _Cutoff);
                #endif

                fixed4 color = _OutlineColor;
                color.rgb = lerp(color.rgb, 1.0h, _IceRate);
                return color;
            }
            ENDCG
        }
    }

    Fallback Off
}
