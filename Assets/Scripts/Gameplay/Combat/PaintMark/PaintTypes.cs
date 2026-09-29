/*
 * Role hierarchy: Gameplay > Combat > PaintMark
 * Primary responsibility: Implements the focused PaintFaction responsibility and collaborates with its role group.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

public enum PaintFaction
{
    Neutral,
    Vaccine,
    Virus,
    Special
}

public enum PaintMarkFaction
{
    None,
    Vaccine,
    Virus
}

public enum PaintChannel
{
    Vaccine,
    Virus,
    PoisonPuddle,

    // 제3자 장판, 힐 장판, 특수 기믹 장판용.
    // 실제 렌더러/마스크는 별도 구현 전까지 선택하지 않는 편이 안전함.
    Special
}

public enum PaintSurfaceState
{
    Neutral,
    Vaccine,
    Virus,
    PoisonPuddle,
    Special,
    CoatedVaccine,
    CoatedVirus,
    CoatedSpecial
}

public static class PaintTypeUtility
{
    // Purpose: Checks whether coated is currently true.
    public static bool IsCoated(PaintSurfaceState state)
    {
        return state == PaintSurfaceState.CoatedVaccine ||
               state == PaintSurfaceState.CoatedVirus ||
               state == PaintSurfaceState.CoatedSpecial;
    }

    // Purpose: Performs the to faction workflow for this component.
    public static PaintFaction ToFaction(PaintChannel channel)
    {
        return channel switch
        {
            PaintChannel.Vaccine => PaintFaction.Vaccine,
            PaintChannel.Virus => PaintFaction.Virus,
            PaintChannel.PoisonPuddle => PaintFaction.Virus,
            PaintChannel.Special => PaintFaction.Special,
            _ => PaintFaction.Neutral
        };
    }

    // Purpose: Performs the to faction workflow for this component.
    public static PaintFaction ToFaction(PaintSurfaceState state)
    {
        return state switch
        {
            PaintSurfaceState.Vaccine => PaintFaction.Vaccine,
            PaintSurfaceState.CoatedVaccine => PaintFaction.Vaccine,

            PaintSurfaceState.Virus => PaintFaction.Virus,
            PaintSurfaceState.PoisonPuddle => PaintFaction.Virus,
            PaintSurfaceState.CoatedVirus => PaintFaction.Virus,

            PaintSurfaceState.Special => PaintFaction.Special,
            PaintSurfaceState.CoatedSpecial => PaintFaction.Special,

            _ => PaintFaction.Neutral
        };
    }

    // Purpose: Performs the to mark faction workflow for this component.
    public static PaintMarkFaction ToMarkFaction(PaintChannel channel)
    {
        return ToFaction(channel) switch
        {
            PaintFaction.Vaccine => PaintMarkFaction.Vaccine,
            PaintFaction.Virus => PaintMarkFaction.Virus,
            _ => PaintMarkFaction.None
        };
    }

    // Purpose: Performs the to mark faction workflow for this component.
    public static PaintMarkFaction ToMarkFaction(PaintSurfaceState state)
    {
        return ToFaction(state) switch
        {
            PaintFaction.Vaccine => PaintMarkFaction.Vaccine,
            PaintFaction.Virus => PaintMarkFaction.Virus,
            _ => PaintMarkFaction.None
        };
    }

    // Purpose: Performs the to surface state workflow for this component.
    public static PaintSurfaceState ToSurfaceState(PaintChannel channel)
    {
        return channel switch
        {
            PaintChannel.Vaccine => PaintSurfaceState.Vaccine,
            PaintChannel.Virus => PaintSurfaceState.Virus,
            PaintChannel.PoisonPuddle => PaintSurfaceState.PoisonPuddle,
            PaintChannel.Special => PaintSurfaceState.Special,
            _ => PaintSurfaceState.Neutral
        };
    }

    // Purpose: Performs the to coated state workflow for this component.
    public static PaintSurfaceState ToCoatedState(PaintChannel channel)
    {
        return ToFaction(channel) switch
        {
            PaintFaction.Vaccine => PaintSurfaceState.CoatedVaccine,
            PaintFaction.Virus => PaintSurfaceState.CoatedVirus,
            PaintFaction.Special => PaintSurfaceState.CoatedSpecial,
            _ => PaintSurfaceState.Neutral
        };
    }

    // Purpose: Performs the to sector owner workflow for this component.
    public static SectorOwner ToSectorOwner(PaintSurfaceState state)
    {
        return ToFaction(state) switch
        {
            PaintFaction.Vaccine => SectorOwner.Player,
            PaintFaction.Virus => SectorOwner.Virus,
            _ => SectorOwner.Neutral
        };
    }

    // Purpose: Checks whether opposite is currently true.
    public static bool IsOpposite(PaintMarkFaction markFaction, PaintSurfaceState surfaceState)
    {
        PaintMarkFaction surfaceMarkFaction = ToMarkFaction(surfaceState);

        if (markFaction == PaintMarkFaction.None ||
            surfaceMarkFaction == PaintMarkFaction.None)
        {
            return false;
        }

        return markFaction != surfaceMarkFaction;
    }

    // Purpose: Checks whether same mark faction is currently true.
    public static bool IsSameMarkFaction(PaintMarkFaction markFaction, PaintSurfaceState surfaceState)
    {
        if (markFaction == PaintMarkFaction.None)
            return false;

        return markFaction == ToMarkFaction(surfaceState);
    }
}
