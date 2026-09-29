/*
 * Role hierarchy: Gameplay > Entities > Common > StateMachine > Actions
 * Primary responsibility: Defines PlayAudioCueActionSO state behavior or transitions that drive the runtime flow.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(menuName = "State Machines/Actions/Play AudioCue")]
public class PlayAudioCueActionSO : StateActionSO<PlayAudioCueAction>
{
	public AudioCueSO audioCue = default;
	public AudioCueEventChannelSO audioCueEventChannel = default;
	public AudioConfigurationSO audioConfiguration = default;
}

public class PlayAudioCueAction : StateAction
{
	private Transform _stateMachineTransform;

	private PlayAudioCueActionSO _originSO => (PlayAudioCueActionSO)base.OriginSO; // The SO this StateAction spawned from

	// Purpose: Caches the component references required before runtime initialization.
	public override void Awake(StateMachine stateMachine)
	{
		_stateMachineTransform = stateMachine.transform;
	}

	// Purpose: Responds to the update callback.
	public override void OnUpdate() { }

	// Purpose: Responds to the state enter callback.
	public override void OnStateEnter()
	{
		if (_originSO.audioCue == null)
			return;

		if (_originSO.audioCueEventChannel == null)
			return;

		if (_originSO.audioConfiguration == null)
			return;

		_originSO.audioCueEventChannel.RaisePlayEvent(_originSO.audioCue, _originSO.audioConfiguration, _stateMachineTransform.position);
	}
}
