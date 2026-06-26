using UnityEngine;

[DisallowMultipleComponent]
public class PlayerSoundDirectionIndicatorUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private SoundStimulusEventChannelSO _soundStimulusEvent;
    [SerializeField] private Transform _player;
    [SerializeField] private RectTransform _indicator;
    [SerializeField] private CanvasGroup _indicatorGroup;

    [Header("Display")]
    [SerializeField, Min(0f)] private float _innerMuteRadius = 12f;
    [SerializeField, Min(1f)] private float _screenRadius = 180f;
    [SerializeField, Min(0.01f)] private float _visibleSeconds = 0.6f;
    [SerializeField] private bool _ignoreOwnSounds = true;

    private float _hideTime;

    private void Reset()
    {
        if (_indicator == null)
            _indicator = transform as RectTransform;

        if (_indicatorGroup == null)
            _indicatorGroup = GetComponent<CanvasGroup>();
    }

    private void OnEnable()
    {
        if (_soundStimulusEvent != null)
            _soundStimulusEvent.OnEventRaised += OnSoundStimulus;

        Hide();
    }

    private void OnDisable()
    {
        if (_soundStimulusEvent != null)
            _soundStimulusEvent.OnEventRaised -= OnSoundStimulus;
    }

    private void Update()
    {
        if (_indicatorGroup == null)
            return;

        if (Time.unscaledTime >= _hideTime)
            Hide();
    }

    private void OnSoundStimulus(SoundStimulus stimulus)
    {
        if (!stimulus.IsValid || _player == null || _indicator == null)
            return;

        if (_ignoreOwnSounds && stimulus.source != null && stimulus.source.transform.IsChildOf(_player))
            return;

        Vector3 delta = stimulus.position - _player.position;
        delta.y = 0f;

        float distance = delta.magnitude;
        if (distance <= _innerMuteRadius || distance > stimulus.radius)
            return;

        Vector2 direction = new Vector2(delta.x, delta.z).normalized;
        _indicator.anchoredPosition = direction * _screenRadius;

        float angle = Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;
        _indicator.localRotation = Quaternion.Euler(0f, 0f, -angle);

        if (_indicatorGroup != null)
        {
            _indicatorGroup.alpha = Mathf.Clamp01(stimulus.intensity);
            _indicatorGroup.interactable = false;
            _indicatorGroup.blocksRaycasts = false;
        }

        _hideTime = Time.unscaledTime + _visibleSeconds;
    }

    private void Hide()
    {
        if (_indicatorGroup != null)
            _indicatorGroup.alpha = 0f;
    }
}
