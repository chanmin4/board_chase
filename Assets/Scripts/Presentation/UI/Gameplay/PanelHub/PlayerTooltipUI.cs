/*
 * Role hierarchy: Presentation > UI > Gameplay > PanelHub
 * Primary responsibility: Implements the focused TooltipPlacementSide responsibility and collaborates with its role group.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum TooltipPlacementSide
{
    Left = 0,
    Right = 1
}

[DisallowMultipleComponent]
public class PlayerTooltipUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private Image _backgroundImage;
    [SerializeField] private TextMeshProUGUI _titleText;
    [SerializeField] private TextMeshProUGUI _descriptionText;
    [SerializeField] private TextMeshProUGUI _costText;
    [SerializeField] private TextMeshProUGUI _stateText;

    [Header("Position")]
    [Tooltip("Screen-space offset from the pointer position.")]
    [SerializeField] private Vector2 _screenOffset = new(24f, -12f);

    [Tooltip("Keeps the tooltip inside the current screen bounds.")]
    [SerializeField] private bool _clampToScreen = true;

    [Tooltip("Smallest tooltip width after its text has been measured.")]
    [SerializeField, Min(0f)] private float _minimumWidth = 160f;

    [Tooltip("Largest tooltip width. Longer descriptions wrap instead of expanding across the screen.")]
    [SerializeField, Min(1f)] private float _maximumWidth = 420f;

    private RectTransform _rectTransform;
    private LayoutElement _layoutElement;
    private Canvas _sortingCanvas;

    // Purpose: Initializes Inspector defaults for this component.
    private void Reset()
    {
        CacheRefs();
    }

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        CacheRefs();
        Hide();
    }

    // Purpose: Shows the text UI or visual state.
    public void ShowText(
        string title,
        string description,
        string costText,
        string stateText,
        Vector2 screenPosition)
    {
        CacheRefs();

        SetText(_titleText, title);
        SetText(_descriptionText, description);
        SetText(_costText, costText);
        SetText(_stateText, stateText);

        gameObject.SetActive(true);
        RebuildFlexibleSize();
        SetPosition(screenPosition);
        BringToFront();
        SetVisible(true);
    }

    // Purpose: Shows the text UI or visual state.
    public void ShowText(
        string title,
        string description,
        string costText,
        string stateText,
        RectTransform anchor,
        TooltipPlacementSide side,
        float gap)
    {
        if (anchor == null)
        {
            ShowText(title, description, costText, stateText, Input.mousePosition);
            return;
        }

        CacheRefs();
        SetText(_titleText, title);
        SetText(_descriptionText, description);
        SetText(_costText, costText);
        SetText(_stateText, stateText);

        gameObject.SetActive(true);
        RebuildFlexibleSize();
        SetPositionBesideAnchor(anchor, side, Mathf.Max(0f, gap));
        BringToFront();
        SetVisible(true);
    }

    // Purpose: Shows the item UI or visual state.
    public void ShowItem(
        ItemSO item,
        RectTransform anchor,
        TooltipPlacementSide side,
        float gap)
    {
        if (item == null)
        {
            Hide();
            return;
        }

        string description = item.Description != null && !item.Description.IsEmpty
            ? item.Description.GetLocalizedString()
            : string.Empty;
        if (item is InfectionCureItemSO ampoule)
            description += ampoule.IsSyringe ? $"\n최대 HP 잠식 {ampoule.InfectionRecoveryAmount:0.#} 복구. 현재 HP는 별도 치료." :
                $"\n{ampoule.InfectionRecoveryDurationSeconds:0.#}초간 감염 유입 ×{ampoule.ExposureGainMultiplier:0.##}, 회복 대기 후 원형 게이지 +{ampoule.InfectionRecoveryPerSecond:0.#}/초 회복. 최대 HP 잠식 회복 불가.";
        else if (item is PassiveItemSO passive)
        {
            description += $"\n{(passive.Category == PassiveCategory.Combat ? "전투" : "생존")} · 원정 한정";
            description += passive.DisabledForRaid ? "\n해제됨 · 재장착/분해 불가 · 귀환 시 소멸" :
                passive.EverEquipped ? "\n귀속 · 타인 획득/분해 불가 · 해제하면 재사용 불가 · 귀환 시 소멸" : "\n미장착 · 반입/시간제 분해 가능 · 판매 불가";
            foreach (var stat in passive.StatModifiers ?? System.Array.Empty<PlayerStatModifier>())
                if (ShooterStatPolicy.IsPassiveStat(stat.stat) && stat.value != 0f)
                {
                    var applied = PassiveModuleRulesSO.Active != null ? PassiveModuleRulesSO.Active.ApplyStrength(passive, stat) : stat;
                    description += applied.stat == PlayerStatId.ExperienceGainBonus ? $"\n획득 경험치: +{applied.value*100:0.#}%" : $"\n{stat.stat}: {applied.value:+0.###;-0.###;0}";
                }
        }
        else if (item is ConsumableItemSO medicine && medicine.InfectionSuppressionSeconds > 0f)
            description += $"\n{medicine.InfectionSuppressionSeconds:0}초간 바이러스 증식을 억제합니다. 감염 수치를 낮추지는 않습니다.";

        if (item is ValuableItemSO valuable) description = "물품 분류 · " + valuable.ValuableCategoryLabel + "\n" + description;
        ShowText(
            InventoryItemDisplayUtility.ResolveItemName(item),
            description,
            string.Empty,
            item is PassiveItemSO ? $"{item.Rarity} / 원정 모듈 · 퀘스트 슬롯 해금" : $"{item.Rarity} / {item.Kind}",
            anchor,
            side,
            gap);
    }

    // Purpose: Hides the hide UI or visual state.
    public void Hide()
    {
        CacheRefs();
        SetVisible(false);
    }

    /// <summary>
    /// Returns the tooltip layer owned by the nearest visible hub. Tooltips stay
    /// inside PlayerPanelHub, ShopHub, or LootHub instead of leaking into the
    /// shared Canvas Center root.
    /// </summary>
    public static Transform GetOrCreatePanelTooltipRoot(
        Component owner,
        Transform preferredRoot = null)
    {
        if (owner == null)
            return preferredRoot;

        Transform panelRoot = ResolveOwningPanelRoot(owner.transform);
        if (panelRoot == null)
            panelRoot = owner.transform;

        if (preferredRoot != null &&
            preferredRoot.name == "TooltipRoot" &&
            (preferredRoot == panelRoot || preferredRoot.IsChildOf(panelRoot)))
        {
            CleanupLegacyTooltipClones(preferredRoot);
            preferredRoot.SetAsLastSibling();
            return preferredRoot;
        }

        Transform existing = panelRoot.Find("TooltipRoot");
        if (existing != null)
        {
            CleanupLegacyTooltipClones(existing);
            existing.SetAsLastSibling();
            return existing;
        }

        GameObject rootObject = new(
            "TooltipRoot",
            typeof(RectTransform),
            typeof(Canvas));
        rootObject.layer = owner.gameObject.layer;
        rootObject.transform.SetParent(panelRoot, false);

        RectTransform rect = (RectTransform)rootObject.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
        rect.localScale = Vector3.one;

        Canvas canvas = rootObject.GetComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = 32750;
        CleanupLegacyTooltipClones(rect);
        rect.SetAsLastSibling();
        return rect;
    }

    // Purpose: Performs the cleanup legacy tooltip clones workflow for this component.
    private static void CleanupLegacyTooltipClones(Transform activeTooltipRoot)
    {
        if (activeTooltipRoot == null)
            return;

        Canvas rootCanvas = activeTooltipRoot.GetComponentInParent<Canvas>();
        if (rootCanvas == null)
            return;

        PlayerTooltipUI[] instances = rootCanvas.rootCanvas
            .GetComponentsInChildren<PlayerTooltipUI>(includeInactive: true);

        for (int i = 0; i < instances.Length; i++)
        {
            PlayerTooltipUI instance = instances[i];
            if (instance == null || !instance.name.EndsWith("(Clone)"))
                continue;

            Transform parent = instance.transform.parent;
            if (parent != null && parent.name == "TooltipRoot")
                continue;

            // Hot Reload can leave the previous hidden tooltip orphaned below
            // Canvas/Center. Remove it when the next panel tooltip is requested.
            instance.gameObject.SetActive(false);
            Destroy(instance.gameObject);
        }
    }

    // Purpose: Resolves the owning panel root needed by this component.
    private static Transform ResolveOwningPanelRoot(Transform source)
    {
        Transform current = source;
        Transform fallback = null;

        while (current != null)
        {
            if (current.name == "ShopHub" || current.name == "LootHub")
                return current;

            if (current.GetComponent<PlayerPanelHub>() != null)
                return current;

            if (fallback == null && current.GetComponent<PanelUI>() != null)
                fallback = current;

            Canvas canvas = current.GetComponent<Canvas>();
            if (canvas != null && canvas.isRootCanvas)
                break;

            current = current.parent;
        }

        return fallback;
    }

    // Purpose: Caches refs for later reuse.
    private void CacheRefs()
    {
        if (_rectTransform == null)
            _rectTransform = transform as RectTransform;

        if (_canvasGroup == null)
            _canvasGroup = GetComponent<CanvasGroup>();

        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();

        if (_backgroundImage == null)
            _backgroundImage = GetComponent<Image>();

        if (_layoutElement == null)
            _layoutElement = GetComponent<LayoutElement>() ?? gameObject.AddComponent<LayoutElement>();

        if (_sortingCanvas == null)
            _sortingCanvas = GetComponent<Canvas>();

        if (_sortingCanvas != null)
        {
            _sortingCanvas.overrideSorting = true;
            _sortingCanvas.sortingOrder = 32760;
        }
    }

    // Purpose: Updates the visible state.
    private void SetVisible(bool visible)
    {
        if (_canvasGroup == null)
            return;

        _canvasGroup.alpha = visible ? 1f : 0f;
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;
    }

    // Purpose: Repairs or rebuilds flexible size from the current source data.
    private void RebuildFlexibleSize()
    {
        if (_rectTransform == null)
            return;

        float preferredWidth = Mathf.Max(
            ResolvePreferredWidth(_titleText),
            ResolvePreferredWidth(_descriptionText),
            ResolvePreferredWidth(_costText),
            ResolvePreferredWidth(_stateText));

        if (_layoutElement != null)
        {
            float maxWidth = Mathf.Max(_minimumWidth, _maximumWidth);
            _layoutElement.preferredWidth = Mathf.Clamp(
                preferredWidth,
                Mathf.Max(0f, _minimumWidth),
                maxWidth);
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(_rectTransform);
    }

    // Purpose: Resolves the preferred width needed by this component.
    private float ResolvePreferredWidth(TextMeshProUGUI text)
    {
        if (text == null || string.IsNullOrEmpty(text.text))
            return 0f;

        float maxWidth = Mathf.Max(_minimumWidth, _maximumWidth);
        return text.GetPreferredValues(text.text, maxWidth, 0f).x;
    }

    // Purpose: Updates the position state.
    private void SetPosition(Vector2 screenPosition)
    {
        if (_rectTransform == null)
            return;

        Vector2 targetPosition = screenPosition + _screenOffset;

        if (_clampToScreen)
            targetPosition = ClampToScreen(targetPosition);

        _rectTransform.position = targetPosition;
    }

    // Purpose: Updates the position beside anchor state.
    private void SetPositionBesideAnchor(
        RectTransform anchor,
        TooltipPlacementSide side,
        float gap)
    {
        if (_rectTransform == null || anchor == null)
            return;

        Canvas rootCanvas = anchor.GetComponentInParent<Canvas>()?.rootCanvas;
        Camera eventCamera = rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? rootCanvas.worldCamera
            : null;

        Vector3[] corners = new Vector3[4];
        anchor.GetWorldCorners(corners);

        Vector3 worldEdge = side == TooltipPlacementSide.Right
            ? (corners[2] + corners[3]) * 0.5f
            : (corners[0] + corners[1]) * 0.5f;

        Vector2 screenEdge = RectTransformUtility.WorldToScreenPoint(eventCamera, worldEdge);
        RectTransform parentRect = _rectTransform.parent as RectTransform;

        _rectTransform.pivot = side == TooltipPlacementSide.Right
            ? new Vector2(0f, 0.5f)
            : new Vector2(1f, 0.5f);

        if (parentRect != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect,
                screenEdge,
                eventCamera,
                out Vector2 localEdge))
        {
            float direction = side == TooltipPlacementSide.Right ? 1f : -1f;
            Vector2 localPosition = localEdge + Vector2.right * (gap * direction);

            if (_clampToScreen)
                localPosition = ClampToParent(localPosition, parentRect);

            _rectTransform.anchoredPosition = localPosition;
            return;
        }

        Vector2 fallback = screenEdge + Vector2.right *
            (side == TooltipPlacementSide.Right ? gap : -gap);
        _rectTransform.position = _clampToScreen ? ClampToScreen(fallback) : fallback;
    }

    // Purpose: Performs the clamp to parent workflow for this component.
    private Vector2 ClampToParent(Vector2 localPosition, RectTransform parentRect)
    {
        Rect parent = parentRect.rect;
        Vector2 size = _rectTransform.rect.size;
        Vector2 pivot = _rectTransform.pivot;

        float minX = parent.xMin + size.x * pivot.x;
        float maxX = parent.xMax - size.x * (1f - pivot.x);
        float minY = parent.yMin + size.y * pivot.y;
        float maxY = parent.yMax - size.y * (1f - pivot.y);

        if (minX <= maxX)
            localPosition.x = Mathf.Clamp(localPosition.x, minX, maxX);

        if (minY <= maxY)
            localPosition.y = Mathf.Clamp(localPosition.y, minY, maxY);

        return localPosition;
    }

    // Purpose: Performs the bring to front workflow for this component.
    private void BringToFront()
    {
        transform.SetAsLastSibling();

        if (_sortingCanvas != null)
            _sortingCanvas.sortingOrder = 32760;
    }

    // Purpose: Performs the clamp to screen workflow for this component.
    private Vector2 ClampToScreen(Vector2 screenPosition)
    {
        if (_rectTransform == null)
            return screenPosition;

        Vector2 size = _rectTransform.rect.size;
        Vector2 pivot = _rectTransform.pivot;

        float minX = size.x * pivot.x;
        float maxX = Screen.width - size.x * (1f - pivot.x);
        float minY = size.y * pivot.y;
        float maxY = Screen.height - size.y * (1f - pivot.y);

        screenPosition.x = Mathf.Clamp(screenPosition.x, minX, maxX);
        screenPosition.y = Mathf.Clamp(screenPosition.y, minY, maxY);
        return screenPosition;
    }

    // Purpose: Updates the text state.
    private static void SetText(TextMeshProUGUI target, string value)
    {
        if (target != null)
            target.text = value ?? string.Empty;
    }
}
