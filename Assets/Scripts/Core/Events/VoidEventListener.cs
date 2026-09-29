/*
 * Role hierarchy: Core > Events
 * Primary responsibility: Implements the focused VoidEventListener responsibility and collaborates with its role group.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// A flexible handler for void events in the form of a MonoBehaviour. Responses can be connected directly from the Unity Inspector.
/// </summary>
public class VoidEventListener : MonoBehaviour
{
	[SerializeField] private VoidEventChannelSO _channel = default;

	public UnityEvent OnEventRaised;

	// Purpose: Activates runtime subscriptions and refreshes the enabled state.
	private void OnEnable()
	{
		if (_channel != null)
			_channel.OnEventRaised += Respond;
	}

	// Purpose: Releases active subscriptions and transient state while disabled.
	private void OnDisable()
	{
		if (_channel != null)
			_channel.OnEventRaised -= Respond;
	}

	// Purpose: Performs the respond workflow for this component.
	private void Respond()
	{
		if (OnEventRaised != null)
			OnEventRaised.Invoke();
	}
}
