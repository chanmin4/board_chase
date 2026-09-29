/*
 * Role hierarchy: Gameplay > Entities > Shooter > Common > Visuals
 * Primary responsibility: Stores a local transform independently of a scene object.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using UnityEngine;

[Serializable]
public struct ShooterVisualLocalTransform
{
    public Vector3 localPosition;
    public Vector3 localEulerAngles;
    public Vector3 localScale;

    public static ShooterVisualLocalTransform Identity => new ShooterVisualLocalTransform(
        Vector3.zero,
        Vector3.zero,
        Vector3.one);

    public ShooterVisualLocalTransform(
        Vector3 position,
        Vector3 eulerAngles,
        Vector3 scale)
    {
        localPosition = position;
        localEulerAngles = eulerAngles;
        localScale = scale;
    }

    // Purpose: Performs the from transform workflow for this component.
    public static ShooterVisualLocalTransform FromTransform(Transform source)
    {
        return source == null
            ? Identity
            : new ShooterVisualLocalTransform(
                source.localPosition,
                source.localEulerAngles,
                source.localScale);
    }

    // Purpose: Applies the requested to change.
    public void ApplyTo(Transform target)
    {
        if (target == null)
            return;

        target.localPosition = localPosition;
        target.localRotation = Quaternion.Euler(localEulerAngles);
        target.localScale = SanitizeScale(localScale);
    }

    // Purpose: Normalizes scale to a safe authored value.
    private static Vector3 SanitizeScale(Vector3 scale)
    {
        const float minimum = 0.0001f;

        return new Vector3(
            Mathf.Abs(scale.x) < minimum ? 1f : scale.x,
            Mathf.Abs(scale.y) < minimum ? 1f : scale.y,
            Mathf.Abs(scale.z) < minimum ? 1f : scale.z);
    }
}

[Serializable]
public struct ShooterVisualSocketPose
{
    public ShooterVisualSocketId socket;
    public ShooterVisualLocalTransform localTransform;
}
