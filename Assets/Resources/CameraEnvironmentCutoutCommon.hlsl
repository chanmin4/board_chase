#ifndef BOARD_CHASE_CAMERA_ENVIRONMENT_CUTOUT_INCLUDED
#define BOARD_CHASE_CAMERA_ENVIRONMENT_CUTOUT_INCLUDED

// Populated only for the owning camera. No gameplay visibility data is written here.
float4 _BCEnvironmentAperture; // viewport center xy, radius, soft edge (vertical viewport units)
float4 _BCEnvironmentCameraParams; // aspect, focus eye depth, enabled, reserved
float _BCEnvironmentFade;
float _BCEnvironmentUnlit;

void BCClipCameraEnvironment(float4 positionCS)
{
    if (_BCEnvironmentCameraParams.z < 0.5 || _BCEnvironmentFade <= 0.0)
        return;

    float eyeDepth = LinearEyeDepth(positionCS.z, _ZBufferParams);
    if (unity_OrthoParams.w > 0.5)
    {
        #if UNITY_REVERSED_Z
            eyeDepth = lerp(_ProjectionParams.z, _ProjectionParams.y, positionCS.z);
        #else
            eyeDepth = lerp(_ProjectionParams.y, _ProjectionParams.z, positionCS.z);
        #endif
    }
    // A combined wall mesh can contain foreground and background geometry.
    // Keep everything behind the focus plane opaque, even in the aperture.
    if (eyeDepth >= _BCEnvironmentCameraParams.y)
        return;

    float2 delta = GetNormalizedScreenSpaceUV(positionCS) - _BCEnvironmentAperture.xy;
    delta.x *= _BCEnvironmentCameraParams.x;
    float coverage = 1.0 - saturate(_BCEnvironmentFade) *
        (1.0 - smoothstep(_BCEnvironmentAperture.z - _BCEnvironmentAperture.w,
                         _BCEnvironmentAperture.z + _BCEnvironmentAperture.w, length(delta)));
    // Stable ordered dither, identical in color, depth and normal passes.
    // Center becomes fully open rather than tinting or drawing a silhouette.
    static const float thresholds[16] =
    {
        0.03125, 0.53125, 0.15625, 0.65625,
        0.78125, 0.28125, 0.90625, 0.40625,
        0.21875, 0.71875, 0.09375, 0.59375,
        0.96875, 0.46875, 0.84375, 0.34375
    };
    uint2 pixel = (uint2)positionCS.xy;
    uint index = (pixel.x & 3u) + ((pixel.y & 3u) << 2u);
    clip(coverage - thresholds[index]);
}
#endif
