/*
 * Role hierarchy: Gameplay > Combat > Projectiles
 * Primary responsibility: Handles ProjectileRootRegistry combat resolution, damage, projectile, or status changes.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

public class ProjectileRootRegistry : MonoBehaviour
{
    public static Transform Root { get; private set; }

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    private void OnEnable()
    {
        Root = transform;
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        if (Root == transform)
            Root = null;
    }
}
