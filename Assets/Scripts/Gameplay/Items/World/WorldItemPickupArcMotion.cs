/*
 * Role hierarchy: Gameplay > Items > World
 * Primary responsibility: Handles WorldItemPickupArcMotion interaction, pickup, trade, loot, or storage flow.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

/// <summary>
/// Short presentation-only launch for a spawned WorldItemPickup. Interaction and
/// colliders remain disabled until the pickup reaches its final floor position.
/// </summary>
[DisallowMultipleComponent]
public sealed class WorldItemPickupArcMotion : MonoBehaviour
{
    private WorldItemPickup _pickup;
    private Vector3 _start;
    private Vector3 _end;
    private float _duration;
    private float _arcHeight;
    private float _startedAt;
    private Collider[] _colliders;
    private bool[] _colliderEnabledStates;
    private bool _configured;
    private bool _completed;

    // Purpose: Configures this component''s configure.
    public void Configure(
        WorldItemPickup pickup,
        Vector3 start,
        Vector3 end,
        float durationSeconds,
        float arcHeight)
    {
        _pickup = pickup;
        _start = start;
        _end = end;
        _duration = Mathf.Max(0.01f, durationSeconds);
        _arcHeight = Mathf.Max(0f, arcHeight);
        _startedAt = Time.time;
        _completed = false;
        _configured = true;

        transform.position = _start;
        CacheAndDisableColliders();

        if (_pickup != null)
            _pickup.SetInteractionEnabled(false);
    }

    // Purpose: Processes this component's per-frame runtime work.
    private void Update()
    {
        if (!_configured)
            return;

        float t = Mathf.Clamp01((Time.time - _startedAt) / _duration);
        Vector3 position = Vector3.Lerp(_start, _end, t);
        position.y += 4f * _arcHeight * t * (1f - t);
        transform.position = position;

        if (t >= 1f)
            Complete();
    }

    // Purpose: Performs the complete workflow for this component.
    private void Complete()
    {
        if (_completed)
            return;

        _completed = true;
        transform.position = _end;
        RestoreColliders();

        if (_pickup != null)
            _pickup.SetInteractionEnabled(true);

        Destroy(this);
    }

    // Purpose: Caches and disable colliders for later reuse.
    private void CacheAndDisableColliders()
    {
        _colliders = GetComponentsInChildren<Collider>(true);
        _colliderEnabledStates = new bool[_colliders.Length];

        for (int i = 0; i < _colliders.Length; i++)
        {
            Collider target = _colliders[i];

            if (target == null)
                continue;

            _colliderEnabledStates[i] = target.enabled;
            target.enabled = false;
        }
    }

    // Purpose: Restores the saved colliders state.
    private void RestoreColliders()
    {
        if (_colliders == null || _colliderEnabledStates == null)
            return;

        for (int i = 0; i < _colliders.Length; i++)
        {
            Collider target = _colliders[i];

            if (target != null)
                target.enabled = _colliderEnabledStates[i];
        }
    }
}
