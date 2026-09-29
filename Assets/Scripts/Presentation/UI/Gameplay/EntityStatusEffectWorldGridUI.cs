/*
 * Role hierarchy: Presentation > UI > Gameplay > World Status Effects
 * Primary responsibility: Renders active entity status effects beneath an entity UI anchor as a centred world-space grid.
 * Execution boundary: Runtime (Assembly-CSharp)
 */
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class EntityStatusEffectWorldGridUI : MonoBehaviour
{
    private sealed class StatusIconView
    {
        private readonly RectTransform _root;
        private readonly Image _icon;
        private readonly Image _durationFill;

        public StatusIconView(Transform parent, float size)
        {
            GameObject root = new GameObject("StatusEffectIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            root.transform.SetParent(parent, false);
            _root = root.GetComponent<RectTransform>();
            _root.sizeDelta = new Vector2(size, size);

            Image background = root.GetComponent<Image>();
            background.color = new Color(0f, 0f, 0f, 0.58f);
            background.raycastTarget = false;

            _icon = CreateImage("Icon", root.transform);
            _icon.rectTransform.anchorMin = Vector2.zero;
            _icon.rectTransform.anchorMax = Vector2.one;
            _icon.rectTransform.offsetMin = new Vector2(2f, 2f);
            _icon.rectTransform.offsetMax = new Vector2(-2f, -2f);
            _icon.preserveAspect = true;

            _durationFill = CreateImage("DurationFill", root.transform);
            _durationFill.rectTransform.anchorMin = Vector2.zero;
            _durationFill.rectTransform.anchorMax = Vector2.one;
            _durationFill.rectTransform.offsetMin = Vector2.zero;
            _durationFill.rectTransform.offsetMax = Vector2.zero;
            _durationFill.type = Image.Type.Filled;
            _durationFill.fillMethod = Image.FillMethod.Radial360;
            _durationFill.fillOrigin = (int)Image.Origin360.Top;
            _durationFill.fillClockwise = true;
            _durationFill.color = new Color(0f, 0f, 0f, 0.62f);
            _durationFill.raycastTarget = false;
        }

        // Purpose: Updates the active state.
        public void SetActive(bool isActive)
        {
            _root.gameObject.SetActive(isActive);
        }

        // Purpose: Binds bind to the active runtime or UI target.
        public void Bind(EntityStatusEffectRuntime.ActiveStatusEffect activeEffect, Vector2 position)
        {
            StatusEffectDefinitionSO definition = activeEffect.Definition;
            _root.anchoredPosition = position;
            _icon.sprite = StatusEffectIconStripUI.GetIcon(definition);
            _icon.color = definition != null ? definition.IconColor : Color.white;

            float duration = definition != null ? Mathf.Max(0.05f, definition.DurationSeconds) : 0.05f;
            _durationFill.fillAmount = Mathf.Clamp01(activeEffect.RemainingSeconds / duration);
            _root.gameObject.name = definition != null ? $"StatusEffectIcon_{definition.Id}" : "StatusEffectIcon";
        }

        // Purpose: Creates the image owned by this workflow.
        private static Image CreateImage(string name, Transform parent)
        {
            GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imageObject.transform.SetParent(parent, false);
            Image image = imageObject.GetComponent<Image>();
            image.raycastTarget = false;
            return image;
        }
    }

    [Header("Runtime Source")]
    [Tooltip("Optional explicit source. When empty, EntityStatusEffectRuntime is resolved from this entity.")]
    [SerializeField] private EntityStatusEffectRuntime _statusEffects;

    [Tooltip("Optional world anchor. When empty, a StatusEffectAnchor is resolved or created beneath the entity's feet.")]
    [SerializeField] private Transform _uiAnchor;

    [Header("World Grid Layout")]
    [Tooltip("Local offset from the foot-level StatusEffectAnchor.")]
    [SerializeField] private Vector3 _localOffset = new(0f, -0.12f, 0f);

    [Tooltip("Maximum icons in one centered row before the next row is added above it.")]
    [SerializeField, Min(1)] private int _maxColumns = 6;

    [Tooltip("One status icon size in world-canvas pixels.")]
    [SerializeField, Min(8f)] private float _iconPixelSize = 26f;

    [Tooltip("Gap in world-canvas pixels between neighboring status icons.")]
    [SerializeField, Min(0f)] private float _iconSpacingPixels = 3f;

    [Tooltip("World-space canvas scale. Keep this small so the grid remains attached to the entity.")]
    [SerializeField, Min(0.001f)] private float _worldCanvasScale = 0.0125f;

    [Tooltip("Sorting order above entity sprites while remaining below screen-space menus.")]
    [SerializeField] private int _sortingOrder = 120;

    private readonly List<StatusIconView> _views = new();
    private RectTransform _gridRoot;
    private bool _subscribed;

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        ResolveDependencies();
        EnsureCanvas();
    }

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    private void OnEnable()
    {
        ResolveDependencies();
        Subscribe();
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        Unsubscribe();
    }

    // Purpose: Processes frame work that must run after the regular Update pass.
    private void LateUpdate()
    {
        ResolveDependencies();
        EnsureCanvas();
        RefreshViews();
        FaceActiveCamera();
    }

    /// <summary>
    /// Allows the entity runtime to explicitly provide its source after it creates this presenter.
    /// </summary>
    public void Bind(EntityStatusEffectRuntime statusEffects)
    {
        if (_statusEffects == statusEffects)
            return;

        Unsubscribe();
        _statusEffects = statusEffects;
        Subscribe();
    }

    // Purpose: Resolves the dependencies needed by this component.
    private void ResolveDependencies()
    {
        if (_statusEffects == null)
            _statusEffects = GetComponent<EntityStatusEffectRuntime>();

        if (_uiAnchor == null)
            _uiAnchor = ResolveUIAnchor();
    }

    // Purpose: Resolves the ui anchor needed by this component.
    private Transform ResolveUIAnchor()
    {
        Transform[] transforms = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform candidate = transforms[i];
            if (string.Equals(candidate.name, "StatusEffectAnchor", StringComparison.Ordinal))
                return candidate;
        }

        return CreateFootStatusEffectAnchor();
    }

    // Purpose: Creates the foot status effect anchor owned by this workflow.
    private Transform CreateFootStatusEffectAnchor()
    {
        GameObject anchorObject = new GameObject("StatusEffectAnchor");
        Transform anchor = anchorObject.transform;
        anchor.SetParent(transform, true);
        anchor.position = ResolveFootAnchorPosition();
        anchor.rotation = Quaternion.identity;
        return anchor;
    }

    // Purpose: Resolves the foot anchor position needed by this component.
    private Vector3 ResolveFootAnchorPosition()
    {
        bool foundBounds = false;
        float lowestY = float.PositiveInfinity;
        Vector3 center = transform.position;

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || !renderer.enabled)
                continue;

            Bounds bounds = renderer.bounds;
            if (!foundBounds)
                center = bounds.center;

            lowestY = Mathf.Min(lowestY, bounds.min.y);
            foundBounds = true;
        }

        if (!foundBounds)
        {
            Collider[] colliders = GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider collider = colliders[i];
                if (collider == null || !collider.enabled || collider.isTrigger)
                    continue;

                Bounds bounds = collider.bounds;
                if (!foundBounds)
                    center = bounds.center;

                lowestY = Mathf.Min(lowestY, bounds.min.y);
                foundBounds = true;
            }
        }

        if (!foundBounds)
            return transform.position + Vector3.down * 0.12f;

        center.y = lowestY - 0.06f;
        return center;
    }

    // Purpose: Ensures canvas is ready before the workflow continues.
    private void EnsureCanvas()
    {
        if (_gridRoot != null)
            return;

        Transform anchor = _uiAnchor != null ? _uiAnchor : transform;
        GameObject root = new GameObject("StatusEffectWorldGrid", typeof(RectTransform), typeof(Canvas));
        root.layer = anchor.gameObject.layer;
        root.transform.SetParent(anchor, false);

        _gridRoot = root.GetComponent<RectTransform>();
        _gridRoot.anchorMin = new Vector2(0.5f, 0.5f);
        _gridRoot.anchorMax = new Vector2(0.5f, 0.5f);
        _gridRoot.pivot = new Vector2(0.5f, 0f);
        _gridRoot.localPosition = _localOffset;
        _gridRoot.localRotation = Quaternion.identity;
        _gridRoot.localScale = Vector3.one * _worldCanvasScale;
        _gridRoot.sizeDelta = new Vector2((_iconPixelSize + _iconSpacingPixels) * _maxColumns, _iconPixelSize * 2f);

        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.overrideSorting = true;
        canvas.sortingOrder = _sortingOrder;
        canvas.worldCamera = Camera.main;
        root.SetActive(false);
    }

    // Purpose: Refreshes the displayed or cached views.
    private void RefreshViews()
    {
        if (_gridRoot == null)
            return;

        IReadOnlyList<EntityStatusEffectRuntime.ActiveStatusEffect> activeEffects = _statusEffects != null
            ? _statusEffects.ActiveEffects
            : null;

        int effectCount = activeEffects != null ? activeEffects.Count : 0;
        if (effectCount == 0)
        {
            if (_gridRoot.gameObject.activeSelf)
                _gridRoot.gameObject.SetActive(false);

            return;
        }

        if (!_gridRoot.gameObject.activeSelf)
            _gridRoot.gameObject.SetActive(true);

        EnsureViewCount(effectCount);

        float step = _iconPixelSize + _iconSpacingPixels;
        for (int index = 0; index < effectCount; index++)
        {
            int row = index / _maxColumns;
            int column = index % _maxColumns;
            int firstIndexInRow = row * _maxColumns;
            int rowCount = Mathf.Min(_maxColumns, effectCount - firstIndexInRow);
            float x = (column - ((rowCount - 1) * 0.5f)) * step;
            float y = row * step;

            StatusIconView view = _views[index];
            view.SetActive(true);
            view.Bind(activeEffects[index], new Vector2(x, y));
        }

        for (int index = effectCount; index < _views.Count; index++)
            _views[index].SetActive(false);
    }

    // Purpose: Ensures view count is ready before the workflow continues.
    private void EnsureViewCount(int requiredCount)
    {
        while (_views.Count < requiredCount)
            _views.Add(new StatusIconView(_gridRoot, _iconPixelSize));
    }

    // Purpose: Aligns this component toward active camera.
    private void FaceActiveCamera()
    {
        if (_gridRoot == null || !_gridRoot.gameObject.activeInHierarchy)
            return;

        Camera camera = Camera.main;
        if (camera == null)
            return;

        Canvas canvas = _gridRoot.GetComponent<Canvas>();
        if (canvas != null && canvas.worldCamera != camera)
            canvas.worldCamera = camera;

        Vector3 direction = camera.transform.position - _gridRoot.position;
        if (direction.sqrMagnitude > 0.0001f)
            _gridRoot.rotation = Quaternion.LookRotation(direction, camera.transform.up);
    }

    // Purpose: Subscribes to subscribe notifications.
    private void Subscribe()
    {
        if (_subscribed || _statusEffects == null)
            return;

        _statusEffects.OnChanged += HandleStatusEffectsChanged;
        _subscribed = true;
    }

    // Purpose: Removes the unsubscribe notification subscription.
    private void Unsubscribe()
    {
        if (!_subscribed || _statusEffects == null)
            return;

        _statusEffects.OnChanged -= HandleStatusEffectsChanged;
        _subscribed = false;
    }

    // Purpose: Handles the incoming status effects changed request or event.
    private void HandleStatusEffectsChanged(EntityStatusEffectRuntime _)
    {
        // The grid also refreshes every frame to animate the duration fill. This callback
        // keeps structural changes immediate without needing a dedicated prefab per effect.
    }
}
