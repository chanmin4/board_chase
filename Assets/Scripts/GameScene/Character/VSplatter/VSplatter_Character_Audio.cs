using UnityEngine;

public class VSplatter_Character_Audio : CharacterAudio
{
	[SerializeField] private AudioCueSO _dash;
	[SerializeField] private AudioCueSO _objectPickup;
	[SerializeField] private AudioCueSO _footsteps;
	[SerializeField] private AudioCueSO _getHit;
	[SerializeField] private AudioCueSO _die;
	[SerializeField] private AudioCueSO _shockwaveCharge;
	[SerializeField] private AudioCueSO _shockwaveRelease;
	[SerializeField] private AudioCueSO _shockwaveCancel;

	public void PlayDash() => PlayAudio(_dash, _audioConfig, transform.position);
	public void PlayFootstep() => PlayAudio(_footsteps, _audioConfig, transform.position);
	public void PlayObjectPickup() => PlayAudio(_objectPickup, _audioConfig, transform.position);
	public void PlayGetHit() => PlayAudio(_getHit, _audioConfig, transform.position);
	public void PlayDie() => PlayAudio(_die, _audioConfig, transform.position);
	public void PlayShockwaveCharge() => PlayAudio(_shockwaveCharge, _audioConfig, transform.position);
	public void PlayShockwaveRelease() => PlayAudio(_shockwaveRelease, _audioConfig, transform.position);
	public void PlayShockwaveCancel() => PlayAudio(_shockwaveCancel, _audioConfig, transform.position);
}
