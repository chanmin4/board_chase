Shader "Hidden/VSplatter/PaintMaskStamp"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            BlendOp Max
            Blend One One

            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _Stamp; // x/y center uv, z/w radius uv
            float _StampSoftness;

            fixed4 frag(v2f_img i) : SV_Target
            {
                float2 radius = max(_Stamp.zw, float2(0.00001, 0.00001));
                float2 delta = (i.uv - _Stamp.xy) / radius;
                float distance01 = length(delta);
                float softness = saturate(_StampSoftness);
                float coverage = 1.0 - smoothstep(max(0.0, 1.0 - softness), 1.0, distance01);

                return fixed4(coverage, coverage, coverage, coverage);
            }
            ENDCG
        }

        Pass
        {
            Blend Zero OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _Stamp; // x/y center uv, z/w radius uv
            float _StampSoftness;

            fixed4 frag(v2f_img i) : SV_Target
            {
                float2 radius = max(_Stamp.zw, float2(0.00001, 0.00001));
                float2 delta = (i.uv - _Stamp.xy) / radius;
                float distance01 = length(delta);
                float softness = saturate(_StampSoftness);
                float coverage = 1.0 - smoothstep(max(0.0, 1.0 - softness), 1.0, distance01);

                return fixed4(coverage, coverage, coverage, coverage);
            }
            ENDCG
        }
    }
    Fallback Off
}
