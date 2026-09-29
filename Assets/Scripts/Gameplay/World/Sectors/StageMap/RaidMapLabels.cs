public static class RaidMapLabels
{
    public static string ForRole(RaidRoomRole role) => UIRuntimeLocalization.Get("UI_Map", "Raid_" + role, role.ToString());
}
