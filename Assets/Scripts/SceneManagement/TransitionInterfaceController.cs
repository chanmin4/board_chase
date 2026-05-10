using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using PixeLadder.EasyTransition;

public class TransitionInterfaceController : MonoBehaviour
{
    private static readonly int RectSizeID = Shader.PropertyToID("_RectSize");

    [Header("Refs")]
    [Tooltip("Receives transition requests from gameplay systems.")]
    [SerializeField] private ScreenTransitionRequestEventChannelSO _transitionRequestChannel;


    [Tooltip("Screen-covering image prefab used by Easy Transition effects.")]
    [SerializeField] private Image _transitionImagePrefab;

    [Tooltip("Default effect used when the request has no custom effect.")]
    [SerializeField] private TransitionEffect _defaultEffect;

    [Header("Canvas Don't Ref  Auto")]
    [Tooltip("Optional parent canvas. If null, this controller creates a Screen Space Overlay canvas.")]
    [SerializeField] private Canvas _transitionCanvas;

    [SerializeField] private int _sortingOrder = 999;

    private Image _transitionImage;
    private Material _runtimeMaterial;
    private bool _isTransitioning;

    private void Awake()
    {
        EnsureTransitionImage();
        HideTransitionImage();
    }

    private void OnEnable()
    {
        if (_transitionRequestChannel != null)
            _transitionRequestChannel.OnEventRaised += HandleTransitionRequest;
    }

    private void OnDisable()
    {
        if (_transitionRequestChannel != null)
            _transitionRequestChannel.OnEventRaised -= HandleTransitionRequest;
    }

    private void HandleTransitionRequest(ScreenTransitionRequest request)
    {
        if (_isTransitioning)
        {
            Debug.LogWarning("[TransitionInterfaceController] Transition already running. Running request without visual transition.", this);

            request?.OnCovered?.Invoke();
            request?.OnComplete?.Invoke();
            return;
        }

        StartCoroutine(RunTransition(request));
    }

    private IEnumerator RunTransition(ScreenTransitionRequest request)
    {
        TransitionEffect effect = request != null && request.Effect != null
            ? request.Effect
            : _defaultEffect;

        if (_transitionImage == null || effect == null)
        {
            request?.OnCovered?.Invoke();
            request?.OnComplete?.Invoke();
            yield break;
        }

        _isTransitioning = true;
        _transitionImage.gameObject.SetActive(true);

        SetupMaterial(effect);

        yield return effect.AnimateOut(_transitionImage);

        request?.OnCovered?.Invoke();

        if (request != null && request.CoveredHoldSeconds > 0f)
            yield return new WaitForSeconds(request.CoveredHoldSeconds);

        yield return effect.AnimateIn(_transitionImage);

        HideTransitionImage();
        CleanupMaterial();

        request?.OnComplete?.Invoke();
        _isTransitioning = false;
    }

    private void EnsureTransitionImage()
    {

        if (_transitionCanvas == null)
            _transitionCanvas = CreateTransitionCanvas();

        if (_transitionImagePrefab == null)
        {
            Debug.LogError("[TransitionInterfaceController] Transition image prefab is missing.", this);
            return;
        }

        _transitionImage = Instantiate(_transitionImagePrefab, _transitionCanvas.transform);
        StretchToFullScreen(_transitionImage.rectTransform);
    }

    private Canvas CreateTransitionCanvas()
    {
        GameObject canvasObject = new GameObject("TransitionCanvas");
        canvasObject.transform.SetParent(transform);

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = _sortingOrder;

        canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();

        return canvas;
    }

    private void StretchToFullScreen(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.sizeDelta = Vector2.zero;
        rectTransform.anchoredPosition = Vector2.zero;
    }

    private void SetupMaterial(TransitionEffect effect)
    {
        CleanupMaterial();

        _runtimeMaterial = new Material(effect.transitionMaterial);

        Rect rect = _transitionImage.rectTransform.rect;
        _runtimeMaterial.SetVector(RectSizeID, new Vector4(rect.width, rect.height, 0f, 0f));

        effect.SetEffectProperties(_runtimeMaterial);
        _transitionImage.material = _runtimeMaterial;
    }

    private void HideTransitionImage()
    {
        if (_transitionImage != null)
            _transitionImage.gameObject.SetActive(false);
    }

    private void CleanupMaterial()
    {
        if (_runtimeMaterial == null)
            return;

        if (_transitionImage != null)
            _transitionImage.material = null;

        Destroy(_runtimeMaterial);
        _runtimeMaterial = null;
    }
}
