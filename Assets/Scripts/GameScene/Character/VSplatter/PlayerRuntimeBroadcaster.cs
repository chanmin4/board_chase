using UnityEngine;

public class PlayerRuntimeBroadcaster : MonoBehaviour
{
    [Header("Broadcasting")]
    [SerializeField] private PlayerRuntimeReadyEventChannelSO _playerRuntimeReadyChannel;

    private void OnEnable()
    {
        if (_playerRuntimeReadyChannel != null)
            _playerRuntimeReadyChannel.RaiseEvent(transform);
    }

    private void OnDisable()
    {
        if (_playerRuntimeReadyChannel != null)
            _playerRuntimeReadyChannel.Clear(transform);
    }
}
