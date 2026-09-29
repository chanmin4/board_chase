/*
 * Role hierarchy: Presentation > Audio
 * Primary responsibility: Handles CameraAlignedAudioListener audio playback and sound-stimulus delivery.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioListener))]
[DefaultExecutionOrder(1000)]
public sealed class CameraAlignedAudioListener : MonoBehaviour
{
    [Header("World Camera")]
    [Tooltip("Provides the active gameplay camera. The listener stays at the Player position but uses this camera's screen orientation for 3D audio.")]
    [SerializeField] private WorldCameraEventChannelSO _worldCameraReadyChannel;

    [Tooltip("Used only when the World Camera event channel has no current camera.")]
    [SerializeField] private bool _useMainCameraFallback = true;

    private Camera _worldCamera;

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    private void OnEnable()
    {
        if (_worldCameraReadyChannel != null)
        {
            _worldCameraReadyChannel.OnEventRaised += HandleWorldCameraChanged;
            _worldCamera = _worldCameraReadyChannel.Current;
        }

        ResolveCameraFallback();
        AlignWithCameraScreen();
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        if (_worldCameraReadyChannel != null)
            _worldCameraReadyChannel.OnEventRaised -= HandleWorldCameraChanged;
    }

    // Purpose: Processes frame work that must run after the regular Update pass.
    private void LateUpdate()
    {
        ResolveCameraFallback();
        AlignWithCameraScreen();
    }

    // Purpose: Handles the incoming world camera changed request or event.
    private void HandleWorldCameraChanged(Camera worldCamera)
    {
        _worldCamera = worldCamera;
        ResolveCameraFallback();
        AlignWithCameraScreen();
    }

    // Purpose: Resolves the camera fallback needed by this component.
    private void ResolveCameraFallback()
    {
        if (_worldCamera == null && _useMainCameraFallback)
            _worldCamera = Camera.main;
    }

    // Purpose: Performs the align with camera screen workflow for this component.
    private void AlignWithCameraScreen()
    {
        if (_worldCamera == null)
            return;

        // Camera up projected onto the ground is the top of the player's screen.
        // This keeps both left/right and top/bottom audio independent of body aim.
        Vector3 screenUpOnGround = Vector3.ProjectOnPlane(
            _worldCamera.transform.up,
            Vector3.up);

        if (screenUpOnGround.sqrMagnitude <= 0.0001f)
        {
            screenUpOnGround = Vector3.ProjectOnPlane(
                _worldCamera.transform.forward,
                Vector3.up);
        }

        if (screenUpOnGround.sqrMagnitude <= 0.0001f)
            return;

        transform.rotation = Quaternion.LookRotation(
            screenUpOnGround.normalized,
            Vector3.up);
    }
}
