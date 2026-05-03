using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class RadialCooldownWidget : MonoBehaviour
{
    [Header("Events")]
    [SerializeField] private CooldownSnapshotEventChannelSO _cooldownChangedChannel;
    [SerializeField] private VoidEventChannelSO _requestCooldownSnapshotChannel;

    [Header("Settings")]
    [SerializeField] private CooldownWidgetSettingsSO _settings;

    [Header("UI")]
    [SerializeField] private Image _iconImage;
    [SerializeField] private Image _radialFill;
    [SerializeField] private TextMeshProUGUI _cooldownText;

    private void Awake()
    {
        ConfigureRadialFill();
        ApplyDefaultState();
    }

    private void OnEnable()
    {
        if (_cooldownChangedChannel != null)
            _cooldownChangedChannel.OnEventRaised += OnCooldownChanged;

        if (_requestCooldownSnapshotChannel != null)
            _requestCooldownSnapshotChannel.RaiseEvent();
        else
            ApplyDefaultState();
    }

    private void OnDisable()
    {
        if (_cooldownChangedChannel != null)
            _cooldownChangedChannel.OnEventRaised -= OnCooldownChanged;
    }

    private void OnCooldownChanged(CooldownSnapshot snapshot)
    {
        Apply(snapshot);
    }

    private void Apply(CooldownSnapshot snapshot)
    {
        bool isReady = snapshot.isReady || snapshot.remainingSeconds <= 0.0001f;

        if (_iconImage != null)
        {
            _iconImage.sprite = _settings != null ? _settings.Icon : null;
            _iconImage.enabled = _iconImage.sprite != null;
            _iconImage.color = _settings != null ? _settings.IconColor : Color.white;
        }

        if (_radialFill != null)
        {
            _radialFill.color = isReady
                ? (_settings != null ? _settings.ReadyFillColor : Color.white)
                : (_settings != null ? _settings.CooldownFillColor : Color.white);

            _radialFill.fillAmount = isReady ? 0f : snapshot.Normalized01;

            bool hideWhenReady = _settings != null && _settings.HideFillWhenReady;
            _radialFill.enabled = !isReady || !hideWhenReady;
        }

        if (_cooldownText != null)
        {
            if (isReady)
            {
                if (_settings != null && _settings.ShowReadyText)
                    _cooldownText.text = _settings.ReadyText;
                else
                    _cooldownText.text = string.Empty;
            }
            else
            {
                string format = _settings != null ? _settings.CooldownFormat : "0.0";
                _cooldownText.text = snapshot.remainingSeconds.ToString(format);
            }
        }
    }

    private void ConfigureRadialFill()
    {
        if (_radialFill == null)
            return;

        _radialFill.type = Image.Type.Filled;
        _radialFill.fillMethod = Image.FillMethod.Radial360;
        _radialFill.fillOrigin = (int)Image.Origin360.Top; // 12시
        _radialFill.fillClockwise = true;
    }

    private void ApplyDefaultState()
    {
        Apply(new CooldownSnapshot(true, false, 0f, 1f));
    }
}
