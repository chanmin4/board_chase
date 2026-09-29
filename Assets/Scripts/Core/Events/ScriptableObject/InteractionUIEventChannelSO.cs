/*
 * Role hierarchy: Core > Events > ScriptableObject
 * Primary responsibility: Defines the InteractionUIEventChannelSO publish-subscribe contract to reduce direct system coupling.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine.Events;
using UnityEngine;

/// <summary>
/// This class is used for Events to toggle the interaction UI.
/// Example: Dispaly or hide the interaction UI via a bool and the interaction type from the enum via int
/// </summary>

[CreateAssetMenu(menuName = "Events/Toogle Interaction UI Event Channel")]
public class InteractionUIEventChannelSO : DescriptionBaseSO
{
	public UnityAction<bool, InteractionType> OnEventRaised;

	// Purpose: Publishes the event notification.
	public void RaiseEvent(bool state, InteractionType interactionType)
	{
		if (OnEventRaised != null)
			OnEventRaised.Invoke(state, interactionType);
	}
}

