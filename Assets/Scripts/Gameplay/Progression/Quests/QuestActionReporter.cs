using UnityEngine;

/// <summary>Author this on a download/plant/investigate device; call Report after its action succeeds.</summary>
public sealed class QuestActionReporter : MonoBehaviour
{
    [SerializeField] private string _actionId;
    [SerializeField, Min(1)] private int _amount = 1;
    [SerializeField] private bool _oncePerInstance = true;
    private bool _reported;
    public void Report()
    {
        if (_oncePerInstance && _reported) return;
        if (string.IsNullOrWhiteSpace(_actionId)) return;
        QuestRuntime.ReportAction(_actionId, _amount);
        _reported = true;
    }
}
