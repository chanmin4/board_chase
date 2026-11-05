/*
 * Role hierarchy: Presentation > UI > Gameplay > Shop
 * Primary responsibility: Handles PlayerShopItemRarity interaction, pickup, trade, loot, or storage flow.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

public enum PlayerShopItemRarity
{
    Normal = 0,
    Rare = 1,
    Unique = 3,
    Legendary = 2,
    Mythic = 4
}
