Shader "UI/VisionFog"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _FogColor ("Fog Color", Color) = (0, 0, 0, 0.55)
        _CenterUV ("Center UV", Vector) = (0.5, 0.5, 0, 0)
        _RadiusPixels ("Radius Pixels", Vector) = (250, 250, 0, 0)
        _CloseRadiusPixels ("Close Radius Pixels", Vector) = (120, 120, 0, 0)
        _ForwardDirPixels ("Forward Dir Pixels", Vector) = (0, 1, 0, 0)
        _ArcCos ("Arc Cos", Float) = 0
        _UseForwardArc ("Use Forward Arc", Float) = 1
        _SoftnessPixels ("Softness Pixels", Float) = 80
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 screenPosition : TEXCOORD1;
            };

            sampler2D _MainTex;
            fixed4 _FogColor;
            float2 _CenterUV;
            float2 _RadiusPixels;
            float2 _CloseRadiusPixels;
            float2 _ForwardDirPixels;
            float _ArcCos;
            float _UseForwardArc;
            float _SoftnessPixels;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord;
                o.color = v.color;
                o.screenPosition = ComputeScreenPos(o.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 screenSize = max(_ScreenParams.xy, float2(1.0, 1.0));
                float2 screenUV = i.screenPosition.xy / max(i.screenPosition.w, 0.0001);
                float2 pixel = screenUV * screenSize;
                float2 center = _CenterUV * screenSize;
                float2 radius = max(_RadiusPixels, float2(1.0, 1.0));
                float2 closeRadius = max(_CloseRadiusPixels, float2(1.0, 1.0));
                float2 toPixel = pixel - center;

                float normalizedDistance = length(toPixel / radius);
                float closeDistance = length(toPixel / closeRadius);
                float softness = max(_SoftnessPixels / max(min(radius.x, radius.y), 1.0), 0.0001);
                float closeSoftness = max(_SoftnessPixels / max(min(closeRadius.x, closeRadius.y), 1.0), 0.0001);
                float circleAlpha = smoothstep(1.0 - closeSoftness, 1.0, closeDistance) * _FogColor.a;
                float coneRangeAlpha = smoothstep(1.0 - softness, 1.0, normalizedDistance) * _FogColor.a;

                float2 dir = normalize(toPixel + float2(0.0001, 0.0001));
                float2 forward = normalize(_ForwardDirPixels + float2(0.0001, 0.0001));
                float inArc = step(_ArcCos, dot(dir, forward));
                float arcAlpha = lerp(_FogColor.a, coneRangeAlpha, inArc);
                float coneAlpha = lerp(coneRangeAlpha, arcAlpha, saturate(_UseForwardArc));
                float alpha = min(circleAlpha, coneAlpha);

                return fixed4(_FogColor.rgb, alpha) * i.color;
            }
            ENDCG
        }
    }
}
