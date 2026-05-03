using System;
using UnityEngine;

[DisallowMultipleComponent]
public class VSplatterWeaponHolder : MonoBehaviour
{
    [Serializable]
    public struct WeaponViewEntry
    {
        public WeaponSO weapon;
        public GameObject rightGunPrefab;
        public GameObject leftGunPrefab;
        public Vector3 rightLocalEuler;
        public Vector3 leftLocalEuler;
    }

    [Header("Loadout")]
    [SerializeField] private WeaponSO _startingWeapon;
    [SerializeField] private WeaponSO _currentWeapon;

    [Header("Sockets")]
    [SerializeField] private Transform _rightGunBone;
    [SerializeField] private Transform _leftGunBone;

    [Header("Gameplay")]
    [SerializeField] private Transform _gameplayFireOrigin;
    [SerializeField] private Transform _fallbackFireOrigin;

    [Header("Views")]
    [SerializeField] private WeaponViewEntry[] _weaponViews;

    [Header("Runtime Scene Refs")]
    [SerializeField] private Transform _projectilesRoot;

    [Header("Visual Aim")]
    [SerializeField] private Transform _visualAimRoot;
    [SerializeField] private bool _enableVisualAim = true;
    [SerializeField] private float _visualAimTurnSmoothTime = 0.02f;

    private GameObject _currentRightGun;
    private GameObject _currentLeftGun;
    private float _visualAimTurnVelocity;

    public event Action<WeaponSO> OnWeaponChanged;

    public WeaponSO CurrentWeapon => _currentWeapon;

    public Transform GameplayFireOrigin =>
        _gameplayFireOrigin != null ? _gameplayFireOrigin :
        _fallbackFireOrigin != null ? _fallbackFireOrigin :
        transform;

    public Transform VisualFireOrigin => FindCurrentFireOrigin();
    public Vector3 FireDirection => FindCurrentFireDirection();
    public Transform ProjectilesRoot => _projectilesRoot;

    private void Awake()
    {
        if (_currentWeapon == null)
            _currentWeapon = _startingWeapon;

        Equip(_currentWeapon, force: true);
    }

    public void SetProjectilesRoot(Transform root)
    {
        _projectilesRoot = root;
    }

    public void UpdateVisualAim(Vector3 worldTarget)
    {
        if (!_enableVisualAim)
            return;

        Transform aimRoot = _visualAimRoot != null ? _visualAimRoot : transform;
        Transform fireOrigin = VisualFireOrigin != null ? VisualFireOrigin : GameplayFireOrigin != null ? GameplayFireOrigin : aimRoot;

        Vector3 flatDirection = worldTarget - fireOrigin.position;
        flatDirection.y = 0f;

        if (flatDirection.sqrMagnitude < 0.0001f)
            return;

        float targetYaw = Mathf.Atan2(flatDirection.x, flatDirection.z) * Mathf.Rad2Deg;
        Vector3 euler = aimRoot.eulerAngles;

        euler.y = Mathf.SmoothDampAngle(
            euler.y,
            targetYaw,
            ref _visualAimTurnVelocity,
            Mathf.Max(0.001f, _visualAimTurnSmoothTime));

        aimRoot.eulerAngles = euler;
    }

    public void Equip(WeaponSO newWeapon)
    {
        Equip(newWeapon, force: false);
    }

    private void Equip(WeaponSO newWeapon, bool force)
    {
        if (newWeapon == null)
            return;

        if (!force && _currentWeapon == newWeapon)
            return;

        _currentWeapon = newWeapon;
        RebuildWeaponView(newWeapon);
        OnWeaponChanged?.Invoke(_currentWeapon);
    }

    private void RebuildWeaponView(WeaponSO weapon)
    {
        ClearCurrentWeaponView();

        if (!TryGetWeaponView(weapon, out WeaponViewEntry entry))
            return;

        _currentRightGun = SpawnWeaponView(entry.rightGunPrefab, _rightGunBone, entry.rightLocalEuler);
        _currentLeftGun = SpawnWeaponView(entry.leftGunPrefab, _leftGunBone, entry.leftLocalEuler);
    }

    private GameObject SpawnWeaponView(GameObject prefab, Transform parent, Vector3 localEuler)
    {
        if (prefab == null || parent == null)
            return null;

        GameObject instance = Instantiate(prefab, parent);
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.Euler(localEuler);
        return instance;
    }

    private VSplatterWeaponView FindWeaponView(GameObject weaponView)
    {
        if (weaponView == null)
            return null;

        return weaponView.GetComponentInChildren<VSplatterWeaponView>();
    }

    private Transform FindCurrentFireOrigin()
    {
        VSplatterWeaponView rightView = FindWeaponView(_currentRightGun);
        if (rightView != null && rightView.FireOrigin != null)
            return rightView.FireOrigin;

        VSplatterWeaponView leftView = FindWeaponView(_currentLeftGun);
        if (leftView != null && leftView.FireOrigin != null)
            return leftView.FireOrigin;

        if (_fallbackFireOrigin != null)
            return _fallbackFireOrigin;

        return transform;
    }

    private Vector3 FindCurrentFireDirection()
    {
        VSplatterWeaponView rightView = FindWeaponView(_currentRightGun);
        if (rightView != null && rightView.FireDirection.sqrMagnitude > 0.0001f)
            return rightView.FireDirection.normalized;

        VSplatterWeaponView leftView = FindWeaponView(_currentLeftGun);
        if (leftView != null && leftView.FireDirection.sqrMagnitude > 0.0001f)
            return leftView.FireDirection.normalized;

        Transform visualFireOrigin = VisualFireOrigin;
        return visualFireOrigin != null ? visualFireOrigin.forward : transform.forward;
    }

    private bool TryGetWeaponView(WeaponSO weapon, out WeaponViewEntry entry)
    {
        for (int i = 0; i < _weaponViews.Length; i++)
        {
            if (_weaponViews[i].weapon == weapon)
            {
                entry = _weaponViews[i];
                return true;
            }
        }

        entry = default;
        return false;
    }

    private void ClearCurrentWeaponView()
    {
        if (_currentRightGun != null)
            Destroy(_currentRightGun);

        if (_currentLeftGun != null)
            Destroy(_currentLeftGun);

        _currentRightGun = null;
        _currentLeftGun = null;
    }
}
