using System;
using UnityEngine;

/// <summary>Independent 아이템 사용 레벨 source. Runtime never reads the retired aggregate.</summary>
[CreateAssetMenu(fileName = "ItemUsageRules", menuName = "Game/Systems/아이템 사용 레벨")]
public sealed class ItemUsageRulesSO : ScriptableObject
{
    private static ItemUsageRulesSO _active;
    public static ItemUsageRulesSO Active => _active != null ? _active : _active = Resources.Load<ItemUsageRulesSO>("ItemUsageRules");
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetCache() => _active = null;
    public UsageLevelRules usage = new();
}

[Serializable]
public sealed class UsageLevelRules
{
    [Tooltip("인덱스=클래스. 미작성 등급은 1(제한 없음).")]
    public int[] armor = {1,1,2,5,11,16,20,1,1};
    public int[] ammunition = {1,1,3,6,12,17,21,1,1};
    [Tooltip("Pistol, Revolver, SMG, AR, LMG, Sniper, Shotgun, Rocket 순. 미확정 무기는 제한 없음.")]
    public int[] weapon = {1,1,4,8,1,8,4,1};
    public int[] passive = {1,9,1,18,1};
    private static int At(int[] values, int index) => values != null && index >= 0 && index < values.Length ? Mathf.Max(1,values[index]) : 1;
    public int Required(ItemSO item)
    {
        if (item == null) return 1;
        if (item.UsageLevelOverride > 0) return item.UsageLevelOverride;
        return item switch {
            ArmorItemSO a => At(armor,a.ArmorClass), BulletSO b => At(ammunition,(int)b.Tier),
            RangedWeaponSO w => At(weapon,(int)w.WeaponType), PassiveItemSO p => At(passive,(int)p.Rarity), _ => 1
        };
    }
}
