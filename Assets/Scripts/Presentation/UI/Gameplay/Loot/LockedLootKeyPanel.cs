/*
 * Role hierarchy: Presentation > UI > Gameplay > Loot
 * Primary responsibility: Presents one reusable Key Tool insertion overlay for
 * optional locked loot objects, then delegates all loot opening back to LootHub.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// One persistent-at-runtime overlay for all <see cref="LockableLootObject"/>
/// instances. It intentionally contains only the lock transaction: existing
/// loot, inventory, and item-detail panels remain owned by their current hubs.
/// </summary>
[DisallowMultipleComponent]
public sealed class LockedLootKeyPanel : MonoBehaviour, IUIOverlayLifecycle
{
    private const float InteractCloseGraceSeconds = 0.15f;
    private const int TierCount = 4;
    private const string StringTable = "UI_Inventory";

    private static LockedLootKeyPanel _instance;
    private static bool _isCreating;

    private UIOverlayPanel _overlay;
    private UIOverlayManager _overlayManager;
    private InputReader _inputReader;
    private LockableLootObject _activeLock;
    private Component _activeActor;
    private PlayerInventoryRuntime _activeInventory;
    private TextMeshProUGUI _containerText;
    private TextMeshProUGUI _requiredKeyText;
    private TextMeshProUGUI _socketText;
    private TextMeshProUGUI _feedbackText;
    private Image _socketImage;
    private Button _confirmButton;
    private TextMeshProUGUI _confirmButtonText;
    private KeyTierRow[] _tierRows;
    private LootKeyTier? _draggedTier;
    private LootKeyTier? _selectedTier;
    private bool _isOpen;
    private float _openedAt;

    private sealed class KeyTierRow
    {
        public LootKeyTier Tier;
        public Image Background;
        public TextMeshProUGUI Label;
        public TextMeshProUGUI Detail;
    }

    // Purpose: Creates the one shared overlay before a locked object publishes its request.
    public static void EnsureInstance()
    {
        if (_instance != null || _isCreating)
            return;

        _isCreating = true;
        GameObject root = new GameObject(
            nameof(LockedLootKeyPanel),
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster),
            typeof(CanvasGroup),
            typeof(UIOverlayPanel),
            typeof(LockedLootKeyPanel));

        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 455;

        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        _isCreating = false;
    }

    // Purpose: Opens the Key Tool insertion overlay for one locked container.
    public static bool TryOpen(
        LockableLootObject lockableLoot,
        Component actor,
        PlayerInventoryRuntime inventory)
    {
        EnsureInstance();
        return _instance != null && _instance.Open(lockableLoot, actor, inventory);
    }

    // Purpose: Caches runtime dependencies and constructs the preallocated overlay once.
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
            UIOverlayId.LockedLootKey,
            GetComponent<CanvasGroup>(),
            true,
            true);

        BuildRuntimeUi();
        ResolveRuntimeReferences();
    }

    // Purpose: Activates input subscriptions needed while the panel is visible.
    private void OnEnable()
    {
        RegisterEscapeHandler();
        ResolveRuntimeReferences();
        SubscribeInputReader();
    }

    // Purpose: Releases input subscriptions when this panel leaves the scene.
    private void OnDisable()
    {
        UIEscapeRouter.Unregister(this);
        UnsubscribeInputReader();
    }

    // Purpose: Clears the singleton reference when this scene-owned overlay is destroyed.
    private void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }

    // Purpose: Handles Escape closing without changing the normal interaction binding.
    private void Update()
    {
        if (_isOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            UIEscapeRouter.TryCloseTopPopup();
    }

    // Purpose: Refreshes the Key Tool rows when the owning overlay becomes visible.
    public void OnOverlayShown()
    {
        _isOpen = true;
        RegisterEscapeHandler();
        _openedAt = Time.unscaledTime;
        RefreshView();
    }

    // Purpose: Clears the local lock binding after the overlay is hidden.
    public void OnOverlayHidden()
    {
        _isOpen = false;
        _draggedTier = null;
        _selectedTier = null;
    }

    private void RegisterEscapeHandler() => UIEscapeRouter.Register(this,
        () => _isOpen && (_overlayManager == null || !_overlayManager.isActiveAndEnabled), Close, 100);

    // Purpose: Starts a represented Key Tool tier drag for the central socket.
    public void BeginTierDrag(LootKeyTier tier)
    {
        if (!_isOpen || _activeInventory == null)
            return;

        if (!_activeInventory.HasUsableKey(tier))
        {
            SetFeedback(Format("Inventory_LockedFeedbackNoUsable", "No usable {0} Key in Key Tool.", LocalizeTier(tier)), false);
            PlayKeyRejectedFeedback();
            return;
        }

        _draggedTier = tier;
        RefreshSocketText();
    }

    // Purpose: Clears the temporary drag marker after the pointer ends its drag.
    public void EndTierDrag()
    {
        _draggedTier = null;
        RefreshSocketText();
    }

    // Purpose: Selects the tier currently dragged onto the Key Tool socket without spending it.
    public void TrySubmitDraggedTier()
    {
        if (!_draggedTier.HasValue)
            return;

        LootKeyTier tier = _draggedTier.Value;
        _draggedTier = null;
        TrySubmitTier(tier);
    }

    // Purpose: Selects one Key Tool tier requested by a double-click or socket drop.
    // The selected key remains untouched until the player presses Confirm.
    public void TrySubmitTier(LootKeyTier tier)
    {
        if (!_isOpen || _activeLock == null || _activeInventory == null)
            return;

        if (!_activeInventory.HasUsableKey(tier))
        {
            SetFeedback(Format("Inventory_LockedFeedbackNoUsable", "No usable {0} Key in Key Tool.", LocalizeTier(tier)), false);
            PlayKeyRejectedFeedback();
            RefreshView();
            return;
        }

        _selectedTier = tier;
        if (tier == _activeLock.RequiredKeyTier)
            SetFeedback(Format("Inventory_LockedFeedbackSelected", "{0} Key selected. Press CONFIRM to spend one use and unlock.", LocalizeTier(tier)), true);
        else
            SetFeedback(Format("Inventory_LockedFeedbackMismatch", "{0} Key does not match this lock. Select {1} Key.", LocalizeTier(tier), LocalizeTier(_activeLock.RequiredKeyTier)), false);

        RefreshView();
    }

    // Purpose: Commits the selected matching key only after explicit confirmation.
    public void ConfirmSelectedKey()
    {
        if (!_isOpen || _activeLock == null || _activeInventory == null)
            return;

        if (!_selectedTier.HasValue)
        {
            SetFeedback(Get("Inventory_LockedFeedbackSelectMatching", "Select a matching Key Tool entry first."), false);
            PlayKeyRejectedFeedback();
            return;
        }

        LootKeyTier tier = _selectedTier.Value;
        if (tier != _activeLock.RequiredKeyTier)
        {
            SetFeedback(Format("Inventory_LockedFeedbackRequires", "This lock requires {0} Key.", LocalizeTier(_activeLock.RequiredKeyTier)), false);
            PlayKeyRejectedFeedback();
            RefreshView();
            return;
        }

        if (!_activeInventory.HasUsableKey(tier))
        {
            SetFeedback(Format("Inventory_LockedFeedbackNoUsableRemaining", "No usable {0} Key remains in Key Tool.", LocalizeTier(tier)), false);
            PlayKeyRejectedFeedback();
            RefreshView();
            return;
        }

        PlayKeyInsertFeedback();

        if (!_activeLock.TryUnlockAndOpen(
                _activeActor,
                _activeInventory,
                tier,
                out string message))
        {
            SetFeedback(message, false);
            PlayKeyRejectedFeedback();
            RefreshView();
            return;
        }

        SetFeedback(message, true);
        PlayKeySuccessFeedback();
        Close();
    }

    // Purpose: Opens and binds this prebuilt overlay to one lock transaction.
    private bool Open(
        LockableLootObject lockableLoot,
        Component actor,
        PlayerInventoryRuntime inventory)
    {
        if (lockableLoot == null || actor == null || inventory == null)
            return false;

        ResolveRuntimeReferences();
        _activeLock = lockableLoot;
        _activeActor = actor;
        _activeInventory = inventory;
        _draggedTier = null;
        _selectedTier = null;
        SetFeedback(Get("Inventory_LockedFeedbackInitial", "Drag or double-click a Key Tool entry, then press CONFIRM."), true);
        RefreshView();

        if (_overlayManager != null)
        {
            _overlayManager.RegisterPanel(_overlay);
            _overlayManager.Open(UIOverlayId.LockedLootKey);
        }
        else
        {
            _overlay.Show();
        }

        return true;
    }

    // Purpose: Plays the player-owned feedback for selecting a key in the Key Tool.
    private void PlayKeyInsertFeedback()
    {
        ResolvePlayerAudio()?.PlayKeyInsert();
    }

    // Purpose: Plays the player-owned feedback after a key transaction succeeds.
    private void PlayKeySuccessFeedback()
    {
        ResolvePlayerAudio()?.PlayKeySuccess();
    }

    // Purpose: Plays the player-owned feedback after a key transaction is rejected.
    private void PlayKeyRejectedFeedback()
    {
        ResolvePlayerAudio()?.PlayKeyRejected();
    }

    // Purpose: Resolves the active player's existing audio component without creating another audio route.
    private PlayerShooterCharacterAudio ResolvePlayerAudio()
    {
        if (_activeActor == null)
            return null;

        return _activeActor.GetComponent<PlayerShooterCharacterAudio>() ??
            _activeActor.GetComponentInParent<PlayerShooterCharacterAudio>();
    }

    // Purpose: Closes this overlay without consuming or reserving a key use.
    private void Close()
    {
        if (!_isOpen)
            return;

        if (_overlayManager != null && _overlayManager.isActiveAndEnabled)
            _overlayManager.Close(UIOverlayId.LockedLootKey);
        else
            _overlay.Hide();
    }

    // Purpose: Handles Interact as a cancel action only after the opening input is finished.
    private void HandleInteractInput()
    {
        if (_isOpen && Time.unscaledTime >= _openedAt + InteractCloseGraceSeconds)
            Close();
    }

    // Purpose: Resolves scene services shared by this standalone overlay.
    private void ResolveRuntimeReferences()
    {
        if (_overlayManager == null)
            _overlayManager = FindAnyObjectByType<UIOverlayManager>(FindObjectsInactive.Include);

        if (_inputReader == null)
        {
            _inputReader = FindAnyObjectByType<InputReader>(FindObjectsInactive.Include);
            SubscribeInputReader();
        }
    }

    // Purpose: Subscribes once to the project-wide Interact binding.
    private void SubscribeInputReader()
    {
        if (_inputReader == null)
            return;

        _inputReader.InteractEvent -= HandleInteractInput;
        _inputReader.InteractEvent += HandleInteractInput;
    }

    // Purpose: Removes the project-wide Interact binding subscription.
    private void UnsubscribeInputReader()
    {
        if (_inputReader != null)
            _inputReader.InteractEvent -= HandleInteractInput;
    }

    // Purpose: Refreshes all labels and interactability from the live Key Tool state.
    private void RefreshView()
    {
        if (_activeLock == null || _activeInventory == null)
            return;

        if (_containerText != null)
            _containerText.text = _activeLock.LockedDisplayName.ToUpperInvariant();

        if (_requiredKeyText != null)
            _requiredKeyText.text = UIRuntimeLocalization.Format(
                "UI_Inventory",
                "Inventory_LockedRequiredKeyFormat",
                "Requires: {0} Key",
                LocalizeTier(_activeLock.RequiredKeyTier));

        if (_tierRows != null)
        {
            for (int index = 0; index < _tierRows.Length; index++)
            {
                KeyTierRow row = _tierRows[index];
                PlayerKeyToolTierSnapshot snapshot =
                    _activeInventory.GetKeyToolTierSnapshot(row.Tier);
                bool usable = snapshot.HasUsableKey;
                bool required = row.Tier == _activeLock.RequiredKeyTier;
                bool selected = _selectedTier.HasValue && row.Tier == _selectedTier.Value;

                row.Background.color = usable
                    ? (selected
                        ? (required
                            ? new Color(0.08f, 0.58f, 0.45f, 1f)
                            : new Color(0.55f, 0.19f, 0.14f, 1f))
                        : (required
                        ? new Color(0.08f, 0.42f, 0.48f, 1f)
                        : new Color(0.08f, 0.21f, 0.27f, 1f)))
                    : new Color(0.08f, 0.1f, 0.13f, 0.88f);

                row.Label.text = UIRuntimeLocalization.Format(
                    "UI_Inventory",
                    "Inventory_LockedKeyTierFormat",
                    "{0} Key",
                    LocalizeTier(row.Tier));
                row.Label.color = usable
                    ? Color.white
                    : new Color(0.5f, 0.54f, 0.57f, 1f);
                row.Detail.text = snapshot.KeyCopies <= 0
                    ? UIRuntimeLocalization.Get("UI_Inventory", "Inventory_LockedKeyEmpty", "Empty")
                    : UIRuntimeLocalization.Format(
                        "UI_Inventory",
                        "Inventory_LockedKeyDetailsFormat",
                        "{0}/{1} Keys  •  Active {2}/{3}  •  {4} Uses",
                        snapshot.KeyCopies,
                        snapshot.MaxKeyCopies,
                        snapshot.ActiveKeyDurability,
                        snapshot.UsesPerKey,
                        snapshot.TotalRemainingUses);
                row.Detail.color = usable
                    ? new Color(0.66f, 0.87f, 0.9f, 1f)
                    : new Color(0.44f, 0.48f, 0.52f, 1f);
            }
        }

        RefreshSocketText();
    }

    // Purpose: Refreshes the central socket instruction without affecting feedback.
    private void RefreshSocketText()
    {
        if (_socketText == null || _socketImage == null)
            return;

        if (_draggedTier.HasValue)
        {
            _socketText.text = UIRuntimeLocalization.Format(
                "UI_Inventory",
                "Inventory_LockedDropToInsertFormat",
                "{0} Key\nDrop to insert",
                LocalizeTier(_draggedTier.Value));
            _socketImage.color = new Color(0.11f, 0.55f, 0.64f, 1f);
            RefreshConfirmButton();
            return;
        }

        if (_selectedTier.HasValue)
        {
            bool matches = _activeLock != null && _selectedTier.Value == _activeLock.RequiredKeyTier;
            _socketText.text = matches
                ? UIRuntimeLocalization.Format(
                    "UI_Inventory",
                    "Inventory_LockedKeyReadyFormat",
                    "{0} Key Ready\nPress Confirm to Unlock",
                    LocalizeTier(_selectedTier.Value))
                : UIRuntimeLocalization.Format(
                    "UI_Inventory",
                    "Inventory_LockedWrongKeyTierFormat",
                    "{0} Key\nWrong Key Tier",
                    LocalizeTier(_selectedTier.Value));
            _socketImage.color = matches
                ? new Color(0.08f, 0.48f, 0.36f, 1f)
                : new Color(0.43f, 0.13f, 0.1f, 1f);
            RefreshConfirmButton();
            return;
        }

        _socketText.text = UIRuntimeLocalization.Get(
            "UI_Inventory",
            "Inventory_LockedSocketIdle",
            "Key Socket\nDrag here or double-click a key");
        _socketImage.color = new Color(0.035f, 0.16f, 0.21f, 1f);
        RefreshConfirmButton();
    }

    // Purpose: Enables confirmation only for the selected matching usable key.
    private void RefreshConfirmButton()
    {
        if (_confirmButton == null)
            return;

        bool canConfirm = _isOpen &&
            _selectedTier.HasValue &&
            _activeLock != null &&
            _activeInventory != null &&
            _selectedTier.Value == _activeLock.RequiredKeyTier &&
            _activeInventory.HasUsableKey(_selectedTier.Value);
        _confirmButton.interactable = canConfirm;

        if (_confirmButtonText != null)
            _confirmButtonText.text = canConfirm
                ? UIRuntimeLocalization.Get("UI_Inventory", "Inventory_LockedConfirmUnlock", "Confirm Unlock")
                : UIRuntimeLocalization.Get("UI_Inventory", "Inventory_LockedSelectMatchingKey", "Select Matching Key");
    }

    // Purpose: Presents one local transaction result without requiring a global system-message channel.
    private void SetFeedback(string message, bool success)
    {
        if (_feedbackText == null)
            return;

        _feedbackText.text = message ?? string.Empty;
        _feedbackText.color = success
            ? new Color(0.55f, 0.95f, 0.72f, 1f)
            : new Color(1f, 0.55f, 0.48f, 1f);
    }

    // Purpose: Returns the localized UI label for one gameplay key tier.
    private static string LocalizeTier(LootKeyTier tier)
    {
        return tier switch
        {
            LootKeyTier.Normal => Get("Inventory_KeyTierNormal", "Normal"),
            LootKeyTier.Rare => Get("Inventory_KeyTierRare", "Rare"),
            LootKeyTier.Unique => Get("Inventory_KeyTierUnique", "Unique"),
            LootKeyTier.Legendary => Get("Inventory_KeyTierLegendary", "Legendary"),
            _ => tier.ToString(),
        };
    }

    private static string Get(string key, string fallback)
    {
        return UIRuntimeLocalization.Get(StringTable, key, fallback);
    }

    private static string Format(string key, string fallback, params object[] arguments)
    {
        return UIRuntimeLocalization.Format(StringTable, key, fallback, arguments);
    }

    // Purpose: Builds the preallocated key rows and socket once for this overlay instance.
    private void BuildRuntimeUi()
    {
        RectTransform root = transform as RectTransform;
        SetFullStretch(root);

        Image backdrop = CreateImage("Backdrop", transform, new Color(0f, 0f, 0f, 0.72f));
        SetFullStretch(backdrop.rectTransform);

        Image frame = CreateImage("LockedLootFrame", transform, new Color(0.012f, 0.075f, 0.105f, 0.99f));
        RectTransform frameRect = frame.rectTransform;
        frameRect.anchorMin = frameRect.anchorMax = new Vector2(0.5f, 0.5f);
        frameRect.pivot = new Vector2(0.5f, 0.5f);
        frameRect.sizeDelta = new Vector2(720f, 650f);
        frameRect.anchoredPosition = Vector2.zero;

        Outline outline = frame.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0.8f, 1f, 0.8f);
        outline.effectDistance = new Vector2(2f, -2f);

        TextMeshProUGUI title = CreateText("Title", frame.transform, 36f, FontStyles.Bold, TextAlignmentOptions.Center);
        title.text = UIRuntimeLocalization.Get(
            "UI_Inventory",
            "Inventory_LockedContainer",
            "Locked Container");
        title.color = new Color(0.76f, 0.96f, 1f, 1f);
        SetAnchors(title.rectTransform, new Vector2(0.08f, 0.88f), new Vector2(0.92f, 0.95f));

        _containerText = CreateText("ContainerName", frame.transform, 20f, FontStyles.Bold, TextAlignmentOptions.Center);
        _containerText.color = new Color(0.62f, 0.78f, 0.84f, 1f);
        SetAnchors(_containerText.rectTransform, new Vector2(0.08f, 0.82f), new Vector2(0.92f, 0.875f));

        _requiredKeyText = CreateText("RequiredKey", frame.transform, 22f, FontStyles.Bold, TextAlignmentOptions.Center);
        _requiredKeyText.color = new Color(1f, 0.78f, 0.42f, 1f);
        SetAnchors(_requiredKeyText.rectTransform, new Vector2(0.08f, 0.75f), new Vector2(0.92f, 0.81f));

        _socketImage = CreateImage("KeySocket", frame.transform, new Color(0.035f, 0.16f, 0.21f, 1f));
        RectTransform socketRect = _socketImage.rectTransform;
        SetAnchors(socketRect, new Vector2(0.16f, 0.53f), new Vector2(0.84f, 0.71f));
        Outline socketOutline = _socketImage.gameObject.AddComponent<Outline>();
        socketOutline.effectColor = new Color(0.08f, 0.73f, 0.9f, 0.9f);
        socketOutline.effectDistance = new Vector2(2f, -2f);
        _socketImage.gameObject.AddComponent<LockedLootKeyPanelDropSlotUI>().Configure(this);
        _socketText = CreateText("SocketLabel", _socketImage.transform, 21f, FontStyles.Bold, TextAlignmentOptions.Center);
        _socketText.color = new Color(0.78f, 0.94f, 1f, 1f);
        SetFullStretch(_socketText.rectTransform);

        RectTransform keyRows = CreateRect("KeyToolEntries", frame.transform);
        SetAnchors(keyRows, new Vector2(0.12f, 0.22f), new Vector2(0.88f, 0.52f));
        VerticalLayoutGroup layout = keyRows.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.spacing = 5f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        _tierRows = new KeyTierRow[TierCount];
        for (int index = 0; index < TierCount; index++)
        {
            LootKeyTier tier = (LootKeyTier)index;
            Image rowImage = CreateImage($"{tier}KeyEntry", keyRows, new Color(0.08f, 0.21f, 0.27f, 1f));
            rowImage.gameObject.AddComponent<LayoutElement>().preferredHeight = 43f;
            rowImage.gameObject.AddComponent<LockedLootKeyPanelEntryUI>().Configure(this, tier);

            TextMeshProUGUI label = CreateText("Label", rowImage.transform, 20f, FontStyles.Bold, TextAlignmentOptions.Left);
            SetAnchors(label.rectTransform, new Vector2(0.04f, 0.15f), new Vector2(0.36f, 0.85f));

            TextMeshProUGUI detail = CreateText("Detail", rowImage.transform, 15f, FontStyles.Normal, TextAlignmentOptions.Right);
            SetAnchors(detail.rectTransform, new Vector2(0.38f, 0.15f), new Vector2(0.96f, 0.85f));

            _tierRows[index] = new KeyTierRow
            {
                Tier = tier,
                Background = rowImage,
                Label = label,
                Detail = detail
            };
        }

        _feedbackText = CreateText("Feedback", frame.transform, 17f, FontStyles.Normal, TextAlignmentOptions.Center);
        SetAnchors(_feedbackText.rectTransform, new Vector2(0.08f, 0.155f), new Vector2(0.92f, 0.205f));

        _confirmButton = CreateButton("ConfirmButton", frame.transform, new Color(0.08f, 0.42f, 0.3f, 1f));
        _confirmButton.onClick.AddListener(ConfirmSelectedKey);
        RectTransform confirmRect = _confirmButton.GetComponent<RectTransform>();
        SetAnchors(confirmRect, new Vector2(0.3f, 0.075f), new Vector2(0.7f, 0.135f));
        _confirmButtonText = CreateText("Label", _confirmButton.transform, 18f, FontStyles.Bold, TextAlignmentOptions.Center);
        _confirmButtonText.color = Color.white;
        SetFullStretch(_confirmButtonText.rectTransform);

        Button closeButton = CreateButton("CloseButton", frame.transform, new Color(0.35f, 0.08f, 0.1f, 1f));
        closeButton.onClick.AddListener(Close);
        RectTransform closeRect = closeButton.GetComponent<RectTransform>();
        closeRect.anchorMin = closeRect.anchorMax = new Vector2(0.93f, 0.93f);
        closeRect.pivot = new Vector2(0.5f, 0.5f);
        closeRect.sizeDelta = new Vector2(52f, 52f);
        TextMeshProUGUI closeText = CreateText("Label", closeButton.transform, 28f, FontStyles.Bold, TextAlignmentOptions.Center);
        closeText.text = "X";
        SetFullStretch(closeText.rectTransform);

        TextMeshProUGUI footer = CreateText("Footer", frame.transform, 15f, FontStyles.Normal, TextAlignmentOptions.Center);
        footer.text = UIRuntimeLocalization.Get(
            "UI_Inventory",
            "Inventory_LockedFooter",
            "Esc / Interact: Cancel  •  Keys are spent only after Confirm");
        footer.color = new Color(0.55f, 0.67f, 0.72f, 1f);
        SetAnchors(footer.rectTransform, new Vector2(0.05f, 0.02f), new Vector2(0.95f, 0.06f));

        RefreshSocketText();
    }

    // Purpose: Creates one rect-transform child owned by this overlay.
    private static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
        rectTransform.SetParent(parent, false);
        return rectTransform;
    }

    // Purpose: Creates one color-filled image child owned by this overlay.
    private static Image CreateImage(string name, Transform parent, Color color)
    {
        RectTransform rectTransform = CreateRect(name, parent);
        Image image = rectTransform.gameObject.AddComponent<Image>();
        image.color = color;
        return image;
    }

    // Purpose: Creates one button child owned by this overlay.
    private static Button CreateButton(string name, Transform parent, Color color)
    {
        Image image = CreateImage(name, parent, color);
        Button button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.16f, 1.16f, 1.16f, 1f);
        colors.pressedColor = new Color(0.72f, 0.72f, 0.72f, 1f);
        button.colors = colors;
        return button;
    }

    // Purpose: Creates one text child owned by this overlay.
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
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;
        return text;
    }

    // Purpose: Applies full-stretch anchors to a rect transform.
    private static void SetFullStretch(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    // Purpose: Applies normalized stretch anchors without introducing offsets.
    private static void SetAnchors(RectTransform rectTransform, Vector2 min, Vector2 max)
    {
        rectTransform.anchorMin = min;
        rectTransform.anchorMax = max;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }
}
