/*
 * Role hierarchy: Presentation > UI > Gameplay > Hideout
 * Primary responsibility: Presents the Hideout stage-selection menu and sends unlocked stage entry requests to StageProgressionManager.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class HideoutStageSelectPanel : MonoBehaviour, IUIOverlayLifecycle
{
    private const int StageCount = 5;
    private const float InteractCloseGraceSeconds = 0.15f;

    private static HideoutStageSelectPanel _instance;
    private static bool _isCreating;

    private UIOverlayPanel _overlay;
    private UIOverlayManager _overlayManager;
    private InputReader _inputReader;
    private StageProgressionManager _stageProgressionManager;
    private Button[] _stageButtons;
    private TextMeshProUGUI[] _stageButtonLabels;
    private bool _isOpen;
    private float _openedAt;

    // Purpose: Ensures instance is ready before the workflow continues.
    public static void EnsureInstance()
    {
        if (_instance != null || _isCreating)
            return;

        _isCreating = true;

        GameObject root = new GameObject(
            nameof(HideoutStageSelectPanel),
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster),
            typeof(CanvasGroup),
            typeof(UIOverlayPanel),
            typeof(HideoutStageSelectPanel));

        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 450;

        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        _isCreating = false;
    }

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;

        _overlay = GetComponent<UIOverlayPanel>();
        _overlay.ConfigureRuntime(
            UIOverlayId.StageSelect,
            GetComponent<CanvasGroup>(),
            true,
            true);

        BuildRuntimeUi();
        ResolveRuntimeReferences();
    }

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    private void OnEnable()
    {
        HideoutStageSelectStation.Requested += HandleStageSelectRequested;
        SubscribeInputReader();
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        HideoutStageSelectStation.Requested -= HandleStageSelectRequested;
        UnsubscribeInputReader();
    }

    // Purpose: Releases owned resources before this component is destroyed.
    private void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }

    // Purpose: Processes this component's per-frame runtime work.
    private void Update()
    {
        if (!_isOpen || Keyboard.current == null)
            return;

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
            Close();
    }

    // Purpose: Responds to the overlay shown callback.
    public void OnOverlayShown()
    {
        _isOpen = true;
        _openedAt = Time.unscaledTime;
        RefreshStageButtons();
    }

    // Purpose: Responds to the overlay hidden callback.
    public void OnOverlayHidden()
    {
        _isOpen = false;
    }

    // Purpose: Handles the incoming stage select requested request or event.
    private void HandleStageSelectRequested(HideoutStageSelectOpenRequest request)
    {
        Open();
    }

    // Purpose: Handles the incoming interact input request or event.
    private void HandleInteractInput()
    {
        if (!_isOpen || Time.unscaledTime < _openedAt + InteractCloseGraceSeconds)
            return;

        Close();
    }

    // Purpose: Opens the open UI or workflow.
    private void Open()
    {
        ResolveRuntimeReferences();
        RefreshStageButtons();

        if (_overlayManager != null)
        {
            _overlayManager.RegisterPanel(_overlay);
            _overlayManager.Open(UIOverlayId.StageSelect);
            return;
        }

        _overlay.Show();
    }

    // Purpose: Closes the active close UI or workflow.
    private void Close()
    {
        if (!_isOpen)
            return;

        if (_overlayManager != null)
        {
            _overlayManager.Close(UIOverlayId.StageSelect);
            return;
        }

        _overlay.Hide();
    }

    // Purpose: Attempts to enter stage and reports whether the request succeeded.
    private void TryEnterStage(int stageIndex)
    {
        ResolveRuntimeReferences();

        if (_stageProgressionManager == null)
        {
            Debug.LogError(
                "[HideoutStageSelectPanel] StageProgressionManager is required to enter a stage.",
                this);
            return;
        }

        if (!_stageProgressionManager.IsStageUnlocked(stageIndex))
            return;

        if (_stageProgressionManager.TryEnterUnlockedStage(stageIndex))
            Close();
    }

    // Purpose: Resolves the runtime references needed by this component.
    private void ResolveRuntimeReferences()
    {
        if (_overlayManager == null)
            _overlayManager = FindAnyObjectByType<UIOverlayManager>(FindObjectsInactive.Include);

        if (_inputReader == null)
        {
            _inputReader = FindAnyObjectByType<InputReader>(FindObjectsInactive.Include);
            SubscribeInputReader();
        }

        if (_stageProgressionManager == null)
            _stageProgressionManager = FindAnyObjectByType<StageProgressionManager>(FindObjectsInactive.Include);
    }

    // Purpose: Subscribes to input reader notifications.
    private void SubscribeInputReader()
    {
        if (_inputReader == null)
            return;

        _inputReader.InteractEvent -= HandleInteractInput;
        _inputReader.InteractEvent += HandleInteractInput;
    }

    // Purpose: Removes the input reader notification subscription.
    private void UnsubscribeInputReader()
    {
        if (_inputReader != null)
            _inputReader.InteractEvent -= HandleInteractInput;
    }

    // Purpose: Refreshes the displayed or cached stage buttons.
    private void RefreshStageButtons()
    {
        ResolveRuntimeReferences();

        for (int i = 0; i < StageCount; i++)
        {
            int stageIndex = i + 1;
            bool unlocked = _stageProgressionManager != null &&
                            _stageProgressionManager.IsStageUnlocked(stageIndex);

            if (_stageButtons != null && _stageButtons[i] != null)
            {
                _stageButtons[i].interactable = unlocked;

                Image image = _stageButtons[i].GetComponent<Image>();
                if (image != null)
                {
                    image.color = unlocked
                        ? new Color(0.04f, 0.37f, 0.46f, 1f)
                        : new Color(0.09f, 0.12f, 0.15f, 0.9f);
                }
            }

            if (_stageButtonLabels != null && _stageButtonLabels[i] != null)
            {
                _stageButtonLabels[i].text = unlocked
                    ? $"STAGE {i + 1}\nENTER"
                    : $"STAGE {i + 1}\nLOCKED";
                _stageButtonLabels[i].color = unlocked
                    ? Color.white
                    : new Color(0.55f, 0.58f, 0.6f, 1f);
            }
        }
    }

    // Purpose: Builds the runtime ui from the current authored data.
    private void BuildRuntimeUi()
    {
        RectTransform root = transform as RectTransform;
        SetFullStretch(root);

        Image backdrop = CreateImage("Backdrop", transform, new Color(0f, 0f, 0f, 0.72f));
        SetFullStretch(backdrop.rectTransform);

        Image frame = CreateImage("StageSelectFrame", transform, new Color(0.015f, 0.09f, 0.13f, 0.98f));
        RectTransform frameRect = frame.rectTransform;
        frameRect.anchorMin = frameRect.anchorMax = new Vector2(0.5f, 0.5f);
        frameRect.pivot = new Vector2(0.5f, 0.5f);
        frameRect.sizeDelta = new Vector2(660f, 610f);
        frameRect.anchoredPosition = Vector2.zero;

        Outline frameOutline = frame.gameObject.AddComponent<Outline>();
        frameOutline.effectColor = new Color(0f, 0.85f, 1f, 0.8f);
        frameOutline.effectDistance = new Vector2(2f, -2f);

        TextMeshProUGUI title = CreateText("Title", frame.transform, 38f, FontStyles.Bold, TextAlignmentOptions.Center);
        title.text = UIRuntimeLocalization.Get(
            "UI_PlayerTimer",
            "Hideout_StageSelectTitle",
            "Stage Select");
        title.color = new Color(0.72f, 0.96f, 1f, 1f);
        RectTransform titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(0.08f, 0.82f);
        titleRect.anchorMax = new Vector2(0.92f, 0.94f);
        titleRect.offsetMin = titleRect.offsetMax = Vector2.zero;

        TextMeshProUGUI subtitle = CreateText("Subtitle", frame.transform, 18f, FontStyles.Normal, TextAlignmentOptions.Center);
        subtitle.text = UIRuntimeLocalization.Get(
            "UI_PlayerTimer",
            "Hideout_StageSelectInstruction",
            "Choose an unlocked stage to deploy.");
        subtitle.color = new Color(0.66f, 0.76f, 0.8f, 1f);
        RectTransform subtitleRect = subtitle.rectTransform;
        subtitleRect.anchorMin = new Vector2(0.08f, 0.75f);
        subtitleRect.anchorMax = new Vector2(0.92f, 0.82f);
        subtitleRect.offsetMin = subtitleRect.offsetMax = Vector2.zero;

        Button closeButton = CreateButton("CloseButton", frame.transform, new Color(0.35f, 0.08f, 0.1f, 1f));
        closeButton.onClick.AddListener(Close);
        RectTransform closeRect = closeButton.GetComponent<RectTransform>();
        closeRect.anchorMin = closeRect.anchorMax = new Vector2(0.93f, 0.93f);
        closeRect.pivot = new Vector2(0.5f, 0.5f);
        closeRect.sizeDelta = new Vector2(52f, 52f);
        TextMeshProUGUI closeText = CreateText("Label", closeButton.transform, 28f, FontStyles.Bold, TextAlignmentOptions.Center);
        closeText.text = "X";
        SetFullStretch(closeText.rectTransform);

        RectTransform buttonRoot = CreateRect("StageButtons", frame.transform);
        buttonRoot.anchorMin = new Vector2(0.12f, 0.14f);
        buttonRoot.anchorMax = new Vector2(0.88f, 0.71f);
        buttonRoot.offsetMin = buttonRoot.offsetMax = Vector2.zero;

        VerticalLayoutGroup layout = buttonRoot.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.spacing = 10f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;
        layout.padding = new RectOffset(0, 0, 0, 0);

        _stageButtons = new Button[StageCount];
        _stageButtonLabels = new TextMeshProUGUI[StageCount];

        for (int i = 0; i < StageCount; i++)
        {
            int stageIndex = i + 1;
            Button stageButton = CreateButton($"Stage{stageIndex}Button", buttonRoot, new Color(0.04f, 0.37f, 0.46f, 1f));
            stageButton.onClick.AddListener(() => TryEnterStage(stageIndex));
            stageButton.gameObject.AddComponent<LayoutElement>().preferredHeight = 78f;

            TextMeshProUGUI stageLabel = CreateText("Label", stageButton.transform, 21f, FontStyles.Bold, TextAlignmentOptions.Center);
            SetFullStretch(stageLabel.rectTransform);

            _stageButtons[i] = stageButton;
            _stageButtonLabels[i] = stageLabel;
        }

        TextMeshProUGUI footer = CreateText("Footer", frame.transform, 17f, FontStyles.Normal, TextAlignmentOptions.Center);
        footer.text = UIRuntimeLocalization.Get(
            "UI_PlayerTimer",
            "Hideout_StageSelectFooter",
            "Esc / Interact: Close");
        footer.color = new Color(0.6f, 0.72f, 0.76f, 1f);
        RectTransform footerRect = footer.rectTransform;
        footerRect.anchorMin = new Vector2(0.08f, 0.045f);
        footerRect.anchorMax = new Vector2(0.92f, 0.11f);
        footerRect.offsetMin = footerRect.offsetMax = Vector2.zero;
    }

    // Purpose: Creates the rect owned by this workflow.
    private static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
        rectTransform.SetParent(parent, false);
        return rectTransform;
    }

    // Purpose: Creates the image owned by this workflow.
    private static Image CreateImage(string name, Transform parent, Color color)
    {
        RectTransform rectTransform = CreateRect(name, parent);
        Image image = rectTransform.gameObject.AddComponent<Image>();
        image.color = color;
        return image;
    }

    // Purpose: Creates the button owned by this workflow.
    private static Button CreateButton(string name, Transform parent, Color color)
    {
        Image image = CreateImage(name, parent, color);
        Button button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.16f, 1.16f, 1.16f, 1f);
        colors.pressedColor = new Color(0.72f, 0.72f, 0.72f, 1f);
        colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.6f);
        button.colors = colors;
        return button;
    }

    // Purpose: Creates the text owned by this workflow.
    private static TextMeshProUGUI CreateText(
        string name,
        Transform parent,
        float fontSize,
        FontStyles fontStyle,
        TextAlignmentOptions alignment)
    {
        RectTransform rectTransform = CreateRect(name, parent);
        TextMeshProUGUI text = rectTransform.gameObject.AddComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = alignment;
        text.enableWordWrapping = false;
        text.raycastTarget = false;
        return text;
    }

    // Purpose: Updates the full stretch state.
    private static void SetFullStretch(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }
}
