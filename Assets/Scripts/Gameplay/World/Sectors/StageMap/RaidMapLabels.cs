public static class RaidMapLabels
{
    public static string ForRole(RaidRoomRole role) => role == RaidRoomRole.UnknownCandidate ? "?" : role == RaidRoomRole.Merchant ? "긴급보급" :
        UIRuntimeLocalization.Get("UI_Map", "Raid_" + role, role.ToString());
}
