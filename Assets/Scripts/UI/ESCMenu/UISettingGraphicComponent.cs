using UnityEngine;
using UnityEngine.Events;

public class UISettingGraphicsComponent : MonoBehaviour
{
    [SerializeField] private UISettingItemFiller _vSyncField;
    [SerializeField] private UISettingItemFiller _frameRateLimitField;

    [SerializeField] private int[] _frameRateOptions = { 30, 60, 90, 120, 144, 165, 240 };

    public event UnityAction<bool, int> SaveRequested = delegate { };

    private bool _useVSync;
    private int _frameRateIndex;

    private void OnEnable()
    {
        if (_vSyncField != null)
        {
            _vSyncField.OnNextOption += EnableVSync;
            _vSyncField.OnPreviousOption += DisableVSync;
        }

        if (_frameRateLimitField != null)
        {
            _frameRateLimitField.OnNextOption += NextFrameRate;
            _frameRateLimitField.OnPreviousOption += PreviousFrameRate;
        }
    }

    private void OnDisable()
    {
        if (_vSyncField != null)
        {
            _vSyncField.OnNextOption -= EnableVSync;
            _vSyncField.OnPreviousOption -= DisableVSync;
        }

        if (_frameRateLimitField != null)
        {
            _frameRateLimitField.OnNextOption -= NextFrameRate;
            _frameRateLimitField.OnPreviousOption -= PreviousFrameRate;
        }
    }

    public void Setup(SettingsSO settings)
    {
        _useVSync = settings.UseVSync;
        _frameRateIndex = FindFrameRateIndex(settings.TargetFrameRate);

        ApplyGraphics();
        RefreshFields();
    }

    private void EnableVSync()
    {
        _useVSync = true;
        ApplyGraphics();
        RefreshFields();
        SaveCurrentSettings();
    }

    private void DisableVSync()
    {
        _useVSync = false;
        ApplyGraphics();
        RefreshFields();
        SaveCurrentSettings();
    }

    private void NextFrameRate()
    {
        _frameRateIndex = Mathf.Clamp(_frameRateIndex + 1, 0, _frameRateOptions.Length - 1);
        ApplyGraphics();
        RefreshFields();
        SaveCurrentSettings();
    }

    private void PreviousFrameRate()
    {
        _frameRateIndex = Mathf.Clamp(_frameRateIndex - 1, 0, _frameRateOptions.Length - 1);
        ApplyGraphics();
        RefreshFields();
        SaveCurrentSettings();
    }

    private void ApplyGraphics()
    {
        QualitySettings.vSyncCount = _useVSync ? 1 : 0;
        Application.targetFrameRate = _useVSync ? -1 : _frameRateOptions[_frameRateIndex];
    }

    private void RefreshFields()
    {
        if (_vSyncField != null)
        {
            _vSyncField.FillSettingField_Localized(
                2,
                _useVSync ? 1 : 0,
                _useVSync ? "On" : "Off");
        }

        if (_frameRateLimitField != null)
        {
            _frameRateLimitField.FillSettingField(
                _frameRateOptions.Length,
                _frameRateIndex,
                _frameRateOptions[_frameRateIndex].ToString());
        }
    }

    private void SaveCurrentSettings()
    {
        SaveRequested.Invoke(_useVSync, _frameRateOptions[_frameRateIndex]);
    }

    private int FindFrameRateIndex(int frameRate)
    {
        for (int i = 0; i < _frameRateOptions.Length; i++)
        {
            if (_frameRateOptions[i] == frameRate)
                return i;
        }

        return Mathf.Clamp(1, 0, _frameRateOptions.Length - 1);
    }
}