Shader "Hidden/VSplatter/UI/PauseSnapshotBlur"
{
    SubShader
    {
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _Offset;

            fixed4 frag(v2f_img input) : SV_Target
            {
                float2 delta = _MainTex_TexelSize.xy * _Offset;
                fixed4 color = tex2D(_MainTex, input.uv) * 0.2;
                color += tex2D(_MainTex, input.uv + float2(delta.x, delta.y)) * 0.2;
                color += tex2D(_MainTex, input.uv + float2(-delta.x, delta.y)) * 0.2;
                color += tex2D(_MainTex, input.uv + float2(delta.x, -delta.y)) * 0.2;
                color += tex2D(_MainTex, input.uv - delta) * 0.2;
                return color;
            }
            ENDCG
        }
    }

    Fallback Off
}
