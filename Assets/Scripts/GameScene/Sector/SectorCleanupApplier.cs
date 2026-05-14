using UnityEngine;

public class SectorCleanupApplier : MonoBehaviour
{
    [Header("Options")]
    [SerializeField] private bool _destroyEnemies = true;
    [SerializeField] private bool _destroyCleanupRootChildren = true;

    public void CleanupSector(SectorRuntime sector)
    {
        if (sector == null)
            return;

        if (_destroyEnemies)
            DestroyComponentsInChildren<Enemy>(sector.transform);

        if (_destroyCleanupRootChildren)
            DestroyCleanupRootChildren(sector);
    }

    private void DestroyComponentsInChildren<T>(Transform root) where T : Component
    {
        T[] components = root.GetComponentsInChildren<T>(true);

        for (int i = 0; i < components.Length; i++)
        {
            if (components[i] == null)
                continue;

            Destroy(components[i].gameObject);
        }
    }

    private void DestroyCleanupRootChildren(SectorRuntime sector)
    {
        Transform[] roots = sector.CleanupRoots;
        if (roots == null)
            return;

        for (int i = 0; i < roots.Length; i++)
            DestroyChildren(roots[i]);
    }

    private void DestroyChildren(Transform root)
    {
        if (root == null)
            return;

        for (int i = root.childCount - 1; i >= 0; i--)
            Destroy(root.GetChild(i).gameObject);
    }
}
