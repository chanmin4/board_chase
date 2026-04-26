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
    [Header("Don't Touch Auto Refs")]

    [SerializeField] private Transform _projectilesRoot;
    public Transform ProjectilesRoot => _projectilesRoot;



    private GameObject _currentRightGun;
    private GameObject _currentLeftGun;

    public event Action<WeaponSO> OnWeaponChanged;

    public WeaponSO CurrentWeapon => _currentWeapon;
    public Transform GameplayFireOrigin =>
    _gameplayFireOrigin != null ? _gameplayFireOrigin :
    _fallbackFireOrigin != null ? _fallbackFireOrigin :
    transform;
    public Transform FireOrigin => VisualFireOrigin;   
    public Transform VisualFireOrigin => FindCurrentFireOrigin();
    public Vector3 FireDirection => FindCurrentFireDirection();

    private void Awake()
    {
        if (_currentWeapon == null)
            _currentWeapon = _startingWeapon;

        Equip(_currentWeapon, force: true);
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

    private Transform FindFireOrigin(GameObject weaponView)
    {
        VSplatterWeaponView view = FindWeaponView(weaponView);
        return view != null ? view.FireOrigin : null;
    }

    private Vector3 FindFireDirection(GameObject weaponView)
    {
        VSplatterWeaponView view = FindWeaponView(weaponView);
        return view != null ? view.FireDirection : Vector3.zero;
    }

    private Transform FindCurrentFireOrigin()
    {
        Transform fireOrigin = FindFireOrigin(_currentRightGun);
        if (fireOrigin != null)
            return fireOrigin;

        fireOrigin = FindFireOrigin(_currentLeftGun);
        if (fireOrigin != null)
            return fireOrigin;

        if (_fallbackFireOrigin != null)
            return _fallbackFireOrigin;

        return transform;
    }

    private Vector3 FindCurrentFireDirection()
    {
        Vector3 fireDirection = FindFireDirection(_currentRightGun);
        if (fireDirection.sqrMagnitude > 0.0001f)
            return fireDirection.normalized;

        fireDirection = FindFireDirection(_currentLeftGun);
        if (fireDirection.sqrMagnitude > 0.0001f)
            return fireDirection.normalized;

        return FireOrigin.forward;
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
    public void SetProjectilesRoot(Transform root)
    {
        _projectilesRoot = root;
        Debug.Log($"[WeaponHolder] SetProjectilesRoot -> {_projectilesRoot}", this);
    }
}
