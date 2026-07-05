using System;
using UnityEngine;

[DisallowMultipleComponent]
public class EntityEquipmentRuntime : MonoBehaviour
{
    [Header("Armor")]
    [SerializeField] private ArmorItemSO _startingArmor;
    [SerializeField] private Transform _armorVisualRoot;

    [Header("Base Armor Multipliers")]
    [Tooltip("Used when entity base armor class exists without an equipped armor item.")]
    [SerializeField, Min(0f)] private float _baseArmorHealthDamageMultiplier = 1f;

    [Tooltip("Health damage multiplier per base armor class gap when no armor item provides its own rule.")]
    [SerializeField, Range(0f, 1f)] private float _baseArmorHealthDamageMultiplierPerClassGap = 0.5f;

    [Tooltip("Paint mark/infection multiplier when base armor class exists without an equipped armor item.")]
    [SerializeField, Min(0f)] private float _baseArmorMarkDamageMultiplier = 1f;

    [Tooltip("Paint mark/infection multiplier per base armor class gap when no armor item provides its own rule.")]
    [SerializeField, Range(0f, 1f)] private float _baseArmorMarkDamageMultiplierPerClassGap = 0.75f;

    private ArmorItemSO _currentArmor;
    private GameObject _armorVisualInstance;
    [ReadOnly] [SerializeField] private float _currentArmorDurability;

    public event Action OnEquipmentChanged;

    public ArmorItemSO CurrentArmor => _currentArmor;

    public int ArmorClass => HasUsableArmor ? _currentArmor.ArmorClass : 0;
    public float CurrentArmorDurability => _currentArmorDurability;
    public float MaxArmorDurability => _currentArmor != null ? _currentArmor.MaxDurability : 0f;
    public bool HasUsableArmor =>
        _currentArmor != null &&
        (_currentArmor.MaxDurability <= 0f || _currentArmorDurability > 0f);

    private void Awake()
    {
        if (_startingArmor != null)
            EquipArmor(_startingArmor);
    }

    public void EquipArmor(ArmorItemSO armor)
    {
        EquipArmor(armor, armor != null ? armor.MaxDurability : 0f);
    }

    public void EquipArmor(ArmorItemSO armor, float durability)
    {
        bool sameArmor = _currentArmor == armor && _armorVisualInstance != null;
        _currentArmor = armor;
        _currentArmorDurability = armor != null
            ? Mathf.Clamp(durability, 0f, armor.MaxDurability)
            : 0f;

        if (!sameArmor)
            RebuildArmorVisual();

        OnEquipmentChanged?.Invoke();
    }

    public void ClearArmor()
    {
        if (_currentArmor == null && _armorVisualInstance == null)
            return;

        _currentArmor = null;
        _currentArmorDurability = 0f;
        RebuildArmorVisual();
        OnEquipmentChanged?.Invoke();
    }

    public int ResolveEffectiveArmorClass(int baseArmorClass, int armorClassDelta)
    {
        return Mathf.Max(0, Mathf.Max(0, baseArmorClass) + ArmorClass + armorClassDelta);
    }

    public float ResolveHealthDamageMultiplier(int penetrationClass, int baseArmorClass, int armorClassDelta)
    {
        return HasUsableArmor
            ? _currentArmor.ResolveHealthDamageMultiplier(penetrationClass, baseArmorClass, armorClassDelta)
            : ResolveBaseArmorMultiplier(
                penetrationClass,
                baseArmorClass,
                armorClassDelta,
                _baseArmorHealthDamageMultiplier,
                _baseArmorHealthDamageMultiplierPerClassGap);
    }

    public float ResolveMarkDamageMultiplier(int penetrationClass, int baseArmorClass, int armorClassDelta)
    {
        return HasUsableArmor
            ? _currentArmor.ResolveMarkDamageMultiplier(penetrationClass, baseArmorClass, armorClassDelta)
            : ResolveBaseArmorMultiplier(
                penetrationClass,
                baseArmorClass,
                armorClassDelta,
                _baseArmorMarkDamageMultiplier,
                _baseArmorMarkDamageMultiplierPerClassGap);
    }

    public void ApplyArmorAbsorbedDamage(float absorbedDamage)
    {
        ApplyArmorAbsorbedDamage(absorbedDamage, 0f, 1f, 0f);
    }

    public void ApplyArmorAbsorbedDamage(
        float healthAbsorbedDamage,
        float infectionAbsorbedDamage,
        float healthDurabilityLossMultiplier,
        float infectionDurabilityLossMultiplier)
    {
        if (!HasUsableArmor)
            return;

        if (_currentArmor.MaxDurability <= 0f)
            return;

        float durabilityDamage =
            Mathf.Max(0f, healthAbsorbedDamage) * Mathf.Max(0f, healthDurabilityLossMultiplier) +
            Mathf.Max(0f, infectionAbsorbedDamage) * Mathf.Max(0f, infectionDurabilityLossMultiplier);

        if (durabilityDamage <= 0f)
            return;

        float previous = _currentArmorDurability;
        _currentArmorDurability = Mathf.Max(0f, _currentArmorDurability - durabilityDamage);

        if (!Mathf.Approximately(previous, _currentArmorDurability))
            OnEquipmentChanged?.Invoke();
    }

    public void RepairArmor(float amount)
    {
        if (_currentArmor == null || _currentArmor.MaxDurability <= 0f)
            return;

        float previous = _currentArmorDurability;
        _currentArmorDurability = Mathf.Clamp(
            _currentArmorDurability + Mathf.Max(0f, amount),
            0f,
            _currentArmor.MaxDurability);

        if (!Mathf.Approximately(previous, _currentArmorDurability))
            OnEquipmentChanged?.Invoke();
    }

    public void RepairArmorToFull()
    {
        if (_currentArmor == null)
            return;

        float previous = _currentArmorDurability;
        _currentArmorDurability = _currentArmor.MaxDurability;

        if (!Mathf.Approximately(previous, _currentArmorDurability))
            OnEquipmentChanged?.Invoke();
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

    private static float ResolveBaseArmorMultiplier(
        int penetrationClass,
        int baseArmorClass,
        int armorClassDelta,
        float sameOrHigherMultiplier,
        float perClassGapMultiplier)
    {
        int effectiveArmorClass = Mathf.Max(0, Mathf.Max(0, baseArmorClass) + armorClassDelta);
        int classGap = Mathf.Max(0, effectiveArmorClass - Mathf.Max(0, penetrationClass));

        return Mathf.Max(
            0f,
            Mathf.Max(0f, sameOrHigherMultiplier) *
            Mathf.Pow(Mathf.Clamp01(perClassGapMultiplier), classGap));
    }
}
