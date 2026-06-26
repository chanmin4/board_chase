using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("Scripts/V Splatter Dash Visual Lean")]
public class VSplatterDashVisualLean : MonoBehaviour
{
    [SerializeField] private VSplatterDashController _dashController;
    [SerializeField] private Transform _leanTarget;

    private Quaternion _baseLocalRotation;

    private void Reset()
    {
        if (_dashController == null)
            _dashController = GetComponentInParent<VSplatterDashController>();

        if (_leanTarget == null)
            _leanTarget = transform;
    }

    private void Awake()
    {
        if (_dashController == null)
            _dashController = GetComponentInParent<VSplatterDashController>();

        if (_leanTarget == null)
            _leanTarget = transform;

        _baseLocalRotation = _leanTarget.localRotation;
    }

    private void OnDisable()
    {
        if (_leanTarget != null)
            _leanTarget.localRotation = _baseLocalRotation;
    }

    private void LateUpdate()
    {
        if (_dashController == null || _leanTarget == null)
            return;

        VSplatterDashConfigSO config = _dashController.Config;

        bool useLean = config == null || config.UseDashVisualLean;
        bool isDashing = useLean && _dashController.IsDashing;

        Vector3 leanEuler = config != null
            ? config.DashVisualLeanEuler
            : new Vector3(18f, 0f, 0f);

        float speed = isDashing
            ? config != null ? config.DashVisualLeanInSpeed : 18f
            : config != null ? config.DashVisualLeanOutSpeed : 12f;

        Quaternion targetRotation = isDashing
            ? _baseLocalRotation * Quaternion.Euler(leanEuler)
            : _baseLocalRotation;

        if (speed <= 0.001f)
        {
            _leanTarget.localRotation = targetRotation;
            return;
        }

        float t = 1f - Mathf.Exp(-speed * Time.deltaTime);
        _leanTarget.localRotation = Quaternion.Slerp(
            _leanTarget.localRotation,
            targetRotation,
            t);
    }
}
