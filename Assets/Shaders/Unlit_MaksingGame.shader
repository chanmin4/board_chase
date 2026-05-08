Shader "Unlit/MaskingGame"
{
    Properties
    {
        //[Header(Paint Color)]
        _TintColor ("Tint Color", Color) = (0.6,0.2,0.8,0.5)
        //[Header(Runtime Mask Do Not Set Manually)]
        _MaskTex ("Mask (R/A)", 2D) = "black" {}
        //[Header(Visible Texture Pattern)]
        _PatternTex ("Pattern Texture", 2D) = "white" {}
        _PatternTiling ("Pattern Tiling", Float) = 8
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
            TEXTURE2D(_PatternTex); SAMPLER(sampler_PatternTex);
            float4 _TintColor;
            float _PatternTiling;
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
                half m = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, i.uv).a;
                half4 pattern = SAMPLE_TEXTURE2D(_PatternTex, sampler_PatternTex, i.uv * _PatternTiling);

                return half4(pattern.rgb * _TintColor.rgb, pattern.a * _TintColor.a * m);
            }
            ENDHLSL
        }
    }
}
