using System;
using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public class EntityWeaponHolder : MonoBehaviour
{
    [Header("Loadout")]
    [SerializeField] private WeaponSO _startingWeapon;
    [SerializeField] private WeaponSO _currentWeapon;

    [Header("Socket")]
    [Tooltip("WeaponView prefab from WeaponSO is instantiated under this root. Former right gun bone.")]
    [FormerlySerializedAs("_rightGunBone")]
    [SerializeField] private Transform _weaponRoot;

    [Header("Runtime Scene Refs")]
    [SerializeField] private Transform _projectilesRoot;

    [Header("Visual Aim")]
    [SerializeField] private Transform _visualAimRoot;
    [SerializeField] private bool _enableVisualAim = true;
    [SerializeField] private float _visualAimTurnSmoothTime = 0.02f;

    [Header("Debug")]
    [SerializeField] private bool _logMissingRefs = true;

    private WeaponView _currentWeaponView;
    private float _visualAimTurnVelocity;
    private bool _visible = true;

    public event Action<WeaponSO> OnWeaponChanged;

    public WeaponSO CurrentWeapon => _currentWeapon;
    public WeaponView CurrentWeaponView => _currentWeaponView;
    public Transform GameplayFireOrigin => _currentWeaponView != null ? _currentWeaponView.FireOrigin : null;
    public Transform VisualFireOrigin => GameplayFireOrigin;
    public Vector3 FireDirection => _currentWeaponView != null ? _currentWeaponView.FireDirection.normalized : Vector3.forward;
    public Transform ProjectilesRoot => _projectilesRoot;

    protected virtual void Awake()
    {
        if (_currentWeapon == null)
            _currentWeapon = _startingWeapon;

        Equip(_currentWeapon, force: true);
    }

    public void SetProjectilesRoot(Transform root)
    {
        _projectilesRoot = root;
    }

    public void SetVisible(bool visible)
    {
        _visible = visible;

        if (_currentWeaponView != null)
            _currentWeaponView.gameObject.SetActive(_visible);
    }

    public void UpdateVisualAim(Vector3 worldTarget)
    {
        if (!_enableVisualAim)
            return;

        Transform aimRoot = _visualAimRoot != null ? _visualAimRoot : transform;
        Transform fireOrigin = VisualFireOrigin;

        if (fireOrigin == null)
            return;

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

    public void ClearWeapon()
    {
        if (_currentWeapon == null && _currentWeaponView == null)
            return;

        _currentWeapon = null;
        ClearCurrentWeaponView();
        OnWeaponChanged?.Invoke(null);
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

        if (weapon == null || weapon.WeaponViewPrefab == null)
        {
            if (_logMissingRefs && weapon != null)
                Debug.LogError($"[EntityWeaponHolder] Weapon view prefab is missing on {weapon.name}.", weapon);

            return;
        }

        if (_weaponRoot == null)
        {
            if (_logMissingRefs)
                Debug.LogError("[EntityWeaponHolder] Weapon Root is not assigned.", this);

            return;
        }

        _currentWeaponView = Instantiate(weapon.WeaponViewPrefab, _weaponRoot);
        weapon.ApplyWeaponViewTransform(_currentWeaponView.transform);
        _currentWeaponView.gameObject.SetActive(_visible);

        if (_logMissingRefs && !_currentWeaponView.HasFireOrigin)
        {
            Debug.LogError(
                $"[EntityWeaponHolder] WeaponView Fire Origin is missing. weapon={weapon.name}, view={_currentWeaponView.name}",
                _currentWeaponView);
        }
    }

    public void PlayMuzzleParticle()
    {
        if (_currentWeapon == null ||
            _currentWeapon.MuzzleParticlePrefab == null ||
            _currentWeaponView == null ||
            !_currentWeaponView.HasFireOrigin)
        {
            return;
        }

        ParticleSystem instance = Instantiate(
            _currentWeapon.MuzzleParticlePrefab,
            _currentWeaponView.FireOrigin.position,
            _currentWeaponView.FireOrigin.rotation,
            _currentWeaponView.FireOrigin);

        instance.Play();
        Destroy(instance.gameObject, ResolveParticleLifetime(instance));
    }

    private static float ResolveParticleLifetime(ParticleSystem particle)
    {
        if (particle == null)
            return 1f;

        ParticleSystem.MainModule main = particle.main;
        float lifetime = main.startLifetime.constantMax;

        return Mathf.Max(0.1f, main.duration + lifetime);
    }

    private void ClearCurrentWeaponView()
    {
        if (_currentWeaponView != null)
            Destroy(_currentWeaponView.gameObject);

        _currentWeaponView = null;
    }
}
