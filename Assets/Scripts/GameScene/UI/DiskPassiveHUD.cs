using System.Collections.Generic;  // [MOD]
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class DiskPassiveHUD : MonoBehaviour
{
    [Header("Refs")]
    public DiskPassiveBank bank;

    [Header("Slide Panel")]
    public RectTransform panel;
    public Vector2 openAnchoredPos;
    public Vector2 closedAnchoredPos;
    public float  slideDuration = 0.25f;
    public bool   startOpen = true;                          // [MOD] 기본 열림
    [Header("Toggle Input")]
    public Button  toggleButton;
    public bool    enableHotkey = false;
    public KeyCode hotkey = KeyCode.Tab;

    [Header("Targets (Live Stats)")]
    public CleanTrailAbility_Disk trail;
    public DiskLauncher           launcher;
    public PerfectBounce          perfectbounce;
    public SurvivalGauge          survivalgauge;
    public SurvivalDirector       survivaldirector;

    [Header("List (Instantiate)")]
    public RectTransform listContainer;
    public Vector2       itemSize = new Vector2(300f, 50f);

    [Header("Options")]
    public bool showOnlyExistingComponents = true;           // [MOD] 없는 컴포넌트 항목은 숨김
    public bool rebuildUIWhenSchemaChanges = false;          // [MOD] 카탈로그가 바뀌면 재생성 옵션
    public float periodicRefreshSec = 0f;                    // [MOD] 0이면 OnChanged/수동만, >0이면 주기 갱신

    class StatRow
    {
        public string label;
        public System.Func<string> get;
        public TextMeshProUGUI tmp;
    }

    readonly List<StatRow> _rows = new();                   // [MOD]
    bool  _listBuilt;
    bool  _isOpen;
    Coroutine slideCo, refreshCo;                            // [MOD]

    void Awake()
    {
        if (!panel) panel = GetComponent<RectTransform>();
        if (!bank)  bank  = FindAnyObjectByType<DiskPassiveBank>();
    }

    void Start()
    {
        _isOpen = startOpen;
        if (panel) panel.anchoredPosition = _isOpen ? openAnchoredPos : closedAnchoredPos;

        if (toggleButton && panel && toggleButton.transform.parent != panel)
            toggleButton.transform.SetParent(panel, false);
        if (toggleButton) toggleButton.transform.SetAsLastSibling();
        if (toggleButton)
        {
            toggleButton.onClick.RemoveListener(Toggle);
            toggleButton.onClick.AddListener(Toggle);
        }

        EnsureTargetRefs();

        BuildCatalogRows();   // [MOD] ★★ 처음부터 “표시할 스텟 카탈로그”로 행 구성
        BuildListOnce();      // [MOD] UI 오브젝트 생성
        RefreshRows();        // [MOD] 값 채워넣기 (OnChanged 없이도 처음부터 꽉 찬다)

        if (periodicRefreshSec > 0f)                          // [MOD]
            refreshCo = StartCoroutine(PeriodicRefreshCo());  // (선택) 주기 갱신
    }

    void OnEnable()
    {
        if (bank) bank.OnChanged.AddListener(Refresh);        // [MOD] 값만 새로고침
        Refresh();
    }

    void OnDisable()
    {
        if (bank) bank.OnChanged.RemoveListener(Refresh);
        if (toggleButton) toggleButton.onClick.RemoveListener(Toggle);
        if (refreshCo != null) { StopCoroutine(refreshCo); refreshCo = null; } // [MOD]
    }

    void Update()
    {
        if (enableHotkey && Input.GetKeyDown(hotkey)) Toggle();
    }

    public void Toggle() => SetOpen(!_isOpen);
    public void Open()   => SetOpen(true);
    public void Close()  => SetOpen(false);

    public void SetOpen(bool open)
    {
        if (!panel || _isOpen == open) return;
        _isOpen = open;
        if (slideCo != null) StopCoroutine(slideCo);
        slideCo = StartCoroutine(SlideCo(_isOpen ? openAnchoredPos : closedAnchoredPos));
        if (toggleButton) toggleButton.transform.SetAsLastSibling();
    }

    System.Collections.IEnumerator SlideCo(Vector2 target)
    {
        Vector2 start = panel.anchoredPosition;
        float t = 0f, dur = Mathf.Max(0.01f, slideDuration);

        while (t < dur)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / dur);
            panel.anchoredPosition = Vector2.LerpUnclamped(start, target, k);
            yield return null;
        }
        panel.anchoredPosition = target;
        slideCo = null;
    }

    void Refresh()
    {
        EnsureTargetRefs();
        // [MOD] 스키마(표시 항목) 자체는 처음에 고정 생성.
        //       OnChanged에서는 텍스트만 갱신한다.
        RefreshRows();
    }

    void EnsureTargetRefs()
    {
        if (!trail)            trail            = FindAnyObjectByType<CleanTrailAbility_Disk>();
        if (!launcher)         launcher         = FindAnyObjectByType<DiskLauncher>();
        if (!perfectbounce)    perfectbounce    = FindAnyObjectByType<PerfectBounce>();
        if (!survivalgauge)    survivalgauge    = FindAnyObjectByType<SurvivalGauge>();
        if (!survivaldirector) survivaldirector = FindAnyObjectByType<SurvivalDirector>();
    }

    // ─────────────────────────────────────────────────────────
    // [MOD] 처음부터 “카탈로그”로 행 구성 (획득 패시브와 무관)
    // ─────────────────────────────────────────────────────────
    void BuildCatalogRows()
    {
        _rows.Clear();

        // 공통 포맷터
        string S(object v)
        {
            if (v == null) return "N/A";
            if (v is float f) return f.ToString("0.##");
            if (v is double d) return d.ToString("0.##");
            return v.ToString();
        }

        // 헬퍼: 존재할 때만 추가
        void AddIf(string label, System.Func<bool> exists, System.Func<string> get)
        {
            if (!showOnlyExistingComponents || exists())
                _rows.Add(new StatRow { label = label, get = get });
        }

        // === Ink / Trail ===
        AddIf("trail.radiusAddWorld",
            () => trail,
            () => trail ? S(trail.radiusAddWorld) : "N/A");

        // === Launcher ===
        AddIf("launcher.cooldownSeconds",
            () => launcher,
            () => launcher ? S(launcher.cooldownSeconds) : "N/A");

        // === PerfectBounce ===
        AddIf("perfectbounce.inkGainOnSuccess",
            () => perfectbounce,
            () => perfectbounce ? S(perfectbounce.inkGainOnSuccess) : "N/A");

        AddIf("perfectbounce.inkLossOnFail",
            () => perfectbounce,
            () => perfectbounce ? S(perfectbounce.inkLossOnFail) : "N/A");

        AddIf("perfectbounce.speedAddOnSuccess",
            () => perfectbounce,
            () => perfectbounce ? S(perfectbounce.speedAddOnSuccess) : "N/A");

        AddIf("perfectbounce.PerfectBounceDeg",
            () => perfectbounce,
            () => perfectbounce ? S(perfectbounce.PerfectBounceDeg) : "N/A");

        // === SurvivalGauge ===
        AddIf("survivalgauge.baseCostPerMeter",
            () => survivalgauge,
            () => survivalgauge ? S(survivalgauge.baseCostPerMeter) : "N/A");

        AddIf("survivalgauge.contamExtraMul",
            () => survivalgauge,
            () => survivalgauge ? S(survivalgauge.contamExtraMul) : "N/A");

        AddIf("survivalgauge.recoverDuration",
            () => survivalgauge,
            () => survivalgauge ? S(survivalgauge.recoverDuration) : "N/A");

        AddIf("survivalgauge.zonebonusarc",
            () => survivalgauge,
            () => survivalgauge ? S(survivalgauge.zonebonusarc) : "N/A");

        // === SurvivalDirector ===
        AddIf("survivaldirector.bonusArcDeg",
            () => survivaldirector,
            () => survivaldirector ? S(survivaldirector.bonusArcDeg) : "N/A");

        // (필요 시 여기 계속 추가하면 됨)
    }

    // ── UI 생성(한 번만) ──
    void BuildListOnce()
    {
        if (!listContainer || _listBuilt) return;

        // 기존 자식 정리(안전)
        for (int i = listContainer.childCount - 1; i >= 0; --i)
            Destroy(listContainer.GetChild(i).gameObject);

        foreach (var r in _rows)
        {
            var go = new GameObject("Stat", typeof(RectTransform));
            go.transform.SetParent(listContainer, false);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot     = new Vector2(0f, 1f);
            rt.sizeDelta = itemSize;

            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth  = itemSize.x;
            le.preferredHeight = itemSize.y;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.alignment = TextAlignmentOptions.Left;
            tmp.textWrappingMode = 0;
            tmp.fontSize = 20;

            r.tmp = tmp;
        }

        _listBuilt = true;
    }

    // ── 값만 새로고침 ──
    void RefreshRows()
    {
        foreach (var r in _rows)
        {
            if (r.tmp)
                r.tmp.text = $"{r.label}: {r.get()}"; // "변수이름: 값"
        }
    }

    System.Collections.IEnumerator PeriodicRefreshCo() // [MOD]
    {
        var wait = new WaitForSecondsRealtime(Mathf.Max(0.05f, periodicRefreshSec));
        while (true)
        {
            RefreshRows();
            yield return wait;
        }
    }
}
