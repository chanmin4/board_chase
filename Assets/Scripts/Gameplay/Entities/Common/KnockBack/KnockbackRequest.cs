/*
 * Role hierarchy: Gameplay > Entities > Common > KnockBack
 * Primary responsibility: Implements the focused KnockbackRequest responsibility and collaborates with its role group.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

[System.Serializable]
public struct KnockbackRequest
{
    public Vector3 worldDirection;
    public float distance;
    public float duration;

    // Purpose: Performs the from source workflow for this component.
    public static KnockbackRequest FromSource(
        Vector3 sourcePosition,
        Vector3 targetPosition,
        float distance,
        float duration)
    {
        Vector3 direction = targetPosition - sourcePosition;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.0001f)
            direction.Normalize();
        else
            direction = Vector3.forward;

        return new KnockbackRequest
        {
            worldDirection = direction,
            distance = distance,
            duration = duration
        };
    }
}
