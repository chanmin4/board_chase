/*
 * Role hierarchy: Core > Events > ScriptableObject
 * Primary responsibility: Defines the TransformEventChannelSO publish-subscribe contract to reduce direct system coupling.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine.Events;
using UnityEngine;

/// <summary>
/// This class is used for Events that have one transform argument.
/// Example: Spawn system initializes player and fire event, where the transform is the position of player.
/// </summary>

[CreateAssetMenu(menuName = "Events/Transform Event Channel")]
public class TransformEventChannelSO : DescriptionBaseSO
{
	public UnityAction<Transform> OnEventRaised;

	// Purpose: Publishes the event notification.
	public void RaiseEvent(Transform value)
	{
		if (OnEventRaised != null)
			OnEventRaised.Invoke(value);
	}
}
