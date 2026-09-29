/*
 * Role hierarchy: Gameplay > Entities > Shooter > Common > Visuals
 * Primary responsibility: Names stable attachment points on a shared shooter visual rig.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

public enum ShooterVisualSocketId
{
    Armor = 0,
    RightHand = 1,
    LeftHand = 2,
    // Keeps the original serialized value so existing Head cosmetics become Hair cosmetics.
    Hair = 3,
    Eyes = 4,
    Back = 5,
    // Keep this appended so existing serialized socket values remain unchanged.
    Mask = 6,
    // Dedicated mouth anchor for respirators and lower-face equipment.
    Mouth = 7,
    // Dedicated whole-face anchor for equipment that covers eyes and mouth.
    Face = 8,
    // Kept appended so existing serialized socket values remain unchanged.
    Body = 9,
    Feet = 10
}
