using UnityEngine;

/// <summary>Optional omnidirectional armor layer. Add to a Shielder prefab; not a physical directional shield.</summary>
[DisallowMultipleComponent]
public sealed class ShieldArmorRuntime : MonoBehaviour
{
    [SerializeField, Min(0)] private float _armorClass = 3f;
    [SerializeField, Min(0)] private float _maximumDurability = 100f;
    [SerializeField, Min(0)] private float _currentDurability = 100f;
    public float ArmorClass => Mathf.Max(0, _armorClass);
    public float CurrentDurability => Mathf.Max(0, _currentDurability);
    public bool IsProtecting => isActiveAndEnabled && CurrentDurability > 0 && ArmorClass > 0;
    public float ResolveMultiplier(int penetration) => IsProtecting
        ? ArmorPenetrationDamageCurve.RollMultiplier(ArmorClass, penetration) : 1f;
    public float Absorb(float damage, int penetration, float durabilityLossMultiplier)
    {
        float incoming = Mathf.Max(0, damage);
        float outgoing = incoming * ResolveMultiplier(penetration);
        _currentDurability = Mathf.Max(0, _currentDurability - Mathf.Max(0, incoming - outgoing) * Mathf.Max(0, durabilityLossMultiplier));
        return outgoing;
    }
    public void Restore(float current) => _currentDurability = Mathf.Clamp(current, 0, _maximumDurability);
    public void Refill() => _currentDurability = Mathf.Max(0, _maximumDurability);
}
