using System;
using UnityEngine;

[DisallowMultipleComponent]
public class EntityEquipmentRuntime : MonoBehaviour
{
    [Header("Armor")]
    [SerializeField] private ArmorItemSO _startingArmor;
    [SerializeField] private Transform _armorVisualRoot;

    private ArmorItemSO _currentArmor;
    private GameObject _armorVisualInstance;

    public event Action OnEquipmentChanged;

    public ArmorItemSO CurrentArmor => _currentArmor;

    public float ArmorDamageTakenAdditive =>
        _currentArmor != null ? _currentArmor.DamageTakenMultiplierAdditive : 0f;

    private void Awake()
    {
        if (_startingArmor != null)
            EquipArmor(_startingArmor);
    }

    public void EquipArmor(ArmorItemSO armor)
    {
        if (_currentArmor == armor && _armorVisualInstance != null)
            return;

        _currentArmor = armor;
        RebuildArmorVisual();
        OnEquipmentChanged?.Invoke();
    }

    public void ClearArmor()
    {
        if (_currentArmor == null && _armorVisualInstance == null)
            return;

        _currentArmor = null;
        RebuildArmorVisual();
        OnEquipmentChanged?.Invoke();
    }

    public float GetDamageTakenMultiplierAdditive()
    {
        return ArmorDamageTakenAdditive;
    }

    private void RebuildArmorVisual()
    {
        if (_armorVisualInstance != null)
        {
            Destroy(_armorVisualInstance);
            _armorVisualInstance = null;
        }

        if (_currentArmor == null ||
            _currentArmor.EquippedVisualPrefab == null ||
            _armorVisualRoot == null)
        {
            return;
        }

        _armorVisualInstance = Instantiate(_currentArmor.EquippedVisualPrefab, _armorVisualRoot);
        _currentArmor.ApplyEquippedVisualTransform(_armorVisualInstance.transform);
    }
}