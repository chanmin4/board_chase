using UnityEngine;

/// <summary>Attaches ordinary item visuals without requiring weapon components on every prop.</summary>
public static class UsableHeldVisualUtility
{
    public static GameObject Create(UsableItemSO item, Transform handSocket)
    {
        if (item == null || handSocket == null || item.HandVisualPrefab == null)
            return null;

        // Existing weapon/throwable overrides retain their authored absolute offsets.
        if (!item.UsesWorldVisualInHand)
        {
            GameObject dedicated = Object.Instantiate(item.HandVisualPrefab, handSocket, false);
            item.ApplyHeldViewTransform(dedicated.transform);
            return dedicated;
        }

        // Corrections belong to a wrapper, not the model: keep the prefab's saved
        // local position, rotation and scale intact (including scaled imports).
        GameObject mount = new("HeldItem_" + item.name);
        mount.SetActive(false);
        mount.transform.SetParent(handSocket, false);
        item.ApplyHeldViewTransform(mount.transform);
        GameObject visual = Object.Instantiate(item.HandVisualPrefab, mount.transform, false);

        // A held display must not become a second physical obstacle or pickup trigger.
        foreach (Collider collider in visual.GetComponentsInChildren<Collider>(true))
            collider.enabled = false;
        foreach (Rigidbody body in visual.GetComponentsInChildren<Rigidbody>(true))
        {
            body.useGravity = false;
            body.isKinematic = true;
            body.detectCollisions = false;
        }
        mount.SetActive(true);
        return mount;
    }
}
