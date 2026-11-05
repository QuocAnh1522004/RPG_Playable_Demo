Shader "Dicegeon/Actor Unlit"
{
    Properties
    {
        [MainTexture] _BaseMap ("Base Map (RGB) Alpha (A)", 2D) = "white" {}
        [MainColor] _BaseColor ("Base Tint", Color) = (1, 1, 1, 1)

        [Header(Fresnel (Rim Light))]
        [HDR] _FresnelColor ("Fresnel Color", Color) = (1, 1, 1, 1)
        _FresnelPower ("Fresnel Power", Range(0.1, 10)) = 3.0
        _FresnelStrength ("Fresnel Strength", Range(0, 5)) = 0.0

        [Header(Alpha Settings)]
        [Toggle(_ALPHATEST_ON)] _AlphaTest ("Enable Alpha Clipping", Float) = 0.0
        _Cutoff ("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

        [Header(Blend Mode)]
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 0
        [Enum(Off, 0, On, 1)] _ZWrite ("ZWrite", Float) = 1.0
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "IgnoreProjector" = "True" }

        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode" = "ForwardBase" }
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            Cull [_Cull]
            ZTest LEqual

            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local _ALPHATEST_ON
            #include "UnityCG.cginc"

            sampler2D _BaseMap;
            float4 _BaseMap_ST;
            fixed4 _BaseColor;
            fixed4 _FresnelColor;
            half _FresnelPower;
            half _FresnelStrength;
            half _Cutoff;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 position : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPosition : TEXCOORD1;
                half3 worldNormal : TEXCOORD2;
            };

            v2f vert(appdata input)
            {
                v2f output;
                output.position = UnityObjectToClipPos(input.vertex);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.worldPosition = mul(unity_ObjectToWorld, input.vertex).xyz;
                output.worldNormal = UnityObjectToWorldNormal(input.normal);
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                fixed4 color = tex2D(_BaseMap, input.uv) * _BaseColor;

                #if defined(_ALPHATEST_ON)
                    clip(color.a - _Cutoff);
                #endif

                half3 normalWS = normalize(input.worldNormal);
                half3 viewDirection = normalize(_WorldSpaceCameraPos - input.worldPosition);
                half normalDotView = saturate(dot(normalWS, viewDirection));
                half fresnel = pow(1.0h - normalDotView, _FresnelPower) * _FresnelStrength;
                color.rgb += _FresnelColor.rgb * fresnel;
                return color;
            }
            ENDCG
        }
    }

    Fallback Off
}
