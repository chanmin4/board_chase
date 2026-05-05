using UnityEngine;

public static class VSplatterAimUtility
{
    public static bool TryGetAimPoint(
        Camera aimCamera,
        LayerMask hitMask,
        bool allowFallbackPlane,
        float fallbackPlaneY,
        Transform ignoredRoot,
        out Vector3 worldPoint,
        out RaycastHit hitInfo)
    {
        worldPoint = default;
        hitInfo = default;

        if (aimCamera == null)
            return false;

        Ray ray = aimCamera.ScreenPointToRay(Input.mousePosition);
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

        return TryGetAimPointOnPlane(aimCamera, fallbackPlaneY, out worldPoint);
    }

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

    public static bool TryGetAimPointOnPlane(Camera aimCamera, float planeY, out Vector3 worldPoint)
    {
        worldPoint = default;

        if (aimCamera == null)
            return false;

        Ray ray = aimCamera.ScreenPointToRay(Input.mousePosition);
        Plane plane = new Plane(Vector3.up, new Vector3(0f, planeY, 0f));

        if (!plane.Raycast(ray, out float enter))
            return false;

        worldPoint = ray.GetPoint(enter);
        return true;
    }

    public static bool IsWithinFlatRange(Vector3 origin, Vector3 target, float maxRange)
    {
        origin.y = 0f;
        target.y = 0f;
        return (target - origin).sqrMagnitude <= maxRange * maxRange;
    }
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
}
