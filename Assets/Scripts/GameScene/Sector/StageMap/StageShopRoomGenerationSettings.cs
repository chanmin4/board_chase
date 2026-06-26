using UnityEngine;

public readonly struct StageShopRoomGenerationSettings
{
    public static StageShopRoomGenerationSettings Disabled =>
        new StageShopRoomGenerationSettings(false, 0, 0, 0f, true);

    public bool Enabled { get; }
    public int MinCount { get; }
    public int MaxCount { get; }
    public float ExtraRoomChance { get; }
    public bool ExcludeFirstRoom { get; }

    public StageShopRoomGenerationSettings(
        bool enabled,
        int minCount,
        int maxCount,
        float extraRoomChance,
        bool excludeFirstRoom)
    {
        Enabled = enabled;
        MinCount = Mathf.Max(0, minCount);
        MaxCount = Mathf.Max(MinCount, maxCount);
        ExtraRoomChance = Mathf.Clamp01(extraRoomChance);
        ExcludeFirstRoom = excludeFirstRoom;
    }

    public int RollShopRoomCount(System.Random rng)
    {
        if (!Enabled)
            return 0;

        if (MaxCount <= MinCount)
            return MinCount;

        rng ??= new System.Random();

        int count = MinCount;
        int extraSlots = MaxCount - MinCount;

        for (int i = 0; i < extraSlots; i++)
        {
            if (rng.NextDouble() <= ExtraRoomChance)
                count++;
        }

        return Mathf.Clamp(count, MinCount, MaxCount);
    }
}
