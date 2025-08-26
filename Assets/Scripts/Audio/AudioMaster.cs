using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using Random = UnityEngine.Random;

/// <summary>
/// 전역 오디오 허브: BGM/SFX/볼륨/키-카탈로그 재생을 한 곳에서 관리.
/// 씬 간 유지(DontDestroyOnLoad).
/// </summary>
public class AudioMaster : MonoBehaviour
{
    public static AudioMaster I { get; private set; }

    [Header("Mixer & Groups")]
    public AudioMixer mixer;
    public AudioMixerGroup BGMGroup;
    public AudioMixerGroup sfxGroup;

    [Header("BGM Clips (fallback)")]
    [Tooltip("카탈로그에 bgm.main / bgm.gameplay 키가 없을 때 사용하는 기본 BGM")]
    public AudioClip mainMenuBGM;
    public AudioClip gameplayBGM;

    [Header("BGM Options")]
    public float bgmFade = 0.8f;
    [Tooltip("씬 로드시 이름 규칙으로 BGM 자동 전환")]
    public bool autoSwitchBGMOnSceneLoaded = true;

    [Header("SFX Options")]
    public int sfxPoolSize = 12;        // 동시 재생 한도
    public float defaultSfxPitchJitter = 0.04f;

    [Header("Catalog")]
    public AudioCatalog catalog;         // 키 → (채널, AudioEvent) 매핑

    AudioSource _musicA, _musicB;
    bool _musicAActive = true;

    AudioSource[] _sfxPool;
    int _sfxIdx;

    // key(Concurrency) → 현재 재생 수
    readonly Dictionary<string, int> _concurrency = new Dictionary<string, int>();

    const string PP_MASTER = "vol_master";
    const string PP_MUSIC  = "vol_music";
    const string PP_SFX    = "vol_sfx";

    void Awake()
    {
        if (I && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);

        // BGM 듀얼 소스(크로스페이드용)
        _musicA = MakeSource("MusicA", BGMGroup, spatialBlend:0f, loop:true);
        _musicB = MakeSource("MusicB", BGMGroup, spatialBlend:0f, loop:true);

        // SFX 풀
        _sfxPool = new AudioSource[sfxPoolSize];
        for (int i = 0; i < sfxPoolSize; i++)
            _sfxPool[i] = MakeSource("SFX_" + i, sfxGroup, spatialBlend:1f, loop:false);

        // 볼륨 로드
        SetMasterVolume(PlayerPrefs.GetFloat(PP_MASTER, 0.8f));
        SetMusicVolume (PlayerPrefs.GetFloat(PP_MUSIC , 0.8f));
        SetSfxVolume   (PlayerPrefs.GetFloat(PP_SFX   , 0.8f));

        if (autoSwitchBGMOnSceneLoaded)
            SceneManager.sceneLoaded += OnSceneLoaded;
    }

    AudioSource MakeSource(string name, AudioMixerGroup grp, float spatialBlend, bool loop)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        var src = go.AddComponent<AudioSource>();
        src.outputAudioMixerGroup = grp;
        src.playOnAwake = false;
        src.loop = loop;
        src.spatialBlend = spatialBlend; // 0:2D(BGM), 1:3D(SFX)
        src.rolloffMode = AudioRolloffMode.Linear;
        src.maxDistance = 40f;
        return src;
    }

    void OnSceneLoaded(Scene s, LoadSceneMode mode)
    {
        string sn = s.name.ToLowerInvariant();
    }

    // ===================== BGM =====================
    public void PlayBGM(AudioClip clip)
    {
        if (!clip) return;

        var from = _musicAActive ? _musicA : _musicB;
        var to   = _musicAActive ? _musicB : _musicA;
        _musicAActive = !_musicAActive;

        to.outputAudioMixerGroup = BGMGroup;
        to.clip = clip;
        to.volume = 0f;
        if (!to.isPlaying) to.Play();

        StopAllCoroutines();
        StartCoroutine(FadeAB(from, to, bgmFade));
    }

    public void PlayBGMKey(string key)
    {
        if (!catalog || !catalog.TryGet(key, out var e) || e.ev == null)
        {
            Debug.LogWarning($"[Audio] BGM key not found: {key}");
            return;
        }
        PlayBGMEvent(e.ev);
    }

    public void PlayBGMEvent(AudioEvent ev)
    {
        if (!ev) return;
        var clip = ev.Pick();
        if (!clip) return;

        var from = _musicAActive ? _musicA : _musicB;
        var to   = _musicAActive ? _musicB : _musicA;
        _musicAActive = !_musicAActive;

        to.outputAudioMixerGroup = BGMGroup;
        to.clip = clip;
        // BGM은 보통 피치 랜덤 X, 볼륨은 이벤트 기본값 사용(최종은 믹서에서 제어)
        to.pitch = 1f;
        to.volume = Mathf.Clamp01(ev.volume);

        if (!to.isPlaying) to.Play();

        StopAllCoroutines();
        StartCoroutine(FadeAB(from, to, bgmFade));
    }

    IEnumerator FadeAB(AudioSource a, AudioSource b, float t)
    {
        float time = 0f;
        float a0 = a.volume, b0 = b.volume;
        while (time < t)
        {
            float k = time / t;
            a.volume = Mathf.Lerp(a0, 0f, k);
            b.volume = Mathf.Lerp(b0, 1f, k);
            time += Time.unscaledDeltaTime;
            yield return null;
        }
        a.volume = 0f; b.volume = 1f;
        a.Stop();
    }

    // ===================== SFX =====================
    public void PlaySFX2D(AudioClip clip, float vol = 1f, float pitchJitter = -1f)
    {
        if (!clip) return;
        var src = _sfxPool[_sfxIdx = (_sfxIdx + 1) % _sfxPool.Length];
        src.transform.position = Camera.main ? Camera.main.transform.position : Vector3.zero;
        src.spatialBlend = 0f;
        src.outputAudioMixerGroup = sfxGroup;
        src.pitch = 1f + ((pitchJitter < 0f ? defaultSfxPitchJitter : pitchJitter) * Random.Range(-1f, 1f));
        src.PlayOneShot(clip, vol);
        src.spatialBlend = 1f; // 복귀
    }

    public void PlaySFXAt(AudioClip clip, Vector3 worldPos, float vol = 1f, float pitchJitter = -1f)
    {
        if (!clip) return;
        var src = _sfxPool[_sfxIdx = (_sfxIdx + 1) % _sfxPool.Length];
        src.transform.position = worldPos;
        src.outputAudioMixerGroup = sfxGroup;
        src.pitch = 1f + ((pitchJitter < 0f ? defaultSfxPitchJitter : pitchJitter) * Random.Range(-1f, 1f));
        src.PlayOneShot(clip, vol);
    }

    // ===== 키/카탈로그 기반 재생 (유지보수 핵심) =====
    public void PlayKey(string key, Vector3? worldPos = null)
    {
        if (!catalog || !catalog.TryGet(key, out var e) || e.ev == null)
        {
            Debug.LogWarning($"[Audio] Key not found: {key}");
            return;
        }

        if (e.channel == AudioChannel.BGM) PlayBGMEvent(e.ev);
        else PlayEvent(e.ev, worldPos);
    }

    public void PlayEvent(AudioEvent ev, Vector3? worldPos = null)
    {
        if (!ev) return;

        // 1) 정책: 쿨다운/동시 제한
        if (ev.cooldown > 0f && Time.unscaledTime - ev.lastPlayTime < ev.cooldown) return;
        if (ev.playingCount >= Mathf.Max(1, ev.maxVoices)) return;

        if (!string.IsNullOrEmpty(ev.concurrencyKey))
        {
            if (!_concurrency.TryGetValue(ev.concurrencyKey, out var cur)) cur = 0;
            if (cur >= ev.maxVoices) return;
            _concurrency[ev.concurrencyKey] = cur + 1;
        }

        // 2) 소스
        var src = _sfxPool[_sfxIdx = (_sfxIdx + 1) % _sfxPool.Length];
        var clip = ev.Pick();
        if (!clip) return;

        // 3) 라우팅/3D
        src.outputAudioMixerGroup = sfxGroup;
        src.spatialBlend = ev.is3D ? 1f : 0f;
        src.maxDistance  = ev.maxDistance;

        // 4) 위치
        src.transform.position = worldPos ?? (Camera.main ? Camera.main.transform.position : Vector3.zero);

        // 5) 볼륨/피치 랜덤
        float vol = Mathf.Clamp01(ev.volume + Random.Range(ev.volumeJitter.x, ev.volumeJitter.y));
        src.pitch = 1f + Random.Range(ev.pitchJitter.x, ev.pitchJitter.y);

        // 6) 재생
        src.PlayOneShot(clip, vol);

        // 7) 상태 갱신
        ev.lastPlayTime = Time.unscaledTime;
        ev.playingCount++;
        StartCoroutine(ReturnVoice(ev));
    }

    IEnumerator ReturnVoice(AudioEvent ev)
    {
        // 대충 0.3초 후 카운트 회수(정밀하게 하려면 clip.length 추적으로 개선 가능)
        yield return new WaitForSecondsRealtime(0.3f);
        ev.playingCount = Mathf.Max(0, ev.playingCount - 1);
        if (!string.IsNullOrEmpty(ev.concurrencyKey))
            _concurrency[ev.concurrencyKey] = Mathf.Max(0, _concurrency[ev.concurrencyKey] - 1);
    }

    // ===================== Volume (0~1 linear) =====================
    public void SetMasterVolume(float v)
    {
        v = Mathf.Clamp01(v);
        if (mixer) mixer.SetFloat("MasterVol", LinearToDb(v));
        PlayerPrefs.SetFloat(PP_MASTER, v);
    }
    public void SetMusicVolume(float v)
    {
        v = Mathf.Clamp01(v);
        if (mixer) mixer.SetFloat("MusicVol", LinearToDb(v));
        PlayerPrefs.SetFloat(PP_MUSIC, v);
    }
    public void SetSfxVolume(float v)
    {
        v = Mathf.Clamp01(v);
        if (mixer) mixer.SetFloat("SFXVol", LinearToDb(v));
        PlayerPrefs.SetFloat(PP_SFX, v);
    }

    public static float LinearToDb(float v) => (v <= 0.0001f) ? -80f : 20f * Mathf.Log10(v);
}
