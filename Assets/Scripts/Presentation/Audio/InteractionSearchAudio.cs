using UnityEngine;

/// <summary>Observes the existing loot cast bar; owns and stops only its own search loop.</summary>
[DisallowMultipleComponent]
public sealed class InteractionSearchAudio : MonoBehaviour
{
    [SerializeField] private InteractionCastBarEventChannelSO _castChannel;
    [SerializeField] private AudioCueEventChannelSO _sfxChannel;
    [SerializeField] private AudioConfigurationSO _configuration;
    [SerializeField] private AudioCueSO _startCue;
    [SerializeField] private AudioCueSO _loopCue;
    [SerializeField] private AudioCueSO _completeCue;
    [SerializeField] private AudioCueSO _cancelCue;
    private AudioCueKey _loopKey = AudioCueKey.Invalid;
    private Transform _actor;
    private Transform _anchor;
    private bool _searching;
    private bool _completed;
    private bool _paused;

    private void OnEnable()
    {
        if (_castChannel != null) _castChannel.OnEventRaised += HandleCast;
    }

    private void OnDisable()
    {
        if (_castChannel != null) _castChannel.OnEventRaised -= HandleCast;
        EndSearch(false);
    }

    private void Update()
    {
        if (!_searching) return;
        if (_actor == null || !_actor.gameObject.activeInHierarchy || _anchor == null)
        {
            EndSearch(false);
            return;
        }
        bool paused = Time.timeScale <= 0f;
        if (paused && !_paused) StopLoop();
        if (!paused && _paused && !_completed) StartLoop();
        _paused = paused;
    }

    private void HandleCast(InteractionCastBarSnapshot snapshot)
    {
        if (!snapshot.Visible || snapshot.InteractionType != InteractionType.Loot)
        {
            EndSearch(true);
            return;
        }
        if (snapshot.Actor == null) { EndSearch(false); return; }
        Transform anchor = snapshot.Anchor != null ? snapshot.Anchor : snapshot.Actor;
        if (!_searching || _actor != snapshot.Actor || _anchor != anchor)
        {
            EndSearch(false);
            _searching = true;
            _actor = snapshot.Actor;
            _anchor = anchor;
            _paused = Time.timeScale <= 0f;
            PlayOneShot(_startCue);
            if (!_paused) StartLoop();
        }
        if (snapshot.Progress01 >= 1f && !_completed)
        {
            _completed = true;
            StopLoop();
            PlayOneShot(_completeCue);
        }
    }

    private Vector3 Position => _anchor != null ? _anchor.position : Vector3.zero;

    private void StartLoop()
    {
        if (_loopKey.IsValid || _sfxChannel == null || _loopCue == null ||
            !_loopCue.looping || !_loopCue.HasPlayableClip) return;
        _loopKey = _sfxChannel.RaisePlayEvent(_loopCue, _configuration, Position);
    }

    private void StopLoop()
    {
        if (_loopKey.IsValid) _sfxChannel?.RaiseStopEvent(_loopKey);
        _loopKey = AudioCueKey.Invalid;
    }

    private void PlayOneShot(AudioCueSO cue)
    {
        if (_sfxChannel != null && cue != null && cue.HasPlayableClip && !cue.looping)
            _sfxChannel.RaisePlayEvent(cue, _configuration, Position);
    }

    private void EndSearch(bool playCancellation)
    {
        StopLoop();
        if (_searching && !_completed && playCancellation) PlayOneShot(_cancelCue);
        _searching = false;
        _completed = false;
        _paused = false;
        _actor = null;
        _anchor = null;
    }
}
