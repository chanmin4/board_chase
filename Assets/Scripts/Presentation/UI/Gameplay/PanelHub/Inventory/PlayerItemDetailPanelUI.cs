/*
 * Role hierarchy: Presentation > UI > Gameplay > PanelHub > Inventory
 * Primary responsibility: Presents PlayerItemDetailPanelUI state and handles its user-facing UI interaction.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class PlayerItemDetailPanelUI : MonoBehaviour
{
    [Header("Authored Refs")]
    [Tooltip("CanvasGroup on the pre-authored DescriptionHub parent. It is hidden until an item is selected.")]
    [SerializeField] private CanvasGroup _descriptionHubCanvasGroup;
    [Tooltip("The pre-authored ItemDetailPanel child. Its RectTransform is positioned in the Canvas prefab, not at runtime.")]
    [SerializeField] private RectTransform _panelRoot;

    [Header("Editor Preview")]
    [Tooltip("Keeps the pre-authored DescriptionHub visible while editing Canvas-GamePlay. Play mode always starts hidden until an item is selected.")]
    [SerializeField] private bool _showEditorPreview = true;

    [Header("Style")]
    [SerializeField] private Color _backgroundColor = new(0.015f, 0.07f, 0.09f, 0.96f);
    [SerializeField] private Color _outlineColor = new(0f, 0.85f, 1f, 0.9f);
    [SerializeField] private Color _secondaryTextColor = new(0.65f, 0.9f, 0.95f, 1f);
    [SerializeField] private Color _warningColor = new(1f, 0.35f, 0.25f, 1f);

    private Image _icon;
    private TextMeshProUGUI _nameText;
    private TextMeshProUGUI _summaryText;
    private TextMeshProUGUI _detailsText;
    private LocalizedString _boundName;
    private LocalizedString _boundDescription;
    private string _localizedDescription;
    private ItemDetailSelection _selection;
    private PlayerStatsRuntime _equippedStats;
    private float _nextWeaponRefreshTime;
    private RectTransformLayout _authoredLayout;
    private bool _hasAuthoredLayout;
    private Transform _authoredHubParent;
    private int _authoredHubSiblingIndex;

    private struct RectTransformLayout
    {
        public Vector2 anchorMin;
        public Vector2 anchorMax;
        public Vector2 pivot;
        public Vector2 anchoredPosition;
        public Vector2 sizeDelta;
        public Vector3 localScale;
    }

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        ResolveAuthoredRefs();
        CaptureAuthoredLayout();
        BuildView();
        ShowEmpty();
    }

    // Purpose: Keeps the pre-authored description column visible and populated in the Scene view without changing Play-mode visibility.
    private void OnValidate()
    {
        if (Application.isPlaying)
            return;

        ResolveAuthoredRefs();
        ApplyEditorPreview();
    }

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    private void OnEnable()
    {
        ResolveAuthoredRefs();
        BuildView();
        ItemDetailSelectionBus.SelectionChanged += HandleSelectionChanged;

        if (ItemDetailSelectionBus.Current.HasItem)
            HandleSelectionChanged(ItemDetailSelectionBus.Current);
        else
            ShowEmpty();
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        ItemDetailSelectionBus.SelectionChanged -= HandleSelectionChanged;
        UnbindLocalizedStrings();
        _equippedStats = null;
    }

    // Only the selected player's equipment is sampled. No scene searches or SO-only matching.
    // Unscaled time keeps condition/status changes visible while inventory input is locked.
    private void LateUpdate()
    {
        if (_equippedStats == null || !_selection.HasItem || Time.unscaledTime < _nextWeaponRefreshTime)
            return;

        _nextWeaponRefreshTime = Time.unscaledTime + 0.2f;
        if (_selection.playerEquipmentOwner == null ||
            _selection.playerEquipmentOwner.GetEquippedWeapon(_selection.playerWeaponSlotIndex) != _selection.item)
        {
            ShowEmpty();
            return;
        }
        RefreshText();
    }

    // Purpose: Handles the incoming selection changed request or event.
    private void HandleSelectionChanged(ItemDetailSelection selection)
    {
        if (!selection.HasItem)
        {
            ShowEmpty();
            return;
        }

        BuildView();
        SetVisible(true);
        UnbindLocalizedStrings();
        _selection = selection;
        _equippedStats = selection.playerEquipmentOwner != null
            ? EntityComponentResolver.Resolve<PlayerStatsRuntime>(selection.playerEquipmentOwner) : null;
        _nextWeaponRefreshTime = 0f;
        _localizedDescription = string.Empty;

        if (_icon != null)
        {
            _icon.sprite = selection.item.PreviewImage;
            _icon.enabled = _icon.sprite != null;
            ModuleRuneView.Bind(_icon, selection.item);
        }

        if (_nameText != null)
            _nameText.text = InventoryItemDisplayUtility.ResolveItemName(selection.item);

        BindLocalizedStrings(selection.item);
        RefreshText();
    }

    // Purpose: Shows the empty UI or visual state.
    private void ShowEmpty()
    {
        BuildView();
        UnbindLocalizedStrings();
        _selection = default;
        _equippedStats = null;
        SetVisible(false);

        if (_icon != null)
        {
            _icon.sprite = null;
            _icon.enabled = false;
        }

        if (_nameText != null)
            _nameText.text = UIRuntimeLocalization.Get(
                "UI_Inventory",
                "Inventory_DetailSelectItem",
                "Select an item");

        if (_summaryText != null)
            _summaryText.text = string.Empty;

        if (_detailsText != null)
            _detailsText.text = UIRuntimeLocalization.Get(
                "UI_Inventory",
                "Inventory_DetailSelectItemDescription",
                "Select an inventory, shop, or loot slot to view details.");
    }

    // Purpose: Refreshes the displayed or cached text.
    private void RefreshText()
    {
        if (!_selection.HasItem)
            return;

        if (_summaryText != null)
        {
            _summaryText.text = BuildSummary(_selection);
            _summaryText.color = IsBrokenWeapon(_selection)
                ? _warningColor
                : _secondaryTextColor;
        }

        if (_detailsText != null)
        {
            StringBuilder builder = new();
            AppendGameplayDetails(builder, _selection);

            if (!string.IsNullOrWhiteSpace(_localizedDescription))
            {
                if (builder.Length > 0)
                    builder.AppendLine().AppendLine();

                builder.Append(_localizedDescription);
            }

            _detailsText.text = builder.Length > 0
                ? builder.ToString()
                : UIRuntimeLocalization.Get(
                    "UI_Inventory",
                    "Inventory_DetailNoDescription",
                    "No details are available.");
        }
    }

    // Purpose: Builds the summary from the current authored data.
    private string BuildSummary(ItemDetailSelection selection)
    {
        string amount = selection.amount > 1 ? $"  x{selection.amount}" : string.Empty;
        string summary = $"{selection.item.Kind}{amount}";
        if (selection.item is ValuableItemSO valuable) summary += "\n물품 분류 · " + valuable.ValuableCategoryLabel;

        if (selection.item is RangedWeaponSO weapon)
        {
            float maximum = ResolveWeaponMaximum(selection, weapon);
            float current = ResolveWeaponCurrent(selection, maximum);
            float condition = maximum > 0f ? Mathf.Clamp01(current / maximum) : 1f;
            float performance = Mathf.Lerp(0.5f, 1f, condition);
            summary += $"\n내구도  {current:0.#} / {maximum:0.#}\n공격·정확도 성능  {performance * 100f:0}%";
        }
        else if (selection.item is ArmorItemSO armor)
        {
            float maximum = selection.armorMaxDurability >= 0f
                ? selection.armorMaxDurability
                : armor.MaxDurability;
            float current = selection.armorDurability >= 0f
                ? selection.armorDurability
                : maximum;
            summary += $"\n내구도  {current:0.#} / {maximum:0.#}";
        }
        else if (selection.item is GasMaskItemSO gasMask)
        {
            float maximum = selection.gasMaskMaxFilterDurability >= 0f
                ? selection.gasMaskMaxFilterDurability
                : gasMask.MaxFilterDurability;
            float current = selection.gasMaskFilterDurability >= 0f
                ? Mathf.Clamp(selection.gasMaskFilterDurability, 0f, maximum)
                : maximum;
            summary += $"\n필터 내구도  {current:0.#} / {maximum:0.#}";
        }

        return summary;
    }

    // Purpose: Performs the append gameplay details workflow for this component.
    private void AppendGameplayDetails(StringBuilder builder, ItemDetailSelection selection)
    {
        if (selection.item != null && selection.item is not PassiveItemSO)
            builder.AppendLine($"무게: {selection.item.WeightKg:0.###} kg / 개");
        switch (selection.item)
        {
            case PassiveItemSO passive:
                builder.AppendLine($"{(passive.Category == PassiveCategory.Combat ? "전투" : "생존")} 패시브 · 원정 한정 효과");
                builder.AppendLine("장착·해제·교체·리롤은 HOME에서만 가능합니다.");
                builder.AppendLine(passive.DisabledForRaid ? "비활성 · 재장착/분해 불가 · 귀환 시 소멸" :
                    passive.EverEquipped ? "귀속 · 해제하면 재장착/분해 불가 · 귀환 시 소멸" : "미장착 · 시간제 분해 가능 · 장착 시 귀속");
                if (passive.StatModifiers != null)
                    foreach (var modifier in passive.StatModifiers)
                    {
                        var applied = PassiveModuleRulesSO.Active != null ? PassiveModuleRulesSO.Active.ApplyStrength(passive, modifier) : modifier;
                        builder.AppendLine(applied.stat == PlayerStatId.ExperienceGainBonus ? $"획득 경험치: +{applied.value*100:0.#}% (획득 순간 적용)" : $"{modifier.stat}: {applied.value:+0.###;-0.###;0} ({modifier.type})");
                    }
                if (passive.IsRolledInstance) builder.AppendLine($"개체 ID: {passive.Guid}");
                break;
            case RangedWeaponSO weapon:
            {
                float maximum = ResolveWeaponMaximum(selection, weapon);
                float current = ResolveWeaponCurrent(selection, maximum);
                float condition = maximum > 0f ? Mathf.Clamp01(current / maximum) : 1f;
                float performance = Mathf.Lerp(0.5f, 1f, condition);

                builder.AppendLine($"총기 종류: {weapon.WeaponType}");
                builder.AppendLine($"사용 탄약: {weapon.RequiredBulletFamily}");
                AppendWeaponStats(builder, selection, weapon);
                builder.AppendLine($"발사당 내구도 감소: {weapon.DurabilityLossPerFire:0.##}");
                builder.AppendLine($"현재 공격력·정확도 계수: x{performance:0.##}");
                builder.Append(EquipmentRepairPolicy.Enabled
                    ? $"다음 수리 후 최대 내구도: {ResolveRepairedMaximum(current, maximum, weapon):0.#}"
                    : EquipmentRepairPolicy.DisabledLabel);
                break;
            }

            case ArmorItemSO armor:
                builder.AppendLine($"방어구 클래스: {armor.ArmorClass}");
                builder.Append("피격 시 흡수한 피해만큼 내구도가 감소합니다.");
                if (!EquipmentRepairPolicy.Enabled) builder.Append("\n" + EquipmentRepairPolicy.DisabledLabel);
                break;

            case GasMaskItemSO gasMask:
            {
                float maximum = selection.gasMaskMaxFilterDurability >= 0f
                    ? selection.gasMaskMaxFilterDurability
                    : gasMask.MaxFilterDurability;
                float current = selection.gasMaskFilterDurability >= 0f
                    ? Mathf.Clamp(selection.gasMaskFilterDurability, 0f, maximum)
                    : maximum;
                float nextMaximum = ResolveRepairedMaximum(current, maximum, gasMask.RepairPermanentLossRatio);
                builder.AppendLine($"가스방어력: {gasMask.GasDefenseLevel} (필터 소진 시 0)");
                builder.AppendLine($"필터 소모율: {gasMask.FilterDurabilityLossPerRawInfection:0.##}");
                builder.Append(EquipmentRepairPolicy.Enabled
                    ? $"다음 수리 후 최대 필터 내구도: {nextMaximum:0.#}"
                    : EquipmentRepairPolicy.DisabledLabel);
                break;
            }

            case BulletSO bullet:
                builder.AppendLine($"탄종: {bullet.Family}");
                if (bullet.InfectionExposureOnHit > 0f)
                    builder.AppendLine($"AR 전용 · 감염치 +{bullet.InfectionExposureOnHit:0.#}");
                else
                    builder.AppendLine($"티어: {(int)bullet.Tier}");
                builder.AppendLine($"관통 클래스: {bullet.PenetrationClass:0.###}");
                builder.AppendLine($"캐릭터 공격 보정: {CombatBalanceRules.AmmoDamageAdjustment(bullet.Tier):+0.##;-0.##;0}");
                builder.AppendLine($"Infection 피해: {bullet.InfectionDamage:0.##}");
                break;

            case ConsumableItemSO consumable:
                if (consumable is InfectionCureItemSO ampoule)
                {
                    if (ampoule.IsSyringe)
                        builder.AppendLine($"최대 HP 잠식 회복: {ampoule.InfectionRecoveryAmount:0.##}. 현재 HP는 별도 치료. 레이드 루팅 전용, 상점 판매 없음.");
                    else
                    {
                        builder.AppendLine($"감염 유입 ×{ampoule.ExposureGainMultiplier:0.##}, 게이지 회복 +{ampoule.InfectionRecoveryPerSecond:0.##}/초, {ampoule.InfectionRecoveryDurationSeconds:0.##}초간.");
                        builder.AppendLine("새 노출 없이 회복 대기가 지난 미완성 게이지만 회복. 최대 HP 잠식 회복 불가. 중첩 불가.");
                    }
                }
                if (consumable.InfectionSuppressionSeconds > 0f)
                    builder.AppendLine($"억제 시간: {consumable.InfectionSuppressionSeconds:0}초. 감염 수치 자체는 낮추지 않습니다.");
                if (consumable is InfectionCureItemSO || consumable.InfectionSuppressionSeconds > 0f)
                {
                    var raid = RaidRuntime.Active;
                    int dose = consumable is InfectionCureItemSO ? 1 : raid != null && raid.IsPlayerInRaid && raid.Definition != null
                        ? raid.Definition.RaidRuntime.MedicalDose(raid.Definition.StageIndex) : 1;
                    builder.AppendLine($"이번 사용 분량: {dose}개 (1 / 2 / 3 스테이지: 1 / 3 / 5개)");
                }
                builder.AppendLine($"체력 회복: {consumable.HealAmount:0.##}");
                builder.Append($"사용 시간: {consumable.UseDurationSeconds:0.##}초");
                break;
        }
    }

    private void AppendWeaponStats(StringBuilder builder, ItemDetailSelection selection, RangedWeaponSO weapon)
    {
        bool active = _equippedStats != null && _equippedStats.IsActiveWeaponSlot(
            selection.playerEquipmentOwner, selection.playerWeaponSlotIndex, weapon);
        if (!active)
        {
            builder.AppendLine("<b>무기 기본 성능 · 패시브 미적용</b>");
            builder.AppendLine($"사거리 {weapon.MaxRange:0.###} / 연사 {weapon.ShotsPerSecond:0.###}/초 / 탄창 {weapon.MagazineSize}");
            builder.AppendLine($"재장전 {weapon.ReloadDurationSeconds:0.###}초 / 조준속도 {weapon.AimSpeed:0.###}");
            builder.AppendLine($"탄퍼짐: 비조준 {weapon.HipFireSpreadAngleDeg:0.###}° / 정조준 {weapon.AimSpreadAngleDeg:0.###}°");
            builder.AppendLine($"발사 반동 {weapon.ShotKickDistance:0.###} / 상한 {weapon.MaximumShotKickDistance:0.###} / 복구 {weapon.ShotKickRecoveryPerSecond:0.###}/초");
            return;
        }

        builder.AppendLine("<b>장착 무기 · 최종 성능</b>");
        builder.AppendLine("<size=80%>무기(무기 보정 포함) + 패시브 [+ 기타] = 최종값</size>");
        AppendWeaponStat(builder, "발사 반동", PlayerStatId.ShotKickDistance, false);
        AppendWeaponStat(builder, "반동 상한", PlayerStatId.MaximumShotKickDistance, false);
        AppendWeaponStat(builder, "반동 복구", PlayerStatId.ShotKickRecoveryPerSecond, true, "/초");
        AppendWeaponStat(builder, "조준속도", PlayerStatId.AimSpeed, true);
        AppendWeaponStat(builder, "연사속도", PlayerStatId.ShotsPerSecond, true, "/초");
        AppendWeaponStat(builder, "재장전", PlayerStatId.ReloadDurationSeconds, false, "초");
        AppendWeaponStat(builder, "비조준 탄퍼짐", PlayerStatId.HipFireSpreadAngleDeg, false, "°");
        AppendWeaponStat(builder, "정조준 탄퍼짐", PlayerStatId.AimSpreadAngleDeg, false, "°");
        AppendWeaponStat(builder, "사거리", PlayerStatId.MaxRange, true);
        AppendWeaponStat(builder, "관통 보정", PlayerStatId.PenetrationClass, true);
        AppendWeaponStat(builder, "총성 반경", PlayerStatId.GunshotSoundRadius, false);
        builder.AppendLine($"탄창  <b>{weapon.MagazineSize}</b>");
        builder.AppendLine("<size=80%>기타: 퀘스트·장비·탄약·내구도·상태 효과. 관통 보정은 탄약 자체 관통을 제외한 값.</size>");
    }

    private void AppendWeaponStat(StringBuilder builder, string label, PlayerStatId stat, bool higherIsBetter, string unit = "")
    {
        if (!_equippedStats.TryGetWeaponStatBreakdown(stat, out var row)) return;
        builder.Append(label).Append("  ").Append(row.weaponValue.ToString("0.###"));
        AppendStatDelta(builder, row.passiveDelta, higherIsBetter, "패시브");
        if (Mathf.Abs(row.otherDelta) >= 0.0005f)
            AppendStatDelta(builder, row.otherDelta, higherIsBetter, "기타");
        builder.Append(" = <b>").Append(row.finalValue.ToString("0.###")).Append(unit).AppendLine("</b>");
    }

    private static void AppendStatDelta(StringBuilder builder, float delta, bool higherIsBetter, string source)
    {
        bool neutral = Mathf.Abs(delta) < 0.0005f;
        bool beneficial = higherIsBetter ? delta > 0f : delta < 0f;
        string color = neutral ? "#A6BFC4" : beneficial ? "#72E69B" : "#FF8B7A";
        builder.Append(" <color=").Append(color).Append('>')
            .Append(delta < -0.0005f ? "- " : "+ ").Append(Mathf.Abs(delta).ToString("0.###"))
            .Append('(').Append(source).Append(")</color>");
    }

    // Purpose: Resolves the weapon maximum needed by this component.
    private static float ResolveWeaponMaximum(ItemDetailSelection selection, RangedWeaponSO weapon)
    {
        if (selection.playerEquipmentOwner != null &&
            selection.playerEquipmentOwner.GetEquippedWeapon(selection.playerWeaponSlotIndex) == weapon)
            return selection.playerEquipmentOwner.GetEquippedWeaponMaxDurability(selection.playerWeaponSlotIndex);
        return selection.weaponMaxDurability > 0f
            ? selection.weaponMaxDurability
            : weapon.MaxDurability;
    }

    // Purpose: Resolves the weapon current needed by this component.
    private static float ResolveWeaponCurrent(ItemDetailSelection selection, float maximum)
    {
        if (selection.playerEquipmentOwner != null &&
            selection.playerEquipmentOwner.GetEquippedWeapon(selection.playerWeaponSlotIndex) == selection.item)
            return Mathf.Clamp(selection.playerEquipmentOwner.GetEquippedWeaponCurrentDurability(selection.playerWeaponSlotIndex), 0f, maximum);
        return selection.weaponCurrentDurability >= 0f
            ? Mathf.Clamp(selection.weaponCurrentDurability, 0f, maximum)
            : maximum;
    }

    // Purpose: Resolves the repaired maximum needed by this component.
    private static float ResolveRepairedMaximum(
        float current,
        float maximum,
        RangedWeaponSO weapon)
    {
        if (current >= maximum)
            return maximum;

        return Mathf.Max(
            1f,
            maximum - ((maximum - current) * weapon.RepairPermanentLossRatio));
    }

    // Purpose: Predicts an item-specific repaired maximum without changing runtime condition.
    private static float ResolveRepairedMaximum(
        float current,
        float maximum,
        float repairPermanentLossRatio)
    {
        if (current >= maximum)
            return maximum;

        return Mathf.Max(
            1f,
            maximum - ((maximum - current) * Mathf.Clamp01(repairPermanentLossRatio)));
    }

    // Purpose: Checks whether broken weapon is currently true.
    private static bool IsBrokenWeapon(ItemDetailSelection selection)
    {
        return selection.item is RangedWeaponSO weapon &&
               ResolveWeaponCurrent(
                   selection,
                   ResolveWeaponMaximum(selection, weapon)) <= 0f;
    }

    // Purpose: Binds localized strings to the active runtime or UI target.
    private void BindLocalizedStrings(ItemSO item)
    {
        if (InventoryItemDisplayUtility.TryGetItemNameReference(item, out _boundName))
        {
            _boundName.StringChanged += HandleLocalizedNameChanged;
            _boundName.RefreshString();
        }

        if (item.Description != null && !item.Description.IsEmpty)
        {
            _boundDescription = item.Description;
            _boundDescription.StringChanged += HandleLocalizedDescriptionChanged;
            _boundDescription.RefreshString();
        }
    }

    // Purpose: Removes the localized strings binding.
    private void UnbindLocalizedStrings()
    {
        if (_boundName != null)
            _boundName.StringChanged -= HandleLocalizedNameChanged;

        if (_boundDescription != null)
            _boundDescription.StringChanged -= HandleLocalizedDescriptionChanged;

        _boundName = null;
        _boundDescription = null;
    }

    // Purpose: Handles the incoming localized name changed request or event.
    private void HandleLocalizedNameChanged(string value)
    {
        if (_nameText != null && !string.IsNullOrWhiteSpace(value))
            _nameText.text = value;
    }

    // Purpose: Handles the incoming localized description changed request or event.
    private void HandleLocalizedDescriptionChanged(string value)
    {
        _localizedDescription = value ?? string.Empty;
        RefreshText();
    }

    // Purpose: Builds the view from the current authored data.
    private void BuildView()
    {
        if (_panelRoot == null)
            return;

        ResolveExistingViewRefs();

        Image background = _panelRoot.GetComponent<Image>();
        if (background != null)
        {
            background.color = _backgroundColor;
            background.raycastTarget = false;
        }

        Outline outline = _panelRoot.GetComponent<Outline>();
        if (outline != null)
        {
            outline.effectColor = _outlineColor;
            outline.effectDistance = new Vector2(2f, -2f);
        }

        // Description uses one solid column backdrop. The scrolling viewport
        // remains a mask only; tinting it as another black Image produced a
        // second nested background in the Loot workspace.
        ConfigureDetailsViewport();

        if (_icon != null && _nameText != null && _summaryText != null && _detailsText != null)
            return;

        if (_icon == null)
            _icon = CreateIcon(_panelRoot);

        if (_nameText == null)
        {
            _nameText = CreateText("ItemName", _panelRoot, 28f, FontStyles.Bold);
            RectTransform nameRect = _nameText.rectTransform;
            nameRect.anchorMin = new Vector2(0f, 1f);
            nameRect.anchorMax = new Vector2(1f, 1f);
            nameRect.pivot = new Vector2(0f, 1f);
            nameRect.anchoredPosition = new Vector2(118f, -20f);
            nameRect.sizeDelta = new Vector2(-136f, 58f);
        }

        if (_summaryText == null)
        {
            _summaryText = CreateText("ItemSummary", _panelRoot, 19f, FontStyles.Normal);
            _summaryText.color = _secondaryTextColor;
            RectTransform summaryRect = _summaryText.rectTransform;
            summaryRect.anchorMin = new Vector2(0f, 1f);
            summaryRect.anchorMax = new Vector2(1f, 1f);
            summaryRect.pivot = new Vector2(0f, 1f);
            summaryRect.anchoredPosition = new Vector2(118f, -82f);
            summaryRect.sizeDelta = new Vector2(-136f, 112f);
        }

        if (_detailsText == null)
            CreateDetailsScroll(_panelRoot);
    }

    // Purpose: Keeps the authored Details viewport as a transparent clipping mask rather than a second visual panel.
    private void ConfigureDetailsViewport()
    {
        if (_panelRoot == null)
            return;

        Transform detailsScroll = _panelRoot.Find("DetailsScroll");
        Transform viewport = detailsScroll != null ? detailsScroll.Find("Viewport") : null;
        if (viewport == null || !viewport.TryGetComponent(out Image viewportImage))
            return;

        viewportImage.color = Color.clear;
        viewportImage.raycastTarget = false;
    }

    // Purpose: Reuses the description controls authored into Canvas-GamePlay instead of creating a second runtime-only set.
    private void ResolveExistingViewRefs()
    {
        _icon ??= FindViewComponent<Image>("ItemIcon");
        _nameText ??= FindViewComponent<TextMeshProUGUI>("ItemName");
        _summaryText ??= FindViewComponent<TextMeshProUGUI>("ItemSummary");
        _detailsText ??= FindViewComponent<TextMeshProUGUI>("Details");
    }

    // Purpose: Finds one pre-authored view component by its stable child name.
    private T FindViewComponent<T>(string objectName) where T : Component
    {
        if (_panelRoot == null)
            return null;

        foreach (T component in _panelRoot.GetComponentsInChildren<T>(true))
        {
            if (component != null && component.gameObject.name == objectName)
                return component;
        }

        return null;
    }

    // Purpose: Applies the Scene-view-only description sample while leaving all runtime selection behavior unchanged.
    private void ApplyEditorPreview()
    {
        if (_descriptionHubCanvasGroup != null)
        {
            _descriptionHubCanvasGroup.alpha = _showEditorPreview ? 1f : 0f;
            _descriptionHubCanvasGroup.interactable = false;
            _descriptionHubCanvasGroup.blocksRaycasts = false;
        }

        if (!_showEditorPreview || _panelRoot == null)
            return;

        _panelRoot.anchorMin = new Vector2(0.5f, 0.5f);
        _panelRoot.anchorMax = new Vector2(0.5f, 0.5f);
        _panelRoot.pivot = new Vector2(0.5f, 0.5f);
        _panelRoot.anchoredPosition = Vector2.zero;
        _panelRoot.sizeDelta = new Vector2(360f, 840f);
        _panelRoot.localScale = Vector3.one;

        BuildView();

        if (_icon != null)
        {
            _icon.sprite = null;
            _icon.enabled = false;
        }

        if (_nameText != null)
            _nameText.text = UIRuntimeLocalization.Get(
                "UI_Inventory",
                "Inventory_DetailPreviewTitle",
                "Description");

        if (_summaryText != null)
        {
            _summaryText.text = UIRuntimeLocalization.Get(
                "UI_Inventory",
                "Inventory_DetailPreviewSummary",
                "Item Details");
            _summaryText.color = _secondaryTextColor;
        }

        if (_detailsText != null)
            _detailsText.text = UIRuntimeLocalization.Get(
                "UI_Inventory",
                "Inventory_DetailPreviewDescription",
                "Scene preview only\n\nSelect an item in Play mode to show its name, stats, condition, and description here.");
    }

    // Purpose: Resolves the authored refs needed by this component.
    private void ResolveAuthoredRefs()
    {
        if (_panelRoot == null)
            _panelRoot = transform as RectTransform;

        if (_descriptionHubCanvasGroup == null)
            _descriptionHubCanvasGroup = GetComponentInParent<CanvasGroup>(true);
    }

    /// <summary>
    /// Uses a compact top-center readout for Shop and a full-height middle column
    /// for Loot. It is still selection-driven, so an empty description hub neither
    /// obscures slots nor consumes drag/drop raycasts.
    /// </summary>
    public void SetContextOverlayPresentation(bool isContextOverlay, bool useLootTransferLayout = false)
    {
        ResolveAuthoredRefs();
        CaptureAuthoredLayout();

        if (_panelRoot == null)
            return;

        if (isContextOverlay && useLootTransferLayout)
        {
            _panelRoot.anchorMin = new Vector2(0.5f, 0.5f);
            _panelRoot.anchorMax = new Vector2(0.5f, 0.5f);
            _panelRoot.pivot = new Vector2(0.5f, 0.5f);
            _panelRoot.anchoredPosition = new Vector2(0f, 200f);
            _panelRoot.sizeDelta = new Vector2(420f, 250f);
            _panelRoot.localScale = Vector3.one;

            if (_descriptionHubCanvasGroup != null)
                _descriptionHubCanvasGroup.transform.SetAsLastSibling();
        }
        else if (isContextOverlay)
        {
            _panelRoot.anchorMin = new Vector2(0.5f, 1f);
            _panelRoot.anchorMax = new Vector2(0.5f, 1f);
            _panelRoot.pivot = new Vector2(0.5f, 1f);
            _panelRoot.anchoredPosition = new Vector2(0f, -72f);
            _panelRoot.sizeDelta = new Vector2(420f, 250f);
            _panelRoot.localScale = Vector3.one;

            if (_descriptionHubCanvasGroup != null)
                _descriptionHubCanvasGroup.transform.SetAsLastSibling();
        }
        else if (_hasAuthoredLayout)
        {
            _panelRoot.anchorMin = _authoredLayout.anchorMin;
            _panelRoot.anchorMax = _authoredLayout.anchorMax;
            _panelRoot.pivot = _authoredLayout.pivot;
            _panelRoot.anchoredPosition = _authoredLayout.anchoredPosition;
            _panelRoot.sizeDelta = _authoredLayout.sizeDelta;
            _panelRoot.localScale = _authoredLayout.localScale;

            if (_descriptionHubCanvasGroup != null && _authoredHubParent != null)
            {
                _descriptionHubCanvasGroup.transform.SetSiblingIndex(
                    Mathf.Clamp(
                        _authoredHubSiblingIndex,
                        0,
                        Mathf.Max(0, _authoredHubParent.childCount - 1)));
            }
        }
    }

    // Purpose: Caches the scene-authored detail panel layout before context presentation overrides it.
    private void CaptureAuthoredLayout()
    {
        if (_hasAuthoredLayout || _panelRoot == null)
            return;

        _authoredLayout = new RectTransformLayout
        {
            anchorMin = _panelRoot.anchorMin,
            anchorMax = _panelRoot.anchorMax,
            pivot = _panelRoot.pivot,
            anchoredPosition = _panelRoot.anchoredPosition,
            sizeDelta = _panelRoot.sizeDelta,
            localScale = _panelRoot.localScale
        };
        _hasAuthoredLayout = true;

        if (_descriptionHubCanvasGroup != null)
        {
            _authoredHubParent = _descriptionHubCanvasGroup.transform.parent;
            _authoredHubSiblingIndex = _descriptionHubCanvasGroup.transform.GetSiblingIndex();
        }
    }

    // Purpose: Updates the visible state.
    private void SetVisible(bool visible)
    {
        if (_descriptionHubCanvasGroup == null)
            return;

        _descriptionHubCanvasGroup.alpha = visible ? 1f : 0f;
        // Detail is information-only. Keeping it non-blocking lets users drag
        // between the left player grid and right Shop/Loot grid below it.
        _descriptionHubCanvasGroup.interactable = false;
        _descriptionHubCanvasGroup.blocksRaycasts = false;
    }

    // Purpose: Creates the icon owned by this workflow.
    private Image CreateIcon(RectTransform parent)
    {
        GameObject iconObject = new("ItemIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rect = iconObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(18f, -20f);
        rect.sizeDelta = new Vector2(84f, 84f);

        Image image = iconObject.GetComponent<Image>();
        image.preserveAspect = true;
        image.raycastTarget = false;
        return image;
    }

    // Purpose: Creates the details scroll owned by this workflow.
    private void CreateDetailsScroll(RectTransform parent)
    {
        GameObject scrollObject = new("DetailsScroll", typeof(RectTransform), typeof(ScrollRect));
        RectTransform scrollRect = scrollObject.GetComponent<RectTransform>();
        scrollRect.SetParent(parent, false);
        scrollRect.anchorMin = Vector2.zero;
        scrollRect.anchorMax = Vector2.one;
        scrollRect.offsetMin = new Vector2(16f, 16f);
        scrollRect.offsetMax = new Vector2(-16f, -208f);

        GameObject viewportObject = new(
            "Viewport",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(RectMask2D));
        RectTransform viewportRect = viewportObject.GetComponent<RectTransform>();
        viewportRect.SetParent(scrollRect, false);
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;
        Image viewportImage = viewportObject.GetComponent<Image>();
        viewportImage.color = Color.clear;
        viewportImage.raycastTarget = false;

        _detailsText = CreateText("Details", viewportRect, 18f, FontStyles.Normal);
        RectTransform contentRect = _detailsText.rectTransform;
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = new Vector2(0f, -10f);
        contentRect.sizeDelta = new Vector2(-20f, 0f);
        _detailsText.enableWordWrapping = true;
        _detailsText.overflowMode = TextOverflowModes.Overflow;

        ContentSizeFitter fitter = _detailsText.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scroll = scrollObject.GetComponent<ScrollRect>();
        scroll.viewport = viewportRect;
        scroll.content = contentRect;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 24f;
    }

    // Purpose: Creates the text owned by this workflow.
    private static TextMeshProUGUI CreateText(
        string name,
        Transform parent,
        float fontSize,
        FontStyles style)
    {
        GameObject textObject = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.raycastTarget = false;
        return text;
    }
}
