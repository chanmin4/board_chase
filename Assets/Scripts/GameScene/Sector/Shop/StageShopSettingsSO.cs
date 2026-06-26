using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "StageShopSettings",
    menuName = "Game/Stage/Stage Shop Settings")]
public class StageShopSettingsSO : ScriptableObject
{
    [Serializable]
    public sealed class StageShopRule
    {
        [Tooltip("Stage index this shop rule applies to.")]
        public int stageIndex = 1;

        [Header("Room Generation")]
        [Tooltip("If enabled, this stage can replace NormalBattle rooms with Shop rooms.")]
        public bool enableShopRooms = true;

        [Min(0)]
        [Tooltip("Minimum Shop rooms generated for this stage.")]
        public int shopRoomMinCount = 0;

        [Min(0)]
        [Tooltip("Maximum Shop rooms generated for this stage.")]
        public int shopRoomMaxCount = 1;

        [Range(0f, 1f)]
        [Tooltip("Chance used for each extra Shop room slot between min and max.")]
        public float extraShopRoomChance = 0f;

        [Tooltip("If true, room (0,0) is not picked as Shop unless there are not enough candidates.")]
        public bool excludeFirstRoomFromShop = true;

        [Header("Shop Inventory")]
        [Tooltip("Drop table used by SectorShop in this stage.")]
        public ShopRoomDropTableSO dropTable;


        [Tooltip("If true, each shop slot can reroll using the same ShopRoomDropTableSO.")]
        public bool allowReroll = true;
    }

    [SerializeField] private StageShopRule[] _stageRules;

    public StageShopRoomGenerationSettings CreateShopRoomGenerationSettings(int stageIndex)
    {
        StageShopRule rule = FindRule(stageIndex);

        if (rule == null)
            return StageShopRoomGenerationSettings.Disabled;

        return new StageShopRoomGenerationSettings(
            rule.enableShopRooms,
            rule.shopRoomMinCount,
            rule.shopRoomMaxCount,
            rule.extraShopRoomChance,
            rule.excludeFirstRoomFromShop);
    }

    public bool TryGetRule(int stageIndex, out StageShopRule rule)
    {
        rule = FindRule(stageIndex);
        return rule != null;
    }

    private StageShopRule FindRule(int stageIndex)
    {
        if (_stageRules == null)
            return null;

        for (int i = 0; i < _stageRules.Length; i++)
        {
            StageShopRule rule = _stageRules[i];

            if (rule != null && rule.stageIndex == stageIndex)
                return rule;
        }

        return null;
    }
}
