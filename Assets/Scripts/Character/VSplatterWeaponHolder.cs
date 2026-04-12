using System;
using UnityEngine;

[DisallowMultipleComponent]
public class VSplatterWeaponHolder : MonoBehaviour
{
    [Header("Loadout")]
    [SerializeField] private WeaponSO _startingWeapon;
    [SerializeField] private WeaponSO _currentWeapon;

    public event Action<WeaponSO> OnWeaponChanged;

    public WeaponSO CurrentWeapon => _currentWeapon;

    private void Awake()
    {
        if (_currentWeapon == null)
            _currentWeapon = _startingWeapon;
    }

    public void Equip(WeaponSO newWeapon)
    {
        if (newWeapon == null)
            return;

        if (_currentWeapon == newWeapon)
            return;
        _currentWeapon = newWeapon;
        OnWeaponChanged?.Invoke(_currentWeapon);
    }
}