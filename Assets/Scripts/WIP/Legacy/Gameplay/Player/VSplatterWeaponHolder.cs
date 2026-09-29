/*
 * Role hierarchy: WIP > Legacy > Gameplay > Player
 * Primary responsibility: Isolates the legacy or deferred VSplatterWeaponHolder implementation from active production code.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

public class VSplatterWeaponHolder : EntityWeaponHolder
{
}
