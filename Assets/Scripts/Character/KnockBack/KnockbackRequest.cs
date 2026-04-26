using UnityEngine;

[System.Serializable]
public struct KnockbackRequest
{
    public Vector3 worldDirection;
    public float distance;
    public float duration;

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
