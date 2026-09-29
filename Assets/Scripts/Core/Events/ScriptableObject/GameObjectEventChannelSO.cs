/*
 * Role hierarchy: Core > Events > ScriptableObject
 * Primary responsibility: Defines the GameObjectEventChannelSO publish-subscribe contract to reduce direct system coupling.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine.Events;
using UnityEngine;

/// <summary>
/// This class is used for Events that have one gameobject argument.
/// Example: A game object pick up event event, where the GameObject is the object we are interacting with.
/// </summary>

[CreateAssetMenu(menuName = "Events/GameObject Event Channel")]
public class GameObjectEventChannelSO : DescriptionBaseSO
{
	public UnityAction<GameObject> OnEventRaised;
	
	// Purpose: Publishes the event notification.
	public void RaiseEvent(GameObject value)
	{
		if (OnEventRaised != null)
			OnEventRaised.Invoke(value);
	}
}
