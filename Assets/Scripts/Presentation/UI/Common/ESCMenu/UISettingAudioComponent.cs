/*
 * Role hierarchy: Presentation > UI > Common > ESCMenu
 * Primary responsibility: Handles UISettingAudioComponent audio playback and sound-stimulus delivery.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using UnityEngine.Events;

public class UISettingAudioComponent : MonoBehaviour
{
    [SerializeField] private UISettingItemFiller _masterVolumeField;
    [SerializeField] private UISettingItemFiller _musicVolumeField;
    [SerializeField] private UISettingItemFiller _sfxVolumeField;

    [Header("Broadcasting")]
    [SerializeField] private FloatEventChannelSO _changemasterVolumeEventChannel;
    [SerializeField] private FloatEventChannelSO _changesFXVolumeEventChannel;
    [SerializeField] private FloatEventChannelSO _changemusicVolumeEventChannel;

    [SerializeField, Min(1)] private int _maxVolume = 10;

    public event UnityAction<float, float, float> _save = delegate { };

    private float _musicVolume;
    private float _sfxVolume;
    private float _masterVolume;

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    private void OnEnable()
    {
        if (_musicVolumeField != null)
        {
            _musicVolumeField.OnNextOption += IncreaseMusicVolume;
            _musicVolumeField.OnPreviousOption += DecreaseMusicVolume;
        }

        if (_sfxVolumeField != null)
        {
            _sfxVolumeField.OnNextOption += IncreaseSFXVolume;
            _sfxVolumeField.OnPreviousOption += DecreaseSFXVolume;
        }

        if (_masterVolumeField != null)
        {
            _masterVolumeField.OnNextOption += IncreaseMasterVolume;
            _masterVolumeField.OnPreviousOption += DecreaseMasterVolume;
        }
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        if (_musicVolumeField != null)
        {
            _musicVolumeField.OnNextOption -= IncreaseMusicVolume;
            _musicVolumeField.OnPreviousOption -= DecreaseMusicVolume;
        }

        if (_sfxVolumeField != null)
        {
            _sfxVolumeField.OnNextOption -= IncreaseSFXVolume;
            _sfxVolumeField.OnPreviousOption -= DecreaseSFXVolume;
        }

        if (_masterVolumeField != null)
        {
            _masterVolumeField.OnNextOption -= IncreaseMasterVolume;
            _masterVolumeField.OnPreviousOption -= DecreaseMasterVolume;
        }
    }

    // Purpose: Updates the setup state.
    public void Setup(float musicVolume, float sfxVolume, float masterVolume)
    {
        _musicVolume = Mathf.Clamp01(musicVolume);
        _sfxVolume = Mathf.Clamp01(sfxVolume);
        _masterVolume = Mathf.Clamp01(masterVolume);

        RefreshAllFields();
        BroadcastAllVolumes();
    }

    // Purpose: Performs the increase master volume workflow for this component.
    private void IncreaseMasterVolume()
    {
        _masterVolume = Mathf.Clamp01(_masterVolume + 1f / _maxVolume);
        RefreshMasterVolumeField();
        BroadcastMasterVolume();
        SaveCurrentVolumes();
    }

    // Purpose: Performs the decrease master volume workflow for this component.
    private void DecreaseMasterVolume()
    {
        _masterVolume = Mathf.Clamp01(_masterVolume - 1f / _maxVolume);
        RefreshMasterVolumeField();
        BroadcastMasterVolume();
        SaveCurrentVolumes();
    }

    // Purpose: Performs the increase music volume workflow for this component.
    private void IncreaseMusicVolume()
    {
        _musicVolume = Mathf.Clamp01(_musicVolume + 1f / _maxVolume);
        RefreshMusicVolumeField();
        BroadcastMusicVolume();
        SaveCurrentVolumes();
    }

    // Purpose: Performs the decrease music volume workflow for this component.
    private void DecreaseMusicVolume()
    {
        _musicVolume = Mathf.Clamp01(_musicVolume - 1f / _maxVolume);
        RefreshMusicVolumeField();
        BroadcastMusicVolume();
        SaveCurrentVolumes();
    }

    // Purpose: Performs the increase sfx volume workflow for this component.
    private void IncreaseSFXVolume()
    {
        _sfxVolume = Mathf.Clamp01(_sfxVolume + 1f / _maxVolume);
        RefreshSfxVolumeField();
        BroadcastSfxVolume();
        SaveCurrentVolumes();
    }

    // Purpose: Performs the decrease sfx volume workflow for this component.
    private void DecreaseSFXVolume()
    {
        _sfxVolume = Mathf.Clamp01(_sfxVolume - 1f / _maxVolume);
        RefreshSfxVolumeField();
        BroadcastSfxVolume();
        SaveCurrentVolumes();
    }

    // Purpose: Refreshes the displayed or cached all fields.
    private void RefreshAllFields()
    {
        RefreshMasterVolumeField();
        RefreshMusicVolumeField();
        RefreshSfxVolumeField();
    }

    // Purpose: Refreshes the displayed or cached master volume field.
    private void RefreshMasterVolumeField()
    {
        RefreshVolumeField(_masterVolumeField, _masterVolume);
    }

    // Purpose: Refreshes the displayed or cached music volume field.
    private void RefreshMusicVolumeField()
    {
        RefreshVolumeField(_musicVolumeField, _musicVolume);
    }

    // Purpose: Refreshes the displayed or cached sfx volume field.
    private void RefreshSfxVolumeField()
    {
        RefreshVolumeField(_sfxVolumeField, _sfxVolume);
    }

    // Purpose: Refreshes the displayed or cached volume field.
    private void RefreshVolumeField(UISettingItemFiller field, float value)
    {
        if (field == null)
            return;

        int selectedIndex = Mathf.RoundToInt(_maxVolume * value);
        field.FillSettingField(_maxVolume + 1, selectedIndex, selectedIndex.ToString());
    }

    // Purpose: Publishes the all volumes notification.
    private void BroadcastAllVolumes()
    {
        BroadcastMasterVolume();
        BroadcastMusicVolume();
        BroadcastSfxVolume();
    }

    // Purpose: Publishes the master volume notification.
    private void BroadcastMasterVolume()
    {
        if (_changemasterVolumeEventChannel != null)
            _changemasterVolumeEventChannel.RaiseEvent(_masterVolume);
    }

    // Purpose: Publishes the music volume notification.
    private void BroadcastMusicVolume()
    {
        if (_changemusicVolumeEventChannel != null)
            _changemusicVolumeEventChannel.RaiseEvent(_musicVolume);
    }

    // Purpose: Publishes the sfx volume notification.
    private void BroadcastSfxVolume()
    {
        if (_changesFXVolumeEventChannel != null)
            _changesFXVolumeEventChannel.RaiseEvent(_sfxVolume);
    }

    // Purpose: Saves the current current volumes state.
    private void SaveCurrentVolumes()
    {
        _save.Invoke(_musicVolume, _sfxVolume, _masterVolume);
    }
}
