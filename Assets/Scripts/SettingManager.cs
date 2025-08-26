using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using TMPro;
using UnityEngine.SceneManagement;

public class SettingManager : MonoBehaviour
{
    [Header("BGM")]
    public Slider bgmSlider;
    public Button bgmMuteButton;
    public Image bgmMuteIcon;
    public Sprite bgm_muteOnSprite;
    public Sprite bgm_muteOffSprite;

    [Header("SFX")]
    public Slider sfxSlider;
    public Button sfxMuteButton;
    public Image sfxMuteIcon;
    public Sprite sfx_muteOnSprite;
    public Sprite sfx_muteOffSprite;

    [Header("AudioMixer")]
    public AudioMixer audioMixer; // MasterMixer 연결

    [Header("resolution&fullscreen")]
    public Toggle fullscreenToggle;
    public TMP_Dropdown resolutionDropdown;

    [Header("language")]
    public TMP_Dropdown languageDropdown;

    private float bgmLastVolume = 0f;
    private float sfxLastVolume = 0f;

    private bool bgmMuted = false;
    private bool sfxMuted = false;
    public Button settingclose;
    public GameObject SettingPanel;
    Resolution[] resolutions;


    void Start()
    {
        // BGM
        bgmSlider.onValueChanged.AddListener(OnBGMVolumeChanged);
        bgmMuteButton.onClick.AddListener(OnBGMMuteToggle);
        audioMixer.GetFloat("BGMVol", out float currentBgmDB);
        bgmSlider.value = Mathf.Pow(10, currentBgmDB / 20f);

        // SFX
        sfxSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        sfxMuteButton.onClick.AddListener(OnSFXMuteToggle);
        audioMixer.GetFloat("SFXVol", out float currentSfxDB);
        sfxSlider.value = Mathf.Pow(10, currentSfxDB / 20f);

        fullscreenToggle.onValueChanged.AddListener(OnToggleFullscreen);

        resolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();
        foreach (Resolution res in resolutions)
        {
            resolutionDropdown.options.Add(new TMP_Dropdown.OptionData(res.width + " x " + res.height));
        }

        resolutionDropdown.onValueChanged.AddListener(OnResolutionChange);
        resolutionDropdown.value = System.Array.FindIndex(resolutions, r => r.width == Screen.currentResolution.width && r.height == Screen.currentResolution.height);

        languageDropdown.onValueChanged.AddListener(OnLanguageChange);
        SettingPanel.SetActive(false);
        settingclose.onClick.AddListener(() =>
        {
            SettingPanel.SetActive(false);
        });
        
        
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            bool isActive = SettingPanel.activeSelf;
            SettingPanel.SetActive(!isActive);
        }
    }

    // BGM
    void OnBGMVolumeChanged(float volume)
    {
        if (!bgmMuted)
        {
            float dB = Mathf.Log10(Mathf.Clamp(volume, 0.0001f, 1f)) * 20f;
            audioMixer.SetFloat("BGMVolume", dB);
            bgmLastVolume = dB;
        }
    }

    void OnBGMMuteToggle()
    {
        bgmMuted = !bgmMuted;
        if (bgmMuted)
        {
            audioMixer.GetFloat("BGMVolume", out bgmLastVolume);
            audioMixer.SetFloat("BGMVolume", -80f);
            bgmMuteIcon.sprite = bgm_muteOnSprite;
        }
        else
        {
            audioMixer.SetFloat("BGMVolume", bgmLastVolume);
            bgmMuteIcon.sprite = bgm_muteOffSprite;
        }

        float sliderValue = Mathf.Pow(10, bgmMuted ? -80f / 20f : bgmLastVolume / 20f);
        bgmSlider.value = sliderValue;
    }

    // SFX must changed to audio mix later  now settied on bgm
    void OnSFXVolumeChanged(float volume)
    {
        if (!sfxMuted)
        {
            float dB = Mathf.Log10(Mathf.Clamp(volume, 0.0001f, 1f)) * 20f;
            audioMixer.SetFloat("SFXVolume", dB);
            sfxLastVolume = dB;
        }
    }

    void OnSFXMuteToggle()
    {
        sfxMuted = !sfxMuted;
        if (sfxMuted)
        {
            audioMixer.GetFloat("SFXVolume", out sfxLastVolume);
            audioMixer.SetFloat("SFXVolume", -80f);
            sfxMuteIcon.sprite = sfx_muteOnSprite;
        }
        else
        {
            audioMixer.SetFloat("SFXVolume", sfxLastVolume);
            sfxMuteIcon.sprite = sfx_muteOffSprite;
        }

        float sliderValue = Mathf.Pow(10, sfxMuted ? -80f / 20f : sfxLastVolume / 20f);
        sfxSlider.value = sliderValue;
    }

    void OnToggleFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        Debug.Log("전체화면 설정됨: " + isFullscreen);
    }

    void OnResolutionChange(int index)
    {
        Resolution res = resolutions[index];
        Screen.SetResolution(res.width, res.height, Screen.fullScreen);
    }

    void OnLanguageChange(int index)
    {
        string selected = languageDropdown.options[index].text;

        if (selected == "Korean")
        {
            Debug.Log("Language: Korean selected");
            // LocalizationManager.SetLanguage("kr");
        }
        else if (selected == "English")
        {
            Debug.Log("Language: English selected");
            // LocalizationManager.SetLanguage("en");
        }
    }
}
