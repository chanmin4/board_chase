/*
 * Role hierarchy: Presentation > UI > Gameplay > Overlay
 * Primary responsibility: Implements the focused UIOverlayId responsibility and collaborates with its role group.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

public enum UIOverlayId
{
    None = 0,
    ShopHub = 2,
    LootHub = 3,
    PlayerPanelHub = 4,
    BossReward = 5,
    Pause = 6,
    ConfirmPopup = 7,
    MetaUpgrade = 8,
    Credit = 9,
    StageSelect = 10,
    LockedLootKey = 11
}
