/*
 * Role hierarchy: Gameplay > Entities > Shooter > Player
 * Primary responsibility: Presents PlayerAimCursorUI state and handles its user-facing UI interaction.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PlayerAimCursorUI : MonoBehaviour
{
    [Header("Refs")]
    [FormerlySerializedAs("_aimAction")]
    [SerializeField] private PlayerAimController _aimController;

    [Header("Auto Refs Don't Touch")]
    [SerializeField] private RectTransform _cursorCanvasRoot;
    [Header("Cursor UI")]
/*
    [Tooltip("직접 연결 못 할 때 찾을 루트 이름")]
*/
    [Tooltip("Name of the UI root used for the runtime aim cursor.")]
    [SerializeField] private string _cursorCanvasRootName = "AimCursorRoot";

    [Header("Cursor Prefab")]
/*
    [Tooltip("Canvas 없이 RectTransform + Image들만 있는 UI 프리팹")]
*/
    [Tooltip("UI prefab with RectTransform and Image components for the aim cursor.")]
    [SerializeField] private GameObject _cursorUIPrefab;

    [Header("Cursor Colors")]
    [SerializeField] private Color _inRangeColor = Color.red;
    [SerializeField] private Color _outOfRangeColor = Color.white;
    [SerializeField] private Color _progressColorMultiplier = new Color(1f, 1f, 1f, 0.6f);

    [Header("Hit Crosshair")]
    [SerializeField] private ShootHitConfirmedEventChannelSO _shootHitConfirmedEvent;
    [SerializeField] private CanvasGroup _hitCrosshairCanvasGroup;
    [SerializeField, Min(0.01f)] private float _hitFadeOutSeconds = 0.5f;
    [SerializeField] private AnimationCurve _hitFadeCurve =
        AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    [Header("Options")]
    [SerializeField] private bool _hideCursorWhenNoAimPoint = true;
    [SerializeField] private bool _hideProgressWhenReady = true;

    [Header("Debug")]
    [SerializeField] private bool _debugLogs = false;

    private RectTransform _cursorUIRect;
    private Canvas _rootCanvas;
    private Image _crosshairImage;
    private Image _progressRingImage;

    private float _hitFeedbackRemaining;

    // Purpose: Initializes Inspector defaults for this component.
    private void Reset()
    {
        if (_aimController == null)
            _aimController = EntityComponentResolver.Resolve<PlayerAimController>(this);
    }

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        if (_aimController == null)
            _aimController = EntityComponentResolver.Resolve<PlayerAimController>(this);

        EnsureCursorCanvasRoot();
        EnsureCursorUI();

        RefreshCursorUI();
        SetCursorVisible(false);
        CacheHitCrosshair();
        HideHitCrosshairImmediate();
    }

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    private void OnEnable()
    {
        if (_shootHitConfirmedEvent != null)
            _shootHitConfirmedEvent.OnEventRaised += HandleAttackHitConfirmed;
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        if (_shootHitConfirmedEvent != null)
            _shootHitConfirmedEvent.OnEventRaised -= HandleAttackHitConfirmed;
    }

    // Purpose: Processes this component's per-frame runtime work.
    private void Update()
    {
        EnsureCursorCanvasRoot();
        EnsureCursorUI();
        UpdateCursorUIPosition();
        RefreshCursorUI();
        UpdateHitCrosshair();
    }

    // Purpose: Ensures cursor canvas root is ready before the workflow continues.
    private void EnsureCursorCanvasRoot()
    {
        if (_cursorCanvasRoot != null)
        {
            _rootCanvas = _cursorCanvasRoot.GetComponentInParent<Canvas>();
            return;
        }

        if (string.IsNullOrWhiteSpace(_cursorCanvasRootName))
            return;

        GameObject rootObject = GameObject.Find(_cursorCanvasRootName);
        if (rootObject == null)
            return;

        _cursorCanvasRoot = rootObject.GetComponent<RectTransform>();
        if (_cursorCanvasRoot != null)
            _rootCanvas = _cursorCanvasRoot.GetComponentInParent<Canvas>();

        if (_debugLogs && _cursorCanvasRoot != null)
            Debug.Log($"[PlayerAimCursorUI] Found cursor canvas root: {_cursorCanvasRoot.name}");
    }

    // Purpose: Ensures cursor ui is ready before the workflow continues.
    private void EnsureCursorUI()
    {
        if (_cursorUIRect != null)
            return;

        if (_cursorUIPrefab == null || _cursorCanvasRoot == null)
            return;

        GameObject go = Instantiate(_cursorUIPrefab, _cursorCanvasRoot);
        go.name = _cursorUIPrefab.name + "_Runtime";

        _cursorUIRect = go.GetComponent<RectTransform>();
        if (_cursorUIRect == null)
            _cursorUIRect = go.AddComponent<RectTransform>();

        DisablePointerRaycasts(go);
        CacheCursorUIComponents();
        CacheHitCrosshair();
    }

    // Purpose: Disables pointer raycasts for this component.
    private static void DisablePointerRaycasts(GameObject cursorRoot)
    {
        if (cursorRoot == null)
            return;

        Graphic[] graphics = cursorRoot.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
            graphics[i].raycastTarget = false;

        CanvasGroup[] groups = cursorRoot.GetComponentsInChildren<CanvasGroup>(true);
        for (int i = 0; i < groups.Length; i++)
        {
            groups[i].interactable = false;
            groups[i].blocksRaycasts = false;
        }
    }

    // Purpose: Caches cursor ui components for later reuse.
    private void CacheCursorUIComponents()
    {
        if (_cursorUIRect == null)
            return;

        Image[] images = _cursorUIRect.GetComponentsInChildren<Image>(true);

        _crosshairImage = null;
        _progressRingImage = null;

        for (int i = 0; i < images.Length; i++)
        {
            if (images[i].type == Image.Type.Filled)
            {
                _progressRingImage = images[i];
            }
            else if (_crosshairImage == null)
            {
                _crosshairImage = images[i];
            }
        }

        if (_progressRingImage != null)
            _progressRingImage.gameObject.SetActive(true);
    }

    // Purpose: Updates the cursor ui position from the current runtime state.
    private void UpdateCursorUIPosition()
    {
        if (_cursorUIRect == null || _cursorCanvasRoot == null || _rootCanvas == null)
            return;

        Camera uiCamera = _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : _rootCanvas.worldCamera;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _cursorCanvasRoot,
            Input.mousePosition,
            uiCamera,
            out Vector2 localPoint))
        {
            _cursorUIRect.anchoredPosition = localPoint;
        }
    }

    // Purpose: Refreshes the displayed or cached cursor ui.
    private void RefreshCursorUI()
    {
        if (_cursorUIRect == null || _aimController == null)
            return;

        bool shouldShowCursor = !_hideCursorWhenNoAimPoint || _aimController.HasAimPoint;
        SetCursorVisible(shouldShowCursor);

        if (!shouldShowCursor)
            return;

        Color baseColor = _aimController.IsAimWithinRange ? _inRangeColor : _outOfRangeColor;

        if (_crosshairImage != null)
            _crosshairImage.color = baseColor;

        if (_progressRingImage != null)
        {
            bool shouldShowProgress =
                _aimController.IsReloading ||
                _aimController.IsOnFireCooldown ||
                !_hideProgressWhenReady;
            Color ringColor = new Color(
                baseColor.r * _progressColorMultiplier.r,
                baseColor.g * _progressColorMultiplier.g,
                baseColor.b * _progressColorMultiplier.b,
                shouldShowProgress ? _progressColorMultiplier.a : 0f);

            _progressRingImage.color = ringColor;
            _progressRingImage.fillAmount = _aimController.ActiveProgress01;
        }
    }

    // Purpose: Updates the cursor visible state.
    private void SetCursorVisible(bool visible)
    {
        if (_cursorUIRect != null)
            _cursorUIRect.gameObject.SetActive(visible);
    }

    // Purpose: Caches hit crosshair for later reuse.
    private void CacheHitCrosshair()
    {
        if (_hitCrosshairCanvasGroup != null || _cursorUIRect == null)
            return;

        _hitCrosshairCanvasGroup =
            _cursorUIRect.GetComponentInChildren<CanvasGroup>(true);
    }
    // Purpose: Handles the incoming attack hit confirmed request or event.
    private void HandleAttackHitConfirmed()
    {
        EnsureCursorCanvasRoot();
        EnsureCursorUI();
        CacheHitCrosshair();

        if (_hitCrosshairCanvasGroup == null)
            return;

        _hitFeedbackRemaining = _hitFadeOutSeconds;
        _hitCrosshairCanvasGroup.alpha = 1f;
    }

    // Purpose: Updates the hit crosshair from the current runtime state.
    private void UpdateHitCrosshair()
    {
        if (_hitCrosshairCanvasGroup == null)
            return;

        if (_hitFeedbackRemaining <= 0f)
        {
            _hitCrosshairCanvasGroup.alpha = 0f;
            return;
        }

        _hitFeedbackRemaining = Mathf.Max(
            0f,
            _hitFeedbackRemaining - Time.unscaledDeltaTime);

        float t = 1f - (_hitFeedbackRemaining / Mathf.Max(0.01f, _hitFadeOutSeconds));
        float alpha = _hitFadeCurve != null
            ? _hitFadeCurve.Evaluate(t)
            : 1f - t;

        _hitCrosshairCanvasGroup.alpha = Mathf.Clamp01(alpha);
    }

    // Purpose: Hides the hit crosshair immediate UI or visual state.
    private void HideHitCrosshairImmediate()
    {
        _hitFeedbackRemaining = 0f;

        if (_hitCrosshairCanvasGroup != null)
            _hitCrosshairCanvasGroup.alpha = 0f;
    }

    // Purpose: Finds the requested child recursive.
    private static Transform FindChildRecursive(Transform root, string childName)
    {
        if (root == null || string.IsNullOrWhiteSpace(childName))
            return null;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);

            if (child.name == childName)
                return child;

            Transform nested = FindChildRecursive(child, childName);
            if (nested != null)
                return nested;
        }

        return null;
    }
}
