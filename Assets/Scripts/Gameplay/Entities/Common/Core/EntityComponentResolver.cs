/*
 * Role hierarchy: Gameplay > Entities > Common > Core
 * Primary responsibility: Implements the focused EntityComponentResolver responsibility and collaborates with its role group.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using UnityEngine.AI;

public static class EntityComponentResolver
{
    public static T Resolve<T>(Component source) where T : Component
    {
        if (source == null)
            return null;

        if (source.TryGetComponent(out T direct))
            return direct;

        T child = source.GetComponentInChildren<T>(true);
        if (child != null)
            return child;

        T parent = source.GetComponentInParent<T>();
        if (parent != null)
            return parent;

        Transform entityRoot = ResolveEntityRoot(source.transform);
        return entityRoot != null ? entityRoot.GetComponentInChildren<T>(true) : null;
    }

    // Purpose: Resolves the entity root needed by this component.
    public static Transform ResolveEntityRoot(Component source)
    {
        return source != null ? ResolveEntityRoot(source.transform) : null;
    }

    // Purpose: Resolves the entity root needed by this component.
    public static Transform ResolveEntityRoot(Transform source)
    {
        if (source == null)
            return null;

        Transform current = source;
        Transform best = null;

        while (current != null)
        {
            if (IsEntityRootCandidate(current))
                best = current;

            current = current.parent;
        }

        return best != null ? best : source;
    }

    // Purpose: Checks whether entity root candidate is currently true.
    private static bool IsEntityRootCandidate(Transform transform)
    {
        return transform.GetComponent<Damageable>() != null ||
               transform.GetComponent<PlayerCharacter>() != null ||
               transform.GetComponent<Enemy>() != null ||
               transform.GetComponent<CharacterController>() != null ||
               transform.GetComponent<NavMeshAgent>() != null;
    }
}
