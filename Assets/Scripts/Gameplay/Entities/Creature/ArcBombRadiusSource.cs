/*
 * Role hierarchy: Gameplay > Entities > Creature
 * Primary responsibility: Implements the focused ArcBombRadiusSource responsibility and collaborates with its role group.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

public enum ArcBombRadiusSource
{
    DamageRadius,
    PaintRadiusWorld,
    Custom
}