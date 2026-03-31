using UnityEngine;

public class VSplatter_Character_Audio : CharacterAudio
{
	[SerializeField] private AudioCueSO _dash;
	[SerializeField] private AudioCueSO _objectPickup;
	[SerializeField] private AudioCueSO _footsteps;
	[SerializeField] private AudioCueSO _getHit;
	[SerializeField] private AudioCueSO _die;

	public void PlayDash() => PlayAudio(_dash, _audioConfig, transform.position);
	public void PlayFootstep() => PlayAudio(_footsteps, _audioConfig, transform.position);
	public void PlayObjectPickup() => PlayAudio(_objectPickup, _audioConfig, transform.position);
	public void PlayGetHit() => PlayAudio(_getHit, _audioConfig, transform.position);
	public void PlayDie() => PlayAudio(_die, _audioConfig, transform.position);

}
