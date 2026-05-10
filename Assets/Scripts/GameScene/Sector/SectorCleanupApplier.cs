using UnityEngine;

public class SectorCleanupApplier : MonoBehaviour
{
    [Header("Options")]
    [SerializeField] private bool _destroyEnemies = true;

    public void CleanupSector(SectorRuntime sector)
    {
        if (sector == null)
            return;

        if (_destroyEnemies)
            DestroyComponentsInChildren<Enemy>(sector.transform);
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
}
