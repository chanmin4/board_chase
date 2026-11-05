using UnityEngine;

/// <summary>Carry capacity is a penalty reference, never a pickup or equipment gate.</summary>
public readonly struct PlayerCarryLoad
{
    public readonly float WeightKg;
    public readonly float CapacityKg;
    public readonly float MoveSpeedMultiplier;
    public float Ratio => CapacityKg > 0f ? WeightKg / CapacityKg : 0f;
    public bool RequiresStandingForRecovery => Ratio >= .9f && Ratio < 1f;

    public PlayerCarryLoad(float weightKg, float capacityKg)
    {
        WeightKg = float.IsNaN(weightKg) ? 0f : Mathf.Max(0f, weightKg);
        CapacityKg = capacityKg > 0f && !float.IsInfinity(capacityKg) ? capacityKg : 70f;
        float ratio = WeightKg / CapacityKg;
        MoveSpeedMultiplier = ratio >= 1f ? .2f :
            ratio >= .9f ? .5f : ratio >= .85f ? .6f : ratio >= .8f ? .7f :
            ratio >= .75f ? .75f : ratio >= .7f ? .8f : 1f;
    }

    public float RecoveryMultiplier(bool isMoving) => Ratio >= 1f || (Ratio >= .9f && isMoving)
        ? 0f : Ratio >= .8f ? .5f : 1f;
}

public partial class PlayerInventoryRuntime
{
    private int _carryWeightFrame = -1;
    private float _carryWeightKg;

    public PlayerCarryLoad CarryLoad
    {
        get
        {
            if (_carryWeightFrame != Time.frameCount)
            {
                _carryWeightKg = CalculateCarriedWeightKg();
                _carryWeightFrame = Time.frameCount;
            }
            return new PlayerCarryLoad(_carryWeightKg, (_statsRuntime?.StatConfig as PlayerStatsConfigSO)?.CarryCapacityKg ?? 70f);
        }
    }

    public float CalculateCarriedWeightKg()
    {
        float total = 0f;
        if (_items != null)
            foreach (var stack in _items)
                if (stack != null && !stack.IsEmpty) total += stack.Item.WeightForAmount(stack.Amount);

        total += (_equippedBackpack?.WeightKg ?? 0f) + (_equippedGasMask?.WeightKg ?? 0f);
        if (_equipmentRuntime != null)
        {
            total += _equipmentRuntime.CurrentArmor?.WeightKg ?? 0f;
            for (int i = 0; i < _equipmentRuntime.WeaponSlotCount; i++)
                total += _equipmentRuntime.GetWeapon(i)?.WeightForAmount(_equipmentRuntime.GetWeaponAmount(i)) ?? 0f;
        }
        // The key tool itself occupies a normal slot; only its nested physical keys are additional.
        if (_keyToolSlots != null && _keyToolSlots.Count > 0 && HasKeyTool)
            foreach (var key in _keyToolSlots)
                if (key != null && key.HasKey) total += key.Key.WeightKg;

        // Weapon/ammo bindings are references, while usable bindings own their own stacks.
        total += _quickSlotRuntime?.CarriedUsableWeightKg ?? 0f;
        total += _bulletLoadout?.CarriedLoadedAndLegacyAmmoWeightKg ?? 0f;
        return total;
    }
}
