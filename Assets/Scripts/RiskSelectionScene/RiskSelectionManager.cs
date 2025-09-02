using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
public class RiskSelectionManager : MonoBehaviour
{
    public Button StartButton;
    [SerializeField]public string GameSceneName = "GameScene";
    [SerializeField] RiskSet currentSet;

    [Header("Roots")]
    public RectTransform Content;       // ScrollRect의 Content (레이아웃 컴포넌트 없음)
    public RectTransform summaryRoot;    // (선택) 총합 요약 패널

    [System.Serializable]
    public struct PointAnchor   // 상단 1pt/2pt/3pt 텍스트의 RectTransform 등록
    {
        public int points;
        public RectTransform header;
    }

    [Header("Column Anchors (1pt/2pt/3pt 헤더 좌표)")]
    public List<PointAnchor> columnAnchors = new();  // 캔버스에 배치한 1pt/2pt/3pt 텍스트를 드롭

    [System.Serializable]
    public class TypeGroup
    {
        public RiskType type;
        [Tooltip("이 타입의 모든 리스크(인스펙터에 모아서 넣기)")]
        public List<RiskDef> defs = new();  // 여기다 한 타입의 모든 항목을 던져넣기
        [Tooltip("행 라벨(비워두면 type.ToString())")]
        public string customRowTitle = "";
    }

    [Header("Manual Groups (타입별로만 넣으면, 포인트는 각 항목의 points필드로 자동 분배)")]
    public List<TypeGroup> groups = new();

    [Header("Prefabs")]
    public GameObject cellContainerPrefab;  // 빈 RectTransform(VerticalLayoutGroup 없어도 됨)
    public GameObject togglePrefab;         // 내부에 Toggle & (선택) Toggle_Title, Toggle_Image

    [Header("Layout")]
    public float topY = -40f;      // 첫 행 Y (gridRoot 기준, 아래로 갈수록 -)
    public float rowHeight = 80f;  // 행 높이(시각적 가이드용)
    public float rowSpacing = 12f; // 행 간격
    public float cellWidthFallback = 240f;
    public float cellMinHeight = 70f;
    public float columnMargin = 24f;  // 열 간 여유(겹침 방지)

    [Header("Summary UI (선택)")]
    public TextMeshProUGUI totalPtsText;
    //public TextMeshProUGUI changesText;

    [Header("Changes List (Prefab 방식)")]
    public RectTransform changesContent;     // ScrollRect의 Content
    public GameObject changeItemPrefab;      // 한 줄짜리 변경점 프리팹(자식에 "Label" TMP 있어야 함)

    Dictionary<RiskDef, GameObject> _changeItems = new(); // def -> 생성된 item

readonly Dictionary<Toggle, ToggleAlpha> _alphaByToggle = new();

    // 내부 계산 캐시
    // 내부: 타입별 토글/선택 관리
    readonly Dictionary<RiskType, List<Toggle>> _togglesByType = new();
    readonly Dictionary<Toggle, RiskDef> _defByToggle = new();
    readonly Dictionary<RiskType, Toggle> _currentOnByType = new();
    readonly Dictionary<int, float> colX = new();   // points -> column center X(local)
    readonly Dictionary<int, float> colW = new();   // points -> usable width

    // 선택 결과
    public List<RiskDef> picked = new();

    void Awake()
    {
        Build();
        RefreshSummary();
    }
    void Start()
    {
        StartButton.onClick.AddListener(OnClickStart);
    }

  public void OnClickStart()
    {
        RiskSession.SetSelection(currentSet, picked); 
        SceneManager.LoadScene(GameSceneName);
    }

    public void Build()
    {
        if (!Content) { Debug.LogWarning("[RiskUI] gridRoot 미할당"); return; }

        // 초기화
        for (int i = Content.childCount - 1; i >= 0; --i)
            Destroy(Content.GetChild(i).gameObject);
        colX.Clear(); colW.Clear();

        // 1) 헤더 centerX 수집 (월드->gridRoot 로컬X)
        foreach (var a in columnAnchors)
        {
            if (!a.header) continue;
            colX[a.points] = WorldCenterToLocalX(a.header, Content);
            Debug.Log($"[RiskUI] col {a.points} → X={colX[a.points]} ({a.header.name})");
        }
        if (colX.Count == 0)
        {
            Debug.LogWarning("[RiskUI] columnAnchors 비어있음");
            return;
        }

        // 1-1) 열 너비 추정(다음 헤더와의 간격 - margin)
        var ordered = colX.OrderBy(p => p.Key).ToList();
        for (int i = 0; i < ordered.Count; i++)
        {
            float curX = ordered[i].Value;
            float nextX = (i < ordered.Count - 1) ? ordered[i + 1].Value : curX + cellWidthFallback * 1.2f;
            float usable = Mathf.Max(120f, (nextX - curX) - columnMargin);
            colW[ordered[i].Key] = usable;
        }

        // 2) 행(타입) 별로 배치
        for (int r = 0; r < groups.Count; r++)
        {
            var g = groups[r];
            if (!_togglesByType.ContainsKey(g.type))
                _togglesByType[g.type] = new List<Toggle>();

            // (A) Row 래퍼 생성 → GridRoot(Content)의 자식으로 추가
            var rowGO = new GameObject($"Row_{g.type}", typeof(RectTransform), typeof(LayoutElement), typeof(RowAutoHeight));
            var row = rowGO.GetComponent<RectTransform>();
            row.SetParent(Content, false);
            row.anchorMin = row.anchorMax = new Vector2(0f, 1f);
            row.pivot = new Vector2(0f, 1f);
            row.GetComponent<RowAutoHeight>().minHeight = rowHeight; // 최소 행 높이

            // (B) defs를 points로 그룹화
            var byPt = g.defs.Where(d => d != null)
                             .GroupBy(d => d.points)
                             .OrderBy(gr => gr.Key);

            // (C) 각 칼럼 셀: X만 마커(colX)로, Y는 0 (행의 맨 위)
            string rowLabel = string.IsNullOrEmpty(g.customRowTitle) ? g.type.ToString() : g.customRowTitle;
            foreach (var bucket in byPt)
            {
                int pt = bucket.Key;
                if (!colX.TryGetValue(pt, out float cx)) continue;

                float w = colW.TryGetValue(pt, out var cw) ? cw : cellWidthFallback;

                var cellGO = Instantiate(cellContainerPrefab, row);
                var cell = cellGO.GetComponent<RectTransform>();
                ForceTopWithPivot(cell, 0.5f);                 // pivot=(0.5,1)
                cell.anchoredPosition = new Vector2(cx, 0f);   // ★ X만 고정, Y=0
                cell.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, w);
                cell.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, cellMinHeight);

                // 셀 내부: 세로 스택 자동
                var v = cellGO.GetComponent<VerticalLayoutGroup>() ?? cellGO.AddComponent<VerticalLayoutGroup>();
                v.spacing = 6; v.childAlignment = TextAnchor.UpperCenter;
                v.childForceExpandWidth = true; v.childForceExpandHeight = false;
                var fitter = cellGO.GetComponent<ContentSizeFitter>() ?? cellGO.AddComponent<ContentSizeFitter>();
                fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                // 토글 생성
                foreach (var def in bucket)
                {
                    var item = Instantiate(togglePrefab, cell);
                    var rt = item.GetComponent<RectTransform>();
                    ForceFixedWidth(rt, w);
                    BindToggle(item, def, g.type, rowLabel, picked.Contains(def));
                }
            }
        }

    }



    // ── UI 바인딩 ────────────────────────────────────────────────
    void BindToggle(GameObject go, RiskDef def, RiskType type, string rowLabel, bool isOn)
    {
        var toggle = go.GetComponentInChildren<Toggle>(true);
        var icon = Find<Image>(go.transform, "Toggle_Image");
        var title = Find<TextMeshProUGUI>(go.transform, "Toggle_Title");

        if (title) title.text = def.title;          // ★ 타입명 표기
        if (icon) icon.sprite = def.icon;

      // 알파 세팅
    var alpha = go.GetComponent<ToggleAlpha>() ?? go.AddComponent<ToggleAlpha>();
    alpha.toggle = toggle;
    alpha.canvasGroup = go.GetComponent<CanvasGroup>() ?? go.AddComponent<CanvasGroup>();

    toggle.SetIsOnWithoutNotify(isOn);
    alpha.Sync(toggle.isOn);

    // 🔹 초기 선택되어 있는 항목이면 변경점 생성
    if (isOn) UpdateChangeItem(def, true);

    // 매핑들...
    _alphaByToggle[toggle] = alpha;
    _defByToggle[toggle]   = def;
    if (!_togglesByType.ContainsKey(type)) _togglesByType[type] = new List<Toggle>();
    _togglesByType[type].Add(toggle);
    if (isOn) _currentOnByType[type] = toggle;

    toggle.onValueChanged.RemoveAllListeners();
    toggle.onValueChanged.AddListener(on =>
    {
        if (on)
        {
            // 동일 타입 이전 선택 해제
            if (_currentOnByType.TryGetValue(type, out var prev) && prev && prev != toggle)
            {
                var prevDef = _defByToggle[prev];
                prev.SetIsOnWithoutNotify(false);
                picked.Remove(prevDef);
                if (_alphaByToggle.TryGetValue(prev, out var prevAlpha)) prevAlpha.Sync(false);

                // 🔹 이전 변경점 제거
                UpdateChangeItem(prevDef, false);
            }

            _currentOnByType[type] = toggle;
            picked.RemoveAll(d => d != null && d.type == type);
            if (!picked.Contains(def)) picked.Add(def);

            // 🔹 새 변경점 추가
            UpdateChangeItem(def, true);
        }
        else
        {
            if (_currentOnByType.TryGetValue(type, out var cur) && cur == toggle)
                _currentOnByType[type] = null;

            picked.Remove(def);

            // 🔹 자기 변경점 제거
            UpdateChangeItem(def, false);
        }

        if (_alphaByToggle.TryGetValue(toggle, out var selfAlpha)) selfAlpha.Sync(on);
        RefreshSummary();
    });

        // (선택) 버튼 보강
        var btn = go.GetComponent<Button>();
        if (btn)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => toggle.isOn = !toggle.isOn);
        }

    }


    void RefreshSummary()
    {
        int sum = picked.Sum(d => d ? Mathf.Max(0, d.points) : 0);
        if (totalPtsText) totalPtsText.text = $"total: {sum} pt";
        // changesText는 미사용 (지우거나 No Change 같은 기본 문구만 남겨도 됨)
    }

    string GetDesc(RiskDef d)
    {
        if (d == null) return "";
        // 프로젝트 필드명이 'desc'가 아니라면 여기에서 바꿔주세요.
        return string.IsNullOrEmpty(d.desc) ? d.title : d.desc;
    }

    // ── 좌표/유틸 ────────────────────────────────────────────────
    float WorldCenterToLocalX(RectTransform header, RectTransform targetRoot)
    {
        // header의 바운드를 targetRoot 좌표계로 환산한 뒤 center.x 사용
        var b = RectTransformUtility.CalculateRelativeRectTransformBounds(targetRoot, header);
        return b.center.x;
    }

    void ForceFixedWidth(RectTransform rt, float width)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.sizeDelta = new Vector2(width, rt.sizeDelta.y);

        var le = rt.GetComponent<LayoutElement>() ?? rt.gameObject.AddComponent<LayoutElement>();
        le.minWidth = width;
        le.preferredWidth = width;
        le.preferredHeight = Mathf.Max(cellMinHeight, 60f);
    }

    void ForceTopLeft(RectTransform rt)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
    }
    void ForceTopWithPivot(RectTransform rt, float pivotX = 0.5f)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f); // 좌상단 기준(스크롤 콘텐츠 좌표)
        rt.pivot = new Vector2(pivotX, 1f);            // ← X 피벗을 가운데(0.5)로
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
    }


    T Find<T>(Transform root, string name) where T : Component
    {
        var t = root.Find(name);
        return t ? t.GetComponent<T>() : null;
    }
    void UpdateChangeItem(RiskDef def, bool add)
    {
        if (!changesContent || !changeItemPrefab || def == null) return;

        if (add)
        {
            if (_changeItems.ContainsKey(def)) return;
            var go = Instantiate(changeItemPrefab, changesContent);
            var label = go.transform.Find("Label")?.GetComponent<TextMeshProUGUI>();
            string desc = string.IsNullOrEmpty(def.desc) ? def.title : def.desc;
            if (label) label.text = desc;
            _changeItems[def] = go;
        }
        else
        {
            if (_changeItems.TryGetValue(def, out var go) && go)
            {
                Destroy(go);
                _changeItems.Remove(def);
            }
        }
    }
    void AdjustContentHeightToChildren()
    {
        if (!Content) return;
        Canvas.ForceUpdateCanvases(); // 레이아웃 즉시 갱신
        var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(Content, Content);
        // gridRoot는 상단 앵커(0,1), pivot(0,1) 가정. 높이는 양수로 내려감.
        float needed = bounds.size.y + 20f;  // 여분
        var sz = Content.sizeDelta;
        if (needed > 0f)
            Content.sizeDelta = new Vector2(sz.x, needed);
    }


}