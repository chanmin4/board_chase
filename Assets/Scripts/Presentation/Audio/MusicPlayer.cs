/*
 * Role hierarchy: Presentation > Audio
 * Primary responsibility: Handles MusicPlayer audio playback and sound-stimulus delivery.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

public class MusicPlayer : MonoBehaviour
{
	[SerializeField] private VoidEventChannelSO _onSceneReady = default;
	[SerializeField] private AudioCueEventChannelSO _playMusicOn = default;
	[SerializeField] private GameSceneSO _thisSceneSO = default;
	[SerializeField] private AudioConfigurationSO _audioConfig = default;

	[Header("Pause menu music")]
	[SerializeField] private AudioCueSO _pauseMusic = default;
	[SerializeField] private BoolEventChannelSO _onPauseOpened = default; 

	// Purpose: Activates runtime subscriptions and refreshes the enabled state.
	private void OnEnable()
	{
		_onPauseOpened.OnEventRaised += PlayPauseMusic;
		_onSceneReady.OnEventRaised += PlayMusic;
	}

	// Purpose: Releases active subscriptions and transient state while disabled.
	private void OnDisable()
	{
		_onSceneReady.OnEventRaised -= PlayMusic;
		_onPauseOpened.OnEventRaised -= PlayPauseMusic;
	}

	// Purpose: Plays the requested music action or effect.
	private void PlayMusic()
	{
		_playMusicOn.RaisePlayEvent(_thisSceneSO.musicTrack, _audioConfig);
	}

	// Purpose: Plays the requested pause music action or effect.
	private void PlayPauseMusic(bool open)
	{
		if (open)
			_playMusicOn.RaisePlayEvent(_pauseMusic, _audioConfig);
		else
			PlayMusic();
	}
}
