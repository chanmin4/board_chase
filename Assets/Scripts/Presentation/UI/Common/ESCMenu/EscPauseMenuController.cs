/*
 * Role hierarchy: Presentation > UI > Common > ESCMenu
 * Primary responsibility: Coordinates the EscPauseMenuController subsystem lifecycle and collaborating objects.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-950)]
public class EscPauseMenuController : MonoBehaviour
{
    [Header("Core")]
    [SerializeField] private InputReader _inputReader;
    [SerializeField] private GameStateSO _gameState;
    [Tooltip("Restores the input mode required by overlays that remain open behind the ESC menu.")]
    [SerializeField] private UIOverlayManager _overlayManager;

    [Header("View")]
    [SerializeField] private Canvas _canvas;
    [SerializeField] private int _sortingOrder = 10000;
    [SerializeField] private CanvasGroup _rootGroup;
    [SerializeField] private CanvasGroup _pauseMenuGroup;
    [SerializeField] private CanvasGroup _settingsGroup;
    [SerializeField] private UIPause _pauseView;
    [SerializeField] private UISettingsController _settingsController;

    [Header("Pause Backdrop")]
    [Tooltip("Blur shader applied once to a downsampled screenshot when Pause opens. The captured frame includes gameplay Overlay UI behind the ESC menu.")]
    [SerializeField] private Shader _pauseBlurShader;
    [Tooltip("Pause screenshot resolution divisor. 4 captures a 3840x2160 screen at 960x540 before upscaling it behind the menu.")]
    [SerializeField, Min(1)] private int _pauseBackdropDownsample = 4;
    [Tooltip("Number of inexpensive blur passes applied only when Pause opens.")]
    [SerializeField, Range(1, 4)] private int _pauseBlurPasses = 3;
    [Tooltip("Sample distance per blur pass. Higher values produce a softer background.")]
    [SerializeField, Range(0.5f, 4f)] private float _pauseBlurOffset = 1.5f;
    [Tooltip("Brightness of the blurred background. 0.5 keeps the paused scene readable while drawing focus to the ESC menu.")]
    [SerializeField, Range(0.1f, 1f)] private float _pauseBackdropBrightness = 0.5f;

    [Header("Scene")]
    [SerializeField] private LoadEventChannelSO _loadMenuChannel;
    [SerializeField] private GameSceneSO _mainMenuScene;

    [Header("Emergency")]
    [SerializeField] private PlayerSectorCenterRescue _sectorCenterRescue;
    [Header("Confirmation Popup")]
    [SerializeField] private UIPopup _confirmationPopup;
    [SerializeField] private CanvasGroup _confirmationPopupGroup;

    private enum PauseConfirmationAction
    {
        None,
        BackToMenu,
        Quit
    }
    private PauseConfirmationAction _pendingConfirmationAction;
    private bool _confirmationOpen;
    private bool _isOpen;
    private bool _settingsOpen;
    private GameState _previousState;
    private RawImage _pauseBackdropImage;
    private Material _pauseBlurMaterial;
    private RenderTexture _pauseBackdropTexture;
    private Coroutine _pauseBackdropRoutine;
    private bool _missingBlurShaderLogged;

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        if (_overlayManager == null)
            _overlayManager = FindFirstObjectByType<UIOverlayManager>(FindObjectsInactive.Include);

        if (_canvas != null)
            _canvas.sortingOrder = _sortingOrder;

        EnsurePauseBackdropView();
        SetPauseBackdropVisible(false);
        SetVisible(_rootGroup, false);
        SetVisible(_pauseMenuGroup, true);
        SetVisible(_settingsGroup, false);
    }

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    private void OnEnable()
    {
        if (_pauseView != null)
        {
            _pauseView.Resumed += ResumeGame;
            _pauseView.SettingsScreenOpened += OpenSettings;
            _pauseView.BackToMainRequested += ShowBackToMenuConfirmation;
            _pauseView.QuitRequested += ShowQuitConfirmation;
            _pauseView.SectorCenterRescueRequested += MovePlayerToCurrentSectorCenter;
        }

        if (_settingsController != null)
            _settingsController.Closed += CloseSettings;

        if (_confirmationPopup != null)
        {
            _confirmationPopup.ConfirmationResponseAction += HandleConfirmationResponse;
            _confirmationPopup.ClosePopupAction += HideConfirmationPopup;
        }
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        CancelPauseBackdropCapture();
        ReleasePauseBackdropTexture();

        if (_pauseView != null)
        {
            _pauseView.Resumed -= ResumeGame;
            _pauseView.SettingsScreenOpened -= OpenSettings;
            _pauseView.BackToMainRequested -= ShowBackToMenuConfirmation;
            _pauseView.QuitRequested -= ShowQuitConfirmation;   
            _pauseView.SectorCenterRescueRequested -= MovePlayerToCurrentSectorCenter;
        }

        if (_settingsController != null)
            _settingsController.Closed -= CloseSettings;

        if (_confirmationPopup != null)
        {
            _confirmationPopup.ConfirmationResponseAction -= HandleConfirmationResponse;
            _confirmationPopup.ClosePopupAction -= HideConfirmationPopup;
        }
    }

    // Purpose: Releases owned resources before this component is destroyed.
    private void OnDestroy()
    {
        if (_pauseBlurMaterial == null)
            return;

        if (Application.isPlaying)
            Destroy(_pauseBlurMaterial);
        else
            DestroyImmediate(_pauseBlurMaterial);
    }

    // Purpose: Processes this component's per-frame runtime work.
    private void Update()
    {
            


        if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame)
            return;
        HandleEscapePressed();
    }

    // Gameplay popup dismissal precedes Pause. Pause submenus retain their existing back order.
    public void HandleEscapePressed()
    {
        if (UIEscapeRouter.IsConsumed) return;
        if (!_isOpen && !_confirmationOpen)
        {
            if (UIEscapeRouter.TryCloseTopPopup()) return;
            if (_overlayManager == null)
                _overlayManager = FindFirstObjectByType<UIOverlayManager>();
            if (_overlayManager != null && _overlayManager.isActiveAndEnabled && _overlayManager.HasOpenOverlays)
            {
                UIEscapeRouter.TryConsume();
                _overlayManager.TryCloseTopOverlay();
                return;
            }
        }
        if (!UIEscapeRouter.TryConsume()) return;
        if (_confirmationOpen)
        {
            HideConfirmationPopup();
            return;
        }
        if (!_isOpen)
        {
            OpenPause();
            return;
        }

        if (_settingsOpen)
        {
            CloseSettings();
            return;
        }

        ResumeGame();
    }

    // Purpose: Opens the pause UI or workflow.
    private void OpenPause()
    {
        _isOpen = true;
        _settingsOpen = false;

        _previousState = _gameState != null ? _gameState.CurrentGameState : GameState.Gameplay;

        GamePause.On();

        if (_gameState != null)
            _gameState.UpdateGameState(GameState.Pause);

        if (_inputReader != null)
            _inputReader.EnableMenuInput();

        // Keep the ESC canvas hidden for one rendered frame so its screenshot
        // contains only the gameplay and any overlays that were already open.
        SetVisible(_rootGroup, false);
        SetVisible(_pauseMenuGroup, true);
        SetVisible(_settingsGroup, false);
        BeginPauseBackdropCapture();
    }

    // Purpose: Resumes the paused game workflow.
    private void ResumeGame()
    {
        _isOpen = false;
        _settingsOpen = false;

        CancelPauseBackdropCapture();
        SetPauseBackdropVisible(false);
        ReleasePauseBackdropTexture();
        SetVisible(_rootGroup, false);
        SetVisible(_settingsGroup, false);
        SetVisible(_pauseMenuGroup, true);

        if (_gameState != null)
            _gameState.UpdateGameState(_previousState);

        GamePause.Off();

        if (_overlayManager != null)
            _overlayManager.RestoreInputForOpenOverlays();
        else if (_inputReader != null)
            _inputReader.EnableGameplayInput();
    }

    // Purpose: Begins the pause backdrop capture workflow.
    private void BeginPauseBackdropCapture()
    {
        CancelPauseBackdropCapture();
        ReleasePauseBackdropTexture();
        EnsurePauseBackdropView();

        if (_pauseBackdropImage == null)
        {
            SetVisible(_rootGroup, true);
            return;
        }

        SetPauseBackdropVisible(false);
        _pauseBackdropRoutine = StartCoroutine(CapturePauseBackdropRoutine());
    }

    // Purpose: Performs the capture pause backdrop routine workflow for this component.
    private IEnumerator CapturePauseBackdropRoutine()
    {
        yield return new WaitForEndOfFrame();

        _pauseBackdropRoutine = null;

        if (!_isOpen || _pauseBackdropImage == null)
            yield break;

        int divisor = Mathf.Max(1, _pauseBackdropDownsample);
        int width = Mathf.Max(1, Screen.width / divisor);
        int height = Mathf.Max(1, Screen.height / divisor);

        RenderTexture capture = CreatePauseRenderTexture(width, height, "PauseCapture");
        RenderTexture blurA = null;
        RenderTexture blurB = null;
        RenderTexture finalTexture = capture;

        ScreenCapture.CaptureScreenshotIntoRenderTexture(capture);

        Material blurMaterial = ResolvePauseBlurMaterial();

        if (blurMaterial != null)
        {
            blurA = CreatePauseRenderTexture(width, height, "PauseBlurA");
            blurB = CreatePauseRenderTexture(width, height, "PauseBlurB");

            RenderTexture source = capture;
            RenderTexture destination = blurA;
            int passCount = Mathf.Clamp(_pauseBlurPasses, 1, 4);

            for (int i = 0; i < passCount; i++)
            {
                blurMaterial.SetFloat("_Offset", _pauseBlurOffset + i * 0.5f);
                Graphics.Blit(source, destination, blurMaterial);

                source = destination;
                destination = destination == blurA ? blurB : blurA;
            }

            finalTexture = source;
        }

        ReleaseIfNotDisplayed(capture, finalTexture);
        ReleaseIfNotDisplayed(blurA, finalTexture);
        ReleaseIfNotDisplayed(blurB, finalTexture);

        _pauseBackdropTexture = finalTexture;
        _pauseBackdropImage.texture = _pauseBackdropTexture;

        float brightness = Mathf.Clamp01(_pauseBackdropBrightness);
        _pauseBackdropImage.color = new Color(brightness, brightness, brightness, 1f);
        SetPauseBackdropVisible(true);
        SetVisible(_rootGroup, true);
    }

    // Purpose: Ensures pause backdrop view is ready before the workflow continues.
    private void EnsurePauseBackdropView()
    {
        if (_pauseBackdropImage != null || _canvas == null)
            return;

        GameObject backdropObject = new GameObject(
            "PauseBackdrop",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(RawImage));

        backdropObject.layer = _canvas.gameObject.layer;

        RectTransform rect = backdropObject.GetComponent<RectTransform>();
        rect.SetParent(_canvas.transform, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
        rect.SetAsFirstSibling();

        _pauseBackdropImage = backdropObject.GetComponent<RawImage>();
        _pauseBackdropImage.raycastTarget = true;
    }

    // Purpose: Resolves the pause blur material needed by this component.
    private Material ResolvePauseBlurMaterial()
    {
        if (_pauseBlurShader == null)
        {
            if (!_missingBlurShaderLogged)
            {
                _missingBlurShaderLogged = true;
                Debug.LogError(
                    "[EscPauseMenuController] Pause blur shader is missing. The pause backdrop will be dimmed without blur.",
                    this);
            }

            return null;
        }

        if (_pauseBlurMaterial != null && _pauseBlurMaterial.shader == _pauseBlurShader)
            return _pauseBlurMaterial;

        if (_pauseBlurMaterial != null)
            Destroy(_pauseBlurMaterial);

        _pauseBlurMaterial = new Material(_pauseBlurShader)
        {
            hideFlags = HideFlags.HideAndDontSave
        };

        return _pauseBlurMaterial;
    }

    // Purpose: Checks whether cancel pause backdrop capture is currently permitted.
    private void CancelPauseBackdropCapture()
    {
        if (_pauseBackdropRoutine == null)
            return;

        StopCoroutine(_pauseBackdropRoutine);
        _pauseBackdropRoutine = null;
    }

    // Purpose: Updates the pause backdrop visible state.
    private void SetPauseBackdropVisible(bool visible)
    {
        if (_pauseBackdropImage != null)
            _pauseBackdropImage.enabled = visible;
    }

    // Purpose: Releases the owned pause backdrop texture resource.
    private void ReleasePauseBackdropTexture()
    {
        if (_pauseBackdropImage != null)
            _pauseBackdropImage.texture = null;

        if (_pauseBackdropTexture == null)
            return;

        RenderTexture.ReleaseTemporary(_pauseBackdropTexture);
        _pauseBackdropTexture = null;
    }

    // Purpose: Creates the pause render texture owned by this workflow.
    private static RenderTexture CreatePauseRenderTexture(int width, int height, string textureName)
    {
        RenderTexture texture = RenderTexture.GetTemporary(
            width,
            height,
            0,
            RenderTextureFormat.Default,
            RenderTextureReadWrite.Default);

        texture.name = textureName;
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        return texture;
    }

    // Purpose: Releases the owned if not displayed resource.
    private static void ReleaseIfNotDisplayed(RenderTexture texture, RenderTexture displayedTexture)
    {
        if (texture != null && texture != displayedTexture)
            RenderTexture.ReleaseTemporary(texture);
    }

    // Purpose: Opens the settings UI or workflow.
    private void OpenSettings()
    {
        _settingsOpen = true;

        SetVisible(_pauseMenuGroup, false);
        SetVisible(_settingsGroup, true);
        if (_settingsController != null)
            _settingsController.OpenSettingsScreen();
    }

    // Purpose: Closes the active settings UI or workflow.
    private void CloseSettings()
    {
        _settingsOpen = false;

        SetVisible(_settingsGroup, false);
        SetVisible(_pauseMenuGroup, true);
    }

    // Purpose: Performs the return to main menu workflow for this component.
    private void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        GamePause.Off();

        if (_loadMenuChannel != null && _mainMenuScene != null)
            _loadMenuChannel.RaiseEvent(_mainMenuScene, true, true);
    }

    // Purpose: Performs the quit game workflow for this component.
    private void QuitGame()
    {
        Time.timeScale = 1f;
        GamePause.Off();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // Purpose: Moves player to current sector center toward its requested destination.
    private void MovePlayerToCurrentSectorCenter()
    {
        if (_sectorCenterRescue == null)
        {
            Debug.LogWarning("[EscPauseMenuController] Sector center rescue requested, but rescue component is not assigned.", this);
            return;
        }

        bool moved = _sectorCenterRescue.TryMovePlayerToCurrentSectorCenter();
        Debug.Log($"[EscPauseMenuController] Sector center rescue result={moved}", this);
    }

    // Purpose: Updates the visible state.
    private static void SetVisible(CanvasGroup group, bool visible)
    {
        if (group == null)
            return;

        group.alpha = visible ? 1f : 0f;
        group.interactable = visible;
        group.blocksRaycasts = visible;
    }

    // Purpose: Shows the back to menu confirmation UI or visual state.
    private void ShowBackToMenuConfirmation()
    {
        ShowConfirmation(PopupType.BackToMenu, PauseConfirmationAction.BackToMenu);
    }

    // Purpose: Shows the quit confirmation UI or visual state.
    private void ShowQuitConfirmation()
    {
        ShowConfirmation(PopupType.Quit, PauseConfirmationAction.Quit);
    }

    // Purpose: Shows the confirmation UI or visual state.
    private void ShowConfirmation(PopupType popupType, PauseConfirmationAction action)
    {
        if (_confirmationPopup == null)
            return;

        _pendingConfirmationAction = action;
        _confirmationOpen = true;

        _confirmationPopup.Show(popupType);
        SetVisible(_confirmationPopupGroup, true);
    }

    // Purpose: Handles the incoming confirmation response request or event.
    private void HandleConfirmationResponse(bool confirmed)
    {
        PauseConfirmationAction action = _pendingConfirmationAction;
        HideConfirmationPopup();

        if (!confirmed)
            return;

        switch (action)
        {
            case PauseConfirmationAction.BackToMenu:
                ReturnToMainMenu();
                break;

            case PauseConfirmationAction.Quit:
                QuitGame();
                break;
        }
    }

    // Purpose: Hides the confirmation popup UI or visual state.
    private void HideConfirmationPopup()
    {
        _pendingConfirmationAction = PauseConfirmationAction.None;
        _confirmationOpen = false;

        if (_confirmationPopup != null)
            _confirmationPopup.Hide();

        SetVisible(_confirmationPopupGroup, false);
    }
}
