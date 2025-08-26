using UnityEngine;
using UnityEngine.UI;

public class VolumeSettingsUI : MonoBehaviour
{
    [Header("Sliders (0~1)")]
    public Slider master;
    public Slider music;
    public Slider sfx;

    void Start()
    {
        // 현재 값으로 초기화(저장된 값은 AudioMaster가 불러옴)
        float m = PlayerPrefs.GetFloat("vol_master", 0.8f);
        float mu= PlayerPrefs.GetFloat("vol_music" , 0.8f);
        float s = PlayerPrefs.GetFloat("vol_sfx"   , 0.8f);

        if (master){ master.value = m; master.onValueChanged.AddListener(AudioMaster.I.SetMasterVolume); }
        if (music ){ music .value = mu; music .onValueChanged.AddListener(AudioMaster.I.SetMusicVolume ); }
        if (sfx   ){ sfx   .value = s; sfx   .onValueChanged.AddListener(AudioMaster.I.SetSfxVolume   ); }
    }
}
