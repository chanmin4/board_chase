public enum PaintChannel
{
    Vaccine,
    Virus,
    PoisonPuddle
}

public enum PaintSurfaceState
{
    Neutral,
    Vaccine,
    Virus,
    PoisonPuddle,
    CoatedVaccine,
    CoatedVirus
}

public static class PaintTypeUtility
{
    public static bool IsCoated(PaintSurfaceState state)
    {
        return state == PaintSurfaceState.CoatedVaccine ||
               state == PaintSurfaceState.CoatedVirus;
    }

    public static PaintSurfaceState ToSurfaceState(PaintChannel channel)
    {
        return channel switch
        {
            PaintChannel.Vaccine => PaintSurfaceState.Vaccine,
            PaintChannel.Virus => PaintSurfaceState.Virus,
            PaintChannel.PoisonPuddle => PaintSurfaceState.PoisonPuddle,
            _ => PaintSurfaceState.Neutral
        };
    }

    public static PaintSurfaceState ToCoatedState(PaintChannel channel)
    {
        return channel switch
        {
            PaintChannel.Vaccine => PaintSurfaceState.CoatedVaccine,
            PaintChannel.Virus => PaintSurfaceState.CoatedVirus,
            PaintChannel.PoisonPuddle => PaintSurfaceState.CoatedVirus,
            _ => PaintSurfaceState.Neutral
        };
    }

    public static SectorOwner ToSectorOwner(PaintSurfaceState state)
    {
        return state switch
        {
            PaintSurfaceState.Vaccine => SectorOwner.Player,
            PaintSurfaceState.CoatedVaccine => SectorOwner.Player,
            PaintSurfaceState.Virus => SectorOwner.Virus,
            PaintSurfaceState.PoisonPuddle => SectorOwner.Virus,
            PaintSurfaceState.CoatedVirus => SectorOwner.Virus,
            _ => SectorOwner.Neutral
        };
    }
}