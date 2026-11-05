using UnityEngine;

/// <summary>Bounds 기반의 명시적인 배치 보조. 메시/프리팹/Anchor는 변경하지 않고 선택 장식의 이동값만 계산한다.</summary>
public static class ShooterCustomizeAccessoryPlacement
{
    /// <summary>백팩 메시의 중심을 몸통 높이에, 몸 쪽 면을 등 표면에 맞춘다. Anchor/회전/크기는 유지한다.</summary>
    public static bool TryFitBackpack(ShooterCustomizePreview preview, ShooterVisualLocalTransform current, out ShooterVisualLocalTransform pose)
    {
        pose = current;
        if (preview?.Rig == null || preview.BackpackMount == null || preview.BackpackVisual == null ||
            !TryBounds(preview.Root.transform, preview.Rig.BodyVisualRoot, out var body) ||
            !TryBounds(preview.Root.transform, preview.BackpackVisual.transform, out var bag)) return false;
        // Authored character front is +Z. The backpack's frontmost geometry meets
        // the body's -Z surface; compensate its actual mesh bounds, not just its pivot.
        float contactZ = body.min.z + body.size.x * .01f;
        Vector3 delta = new(body.center.x - bag.center.x, body.center.y - bag.center.y, contactZ - bag.max.z);
        pose.localPosition += preview.BackpackMount.InverseTransformVector(preview.Root.transform.TransformVector(delta));
        return true;
    }

    // Root Transform=0 and mesh bounds.center=0 are different checks. A centered ring's pivot
    // belongs on the body axis, whereas a centered, curved eye decal belongs near the front surface.
    public static bool TryBounds(Transform basis, Transform visual, out Bounds bounds)
    {
        bounds = default; bool found = false;
        if (basis == null || visual == null) return false;
        foreach (var renderer in visual.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer is not MeshRenderer && renderer is not SkinnedMeshRenderer) continue;
            Matrix4x4 matrix = basis.worldToLocalMatrix * renderer.localToWorldMatrix;
            Bounds local = renderer.localBounds;
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = local.center + Vector3.Scale(local.extents,
                    new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                Vector3 point = matrix.MultiplyPoint3x4(corner);
                if (!found) { bounds = new Bounds(point, Vector3.zero); found = true; } else bounds.Encapsulate(point);
            }
        }
        return found;
    }

    public static bool TryFit(ShooterCustomizePreview preview, ShooterCustomizeAttachment entry, bool front, out ShooterVisualLocalTransform pose)
    {
        pose = entry.localTransform;
        if (preview?.Rig == null || entry.visualPrefab == null ||
            !TryBounds(preview.Root.transform, preview.Rig.BodyVisualRoot, out var body) ||
            !TryBounds(entry.visualPrefab.transform, entry.visualPrefab.transform, out var geometry)) return false;
        Transform socket = preview.Rig.ResolveSocket(entry.socket);
        if (socket == null) return false;
        Vector3 scale = pose.localScale == Vector3.zero ? Vector3.one : pose.localScale;
        Matrix4x4 placement = preview.Root.transform.worldToLocalMatrix * socket.localToWorldMatrix *
            Matrix4x4.TRS(pose.localPosition, Quaternion.Euler(pose.localEulerAngles), scale);
        Bounds placed = default;
        for (int i = 0; i < 8; i++)
        {
            Vector3 corner = geometry.center + Vector3.Scale(geometry.extents,
                new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
            Vector3 point = placement.MultiplyPoint3x4(corner);
            if (i == 0) placed = new Bounds(point, Vector3.zero); else placed.Encapsulate(point);
        }
        // Preserve height, rotation and scale. Local +Z is the authored character's front.
        // Front mode places the outermost geometry near the body's surface, not the prefab pivot.
        float targetZ = front ? body.max.z + body.size.x * .01f : body.center.z;
        Vector3 delta = new(body.center.x - placed.center.x, 0, targetZ - (front ? placed.max.z : placed.center.z));
        pose.localPosition += socket.InverseTransformVector(preview.Root.transform.TransformVector(delta));
        return true;
    }
}
