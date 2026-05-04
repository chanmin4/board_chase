using System.Collections;
using UnityEngine;

/// <summary>
/// Controls playback of particles connected to movement. Methods invoked by the StateMachine StateActions
/// </summary>
public class PlayerEffectController : MonoBehaviour
{
	[SerializeField] private ParticleSystem _dashParticles = default;


	private void Start()
	{
	}
	/*
	public void EnableWalkParticles()
	{
		_walkingParticles.Play();
	}

	public void DisableWalkParticles()
	{
		_walkingParticles.Stop();
	}
*/
	public void PlayDashParticles()
	{
		if (_dashParticles != null)
			_dashParticles.Play(true);
	}

	public void StopDashParticles()
	{
		if (_dashParticles != null)
			_dashParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
	}

	private IEnumerator ResetMinMaxCurve(ParticleSystem ps, ParticleSystem.MinMaxCurve curve)
	{
		while (ps.isEmitting)
		{
			yield return null;
		}

		ParticleSystem.MainModule main = ps.main;
		main.startSize = curve;
	}

}
