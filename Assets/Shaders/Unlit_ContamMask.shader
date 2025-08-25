Shader "Unlit/ContamMask"
{
    Properties
    {
        _TintColor ("Tint Color", Color) = (0.6,0.2,0.8,0.5)
        _MaskTex ("Mask (R/A)", 2D) = "black" {}
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MaskTex); SAMPLER(sampler_MaskTex);
            float4 _TintColor;

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings  { float4 positionHCS : SV_POSITION; float2 uv:TEXCOORD0; };

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                half m = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, i.uv).a; // 0~1
                return half4(_TintColor.rgb, _TintColor.a * m);
            }
            ENDHLSL
        }
    }
}
