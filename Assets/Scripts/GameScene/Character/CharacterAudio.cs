using UnityEngine;

public class CharacterAudio : MonoBehaviour
{
    [SerializeField] protected AudioCueEventChannelSO _sfxEventChannel = default;
	[SerializeField] protected AudioConfigurationSO _audioConfig = default;
	[SerializeField] protected GameStateSO _gameState = default;
	
	protected void PlayAudio(AudioCueSO audioCue, AudioConfigurationSO audioConfiguration, Vector3 positionInSpace = default)
	{
		if (!CanPlayAudio(audioCue, audioConfiguration))
			return;

		_sfxEventChannel.RaisePlayEvent(audioCue, audioConfiguration, positionInSpace);
	}

	private bool CanPlayAudio(AudioCueSO audioCue, AudioConfigurationSO audioConfiguration)
	{
		if (!isActiveAndEnabled)
			return false;

		if (audioCue == null)
			return false;

		if (audioConfiguration == null)
			return false;

		if (_sfxEventChannel == null)
			return false;

		if (_gameState != null && _gameState.CurrentGameState == GameState.Cutscene)
			return false;

		return true;
	}
}
