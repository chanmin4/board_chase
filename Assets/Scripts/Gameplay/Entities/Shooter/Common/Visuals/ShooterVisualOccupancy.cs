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

/// <summary>외형 장식에만 적용되는 숨김 조건. 아이템 소유/능력치/충돌체에는 영향을 주지 않는다.</summary>
[Flags]
public enum ShooterVisualEquipmentKind
{
    None = 0,
    Weapon = 1 << 0,
    Armor = 1 << 1,
    GasMask = 1 << 2,
    Backpack = 1 << 3
}
