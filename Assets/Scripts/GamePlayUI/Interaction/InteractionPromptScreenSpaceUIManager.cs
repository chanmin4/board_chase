using UnityEngine;

[DisallowMultipleComponent]
public class InteractionPromptScreenSpaceUIManager : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RectTransform _widgetRoot;
    [SerializeField] private InteractionPromptWidget _promptPrefab;

    [Header("Listening To")]
    [SerializeField] private WorldCameraEventChannelSO _worldCameraReadyChannel;
    [SerializeField] private InteractionPromptEventChannelSO _promptChannel;

    [Header("Follow")]
    [SerializeField] private Vector2 _screenPixelOffset = new Vector2(0f, 36f);
    [SerializeField] private bool _hideWhenBehindCamera = true;
    [SerializeField] private bool _hideWhenOffScreen = true;
    [SerializeField] private bool _clampToScreen = true;
    [SerializeField] private float _screenEdgePadding = 32f;

    private Camera _worldCamera;
    private InteractionPromptWidget _widget;
    private InteractionPromptSnapshot _current;
    private bool _hasPrompt;

    private void Awake()
    {
        EnsureWidget();

        if (_widget != null)
            _widget.SetVisible(false);
    }

    private void OnEnable()
    {
        if (_worldCameraReadyChannel != null)
        {
            _worldCameraReadyChannel.OnEventRaised += OnWorldCameraChanged;

            if (_worldCameraReadyChannel.Current != null)
                OnWorldCameraChanged(_worldCameraReadyChannel.Current);
        }

        if (_promptChannel != null)
            _promptChannel.OnEventRaised += HandlePromptChanged;
    }

    private void OnDisable()
    {
        if (_worldCameraReadyChannel != null)
            _worldCameraReadyChannel.OnEventRaised -= OnWorldCameraChanged;

        if (_promptChannel != null)
            _promptChannel.OnEventRaised -= HandlePromptChanged;
    }

    private void LateUpdate()
    {
        if (!_hasPrompt || _current.Anchor == null || _worldCamera == null || _widgetRoot == null || _widget == null)
            return;

        Vector3 screenPos = _worldCamera.WorldToScreenPoint(_current.Anchor.position);

        if (_hideWhenBehindCamera && screenPos.z <= 0f)
        {
            _widget.SetVisible(false);
            return;
        }

        bool isOffScreen =
            screenPos.x < 0f || screenPos.x > Screen.width ||
            screenPos.y < 0f || screenPos.y > Screen.height;

        if (_hideWhenOffScreen && isOffScreen)
        {
            _widget.SetVisible(false);
            return;
        }

        Vector2 finalScreen = new Vector2(screenPos.x, screenPos.y);

        if (_clampToScreen)
        {
            finalScreen.x = Mathf.Clamp(finalScreen.x, _screenEdgePadding, Screen.width - _screenEdgePadding);
            finalScreen.y = Mathf.Clamp(finalScreen.y, _screenEdgePadding, Screen.height - _screenEdgePadding);
        }

        finalScreen += _screenPixelOffset;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _widgetRoot,
            finalScreen,
            null,
            out Vector2 localPoint);

        _widget.SetScreenPosition(localPoint);
        _widget.SetVisible(true);
    }

    private void HandlePromptChanged(InteractionPromptSnapshot snapshot)
    {
        EnsureWidget();

        _current = snapshot;
        _hasPrompt = snapshot.Visible && snapshot.Anchor != null;

        if (_widget == null)
            return;

        if (!_hasPrompt)
        {
            _widget.SetVisible(false);
            return;
        }

        _widget.Bind(snapshot);
        _widget.SetVisible(true);
    }

    private void OnWorldCameraChanged(Camera camera)
    {
        _worldCamera = camera;
    }

    private void EnsureWidget()
    {
        if (_widget != null || _promptPrefab == null || _widgetRoot == null)
            return;

        _widget = Instantiate(_promptPrefab, _widgetRoot);
    }
}
