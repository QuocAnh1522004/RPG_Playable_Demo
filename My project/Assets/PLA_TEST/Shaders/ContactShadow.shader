Shader "Dicegeon/Contact Shadow"
{
    Properties
    {
        _ShadowColor ("Shadow Color", Color) = (0.04, 0.035, 0.03, 0.5)
        _FalloffStart ("Soft Edge Start", Range(0, 0.95)) = 0.2
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }

        Pass
        {
            Tags { "LightMode" = "ForwardBase" }
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZWrite Off
            ZTest LEqual

            CGPROGRAM
            #pragma target 2.0
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _ShadowColor;
            half _FalloffStart;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 position : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata input)
            {
                v2f output;
                output.position = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                float2 radialUV = (input.uv - 0.5) * 2.0;
                half radius = length(radialUV);
                half alpha = 1.0h - smoothstep(_FalloffStart, 1.0h, radius);
                return fixed4(_ShadowColor.rgb, _ShadowColor.a * alpha);
            }
            ENDCG
        }
    }

    Fallback Off
}
