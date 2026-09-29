/*
 * Role hierarchy: Gameplay > Combat > PaintMark
 * Primary responsibility: Implements the focused TemporaryMaskCircleVisual responsibility and collaborates with its role group.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

[DisallowMultipleComponent]
public sealed class TemporaryMaskCircleVisual : MonoBehaviour
{
    private Material _ownedMaterial;
    private float _expiresAt;
    private bool _destroyOnExpiry;

    // Purpose: Initializes initialize for use by this component.
    public void Initialize(Material ownedMaterial, float durationSeconds)
    {
        _ownedMaterial = ownedMaterial;
        _expiresAt = Time.time + Mathf.Max(0.01f, durationSeconds);
        _destroyOnExpiry = true;
    }

    // Purpose: Initializes with lifetime owner for use by this component.
    public void InitializeWithLifetimeOwner(Material ownedMaterial)
    {
        _ownedMaterial = ownedMaterial;
        _expiresAt = 0f;
        _destroyOnExpiry = false;
    }

    // Purpose: Processes this component's per-frame runtime work.
    private void Update()
    {
        if (_destroyOnExpiry && Time.time >= _expiresAt)
            Destroy(gameObject);
    }

    // Purpose: Releases owned resources before this component is destroyed.
    private void OnDestroy()
    {
        if (_ownedMaterial != null)
            Destroy(_ownedMaterial);
    }
}
