/*
 * Role hierarchy: Presentation > UI > Gameplay > PanelHub > Inventory
 * Primary responsibility: Presents InventoryDragGhostUI state and handles its user-facing UI interaction.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class InventoryDragGhostUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Canvas _canvas;
    [SerializeField] private RectTransform _root;
    [SerializeField] private CanvasGroup _group;
    [SerializeField] private Image _iconImage;

    [Header("Display")]
    [SerializeField] private Vector2 _size = new Vector2(72f, 72f);
    [SerializeField] private Vector2 _screenOffset = new Vector2(18f, -18f);
    [SerializeField, Range(0f, 1f)] private float _alpha = 0.65f;

    private static InventoryDragGhostUI _instance;
    private bool _visible;

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        EnsureRefs();
        SetVisible(false);
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        if (_instance == this)
            _instance = null;
    }

    // Purpose: Shows the show UI or visual state.
    public static void Show(
        Transform source,
        Sprite sprite,
        Vector2 screenPosition)
    {
        if (sprite == null)
            return;

        InventoryDragGhostUI ghost = ResolveInstance(source);

        if (ghost == null)
            return;

        ghost.ShowInternal(sprite, screenPosition);
    }

    // Purpose: Moves move toward its requested destination.
    public static void Move(Vector2 screenPosition)
    {
        if (_instance == null || !_instance._visible)
            return;

        _instance.MoveInternal(screenPosition);
    }

    // Purpose: Hides the hide UI or visual state.
    public static void Hide()
    {
        if (_instance == null)
            return;

        _instance.SetVisible(false);
    }

    // Purpose: Resolves the instance needed by this component.
    private static InventoryDragGhostUI ResolveInstance(Transform source)
    {
        if (_instance != null)
            return _instance;

        InventoryDragGhostUI existing =
            source != null
                ? source.GetComponentInParent<InventoryDragGhostUI>(true)
                : null;

        if (existing == null)
            existing = FindAnyObjectByType<InventoryDragGhostUI>();

        if (existing != null)
        {
            _instance = existing;
            _instance.EnsureRefs();
            return _instance;
        }

        Canvas canvas = ResolveCanvas(source);

        if (canvas == null)
            return null;

        GameObject ghostObject = new GameObject(
            "InventoryDragGhostUI",
            typeof(RectTransform),
            typeof(CanvasGroup),
            typeof(Image),
            typeof(InventoryDragGhostUI));

        ghostObject.transform.SetParent(canvas.transform, false);
        _instance = ghostObject.GetComponent<InventoryDragGhostUI>();
        _instance._canvas = canvas;
        _instance.EnsureRefs();
        _instance.SetVisible(false);

        return _instance;
    }

    // Purpose: Resolves the canvas needed by this component.
    private static Canvas ResolveCanvas(Transform source)
    {
        if (source != null)
        {
            Canvas parentCanvas = source.GetComponentInParent<Canvas>();

            if (parentCanvas != null)
                return parentCanvas;
        }

        return FindAnyObjectByType<Canvas>();
    }

    // Purpose: Shows the internal UI or visual state.
    private void ShowInternal(Sprite sprite, Vector2 screenPosition)
    {
        EnsureRefs();

        if (_iconImage == null)
            return;

        _iconImage.sprite = sprite;
        _iconImage.enabled = true;

        if (_root != null)
        {
            _root.sizeDelta = _size;
            _root.SetAsLastSibling();
        }

        SetVisible(true);
        MoveInternal(screenPosition);
    }

    // Purpose: Moves internal toward its requested destination.
    private void MoveInternal(Vector2 screenPosition)
    {
        EnsureRefs();

        if (_root == null || _canvas == null)
            return;

        RectTransform canvasRect = _canvas.transform as RectTransform;

        if (canvasRect == null)
            return;

        Camera eventCamera =
            _canvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : _canvas.worldCamera;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPosition + _screenOffset,
                eventCamera,
                out Vector2 localPoint))
        {
            _root.anchoredPosition = localPoint;
        }

        _root.SetAsLastSibling();
    }

    // Purpose: Updates the visible state.
    private void SetVisible(bool visible)
    {
        _visible = visible;
        EnsureRefs();

        if (_group != null)
        {
            _group.alpha = visible ? _alpha : 0f;
            _group.interactable = false;
            _group.blocksRaycasts = false;
        }

        if (_iconImage != null)
            _iconImage.enabled = visible && _iconImage.sprite != null;
    }

    // Purpose: Ensures refs is ready before the workflow continues.
    private void EnsureRefs()
    {
        if (_root == null)
            _root = transform as RectTransform;

        if (_group == null)
            _group = GetComponent<CanvasGroup>();

        if (_group == null)
            _group = gameObject.AddComponent<CanvasGroup>();

        if (_iconImage == null)
            _iconImage = GetComponent<Image>();

        if (_iconImage == null)
            _iconImage = gameObject.AddComponent<Image>();

        if (_canvas == null)
            _canvas = GetComponentInParent<Canvas>();

        if (_root != null)
        {
            _root.anchorMin = new Vector2(0.5f, 0.5f);
            _root.anchorMax = new Vector2(0.5f, 0.5f);
            _root.pivot = new Vector2(0.5f, 0.5f);
            _root.sizeDelta = _size;
        }

        _group.interactable = false;
        _group.blocksRaycasts = false;
        _iconImage.raycastTarget = false;
        _iconImage.preserveAspect = true;
    }
}
