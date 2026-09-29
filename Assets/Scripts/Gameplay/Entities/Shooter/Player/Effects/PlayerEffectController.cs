/*
 * Role hierarchy: Gameplay > Entities > Shooter > Player > Effects
 * Primary responsibility: Coordinates the PlayerEffectController subsystem lifecycle and collaborating objects.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System.Collections;
using UnityEngine;

/// <summary>
/// Controls playback of particles connected to movement. Methods invoked by the StateMachine StateActions
/// </summary>
public class PlayerEffectController : PlayerShooterEffectController
{
	private void Start()
	{
	}
	/*
	public void EnableWalkParticles()
	{
		_walkingParticles.Play();
	}

	// Purpose: Disables walk particles for this component.
	public void DisableWalkParticles()
	{
		_walkingParticles.Stop();
	}
*/
}
