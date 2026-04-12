using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class VSplatterAimUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private VSplatterAimAction _aimAction;

    [Header("Auto Refs Don't Touch")]
    [SerializeField] private RectTransform _cursorCanvasRoot;
    [Header("Cursor UI")]
    [Tooltip("직접 연결 못 할 때 찾을 루트 이름")]
    [SerializeField] private string _cursorCanvasRootName = "AimCursorRoot";

    [Header("Cursor Prefab")]
    [Tooltip("Canvas 없이 RectTransform + Image들만 있는 UI 프리팹")]
    [SerializeField] private GameObject _cursorUIPrefab;

    [Header("Cursor Colors")]
    [SerializeField] private Color _inRangeColor = Color.red;
    [SerializeField] private Color _outOfRangeColor = Color.white;
    [SerializeField] private Color _progressColorMultiplier = new Color(1f, 1f, 1f, 0.6f);

    [Header("Options")]
    [SerializeField] private bool _hideCursorWhenNoAimPoint = true;
    [SerializeField] private bool _hideProgressWhenReady = true;

    [Header("Debug")]
    [SerializeField] private bool _debugLogs = false;

    private RectTransform _cursorUIRect;
    private Canvas _rootCanvas;
    private Image _crosshairImage;
    private Image _progressRingImage;

    private void Reset()
    {
        if (_aimAction == null)
            _aimAction = GetComponent<VSplatterAimAction>();
    }

    private void Awake()
    {
        if (_aimAction == null)
            _aimAction = GetComponent<VSplatterAimAction>();

        EnsureCursorCanvasRoot();
        EnsureCursorUI();

        RefreshCursorUI();
        SetCursorVisible(false);
    }

    private void Update()
    {
        EnsureCursorCanvasRoot();
        EnsureCursorUI();
        UpdateCursorUIPosition();
        RefreshCursorUI();
    }

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
            Debug.Log($"[VSplatterAimUI] Found cursor canvas root: {_cursorCanvasRoot.name}");
    }

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

        CacheCursorUIComponents();
    }

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

    private void RefreshCursorUI()
    {
        if (_cursorUIRect == null || _aimAction == null)
            return;

        bool shouldShowCursor = !_hideCursorWhenNoAimPoint || _aimAction.HasAimPoint;
        SetCursorVisible(shouldShowCursor);

        if (!shouldShowCursor)
            return;

        Color baseColor = _aimAction.IsAimWithinRange ? _inRangeColor : _outOfRangeColor;

        if (_crosshairImage != null)
            _crosshairImage.color = baseColor;

        if (_progressRingImage != null)
        {
            bool shouldShowProgress =
                _aimAction.IsReloading ||
                _aimAction.IsOnFireCooldown ||
                !_hideProgressWhenReady;
            Debug.Log($"shouldShowProgress? {_aimAction.IsReloading} || {_aimAction.IsOnFireCooldown} || {!_hideProgressWhenReady} => {shouldShowProgress}");
            Color ringColor = new Color(
                baseColor.r * _progressColorMultiplier.r,
                baseColor.g * _progressColorMultiplier.g,
                baseColor.b * _progressColorMultiplier.b,
                shouldShowProgress ? _progressColorMultiplier.a : 0f);

            _progressRingImage.color = ringColor;
            _progressRingImage.fillAmount = _aimAction.ActiveProgress01;
        }
    }

    private void SetCursorVisible(bool visible)
    {
        if (_cursorUIRect != null)
            _cursorUIRect.gameObject.SetActive(visible);
    }
}