using UnityEngine;

[DisallowMultipleComponent]
public class VSplatterActionGate : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private VSplatterDashController _dashController;
    [SerializeField] private VSplatterShockwaveController _shockwaveController;

    public bool IsDashActive => _dashController != null && _dashController.IsDashing;
    public bool IsShockwaveActive => _shockwaveController != null && _shockwaveController.IsCharging;

    public bool CanUseAttack => !IsDashActive && !IsShockwaveActive;
    public bool CanUsePaint => !IsDashActive && !IsShockwaveActive;
    public bool CanUseShockwave => !IsDashActive;
    public bool CanUseDash => true;

    private void Reset()
    {
        if (_dashController == null)
            _dashController = GetComponent<VSplatterDashController>();

        if (_shockwaveController == null)
            _shockwaveController = GetComponent<VSplatterShockwaveController>();
    }

    private void Awake()
    {
        if (_dashController == null)
            _dashController = GetComponent<VSplatterDashController>();

        if (_shockwaveController == null)
            _shockwaveController = GetComponent<VSplatterShockwaveController>();
    }
}
