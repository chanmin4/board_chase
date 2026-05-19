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

    public void Setup(float musicVolume, float sfxVolume, float masterVolume)
    {
        _musicVolume = Mathf.Clamp01(musicVolume);
        _sfxVolume = Mathf.Clamp01(sfxVolume);
        _masterVolume = Mathf.Clamp01(masterVolume);

        RefreshAllFields();
        BroadcastAllVolumes();
    }

    private void IncreaseMasterVolume()
    {
        _masterVolume = Mathf.Clamp01(_masterVolume + 1f / _maxVolume);
        RefreshMasterVolumeField();
        BroadcastMasterVolume();
        SaveCurrentVolumes();
    }

    private void DecreaseMasterVolume()
    {
        _masterVolume = Mathf.Clamp01(_masterVolume - 1f / _maxVolume);
        RefreshMasterVolumeField();
        BroadcastMasterVolume();
        SaveCurrentVolumes();
    }

    private void IncreaseMusicVolume()
    {
        _musicVolume = Mathf.Clamp01(_musicVolume + 1f / _maxVolume);
        RefreshMusicVolumeField();
        BroadcastMusicVolume();
        SaveCurrentVolumes();
    }

    private void DecreaseMusicVolume()
    {
        _musicVolume = Mathf.Clamp01(_musicVolume - 1f / _maxVolume);
        RefreshMusicVolumeField();
        BroadcastMusicVolume();
        SaveCurrentVolumes();
    }

    private void IncreaseSFXVolume()
    {
        _sfxVolume = Mathf.Clamp01(_sfxVolume + 1f / _maxVolume);
        RefreshSfxVolumeField();
        BroadcastSfxVolume();
        SaveCurrentVolumes();
    }

    private void DecreaseSFXVolume()
    {
        _sfxVolume = Mathf.Clamp01(_sfxVolume - 1f / _maxVolume);
        RefreshSfxVolumeField();
        BroadcastSfxVolume();
        SaveCurrentVolumes();
    }

    private void RefreshAllFields()
    {
        RefreshMasterVolumeField();
        RefreshMusicVolumeField();
        RefreshSfxVolumeField();
    }

    private void RefreshMasterVolumeField()
    {
        RefreshVolumeField(_masterVolumeField, _masterVolume);
    }

    private void RefreshMusicVolumeField()
    {
        RefreshVolumeField(_musicVolumeField, _musicVolume);
    }

    private void RefreshSfxVolumeField()
    {
        RefreshVolumeField(_sfxVolumeField, _sfxVolume);
    }

    private void RefreshVolumeField(UISettingItemFiller field, float value)
    {
        if (field == null)
            return;

        int selectedIndex = Mathf.RoundToInt(_maxVolume * value);
        field.FillSettingField(_maxVolume + 1, selectedIndex, selectedIndex.ToString());
    }

    private void BroadcastAllVolumes()
    {
        BroadcastMasterVolume();
        BroadcastMusicVolume();
        BroadcastSfxVolume();
    }

    private void BroadcastMasterVolume()
    {
        if (_changemasterVolumeEventChannel != null)
            _changemasterVolumeEventChannel.RaiseEvent(_masterVolume);
    }

    private void BroadcastMusicVolume()
    {
        if (_changemusicVolumeEventChannel != null)
            _changemusicVolumeEventChannel.RaiseEvent(_musicVolume);
    }

    private void BroadcastSfxVolume()
    {
        if (_changesFXVolumeEventChannel != null)
            _changesFXVolumeEventChannel.RaiseEvent(_sfxVolume);
    }

    private void SaveCurrentVolumes()
    {
        _save.Invoke(_musicVolume, _sfxVolume, _masterVolume);
    }
}