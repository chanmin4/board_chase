/*
 * Role hierarchy: Core > Events > ScriptableObject
 * Primary responsibility: Defines the ItemEventChannelSO publish-subscribe contract to reduce direct system coupling.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine.Events;
using UnityEngine;

/// <summary>
/// This class is used for Item interaction events.
/// Example: Pick up an item passed as paramater
/// </summary>

[CreateAssetMenu(menuName = "Events/UI/Item Event Channel")]
public class ItemEventChannelSO : DescriptionBaseSO
{
	public UnityAction<ItemSO> OnEventRaised;
	
	// Purpose: Publishes the event notification.
	public void RaiseEvent(ItemSO item)
	{
		if (OnEventRaised != null)
			OnEventRaised.Invoke(item);
	}
}

