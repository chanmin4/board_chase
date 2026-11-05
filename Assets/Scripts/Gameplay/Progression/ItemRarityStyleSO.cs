using UnityEngine;

[CreateAssetMenu(fileName = "ItemRarityStyle", menuName = "Game/Systems/아이템 등급 색상")]
public sealed class ItemRarityStyleSO : ScriptableObject
{
    private static ItemRarityStyleSO _active;
    public static ItemRarityStyleSO Active => _active != null ? _active : _active = Resources.Load<ItemRarityStyleSO>("ItemRarityStyle");
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetCache() => _active = null;
    [Tooltip("Normal, Rare, Legendary, Unique, Mythic: 저장 enum 순서")]
    public Color[] rarityColors={new(.65f,.65f,.65f),new(.2f,.55f,1f),new(1f,.2f,.2f),new(1f,.3f,.7f),new(1f,.85f,.15f)};

    public Color RarityColor(PlayerShopItemRarity rarity) => rarityColors!=null&&(int)rarity>=0&&(int)rarity<rarityColors.Length
        ?rarityColors[(int)rarity]:rarity switch
    {
        PlayerShopItemRarity.Rare => new Color(.2f,.55f,1f),
        PlayerShopItemRarity.Unique => new Color(1f,.3f,.7f),
        PlayerShopItemRarity.Legendary => new Color(1f,.2f,.2f),
        PlayerShopItemRarity.Mythic => new Color(1f,.85f,.15f),
        _ => new Color(.65f,.65f,.65f)
    };
}
