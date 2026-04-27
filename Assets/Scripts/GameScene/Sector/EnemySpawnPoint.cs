using UnityEngine;

[DisallowMultipleComponent]
public class EnemySpawnPoint : MonoBehaviour
{
    [SerializeField] private bool _enabledForSpawning = true;
    [SerializeField] private float _gizmoRadius = 0.25f;
    [SerializeField] private Color _gizmoColor = new Color(1f, 0.35f, 0.1f, 0.9f);

    public bool EnabledForSpawning => _enabledForSpawning && gameObject.activeInHierarchy;
    public Vector3 Position => transform.position;

    private void OnDrawGizmos()
    {
        Gizmos.color = _gizmoColor;
        Gizmos.DrawWireSphere(transform.position, Mathf.Max(0.01f, _gizmoRadius));
    }
}
