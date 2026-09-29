/*
 * Role hierarchy: Gameplay > Entities > Shooter > Player
 * Primary responsibility: Coordinates the VSplatterDashController subsystem lifecycle and collaborating objects.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("Scripts/V Splatter Dash Controller")]
public sealed class VSplatterDashController : PlayerShooterDashController
{
}
