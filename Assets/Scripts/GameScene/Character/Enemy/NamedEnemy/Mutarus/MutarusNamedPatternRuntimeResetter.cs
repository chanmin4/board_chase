using UnityEngine;

[DisallowMultipleComponent]
public class MutarusNamedPatternRuntimeResetter : MonoBehaviour, INamedPatternRuntimeResetter
{
    [SerializeField] private MutarusQTEPatternResetRequestEventChannelSO _qtePatternResetRequestChannel;

    public void ForceResetNamedPatternRuntime()
    {
        if (_qtePatternResetRequestChannel != null)
            _qtePatternResetRequestChannel.RaiseEvent();
    }
}
