Shader "Dicegeon/Water"
{
    Properties
    {
        _DeepColor ("Deep Color", Color) = (0.46662515, 0.8489979, 0.9245283, 1)
        _SkyTexture ("Moving Clouds", 2D) = "white" {}
        _CloudTint ("Cloud Tint", Color) = (1, 1, 1, 1)
        _ReflectionStrength ("Cloud Reflection", Range(0, 1)) = 0.64
        _ParallaxStrength ("Camera Parallax", Float) = 0.5
        _SkyWorldScale ("Sky World Scale", Float) = 0.03
        _CloudSpeed ("Cloud Drift (UV/sec)", Vector) = (0.002, -0.004, 0, 0)
        _BackgroundBlurStrength ("Background Blur (Texels)", Range(0, 32)) = 0
    }

    SubShader
    {
        Tags { "Queue" = "Geometry" "RenderType" = "Opaque" }

        Pass
        {
            Name "Water"
            Cull Back
            ZWrite On
            ZTest LEqual

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _SkyTexture;
            float4 _DeepColor;
            float4 _CloudTint;
            float _ReflectionStrength;
            float _ParallaxStrength;
            float _SkyWorldScale;
            float4 _CloudSpeed;
            float _BackgroundBlurStrength;
            float4 _SkyTexture_TexelSize;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 position : SV_POSITION;
                float3 worldPosition : TEXCOORD0;
            };

            v2f vert(appdata input)
            {
                v2f output;
                output.position = UnityObjectToClipPos(input.vertex);
                output.worldPosition = mul(unity_ObjectToWorld, input.vertex).xyz;
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                float3 cameraForwardWS = -float3(
                    UNITY_MATRIX_I_V._m02,
                    UNITY_MATRIX_I_V._m12,
                    UNITY_MATRIX_I_V._m22);
                float2 viewForward = length(cameraForwardWS.xz) > 0.001
                    ? normalize(cameraForwardWS.xz)
                    : float2(0.0, 1.0);
                float2 viewRight = float2(viewForward.y, -viewForward.x);

                float2 alignedUV = float2(
                    dot(input.worldPosition.xz, viewRight),
                    dot(input.worldPosition.xz, viewForward));
                float2 cameraOffset = float2(
                    dot(_WorldSpaceCameraPos.xz, viewRight),
                    dot(_WorldSpaceCameraPos.xz, viewForward)) * _ParallaxStrength;
                float2 cloudUV = (alignedUV - cameraOffset) * _SkyWorldScale + _CloudSpeed.xy * _Time.y;

                fixed4 cloudColor = tex2D(_SkyTexture, cloudUV) * _CloudTint;
                if (_BackgroundBlurStrength > 0.0)
                {
                    float2 blurOffset = _SkyTexture_TexelSize.xy * _BackgroundBlurStrength;
                    float4 blurredCloud = cloudColor;
                    blurredCloud += tex2D(_SkyTexture, cloudUV + float2(blurOffset.x, 0.0)) * _CloudTint;
                    blurredCloud += tex2D(_SkyTexture, cloudUV - float2(blurOffset.x, 0.0)) * _CloudTint;
                    blurredCloud += tex2D(_SkyTexture, cloudUV + float2(0.0, blurOffset.y)) * _CloudTint;
                    blurredCloud += tex2D(_SkyTexture, cloudUV - float2(0.0, blurOffset.y)) * _CloudTint;
                    cloudColor = blurredCloud * 0.2;
                }

                fixed3 skyColor = cloudColor.rgb * cloudColor.a;
                float3 viewDirectionWS = normalize(_WorldSpaceCameraPos - input.worldPosition);
                float NdotV = saturate(dot(float3(0.0, 1.0, 0.0), viewDirectionWS));
                float fresnel = saturate(pow(1.0 - NdotV, 2.0) * 0.5 + 0.6);
                float reflectionBlend = fresnel * _ReflectionStrength;
                fixed3 waterColor = lerp(_DeepColor.rgb, skyColor, reflectionBlend);
                return fixed4(waterColor, 1.0);
            }
            ENDCG
        }
    }
}
