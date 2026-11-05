/*
 * Role hierarchy: Gameplay > Items > Definitions
 * Primary responsibility: Defines the PassiveItemSO data asset used to configure runtime rules in the Inspector.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

[CreateAssetMenu(
    fileName = "PassiveItem",
    menuName = "Game/Passive Item")]
public class PassiveItemSO : ItemSO
{
    [SerializeField] private PassiveCategory _category;
    public PassiveCategory Category => SourceDefinition._category;
    [System.NonSerialized] private bool _disabledForRaid;
    [System.NonSerialized] private bool _everEquipped;
    [System.NonSerialized] private int _maximumDismantleYield;
    public bool DisabledForRaid => _disabledForRaid;
    public bool EverEquipped => _everEquipped || IsBound;
    public int MaximumDismantleYield => _maximumDismantleYield;
    [Header("개체별 랜덤 스탯 · 기본 OFF (기존 데이터 유지)")]
    [SerializeField] private bool _randomStatsEnabled;
    [SerializeField] private bool _allowNumericReroll = true;
    [SerializeField,Min(1)] private int _dismantlePartAmount = 1;
    public bool AllowNumericReroll=>_allowNumericReroll;
    public int DismantlePartAmount=>Mathf.Max(1,_dismantlePartAmount);
    [SerializeField] private PassiveRarityRoll[] _rarityRolls = System.Array.Empty<PassiveRarityRoll>();
    [System.NonSerialized] private PassiveItemSO _sourceDefinition;
    [System.NonSerialized] private PlayerStatModifier[] _instanceModifiers;
    [System.NonSerialized] private PlayerShopItemRarity _instanceRarity;
    [System.NonSerialized] private bool _boundToOwner;
    public bool IsBound => _boundToOwner;
    [Tooltip("같은 효과 종류의 중복 장착 방지 ID. 페널티 스탯이 겹치는 다른 종류와는 구분합니다. 빈 값은 이전 판정을 유지합니다.")]
    [SerializeField] private string _effectGroup;
    public bool SharesEffectWith(PassiveItemSO other)
    {
        if (other == null) return false;
        if (SourceDefinition == other.SourceDefinition) return true;
        if(!string.IsNullOrEmpty(SourceDefinition._effectGroup)&&!string.IsNullOrEmpty(other.SourceDefinition._effectGroup))
            return SourceDefinition._effectGroup==other.SourceDefinition._effectGroup;
        foreach (var a in StatModifiers ?? System.Array.Empty<PlayerStatModifier>())
            foreach (var b in other.StatModifiers ?? System.Array.Empty<PlayerStatModifier>())
                if (a.stat == b.stat && a.value != 0f && b.value != 0f && ShooterStatPolicy.IsPassiveStat(a.stat)) return true;
        return false;
    }
    public bool RandomStatsEnabled => _randomStatsEnabled;
    public bool IsRolledInstance => _sourceDefinition != null;
    public PassiveItemSO SourceDefinition => IsRolledInstance ? _sourceDefinition : this;
    public override PlayerShopItemRarity Rarity => IsRolledInstance ? _instanceRarity : base.Rarity;
    public override int MaxStack => 1;
    public PassiveRarityRoll FindRoll(PlayerShopItemRarity rarity) =>
        System.Array.Find(_rarityRolls ?? System.Array.Empty<PassiveRarityRoll>(), row => row != null && row.rarity == rarity);
    internal void ConfigureInstance(PassiveItemSO source, PassiveInstanceSaveData record)
    {
        _sourceDefinition = source;
        _instanceModifiers = (PlayerStatModifier[])record.modifiers.Clone();
        _instanceRarity = record.rarity;
        _boundToOwner = record.boundToOwner;
        _disabledForRaid = record.disabledForRaid;
        _everEquipped = record.everEquipped;
        _maximumDismantleYield = record.maximumDismantleYield;
        _featureFlags = record.featureFlags != null ? (PlayerFeatureFlagModifier[])record.featureFlags.Clone() : System.Array.Empty<PlayerFeatureFlagModifier>();
        SetRuntimeGuid(record.instanceId);
        name = source.name;
        hideFlags = HideFlags.DontSave;
    }
    [Header("Pickup Rules")]
    [Tooltip("If true, this passive item can be picked up multiple times.")]
    [SerializeField, HideInInspector] private bool _allowDuplicatePickup = false;
    [Tooltip("서로 다른 랜덤 개체라도 같은 종류를 여러 칸에 장착할 수 있는지. 같은 개체의 중복 장착은 항상 금지.")]
    [SerializeField, HideInInspector] private bool _allowDuplicateEquip;
    public bool AllowDuplicateEquip => false;

    [Header("Passive Stat Modifiers")]
    [Tooltip("Player stat modifiers applied only while this passive is equipped.")]
    [SerializeField] private PlayerStatModifier[] _statModifiers;

    [Tooltip("Balance strength relative to the authored modifiers. 0.5 halves bonuses and penalties; multiplier modifiers are blended toward 1. Overrides and boolean features are not scaled.")]
    [SerializeField, Range(0f, 1f)] private float _effectStrength = 1f;
    private PlayerStatModifier[] _resolvedModifiers;
    public float EffectStrength => Mathf.Clamp01(_effectStrength);

    [Tooltip("Player feature flags applied only while this passive is equipped.")]
    [SerializeField] private PlayerFeatureFlagModifier[] _featureFlags;

    public bool AllowDuplicatePickup => true; // Repeated loot is dismantling/reroll material; only equipping is restricted.
    public PlayerStatModifier[] StatModifiers
    {
        get
        {
            if (IsRolledInstance) return _instanceModifiers;
            if (_statModifiers == null || EffectStrength == 1f) return _statModifiers;
            if (_resolvedModifiers == null || _resolvedModifiers.Length != _statModifiers.Length)
                _resolvedModifiers = new PlayerStatModifier[_statModifiers.Length];
            for (int i = 0; i < _statModifiers.Length; i++)
            {
                var modifier = _statModifiers[i];
                modifier.value = modifier.type switch
                {
                    StatModifierType.PercentMultiply => Mathf.Lerp(1f, modifier.value, EffectStrength),
                    StatModifierType.Override => modifier.value,
                    _ => modifier.value * EffectStrength
                };
                _resolvedModifiers[i] = modifier;
            }
            return _resolvedModifiers;
        }
    }
    public PlayerFeatureFlagModifier[] FeatureFlags => _featureFlags;
    public override ItemKind Kind => ItemKind.Passive;
}
