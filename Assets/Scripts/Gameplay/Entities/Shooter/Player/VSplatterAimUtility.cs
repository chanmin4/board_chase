/*
 * Role hierarchy: Gameplay > Entities > Shooter > Player
 * Primary responsibility: Handles VSplatterAimUtility camera, vision, range, or aiming calculations and presentation.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

public static class VSplatterAimUtility
{
    // Purpose: Attempts to get aim point and reports whether the request succeeded.
    public static bool TryGetAimPoint(
        Camera aimCamera,
        LayerMask hitMask,
        bool allowFallbackPlane,
        float fallbackPlaneY,
        Transform ignoredRoot,
        out Vector3 worldPoint,
        out RaycastHit hitInfo,
        Vector2? screenPoint = null)
    {
        worldPoint = default;
        hitInfo = default;

        if (aimCamera == null)
            return false;

        Ray ray = aimCamera.ScreenPointToRay(screenPoint.HasValue ? (Vector3)screenPoint.Value : Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, 999f, hitMask, QueryTriggerInteraction.Ignore);

        if (hits.Length > 0)
        {
            System.Array.Sort(hits, static (a, b) => a.distance.CompareTo(b.distance));

            for (int i = 0; i < hits.Length; i++)
            {
                Collider collider = hits[i].collider;
                if (collider == null)
                    continue;

                if (ignoredRoot != null && collider.transform.IsChildOf(ignoredRoot))
                    continue;

                hitInfo = hits[i];
                worldPoint = hitInfo.point;
                return true;
            }
        }

        if (!allowFallbackPlane)
            return false;

        return TryGetAimPointOnPlane(aimCamera, fallbackPlaneY, out worldPoint, screenPoint);
    }

    // Purpose: Attempts to get aim point and reports whether the request succeeded.
    public static bool TryGetAimPoint(
        Camera aimCamera,
        LayerMask hitMask,
        bool allowFallbackPlane,
        float fallbackPlaneY,
        out Vector3 worldPoint,
        out RaycastHit hitInfo)
    {
        return TryGetAimPoint(
            aimCamera,
            hitMask,
            allowFallbackPlane,
            fallbackPlaneY,
            null,
            out worldPoint,
            out hitInfo);
    }

    // Purpose: Attempts to get aim point on plane and reports whether the request succeeded.
    public static bool TryGetAimPointOnPlane(Camera aimCamera, float planeY, out Vector3 worldPoint, Vector2? screenPoint = null)
    {
        worldPoint = default;

        if (aimCamera == null)
            return false;

        Ray ray = aimCamera.ScreenPointToRay(screenPoint.HasValue ? (Vector3)screenPoint.Value : Input.mousePosition);
        Plane plane = new Plane(Vector3.up, new Vector3(0f, planeY, 0f));

        if (!plane.Raycast(ray, out float enter))
            return false;

        worldPoint = ray.GetPoint(enter);
        return true;
    }

    // Purpose: Checks whether within flat range is currently true.
    public static bool IsWithinFlatRange(Vector3 origin, Vector3 target, float maxRange)
    {
        origin.y = 0f;
        target.y = 0f;
        return (target - origin).sqrMagnitude <= maxRange * maxRange;
    }
    // Purpose: Performs the clamp flat point to range workflow for this component.
    public static Vector3 ClampFlatPointToRange(Vector3 origin, Vector3 target, float maxRange)
    {
        maxRange = Mathf.Max(0f, maxRange);

        Vector3 flatDelta = target - origin;
        flatDelta.y = 0f;

        if (flatDelta.sqrMagnitude < 0.0001f)
            return target;

        if (flatDelta.sqrMagnitude <= maxRange * maxRange)
            return target;

        Vector3 clamped = origin + flatDelta.normalized * maxRange;
        clamped.y = target.y;
        return clamped;
    }
    // Purpose: Attempts to get point on y plane and reports whether the request succeeded.
    public static bool TryGetPointOnYPlane(Ray ray, float planeY, out Vector3 worldPoint)
    {
        worldPoint = default;

        Plane plane = new Plane(Vector3.up, new Vector3(0f, planeY, 0f));

        if (!plane.Raycast(ray, out float enter))
            return false;

        if (enter < 0f)
            return false;

        worldPoint = ray.GetPoint(enter);
        return true;
    }

    // Purpose: Attempts to get flat circle exit distance and reports whether the request succeeded.
    public static bool TryGetFlatCircleExitDistance(
        Vector3 start,
        Vector3 direction,
        Vector3 center,
        float radius,
        out float distance)
    {
        distance = 0f;

        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f)
            return false;

        direction.Normalize();

        Vector2 s = new Vector2(start.x - center.x, start.z - center.z);
        Vector2 d = new Vector2(direction.x, direction.z);

        float b = 2f * Vector2.Dot(s, d);
        float c = Vector2.Dot(s, s) - radius * radius;
        float discriminant = b * b - 4f * c;

        if (discriminant < 0f)
            return false;

        float sqrt = Mathf.Sqrt(discriminant);
        float t0 = (-b - sqrt) * 0.5f;
        float t1 = (-b + sqrt) * 0.5f;

        float exit = Mathf.Max(t0, t1);
        if (exit <= 0.001f)
            return false;

        distance = exit;
        return true;
    }
}
