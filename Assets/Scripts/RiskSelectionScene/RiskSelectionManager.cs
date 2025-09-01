using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class RiskSelectionManager : MonoBehaviour
{
    [Header("Data")]
    public RiskSet riskSet;
    public List<RiskDef> currentPicked = new();

    [Header("UI Roots")]
    public Transform headerRowRoot;   // 가로 헤더(좌–우)
    public Transform gridRoot;        // 세로 본문(ScrollRect Content)
    public Transform summaryRoot;

    [Header("Prefabs (Project 에셋)")]
    public GameObject pointHeaderPrefab;
    public GameObject typeHeaderPrefab;
    public GameObject cellContainerPrefab;   // 비어있는 RectTransform(자식 없음)
    public GameObject togglePrefab;          // Toggle 루트

    [Header("Summary UI")]
    public TextMeshProUGUI totalPtsText;
    public TextMeshProUGUI targetTimeText;
    public TextMeshProUGUI changesText;

    [Header("Layout Sizes")]
    public float typeColWidth = 160f;
    public float colWidth = 220f;
    public float rowHeight = 72f;

    [Header("Start")]
    public Button startButton;
    public string gameSceneName = "GameScene";

    readonly Dictionary<(RiskType type, int points), RectTransform> _cellMap = new();
    List<int> _pointColumns = new();

    void Awake()
    {
        BuildGrid();
        RefreshAll();
        if (startButton) startButton.onClick.AddListener(OnClickStartGame);
    }

    // ──────────────────────────────────────────────────────────────
    #region Build

    void BuildGrid()
    {
        if (!riskSet)
        {
            Debug.LogWarning("[RiskUI] riskSet 미지정");
            return;
        }

        Clear(headerRowRoot);
        Clear(gridRoot);
        _cellMap.Clear();

        // 1) 컬럼(점수) 수집
        _pointColumns = riskSet.available
            .Where(d => d)
            .Select(d => Mathf.Max(0, d.points))
            .Distinct()
            .OrderBy(p => p)
            .ToList();

        // 2) 헤더(좌상단 코너 + pt 헤더들)
        // 2-1) 코너
        var corner = Instantiate(typeHeaderPrefab, headerRowRoot, false)
                     .GetComponent<RectTransform>();
        SetPreferred(corner, typeColWidth, rowHeight);
        var cTxt = corner.GetComponentInChildren<TextMeshProUGUI>();
        if (cTxt) cTxt.text = "";

        // 2-2) 포인트 헤더
        foreach (var p in _pointColumns)
        {
            var h = Instantiate(pointHeaderPrefab, headerRowRoot, false)
                    .GetComponent<RectTransform>();
            SetPreferred(h, colWidth, rowHeight);
            var t = h.GetComponentInChildren<TextMeshProUGUI>();
            if (t) t.text = $"{p} pt";
        }

        // 3) 행(타입별)
        var types = riskSet.available.Where(d => d).Select(d => d.type).Distinct().ToList();

        foreach (var type in types)
        {
            // 3-1) 수평 행 컨테이너
            var rowGO = new GameObject($"{type}_Row",
                typeof(RectTransform),
                typeof(HorizontalLayoutGroup),
                typeof(LayoutElement));

            var row = rowGO.GetComponent<RectTransform>();
            row.SetParent(gridRoot, false);
            SetPreferred(row, -1, rowHeight);

            var hl = rowGO.GetComponent<HorizontalLayoutGroup>();
            hl.spacing = 6;
            hl.childControlWidth = true;
            hl.childControlHeight = true;
            hl.childForceExpandWidth = false;
            hl.childForceExpandHeight = false;
            hl.childAlignment = TextAnchor.MiddleCenter;

            // 3-2) 좌측 타입 헤더
            var th = Instantiate(typeHeaderPrefab, row, false).GetComponent<RectTransform>();
            SetPreferred(th, typeColWidth, rowHeight);
            var thText = th.GetComponentInChildren<TextMeshProUGUI>();
            if (thText) thText.text = type.ToString();

            // 3-3) 각 점수 칸(빈 셀)
            foreach (var p in _pointColumns)
            {
                var cell = Instantiate(cellContainerPrefab, row, false).GetComponent<RectTransform>();
                SetPreferred(cell, colWidth, rowHeight);
                EnsureEmpty(cell);
                _cellMap[(type, p)] = cell;
            }
        }

        // 4) RiskDef → 해당 셀에 토글 배치
        foreach (var def in riskSet.available.Where(d => d))
        {
            var key = (def.type, Mathf.Max(0, def.points));
            if (!_cellMap.TryGetValue(key, out var cell)) continue;

            var item = Instantiate(togglePrefab);
            // ★ 강제 리패런팅(셀 밖으로 튀는 레이아웃 이슈 방지)
            item.transform.SetParent(cell, false);

            var rt = item.GetComponent<RectTransform>();
            SetPreferred(rt, colWidth, rowHeight);

            BindToggleItem(item, def, currentPicked.Contains(def));

#if UNITY_EDITOR
            if (item.transform.parent != cell)
                Debug.LogWarning($"[RiskUI] Toggle reparent failed → {def.title}");
#endif
        }
    }

    void Clear(Transform root)
    {
        if (!root) return;
        for (int i = root.childCount - 1; i >= 0; --i)
            Destroy(root.GetChild(i).gameObject);
    }

    void EnsureEmpty(RectTransform rt)
    {
        for (int i = rt.childCount - 1; i >= 0; --i)
            Destroy(rt.GetChild(i).gameObject);
    }

    void SetPreferred(RectTransform rt, float w, float h)
    {
        if (!rt) return;
        var le = rt.GetComponent<LayoutElement>() ?? rt.gameObject.AddComponent<LayoutElement>();
        if (w > 0) le.preferredWidth = w;
        if (h > 0) le.preferredHeight = h;
    }

    #endregion

    // ──────────────────────────────────────────────────────────────
    #region Toggle / Refresh

    void BindToggleItem(GameObject go, RiskDef def, bool isOn)
    {
        var toggle = go.GetComponentInChildren<Toggle>(true);
        var icon   = Find<Image>(go.transform, "Toggle_Image");
        var title  = Find<TextMeshProUGUI>(go.transform, "Toggle_Title");

        if (title) title.text = def.title;
        if (icon)  icon.sprite = def.icon;

        var meta = go.GetComponent<RiskToggleMeta>() ?? go.AddComponent<RiskToggleMeta>();
        meta.def = def;

        if (toggle)
        {
            toggle.SetIsOnWithoutNotify(isOn);
            toggle.onValueChanged.AddListener(on => OnToggle(def, toggle, on));
        }
    }

    void OnToggle(RiskDef def, Toggle ui, bool isOn)
    {
        string reason;
        var temp = new List<RiskDef>(currentPicked);
        if (isOn) temp.Add(def); else temp.Remove(def);

        if (!riskSet.CanToggle(currentPicked, def, isOn, out reason))
        {
            ui.SetIsOnWithoutNotify(!isOn);
            Debug.Log($"[Risk] 선택 불가: {reason}");
            return;
        }
        currentPicked = temp;
        RefreshAll();
    }

    void RefreshAll()
    {
        // 토글 상호작용 상태 갱신
        foreach (var rt in _cellMap.Values)
        {
            foreach (var t in rt.GetComponentsInChildren<Toggle>(true))
            {
                var meta = t.GetComponent<RiskToggleMeta>();
                if (!meta || !meta.def) { t.interactable = false; continue; }

                bool isOn = currentPicked.Contains(meta.def);
                if (t.isOn != isOn) t.SetIsOnWithoutNotify(isOn);

                string reason;
                bool canOn = riskSet.CanToggle(currentPicked, meta.def, true, out reason);
                t.interactable = isOn || canOn; // 켜는 건 검증, 끄는 건 항상 허용
            }
        }

        // 요약
        int pts = riskSet.SumPoints(currentPicked);
        if (totalPtsText)   totalPtsText.text   = $"total: {pts} pt";
        float target = riskSet.ComputeTargetSeconds(currentPicked);
        if (targetTimeText) targetTimeText.text = $"target Time:\n{FormatTime(target)}";
        if (changesText)    changesText.text    = BuildChangesSummary();
    }

    class RiskToggleMeta : MonoBehaviour { public RiskDef def; }

    #endregion

    // ──────────────────────────────────────────────────────────────
    #region Summary / Start

    string BuildChangesSummary()
    {
        if (currentPicked.Count == 0) return "No Change";
        // 간단: 선택된 Risk 제목 나열
        return string.Join("\n", currentPicked.Where(d => d).Select(d => d.title));
    }

    string FormatTime(float sec)
    {
        int s = Mathf.RoundToInt(sec);
        int m = s / 60; s %= 60;
        return $"{m:00}:{s:00}";
    }

    public void OnClickStartGame()
    {
        RiskSession.SetSelection(riskSet, currentPicked);
        RiskInstaller.Spawn(gameSceneName);
        SceneManager.LoadScene(gameSceneName);
    }

    #endregion

    // ──────────────────────────────────────────────────────────────
    #region Helpers

    T Find<T>(Transform root, string name) where T : Component
    {
        var t = root.Find(name);
        return t ? t.GetComponent<T>() : null;
    }

    #endregion
}
