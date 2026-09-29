/*
 * Role hierarchy: Gameplay > World > Sectors
 * Primary responsibility: Manages EnemySpawnPoint stage, sector, navigation, spawn, or spatial progression state.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemySpawnPoint : MonoBehaviour
{
    [Header("Spawn")]
    [Tooltip("Disabled points remain authored in the prefab but are excluded from random enemy spawning.")]
    [SerializeField] private bool _enabledForSpawning = true;

    [Header("Scene Gizmo")]
    [Tooltip("Radius of the enemy spawn marker shown in the Scene view.")]
    [SerializeField, Min(0.01f)] private float _gizmoRadius = 0.35f;

    [SerializeField] private Color _gizmoColor = new(1f, 0.1f, 0.1f, 0.9f);

    public bool EnabledForSpawning =>
        _enabledForSpawning && gameObject.activeInHierarchy;

    // Purpose: Responds to the draw gizmos callback.
    private void OnDrawGizmos()
    {
        Gizmos.color = _gizmoColor;
        Gizmos.DrawWireSphere(transform.position, Mathf.Max(0.01f, _gizmoRadius));

        Vector3 forward = transform.forward * Mathf.Max(0.25f, _gizmoRadius * 1.5f);
        Gizmos.DrawLine(transform.position, transform.position + forward);
    }
}
