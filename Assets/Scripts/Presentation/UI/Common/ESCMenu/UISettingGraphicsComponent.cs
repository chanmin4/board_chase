/*
 * Role hierarchy: Presentation > UI > Common > ESCMenu
 * Primary responsibility: Implements the focused UISettingGraphicsComponent responsibility and collaborates with its role group.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using UnityEngine.Events;

public class UISettingGraphicsComponent : MonoBehaviour
{
    private static readonly string[] VSyncOptionEntryKeys =
    {
        "Settings_Option_Off",
        "Settings_Option_On"
    };

    [SerializeField] private UISettingItemFiller _vSyncField;
    [SerializeField] private UISettingItemFiller _frameRateLimitField;

    [SerializeField] private int[] _frameRateOptions = { 30, 60, 90, 120, 144, 165, 240 };

    public event UnityAction<bool, int> SaveRequested = delegate { };

    private bool _useVSync;
    private int _frameRateIndex;

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
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

    // Purpose: Releases active subscriptions and transient state while disabled.
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

    // Purpose: Updates the setup state.
    public void Setup(SettingsSO settings)
    {
        _useVSync = settings.UseVSync;
        _frameRateIndex = FindFrameRateIndex(settings.TargetFrameRate);

        ApplyGraphics();
        RefreshFields();
    }

    // Purpose: Enables v sync for this component.
    private void EnableVSync()
    {
        _useVSync = true;
        ApplyGraphics();
        RefreshFields();
        SaveCurrentSettings();
    }

    // Purpose: Disables v sync for this component.
    private void DisableVSync()
    {
        _useVSync = false;
        ApplyGraphics();
        RefreshFields();
        SaveCurrentSettings();
    }

    // Purpose: Performs the next frame rate workflow for this component.
    private void NextFrameRate()
    {
        _frameRateIndex = Mathf.Clamp(_frameRateIndex + 1, 0, _frameRateOptions.Length - 1);
        ApplyGraphics();
        RefreshFields();
        SaveCurrentSettings();
    }

    // Purpose: Performs the previous frame rate workflow for this component.
    private void PreviousFrameRate()
    {
        _frameRateIndex = Mathf.Clamp(_frameRateIndex - 1, 0, _frameRateOptions.Length - 1);
        ApplyGraphics();
        RefreshFields();
        SaveCurrentSettings();
    }

    // Purpose: Applies the requested graphics change.
    private void ApplyGraphics()
    {
        QualitySettings.vSyncCount = _useVSync ? 1 : 0;
        Application.targetFrameRate = _useVSync ? -1 : _frameRateOptions[_frameRateIndex];
    }

    // Purpose: Refreshes the displayed or cached fields.
    private void RefreshFields()
    {
        if (_vSyncField != null)
        {
            _vSyncField.FillLocalizedDropdown(
                VSyncOptionEntryKeys,
                _useVSync ? 1 : 0);
        }

        if (_frameRateLimitField != null)
        {
            _frameRateLimitField.FillSettingField(
                _frameRateOptions.Length,
                _frameRateIndex,
                _frameRateOptions[_frameRateIndex].ToString());
        }
    }

    // Purpose: Saves the current current settings state.
    private void SaveCurrentSettings()
    {
        SaveRequested.Invoke(_useVSync, _frameRateOptions[_frameRateIndex]);
    }

    // Purpose: Finds the requested frame rate index.
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
