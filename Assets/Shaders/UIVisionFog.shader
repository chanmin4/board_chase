Shader "UI/VisionFog"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _FogColor ("Fog Color", Color) = (0, 0, 0, 0.55)
        _VisionOriginWorld ("Vision Origin World", Vector) = (0, 0, 0, 0)
        _ForwardWorld ("Forward World", Vector) = (0, 0, 1, 0)
        _CircleRadiusWorld ("Circle Radius World", Float) = 0
        _ConeRangeWorld ("Cone Range World", Float) = 0
        _ArcCos ("Arc Cos", Float) = 0
        _UseForwardArc ("Use Forward Arc", Float) = 1
        _SoftnessWorld ("Softness World", Float) = 1
        _OcclusionSampleCount ("Occlusion Sample Count", Float) = 256
        _UseOcclusion ("Use Occlusion", Float) = 0
        _OcclusionSoftnessWorld ("Occlusion Softness World", Float) = 0.1
        _OcclusionSafetyBiasWorld ("Occlusion Safety Bias World", Float) = 0.08
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
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            #define MAX_OCCLUSION_SAMPLES 256
            #define TWO_PI 6.28318530718

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
            float3 _VisionOriginWorld;
            float3 _ForwardWorld;
            float _CircleRadiusWorld;
            float _ConeRangeWorld;
            float3 _WorldCameraPosition;
            float3 _WorldCameraRight;
            float3 _WorldCameraUp;
            float3 _WorldCameraForward;
            float4 _CameraProjection;
            float _ArcCos;
            float _UseForwardArc;
            float _SoftnessWorld;
            float _OcclusionDistances[MAX_OCCLUSION_SAMPLES];
            float _OcclusionSampleCount;
            float _UseOcclusion;
            float _OcclusionSoftnessWorld;
            float _OcclusionSafetyBiasWorld;

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
                float2 screenUV = i.screenPosition.xy / max(i.screenPosition.w, 0.0001);
                float2 viewport = screenUV * 2.0 - 1.0;
                float tanHalfFov = max(_CameraProjection.x, 0.0001);
                float aspect = max(_CameraProjection.y, 0.0001);
                float orthoSize = max(_CameraProjection.z, 0.0001);
                float isOrthographic = step(0.5, _CameraProjection.w);

                float3 perspectiveDirection = normalize(
                    _WorldCameraForward +
                    _WorldCameraRight * (viewport.x * tanHalfFov * aspect) +
                    _WorldCameraUp * (viewport.y * tanHalfFov));

                float3 orthographicOrigin =
                    _WorldCameraPosition +
                    _WorldCameraRight * (viewport.x * orthoSize * aspect) +
                    _WorldCameraUp * (viewport.y * orthoSize);

                float3 rayOrigin = lerp(_WorldCameraPosition, orthographicOrigin, isOrthographic);
                float3 rayDirection = normalize(lerp(perspectiveDirection, _WorldCameraForward, isOrthographic));
                float denominator = rayDirection.y;
                float validRay = step(0.0001, abs(denominator));
                float rayDistance = (_VisionOriginWorld.y - rayOrigin.y) / (denominator + (denominator >= 0.0 ? 0.00001 : -0.00001));
                float inFront = step(0.0, rayDistance);
                float3 worldPoint = rayOrigin + rayDirection * max(0.0, rayDistance);
                float2 toPoint = worldPoint.xz - _VisionOriginWorld.xz;
                float worldDistance = length(toPoint);
                float softness = max(_SoftnessWorld, 0.0001);

                float circleVisible = 1.0 - smoothstep(
                    max(0.0, _CircleRadiusWorld - softness * 0.5),
                    _CircleRadiusWorld + softness * 0.5,
                    worldDistance);
                float coneRangeVisible = 1.0 - smoothstep(
                    max(0.0, _ConeRangeWorld - softness * 0.5),
                    _ConeRangeWorld + softness * 0.5,
                    worldDistance);

                float2 dir = normalize(toPoint + float2(0.0001, 0.0001));
                float2 forward = normalize(_ForwardWorld.xz + float2(0.0001, 0.0001));
                float inArc = step(_ArcCos, dot(dir, forward));
                float coneShape = lerp(1.0, inArc, saturate(_UseForwardArc));
                float coneVisible = coneRangeVisible * coneShape;
                float visible = saturate(max(circleVisible, coneVisible)) * validRay * inFront;

                // The CPU ray fan uses the same eye, mask, and trigger rule as entity LOS checks.
                float normalizedAngle = frac(atan2(toPoint.y, toPoint.x) / TWO_PI + 1.0);
                int sampleCount = clamp((int)_OcclusionSampleCount, 1, MAX_OCCLUSION_SAMPLES);
                float samplePosition = normalizedAngle * sampleCount;
                int sampleA = min((int)floor(samplePosition), sampleCount - 1);
                int sampleB = sampleA + 1;

                if (sampleB >= sampleCount)
                    sampleB = 0;

                // Smooth angular interpolation removes whole-sector steps. A small inward
                // world-space bias keeps interpolated thin corners from leaking light.
                float angularBlend = smoothstep(0.0, 1.0, frac(samplePosition));
                float distanceA = _OcclusionDistances[sampleA];
                float distanceB = _OcclusionDistances[sampleB];
                float maximumVisionDistance = max(_CircleRadiusWorld, _ConeRangeWorld);
                float hasAdjacentBlocker = step(
                    0.0001,
                    maximumVisionDistance - min(distanceA, distanceB));
                float openDistance = max(
                    0.0,
                    lerp(
                        distanceA,
                        distanceB,
                        angularBlend) -
                    max(0.0, _OcclusionSafetyBiasWorld) * hasAdjacentBlocker);
                float occlusionSoftness = max(_OcclusionSoftnessWorld, 0.0001);
                float lineOfSightVisible = 1.0 - smoothstep(
                    max(0.0, openDistance - occlusionSoftness * 0.5),
                    openDistance + occlusionSoftness * 0.5,
                    worldDistance);

                visible *= lerp(1.0, lineOfSightVisible, saturate(_UseOcclusion));
                float alpha = _FogColor.a * (1.0 - visible);

                return fixed4(_FogColor.rgb, alpha) * i.color;
            }
            ENDCG
        }
    }
}
