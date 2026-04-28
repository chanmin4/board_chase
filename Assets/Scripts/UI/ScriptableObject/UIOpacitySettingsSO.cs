using UnityEngine;

[CreateAssetMenu(fileName = "UIOpacitySettings", menuName = "Game/UI/UI Opacity Settings")]
public class UIOpacitySettingsSO : ScriptableObject
{
    [Header("Alpha")]
    [SerializeField, Range(0f, 1f)] private float _defaultAlpha = 0.6f;
    [SerializeField, Range(0f, 1f)] private float _hiddenAlpha = 0f;
    [SerializeField, Range(0f, 1f)] private float _emphasisAlpha = 1f;
    [SerializeField, Range(0f, 1f)] private float _minAlpha = 0f;
    [SerializeField, Range(0f, 1f)] private float _maxAlpha = 1f;

    [Header("Fade")]
    [SerializeField, Min(0f)] private float _fadeInDuration = 0.12f;
    [SerializeField, Min(0f)] private float _fadeOutDuration = 0.2f;

    public float DefaultAlpha => Mathf.Clamp(_defaultAlpha, MinAlpha, MaxAlpha);
    public float HiddenAlpha => Mathf.Clamp(_hiddenAlpha, MinAlpha, MaxAlpha);
    public float EmphasisAlpha => Mathf.Clamp(_emphasisAlpha, MinAlpha, MaxAlpha);
    public float MinAlpha => Mathf.Min(_minAlpha, _maxAlpha);
    public float MaxAlpha => Mathf.Max(_minAlpha, _maxAlpha);
    public float FadeInDuration => _fadeInDuration;
    public float FadeOutDuration => _fadeOutDuration;
}
