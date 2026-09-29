/*
 * Role hierarchy: Gameplay > Entities > Shooter > Common > Visuals
 * Primary responsibility: Declares the visible body regions claimed by an equipped visual.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;

/// <summary>
/// Data-only occupancy used by visual equipment and cosmetics. It deliberately
/// describes appearance overlap, not hit boxes or gameplay armor coverage.
/// </summary>
[Flags]
public enum ShooterVisualOccupancy
{
    None = 0,
    Head = 1 << 0,
    Eyes = 1 << 1,
    Mouth = 1 << 2,
    Torso = 1 << 3,
    Back = 1 << 4,
    Hands = 1 << 5
}
