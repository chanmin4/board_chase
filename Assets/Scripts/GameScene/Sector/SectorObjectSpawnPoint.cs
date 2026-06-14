using System;
using UnityEngine;

[Flags]
public enum SectorObjectSpawnPointUsage
{
    None = 0,
    Enemy = 1 << 0,
    SectorObject = 1 << 1,
    Any = Enemy | SectorObject
}

[DisallowMultipleComponent]
public class SectorObjectSpawnPoint : MonoBehaviour
{
    [Header("Spawn")]
    [SerializeField] private bool _enabledForSpawning = true;

    [Tooltip("Which runtime systems can use this point.")]
    [SerializeField] private SectorObjectSpawnPointUsage _usage = SectorObjectSpawnPointUsage.Any;

    [Tooltip("Optional category filter for sector object presets. Empty means any category.")]
    [SerializeField] private string _spawnPointTag;

    [Header("Gizmo")]
    [SerializeField] private float _gizmoRadius = 0.25f;
    [SerializeField] private Color _gizmoColor = new Color(1f, 0.35f, 0.1f, 0.9f);

    public bool EnabledForSpawning => _enabledForSpawning && gameObject.activeInHierarchy;
    public Vector3 Position => transform.position;
    public SectorObjectSpawnPointUsage Usage => _usage;
    public string SpawnPointTag => _spawnPointTag;

    public bool CanUseFor(SectorObjectSpawnPointUsage usage, string requiredTag = null)
    {
        if (!EnabledForSpawning)
            return false;

        if ((_usage & usage) == 0)
            return false;

        if (string.IsNullOrWhiteSpace(requiredTag))
            return true;

        return string.Equals(
            _spawnPointTag,
            requiredTag,
            StringComparison.OrdinalIgnoreCase);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = _gizmoColor;
        Gizmos.DrawWireSphere(transform.position, Mathf.Max(0.01f, _gizmoRadius));
    }
}
