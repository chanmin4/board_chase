/*
 * Role hierarchy: Presentation > UI > Gameplay > Shop
 * Primary responsibility: Displays a reusable item-payment submission overlay for Home-shop purchases.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// One temporary submit-only trade overlay. Dragging or double-clicking an
/// inventory item fills a visual requirement slot only; no inventory mutation
/// is performed until the player presses Confirm.
/// </summary>
[DisallowMultipleComponent]
public sealed class ShopPurchaseTradePanel : MonoBehaviour
{
    private static ShopPurchaseTradePanel _instance;
    private static bool _isCreating;

    private sealed class RequirementRow
    {
        public ShopTradeRequirement Requirement;
        public int AssignedAmount;
        public Image Background;
        public Image Icon;
        public TextMeshProUGUI Label;
        public TextMeshProUGUI Amount;
    }

    private readonly List<RequirementRow> _rows = new List<RequirementRow>();

    private PlayerShopPanelUI _owner;
    private PlayerShopOffer _offer;
    private PlayerInventoryRuntime _inventory;
    private PlayerInventoryUI _inventoryUi;
    private TextMeshProUGUI _titleText;
    private TextMeshProUGUI _productText;
    private TextMeshProUGUI _feedbackText;
    private Transform _rowRoot;
    private Button _confirmButton;
    private bool _isOpen;
    public delegate bool KeySubmissionCommit(int slotIndex, KeyItemSO expectedKey, out string message);
    private bool _isKeySubmission;
    private UnityEngine.Object _keyOwner;
    private PlayerPanelHub _keyHub;
    private LootKeyTier _requiredKeyTier;
    private KeySubmissionCommit _keyCommit;
    private System.Func<bool> _keyRequestValid;
    private int _submittedKeySlot = -1;
    private KeyItemSO _submittedKey;
    private TextMeshProUGUI _instructionText;
    private TextMeshProUGUI _confirmText;
    private string _keyConfirmationLabel;
    private string _keyTitle;
    private string _keyDescription;
    public static bool IsKeySubmissionOpen => _instance != null && _instance._isOpen && _instance._isKeySubmission;
    public static bool IsKeySubmissionOwnedBy(UnityEngine.Object owner) => IsKeySubmissionOpen && _instance._keyOwner == owner;

    public static bool TryOpenKeySubmission(UnityEngine.Object owner, LootKeyTier tier,
        PlayerInventoryRuntime inventory, KeySubmissionCommit commit, System.Func<bool> stillValid,
        string confirmationLabel = "입장 확정", string title = "금고방 · 열쇠 확인", string description = null)
    {
        if (owner == null || inventory == null || commit == null) return false;
        EnsureInstance();
        if (_instance == null) return false;
        _instance.Close();
        PlayerPanelHub hub = FindFirstObjectByType<PlayerPanelHub>(FindObjectsInactive.Include);
        if (hub == null || !hub.OpenKeySubmissionInventory()) return false;
        var panel = _instance;
        panel._isKeySubmission = true;
        panel._keyOwner = owner;
        panel._keyHub = hub;
        panel._requiredKeyTier = tier;
        panel._keyCommit = commit;
        panel._keyRequestValid = stillValid;
        panel._keyConfirmationLabel = confirmationLabel;
        panel._keyTitle = title;
        panel._keyDescription = description ?? "확정: 열쇠 1회 사용 후 금고방 입장 · 취소: 소모 없음";
        panel._submittedKeySlot = -1;
        panel._submittedKey = null;
        panel._inventory = inventory;
        panel._inventoryUi = hub.PlayerInventoryPanel;
        panel._isOpen = true;
        panel.RebuildRequirementRows();
        panel.gameObject.SetActive(true);
        panel.RegisterEscapeHandler();
        inventory.KeyToolChanged += panel.RefreshRows;
        if (inventory.HasKeyTool) KeyToolContainerPanel.TryOpen(panel._inventoryUi, inventory, inventory.KeyTool);
        panel.SetFeedback(inventory.HasKeyTool ? "열쇠를 제출한 후 입장 확정을 누르세요. 취소하면 소모하지 않습니다." :
            "Key Tool이 없습니다. 도구와 필요한 열쇠를 준비하세요.", inventory.HasKeyTool);
        panel.RefreshView();
        return true;
    }

    public static bool TrySubmitContainedKey(PlayerInventoryRuntime inventory, int slotIndex)
    {
        if (!IsKeySubmissionOpen || _instance._inventory != inventory) return false;
        _instance.SubmitContainedKey(slotIndex);
        return true;
    }

    private void SubmitContainedKey(int slotIndex)
    {
        PlayerKeyToolSlotSnapshot key = _inventory.GetKeyToolSlotSnapshot(slotIndex);
        if (!key.HasKey || key.Key.Tier != _requiredKeyTier)
        {
            SetFeedback("필요한 등급의 사용 가능한 열쇠를 제출하세요.", false);
            return;
        }
        _submittedKeySlot = slotIndex;
        _submittedKey = key.Key;
        SetFeedback("열쇠 제출 완료. 확정해야 사용 횟수 1회를 소모합니다.", true);
        RefreshRows();
    }

    /// <summary>Creates the one reusable overlay before an offer requests it.</summary>
    private static void EnsureInstance()
    {
        if (_instance != null || _isCreating)
            return;

        _isCreating = true;
        GameObject root = new GameObject(
            nameof(ShopPurchaseTradePanel),
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster),
            typeof(ShopPurchaseTradePanel));

        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 460;

        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        _isCreating = false;
        root.SetActive(false);
    }

    /// <summary>Opens the physical-payment submission window for an offer.</summary>
    public static bool TryOpen(
        PlayerShopPanelUI owner,
        PlayerShopOffer offer,
        PlayerInventoryRuntime inventory,
        PlayerInventoryUI inventoryUi)
    {
        EnsureInstance();
        return _instance != null && _instance.Open(owner, offer, inventory, inventoryUi);
    }

    /// <summary>Closes the current window only when the supplied shop owns it.</summary>
    public static void CloseIfOwned(PlayerShopPanelUI owner)
    {
        if (_instance != null && _instance._owner == owner)
            _instance.Close();
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        BuildRuntimeUi();
    }

    private void OnDestroy()
    {
        UIEscapeRouter.Unregister(this);
        if (_instance == this)
            _instance = null;
    }

    private void Update()
    {
        if (_isOpen && _isKeySubmission && (_keyOwner == null || _keyHub == null ||
            !_keyHub.IsKeySubmissionInventoryVisible || _keyRequestValid != null && !_keyRequestValid()))
        {
            Close();
            return;
        }
        if (_isOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            UIEscapeRouter.TryCloseTopPopup();
    }

    private void OnEnable() => RegisterEscapeHandler();
    private void OnDisable() => UIEscapeRouter.Unregister(this);
    private void RegisterEscapeHandler() => UIEscapeRouter.Register(this, () => _isOpen, Close, 100);

    /// <summary>Receives one inventory item selected by a double-click.</summary>
    private void HandleInventoryTradeRequested(int slotIndex, PlayerInventoryItemStack stack)
    {
        if (stack == null || stack.IsEmpty)
            return;

        SubmitItemToFirstMatchingRequirement(stack.Item, stack.Amount);
    }

    /// <summary>Receives one inventory drag dropped on an explicit payment row.</summary>
    public void HandleRequirementDrop(int rowIndex)
    {
        if (_isOpen && _isKeySubmission)
        {
            if (InventoryDragContext.SourceKind == InventoryDragSourceKind.KeyTool &&
                InventoryDragContext.KeyToolInventoryOwner == _inventory)
                SubmitContainedKey(InventoryDragContext.KeyToolSlotIndex);
            else SetFeedback("Key Tool 안의 열쇠를 이 칸으로 드래그하세요.", false);
            return;
        }
        if (!_isOpen || _inventory == null ||
            InventoryDragContext.SourceKind != InventoryDragSourceKind.Inventory)
        {
            return;
        }

        int inventoryIndex = InventoryDragContext.InventoryIndex;
        IReadOnlyList<PlayerInventoryItemStack> stacks = _inventory.Items;
        if (stacks == null || inventoryIndex < 0 || inventoryIndex >= stacks.Count)
            return;

        PlayerInventoryItemStack stack = stacks[inventoryIndex];
        if (stack == null || stack.IsEmpty)
            return;

        SubmitItemToRequirement(rowIndex, stack.Item, stack.Amount);
    }

    /// <summary>Clears one visual submission row without touching inventory.</summary>
    public void ClearRequirement(int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= _rows.Count)
            return;

        _rows[rowIndex].AssignedAmount = 0;
        _submittedKeySlot = -1;
        _submittedKey = null;
        SetFeedback("Payment slot cleared.", true);
        RefreshRows();
    }

    private bool Open(
        PlayerShopPanelUI owner,
        PlayerShopOffer offer,
        PlayerInventoryRuntime inventory,
        PlayerInventoryUI inventoryUi)
    {
        if (owner == null || offer == null || !offer.HasValidPhysicalPayment || inventory == null)
            return false;

        if (_isOpen)
            Close();

        _owner = owner;
        _offer = offer;
        _inventory = inventory;
        _inventoryUi = inventoryUi;
        _isOpen = true;
        RegisterEscapeHandler();

        RebuildRequirementRows();
        gameObject.SetActive(true);

        if (_inventoryUi != null)
        {
            _inventoryUi.SetInteractionMode(PlayerInventoryInteractionMode.TradeSelect);
            _inventoryUi.OnInventoryTradeRequested -= HandleInventoryTradeRequested;
            _inventoryUi.OnInventoryTradeRequested += HandleInventoryTradeRequested;
        }

        SetFeedback("Double-click an inventory item or drag it into its matching payment slot.", true);
        RefreshView();
        return true;
    }

    private void Close()
    {
        if (!_isOpen)
            return;

        bool keySubmission = _isKeySubmission;
        _isOpen = false;
        if (keySubmission && _inventory != null) _inventory.KeyToolChanged -= RefreshRows;
        if (_inventoryUi != null)
        {
            _inventoryUi.OnInventoryTradeRequested -= HandleInventoryTradeRequested;
            _inventoryUi.SetInteractionMode(keySubmission ? PlayerInventoryInteractionMode.Normal : PlayerInventoryInteractionMode.SellSelect);
        }

        InventoryDragContext.Clear();
        _isOpen = false;
        if (keySubmission && _keyHub != null) _keyHub.CloseKeySubmissionInventory();
        _isKeySubmission = false;
        _keyOwner = null;
        _keyHub = null;
        _keyCommit = null;
        _keyRequestValid = null;
        _submittedKeySlot = -1;
        _submittedKey = null;
        UIEscapeRouter.Unregister(this);
        _owner = null;
        _offer = null;
        _inventory = null;
        _inventoryUi = null;
        gameObject.SetActive(false);
    }

    private void HandleConfirmClicked()
    {
        if (_isOpen && _isKeySubmission)
        {
            if (_submittedKeySlot < 0 || _submittedKey == null ||
                _keyRequestValid != null && !_keyRequestValid())
            { SetFeedback("열쇠 제출 또는 금고 입장 조건을 다시 확인하세요.", false); return; }
            if (!_keyCommit(_submittedKeySlot, _submittedKey, out string keyMessage))
            { SetFeedback(keyMessage, false); RefreshRows(); return; }
            SetFeedback(keyMessage, true);
            Close();
            return;
        }
        if (!_isOpen || _owner == null || _offer == null)
            return;

        for (int index = 0; index < _rows.Count; index++)
        {
            RequirementRow row = _rows[index];
            if (row.AssignedAmount < row.Requirement.Amount)
            {
                SetFeedback("Fill every payment slot before confirming.", false);
                return;
            }
        }

        if (!_owner.TryCompletePhysicalPurchase(_offer, out string message))
        {
            SetFeedback(message, false);
            RefreshRows();
            return;
        }

        SetFeedback(message, true);
        Close();
    }

    private void SubmitItemToFirstMatchingRequirement(ItemSO item, int stackAmount)
    {
        for (int index = 0; index < _rows.Count; index++)
        {
            RequirementRow row = _rows[index];
            if (row.AssignedAmount < row.Requirement.Amount && IsSameItem(row.Requirement.Item, item))
            {
                SubmitItemToRequirement(index, item, stackAmount);
                return;
            }
        }

        SetFeedback("That item is not required, or its payment slot is already full.", false);
    }

    private void SubmitItemToRequirement(int rowIndex, ItemSO item, int stackAmount)
    {
        if (rowIndex < 0 || rowIndex >= _rows.Count || item == null)
            return;

        RequirementRow row = _rows[rowIndex];
        if (!IsSameItem(row.Requirement.Item, item))
        {
            SetFeedback($"This slot requires {InventoryItemDisplayUtility.ResolveItemName(row.Requirement.Item)}.", false);
            return;
        }

        int remainingForRow = Mathf.Max(0, row.Requirement.Amount - row.AssignedAmount);
        int availableToReserve = Mathf.Max(0, GetAvailableUnreservedAmount(item));
        int submitted = Mathf.Min(Mathf.Max(0, stackAmount), Mathf.Min(remainingForRow, availableToReserve));
        if (submitted <= 0)
        {
            SetFeedback("No unreserved copy of that item is available for this payment.", false);
            return;
        }

        row.AssignedAmount += submitted;
        SetFeedback($"Added {submitted} {InventoryItemDisplayUtility.ResolveItemName(item)}.", true);
        RefreshRows();
    }

    private int GetAvailableUnreservedAmount(ItemSO item)
    {
        int reserved = 0;
        for (int index = 0; index < _rows.Count; index++)
        {
            RequirementRow row = _rows[index];
            if (IsSameItem(row.Requirement.Item, item))
                reserved += row.AssignedAmount;
        }

        return Mathf.Max(0, _inventory.GetInventoryItemAmount(item) - reserved);
    }

    private void RebuildRequirementRows()
    {
        for (int index = _rowRoot.childCount - 1; index >= 0; index--)
            Destroy(_rowRoot.GetChild(index).gameObject);

        _rows.Clear();
        IReadOnlyList<ShopTradeRequirement> requirements = _offer != null ? _offer.TradeRequirements : null;

        int rowCount = _isKeySubmission ? 1 : requirements?.Count ?? 0;
        for (int index = 0; index < rowCount; index++)
        {
            ShopTradeRequirement requirement = _isKeySubmission ? null : requirements[index];
            if (requirement == null && !_isKeySubmission)
                continue;

            Image rowImage = CreateImage($"PaymentSlot_{index + 1}", _rowRoot, new Color(0.035f, 0.16f, 0.21f, 1f));
            rowImage.gameObject.AddComponent<LayoutElement>().preferredHeight = 78f;
            Outline outline = rowImage.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.08f, 0.73f, 0.9f, 0.85f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            Image icon = CreateImage("Icon", rowImage.transform, Color.white);
            icon.preserveAspect = true;
            SetAnchors(icon.rectTransform, new Vector2(0.035f, 0.15f), new Vector2(0.15f, 0.85f));

            TextMeshProUGUI label = CreateText("Item", rowImage.transform, 19f, FontStyles.Bold, TextAlignmentOptions.Left);
            SetAnchors(label.rectTransform, new Vector2(0.18f, 0.5f), new Vector2(0.73f, 0.87f));

            TextMeshProUGUI amount = CreateText("Amount", rowImage.transform, 17f, FontStyles.Normal, TextAlignmentOptions.Left);
            SetAnchors(amount.rectTransform, new Vector2(0.18f, 0.12f), new Vector2(0.82f, 0.48f));

            TextMeshProUGUI hint = CreateText("Hint", rowImage.transform, 13f, FontStyles.Normal, TextAlignmentOptions.Right);
            hint.text = UIRuntimeLocalization.Get(
                "UI_Inventory",
                "Inventory_ShopDropClear",
                "Drop / Right-click Clear");
            hint.color = new Color(0.55f, 0.7f, 0.76f, 1f);
            SetAnchors(hint.rectTransform, new Vector2(0.68f, 0.1f), new Vector2(0.96f, 0.44f));

            _rows.Add(new RequirementRow
            {
                Requirement = requirement,
                Background = rowImage,
                Icon = icon,
                Label = label,
                Amount = amount
            });

            rowImage.gameObject.AddComponent<ShopPurchaseTradeDropSlotUI>().Configure(this, _rows.Count - 1);
        }
    }

    private void RefreshView()
    {
        if (_instructionText != null) _instructionText.text = _isKeySubmission
            ? "왼쪽 인벤토리의 Key Tool에서 열쇠를 드래그하거나 더블클릭하세요."
            : "Double-click an inventory item or drag it into its matching payment slot.";
        if (_confirmText != null) _confirmText.text = _isKeySubmission ? _keyConfirmationLabel : UIRuntimeLocalization.Get(
            "UI_Inventory", "Inventory_ShopConfirmTrade", "Confirm Trade");
        if (_isKeySubmission)
        {
            _titleText.text = _keyTitle;
            _productText.text = _keyDescription;
            RefreshRows();
            return;
        }
        if (_titleText != null)
            _titleText.text = UIRuntimeLocalization.Get(
                "UI_Inventory",
                "Inventory_ShopPurchasePayment",
                "Purchase Payment");

        if (_productText != null)
            _productText.text = _offer != null
                ? UIRuntimeLocalization.Format(
                    "UI_Inventory",
                    "Inventory_ShopReceiveFormat",
                    "Receive: {0}",
                    _offer.DisplayName)
                : string.Empty;

        RefreshRows();
    }

    private void RefreshRows()
    {
        if (_isKeySubmission)
        {
            if (_rows.Count == 0 || _inventory == null) return;
            var snapshot = _inventory.GetKeyToolSlotSnapshot(_submittedKeySlot);
            bool filled = snapshot.HasKey && snapshot.Key == _submittedKey && snapshot.Key.Tier == _requiredKeyTier;
            RequirementRow row = _rows[0];
            row.Icon.sprite = filled ? snapshot.Key.PreviewImage : null;
            row.Icon.enabled = row.Icon.sprite != null;
            row.Label.text = "필요 열쇠: " + (_requiredKeyTier switch
            {
                LootKeyTier.Normal => "일반", LootKeyTier.Rare => "희귀",
                LootKeyTier.Unique => "유니크", LootKeyTier.Legendary => "전설", _ => _requiredKeyTier.ToString()
            });
            row.Amount.text = filled ? $"제출 완료 · 남은 사용 횟수 {snapshot.RemainingUses}" : "열쇠를 여기에 제출하세요";
            row.Background.color = filled ? new Color(.07f, .36f, .25f, 1f) : new Color(.035f, .16f, .21f, 1f);
            _confirmButton.interactable = filled;
            return;
        }
        bool allFilled = _rows.Count > 0;
        for (int index = 0; index < _rows.Count; index++)
        {
            RequirementRow row = _rows[index];
            int owned = _inventory != null ? _inventory.GetInventoryItemAmount(row.Requirement.Item) : 0;
            bool filled = row.AssignedAmount >= row.Requirement.Amount;
            allFilled &= filled;

            row.Icon.sprite = row.Requirement.Item != null ? row.Requirement.Item.PreviewImage : null;
            row.Icon.enabled = row.Icon.sprite != null;
            row.Label.text = InventoryItemDisplayUtility.ResolveItemName(row.Requirement.Item);
            row.Amount.text = UIRuntimeLocalization.Format(
                "UI_Inventory",
                "Inventory_ShopPaymentAmountFormat",
                "Inserted {0}/{1}   •   Owned {2}",
                row.AssignedAmount,
                row.Requirement.Amount,
                owned);
            row.Background.color = filled
                ? new Color(0.07f, 0.36f, 0.25f, 1f)
                : new Color(0.035f, 0.16f, 0.21f, 1f);
        }

        if (_confirmButton != null)
            _confirmButton.interactable = allFilled;
    }

    private void SetFeedback(string message, bool success)
    {
        if (_feedbackText == null)
            return;

        _feedbackText.text = message ?? string.Empty;
        _feedbackText.color = success
            ? new Color(0.55f, 0.95f, 0.72f, 1f)
            : new Color(1f, 0.55f, 0.48f, 1f);
    }

    private static bool IsSameItem(ItemSO left, ItemSO right)
    {
        if (left == null || right == null)
            return false;

        return left == right ||
               (!string.IsNullOrWhiteSpace(left.Guid) && left.Guid == right.Guid);
    }

    private void BuildRuntimeUi()
    {
        RectTransform root = transform as RectTransform;
        SetFullStretch(root);

        // No full-screen raycast backdrop: the player must still be able to
        // drag and double-click the existing inventory panel on the left.
        Image frame = CreateImage("PurchaseTradeFrame", transform, new Color(0.012f, 0.075f, 0.105f, 0.99f));
        RectTransform frameRect = frame.rectTransform;
        frameRect.anchorMin = frameRect.anchorMax = new Vector2(0.78f, 0.52f);
        frameRect.pivot = new Vector2(0.5f, 0.5f);
        frameRect.sizeDelta = new Vector2(620f, 590f);
        frameRect.anchoredPosition = Vector2.zero;
        Outline outline = frame.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0.8f, 1f, 0.82f);
        outline.effectDistance = new Vector2(2f, -2f);

        _titleText = CreateText("Title", frame.transform, 32f, FontStyles.Bold, TextAlignmentOptions.Center);
        _titleText.color = new Color(0.76f, 0.96f, 1f, 1f);
        SetAnchors(_titleText.rectTransform, new Vector2(0.08f, 0.89f), new Vector2(0.92f, 0.96f));

        _productText = CreateText("Product", frame.transform, 20f, FontStyles.Bold, TextAlignmentOptions.Center);
        _productText.color = new Color(1f, 0.82f, 0.46f, 1f);
        SetAnchors(_productText.rectTransform, new Vector2(0.08f, 0.82f), new Vector2(0.92f, 0.88f));

        TextMeshProUGUI instruction = CreateText("Instruction", frame.transform, 16f, FontStyles.Normal, TextAlignmentOptions.Center);
        _instructionText = instruction;
        instruction.text = UIRuntimeLocalization.Get(
            "UI_Inventory",
            "Inventory_ShopPaymentInstruction",
            "Double-click an inventory item or drag it into the matching slot.\nItems are consumed only after Confirm.");
        instruction.color = new Color(0.58f, 0.75f, 0.82f, 1f);
        SetAnchors(instruction.rectTransform, new Vector2(0.08f, 0.72f), new Vector2(0.92f, 0.80f));

        RectTransform rows = CreateRect("PaymentSlots", frame.transform);
        SetAnchors(rows, new Vector2(0.08f, 0.24f), new Vector2(0.92f, 0.69f));
        VerticalLayoutGroup layout = rows.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.spacing = 10f;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        _rowRoot = rows;

        _feedbackText = CreateText("Feedback", frame.transform, 16f, FontStyles.Normal, TextAlignmentOptions.Center);
        SetAnchors(_feedbackText.rectTransform, new Vector2(0.08f, 0.16f), new Vector2(0.92f, 0.22f));

        _confirmButton = CreateButton("ConfirmButton", frame.transform, new Color(0.06f, 0.42f, 0.5f, 1f));
        _confirmButton.onClick.AddListener(HandleConfirmClicked);
        RectTransform confirmRect = _confirmButton.GetComponent<RectTransform>();
        SetAnchors(confirmRect, new Vector2(0.12f, 0.055f), new Vector2(0.54f, 0.13f));
        TextMeshProUGUI confirmText = CreateText("Label", _confirmButton.transform, 19f, FontStyles.Bold, TextAlignmentOptions.Center);
        _confirmText = confirmText;
        confirmText.text = UIRuntimeLocalization.Get(
            "UI_Inventory",
            "Inventory_ShopConfirmTrade",
            "Confirm Trade");
        SetFullStretch(confirmText.rectTransform);

        Button cancelButton = CreateButton("CancelButton", frame.transform, new Color(0.32f, 0.08f, 0.1f, 1f));
        cancelButton.onClick.AddListener(Close);
        RectTransform cancelRect = cancelButton.GetComponent<RectTransform>();
        SetAnchors(cancelRect, new Vector2(0.57f, 0.055f), new Vector2(0.88f, 0.13f));
        TextMeshProUGUI cancelText = CreateText("Label", cancelButton.transform, 19f, FontStyles.Bold, TextAlignmentOptions.Center);
        cancelText.text = UIRuntimeLocalization.Get(
            "UI_Popup",
            "Popup_Cancel",
            "Cancel");
        SetFullStretch(cancelText.rectTransform);
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject child = new GameObject(name, typeof(RectTransform));
        RectTransform rect = child.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    private static Image CreateImage(string name, Transform parent, Color color)
    {
        Image image = CreateRect(name, parent).gameObject.AddComponent<Image>();
        image.color = color;
        return image;
    }

    private static Button CreateButton(string name, Transform parent, Color color)
    {
        Image image = CreateImage(name, parent, color);
        Button button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        return button;
    }

    private static TextMeshProUGUI CreateText(
        string name,
        Transform parent,
        float size,
        FontStyles style,
        TextAlignmentOptions alignment)
    {
        TextMeshProUGUI text = CreateRect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
        text.fontSize = size;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.Normal;
        return text;
    }

    private static void SetFullStretch(RectTransform rect)
    {
        if (rect == null)
            return;

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void SetAnchors(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}

/// <summary>Accepts one inventory drag on an authored-by-code payment row.</summary>
public sealed class ShopPurchaseTradeDropSlotUI : MonoBehaviour, IDropHandler, IPointerClickHandler
{
    private ShopPurchaseTradePanel _owner;
    private int _rowIndex;

    public void Configure(ShopPurchaseTradePanel owner, int rowIndex)
    {
        _owner = owner;
        _rowIndex = rowIndex;
    }

    public void OnDrop(PointerEventData eventData)
    {
        _owner?.HandleRequirementDrop(_rowIndex);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData != null && eventData.button == PointerEventData.InputButton.Right)
            _owner?.ClearRequirement(_rowIndex);
    }
}
