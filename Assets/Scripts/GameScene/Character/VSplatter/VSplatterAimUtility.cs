using UnityEngine;

public static class VSplatterAimUtility
{
    public static bool TryGetAimPoint(
        Camera aimCamera,
        LayerMask hitMask,
        bool allowFallbackPlane,
        float fallbackPlaneY,
        out Vector3 worldPoint,
        out RaycastHit hitInfo)
    {
        worldPoint = default;
        hitInfo = default;

        if (aimCamera == null)
            return false;

        Ray ray = aimCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out hitInfo, 999f, hitMask, QueryTriggerInteraction.Ignore))
        {
            worldPoint = hitInfo.point;
            return true;
        }

        if (!allowFallbackPlane)
            return false;

        Plane plane = new Plane(Vector3.up, new Vector3(0f, fallbackPlaneY, 0f));
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
}