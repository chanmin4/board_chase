using UnityEngine;

[DisallowMultipleComponent]
public class FeverManager : MonoBehaviour
{
    public static FeverManager Instance { get; private set; }
    public static bool IsActive => Instance && Instance._feverActive;

    /// <summary>카드 충전 배수 (FEVER중). CardManager가 이 값을 읽어 씀.</summary>
    public static float ChargeMul => (Instance && Instance._feverActive)
        ? Mathf.Max(1f, Instance.cardChargeMultiplierDuringFever)
        : 1f;

    [Header("Refs")]
    public SurvivalDirector director;          // OnZonesReset, OnWallHitsChanged 듣기
    public ParticleSystem burningFX;           // 디스크 불 파티클(Loop 권장)
    public AudioSource feverSfx;               // FEVER 효과음(원샷/루프 아무거나)

    [Header("Trigger")]
    [Tooltip("이 사이클(라운드) 동안 누적 벽튕김 수가 이 값에 도달하면 FEVER 시작")]
    public int requiredHitsThisCycle = 5;

    [Header("Effect (Inspector 조절)")]
    [Tooltip("FEVER 중 벽튕김 1회당 카드 충전 배수")]
    public float cardChargeMultiplierDuringFever = 2f;

    [Header("Debug")]
    public bool log = false;

    bool _feverActive = false;
    int  _hitsAtCycleStart = 0;
    int  _lastHits = 0;

    void Awake()
    {
        Instance = this;
        if (!director) director = FindAnyObjectByType<SurvivalDirector>();
        StopFX(true);
    }

    void OnEnable()
    {
        if (director)
        {
            director.OnZonesReset += OnCycleReset;
            director.OnWallHitsChanged += OnWallHitsChanged;
        }
    }
    void OnDisable()
    {
        if (director)
        {
            director.OnZonesReset -= OnCycleReset;
            director.OnWallHitsChanged -= OnWallHitsChanged;
        }
        if (Instance == this) Instance = null;
    }

    void OnCycleReset()
    {
        // 사이클 시작 → FEVER 해제 + 카운터 리셋
        if (log) Debug.Log("[FEVER] Cycle reset → clear");
        _feverActive = false;
        _hitsAtCycleStart = _lastHits; // 보수적으로 스냅샷(대부분 0일 것)
        StopFX(false);
    }

    void OnWallHitsChanged(int hitsNow)
    {
        if (_hitsAtCycleStart == 0 && _lastHits == 0)
            _hitsAtCycleStart = hitsNow; // 첫 콜백에서 기준점 스냅샷

        _lastHits = hitsNow;

        if (_feverActive) return;

        int gainedThisCycle = Mathf.Max(0, hitsNow - _hitsAtCycleStart);
        if (gainedThisCycle >= requiredHitsThisCycle)
        {
            StartFever();
        }
    }

    public void StartFever()
    {
        if (_feverActive) return;
        _feverActive = true;
        if (log) Debug.Log("[FEVER] START");
        if (burningFX) burningFX.Play(true);
        if (feverSfx)
        {
            // 루프 소스면 Play(), 원샷이면 PlayOneShot에 클립 넣기
            if (feverSfx.clip && !feverSfx.loop) feverSfx.PlayOneShot(feverSfx.clip);
            else feverSfx.Play();
        }
    }

    public void StopFever()
    {
        if (!_feverActive) return;
        _feverActive = false;
        if (log) Debug.Log("[FEVER] END");
        StopFX(false);
    }

    void StopFX(bool clearNow)
    {
        if (burningFX)
        {
            burningFX.Stop(true,
                clearNow ? ParticleSystemStopBehavior.StopEmittingAndClear
                         : ParticleSystemStopBehavior.StopEmitting);
        }
        if (feverSfx && !feverSfx.loop) { /* 원샷은 자연감쇠 */ }
    }
}
