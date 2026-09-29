Shader "VSplatter/FX/CameraEntityOcclusionSilhouette"
{
    Properties
    {
        [MainTexture] _BaseMap ("Source Alpha Map", 2D) = "white" {}
        _SilhouetteColor ("Silhouette Color", Color) = (0.25, 0.85, 1.0, 0.78)
        _AlphaCutoff ("Source Alpha Cutoff", Range(0, 1)) = 0.01
        _DepthBias ("Raw Depth Bias", Range(0.00001, 0.01)) = 0.0002
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Transparent"
            "Queue"="Transparent+100"
        }

        Pass
        {
            Name "HiddenCharacterSilhouette"
            Tags { "LightMode"="UniversalForward" }

            // The source Character still renders normally. This pass performs its own
            // depth comparison so only the actually hidden pixels become silhouettes.
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest Always
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _SilhouetteColor;
                float _AlphaCutoff;
                float _DepthBias;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // Scene depth naturally follows the real mesh silhouette and camera angle;
                // tall gameplay colliders no longer influence this visual decision.
                float2 screenUV = GetNormalizedScreenSpaceUV(input.positionHCS);
                float sceneRawDepth = SampleSceneDepth(screenUV);
                float fragmentRawDepth = input.positionHCS.z;

                // Unity reverses the depth buffer on modern desktop APIs. Compare raw
                // depth explicitly so perspective and orthographic cameras behave alike.
                #if UNITY_REVERSED_Z
                    clip(sceneRawDepth - fragmentRawDepth - _DepthBias);
                #else
                    clip(fragmentRawDepth - sceneRawDepth - _DepthBias);
                #endif

                half sourceAlpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).a;
                clip(sourceAlpha - _AlphaCutoff);

                return half4(
                    _SilhouetteColor.rgb,
                    saturate(_SilhouetteColor.a * sourceAlpha));
            }
            ENDHLSL
        }
    }

    FallBack Off
}
