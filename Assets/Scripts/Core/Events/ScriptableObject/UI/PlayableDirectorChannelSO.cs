/*
 * Role hierarchy: Core > Events > ScriptableObject > UI
 * Primary responsibility: Defines the PlayableDirectorChannelSO data asset used to configure runtime rules in the Inspector.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;

[CreateAssetMenu(menuName = "Events/Playable Director Channel")]
public class PlayableDirectorChannelSO : DescriptionBaseSO
{
	public UnityAction<PlayableDirector> OnEventRaised;
	
	// Purpose: Publishes the event notification.
	public void RaiseEvent(PlayableDirector playable)
	{
		if (OnEventRaised != null)
			OnEventRaised.Invoke(playable);
	}
}
