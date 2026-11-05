/*
 * Role hierarchy: Gameplay > Entities > Common > Core
 * Primary responsibility: Implements the focused BoolEvent responsibility and collaborates with its role group.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class BoolEvent : UnityEvent<bool, GameObject> { }

/// <summary>
/// A generic class for a "zone", that is a trigger collider that can detect if an object of a certain type (layer) entered or exited it.
/// Implements <code>OnTriggerEnter</code> and <code>OnTriggerExit</code> so it needs to be on the same object that holds the Collider.
/// </summary>
public class ZoneTriggerController : MonoBehaviour
{
	[SerializeField] private BoolEvent _enterZone = default;
	[SerializeField] private LayerMask _layers = default;
    private Collider _zoneCollider;
    private readonly List<Collider> _sourceColliders = new();

    /// <summary>Revalidates the actual detector geometry, not a remembered Enter event.</summary>
    public bool IsObjectInZone(GameObject source)
    {
        if (source == null || !source.activeInHierarchy || !isActiveAndEnabled) return false;
        if (_zoneCollider == null) _zoneCollider = GetComponent<Collider>();
        if (_zoneCollider == null || !_zoneCollider.enabled || !_zoneCollider.isTrigger) return false;
        if ((_layers.value & (1 << source.layer)) == 0 ||
            Physics.GetIgnoreLayerCollision(gameObject.layer, source.layer)) return false;
        // Query only the exact GameObject supplied by the trigger callback.
        // Do not search an NPC's children for distant/disabled interaction shapes.
        _sourceColliders.Clear();
        source.GetComponents(_sourceColliders);
        foreach (Collider other in _sourceColliders)
        {
            if (other == null || other == _zoneCollider || !other.enabled ||
                Physics.GetIgnoreCollision(_zoneCollider, other)) continue;
            if (Physics.ComputePenetration(_zoneCollider, _zoneCollider.transform.position,
                _zoneCollider.transform.rotation, other, other.transform.position,
                other.transform.rotation, out _, out _)) return true;
        }
        return false;
    }

	// Purpose: Responds to the trigger enter callback.
	private void OnTriggerEnter(Collider other)
	{

		if ((1 << other.gameObject.layer & _layers) != 0)
		{
			//Debug.Log("[ZoneTriggerController] Detected object entering zone. obj=" + other.gameObject.name);
			_enterZone.Invoke(true, other.gameObject);
		}
	}

	// Purpose: Responds to the trigger exit callback.
	private void OnTriggerExit(Collider other)
	{
		if ((1 << other.gameObject.layer & _layers) != 0)
		{
			_enterZone.Invoke(false, other.gameObject);
		}
	}
}
