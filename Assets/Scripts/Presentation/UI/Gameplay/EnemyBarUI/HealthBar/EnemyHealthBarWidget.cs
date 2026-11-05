/*
 * Role hierarchy: Presentation > UI > Gameplay > EnemyBarUI > HealthBar
 * Primary responsibility: Presents EnemyHealthBarWidget state and handles its user-facing UI interaction.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBarWidget : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RectTransform _root;
    [SerializeField] private RectTransform _fillRect;
    [SerializeField] private Image _fillImage;
    [SerializeField] private UICanvasGroupOpacity _uicanvasgroupopacity;

    [Header("Infection")]
    [Tooltip("Maximum-health erosion overlay, normalized to uninfected maximum health. Ordinary wounds do not create an infection death threshold.")]
    [SerializeField] private RectTransform _infectionRoot;
    [SerializeField] private Image _infectionFillImage;

    [Header("Shooter State")]
    [Tooltip("Optional investigate/combat state icon under this widget. It is bound to the spawned enemy through EnemyScreenSpaceHPUIAnchor.")]
    [SerializeField] private ShooterStateIconUI _shooterStateIcon;

    [Header("Status Effects")]
    [Tooltip("Optional icon row near the enemy health bar. When empty it is created beneath the widget at runtime.")]
    [SerializeField] private RectTransform _statusEffectIconRoot;

    [Header("Armor Class")]
    [SerializeField] private TextMeshProUGUI _finalArmorClassText;
    [SerializeField] private string _finalArmorClassFormat = "{0}";

    [Header("Paint Mark")]
    [SerializeField] private RectTransform _paintMarkRoot;
    [SerializeField] private Image _paintMarkBackgroundImage;
    [SerializeField] private Image _paintMarkFillImage;
    [SerializeField] private Image _paintMarkOutlineImage;
    [SerializeField] private Image _paintMarkFactionIconImage;
    [SerializeField] private TextMeshProUGUI _paintMarkStackText;

    [Header("Paint Mark Icons")]
    [SerializeField] private Sprite _vaccineMarkIcon;
    [SerializeField] private Sprite _virusMarkIcon;

    [Header("Paint Mark Colors")]
    [SerializeField] private Color _vaccineMarkColor =
        new Color(0.2f, 0.85f, 1f, 1f);
    [SerializeField] private Color _virusMarkColor =
        new Color(0.65f, 0.2f, 1f, 1f);
    [SerializeField] private Color _emptyMarkColor =
        new Color(1f, 1f, 1f, 0.18f);
    [SerializeField] private Color _activeOutlineColor = Color.white;

    [Header("Paint Mark Format")]
    [SerializeField] private string _paintMarkStackFormat = "{0}";

    [Header("Debug")]
    [SerializeField] private bool _debugPaintMarkLogs;
    [SerializeField, Min(0.1f)] private float _debugPaintMarkLogInterval = 1f;
    [Tooltip("Logs the bound enemy health/Infection values together with the rendered purple fill state. Disable after verification.")]
    [SerializeField] private bool _debugInfectionLogs;
    [SerializeField, Min(0.1f)] private float _debugInfectionLogInterval = 0.5f;

    private EnemyScreenSpaceHPUIAnchor _anchor;
    private Damageable _damageable;
    private ShooterInfection _infection;
    private EntityPaintMarkController _paintMark;
    private EntityStatusEffectRuntime _statusEffects;
    private StatusEffectIconStripUI _statusEffectIcons;
    private EnemyHealthBarSettingsSO _settings;

    private float _emphasisUntilTime;
    private float _nextPaintMarkDebugLogTime;
    private float _nextInfectionDebugLogTime;
    private float _fillFullWidth;

    private bool _managerVisible;
    private bool _hasValidScreenPosition;

    public RectTransform Root => _root != null
        ? _root
        : transform as RectTransform;

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        if (_root == null)
            _root = transform as RectTransform;

        if (_shooterStateIcon == null)
            _shooterStateIcon = GetComponentInChildren<ShooterStateIconUI>(true);

        EnsureStatusEffectIcons();

        if (_fillRect != null && _fillImage == null)
        {
            _fillFullWidth = _fillRect.sizeDelta.x;
            _fillRect.pivot = new Vector2(0f, 0.5f);
            _fillRect.anchorMin = new Vector2(0f, 0.5f);
            _fillRect.anchorMax = new Vector2(0f, 0.5f);
        }

        EnsureFillImageMode();
        EnsureInfectionImageMode();
        EnsurePaintMarkImageMode();
        ForceHiddenUntilPositioned();
        _uicanvasgroupopacity?.HideImmediate();
    }

    // Purpose: Binds bind to the active runtime or UI target.
    public void Bind(EnemyScreenSpaceHPUIAnchor anchor)
    {
        Unbind();

        _anchor = anchor;
        _damageable = anchor != null ? anchor.Damageable : null;
        _infection = ResolveInfection(anchor);

        if (_infection != null && _damageable != null && _infection.Damageable != _damageable)
        {
            Debug.LogError(
                "[EnemyHealthBarWidget] Infection and health references belong to different Damageable components.",
                anchor);
            _infection = null;
        }
        _paintMark = ResolvePaintMarkController(anchor);
        _statusEffects = ResolveStatusEffects(anchor);
        _settings = anchor != null ? anchor.HealthBarSettings : null;
        _shooterStateIcon?.Bind(anchor);
        EnsureStatusEffectIcons();
        _statusEffectIcons?.Bind(_statusEffects);

        _managerVisible = false;
        _hasValidScreenPosition = false;
        _uicanvasgroupopacity?.HideImmediate();

        if (_damageable != null)
            _damageable.OnHealthChanged += OnHealthChanged;

        if (_infection != null)
            _infection.OnChanged += OnInfectionChanged;

        if (_paintMark != null)
            _paintMark.OnChanged += OnPaintMarkChanged;

        EnsureFillImageMode();
        EnsureInfectionImageMode();
        EnsurePaintMarkImageMode();
        RefreshImmediate();
        LogInfectionDebug("Bind", true);
        ForceHiddenUntilPositioned();
    }

    // Purpose: Removes the unbind binding.
    public void Unbind()
    {
        if (_damageable != null)
            _damageable.OnHealthChanged -= OnHealthChanged;

        if (_infection != null)
            _infection.OnChanged -= OnInfectionChanged;

        if (_paintMark != null)
            _paintMark.OnChanged -= OnPaintMarkChanged;

        _shooterStateIcon?.Unbind();
        _statusEffectIcons?.Bind(null);

        _anchor = null;
        _damageable = null;
        _infection = null;
        _paintMark = null;
        _statusEffects = null;
        _settings = null;

        _managerVisible = false;
        _hasValidScreenPosition = false;
    }

    // Purpose: Processes one update step for visual state.
    public void TickVisualState()
    {
        if (_damageable == null || _uicanvasgroupopacity == null)
            return;

        RefreshImmediate();
        LogInfectionDebug("Tick", false);

        if (!_managerVisible || !_hasValidScreenPosition)
        {
            _uicanvasgroupopacity.HideImmediate();
            return;
        }

        if (_settings != null && _settings.HideWhenDead && _damageable.IsDead)
        {
            _uicanvasgroupopacity.Hide();
            return;
        }

        // The state icon is a child of this widget. Keep the parent canvas
        // visible while investigate/combat feedback is being displayed.
        if (_shooterStateIcon != null && _shooterStateIcon.IsDisplaying)
        {
            _uicanvasgroupopacity.SetDefault();
            return;
        }

        if (Time.time <= _emphasisUntilTime)
        {
            _uicanvasgroupopacity.SetEmphasis();
            return;
        }

        if (_settings != null &&
            _settings.HideWhenFull &&
            Mathf.Approximately(_damageable.HealthNormalized, 1f) &&
            (_infection == null || Mathf.Approximately(_infection.CurrentInfection, 0f)))
        {
            _uicanvasgroupopacity.Hide();
            return;
        }

        _uicanvasgroupopacity.SetDefault();
    }

    // Purpose: Updates the screen position state.
    public void SetScreenPosition(Vector2 anchoredPosition)
    {
        Root.anchoredPosition = anchoredPosition;
        _hasValidScreenPosition = true;
    }

    // Purpose: Updates the manager visible state.
    public void SetManagerVisible(bool visible)
    {
        _managerVisible = visible;

        if (_uicanvasgroupopacity != null && !visible)
            _uicanvasgroupopacity.Hide();
    }

    // Purpose: Responds to the health changed callback.
    private void OnHealthChanged(Damageable damageable)
    {
        RefreshImmediate();
        LogInfectionDebug("HealthChanged", true);

        if (_settings != null)
            _emphasisUntilTime = Time.time + _settings.EmphasisDuration;
    }

    // Purpose: Responds to the paint mark changed callback.
    private void OnPaintMarkChanged(EntityPaintMarkController controller)
    {
        RefreshPaintMark();
    }

    // Purpose: Responds to the infection changed callback.
    private void OnInfectionChanged(ShooterInfection infection)
    {
        RefreshInfection();
        LogInfectionDebug("InfectionChanged", true);

        if (_settings != null)
            _emphasisUntilTime = Time.time + _settings.EmphasisDuration;
    }

    // Purpose: Refreshes the displayed or cached immediate.
    private void RefreshImmediate()
    {
        if (_damageable == null)
            return;

        float normalized = _damageable.HealthNormalized;

        if (_fillImage != null)
        {
            EnsureFillImageMode();
            _fillImage.fillAmount = normalized;
        }

        if (_fillRect != null && _fillImage == null)
        {
            Vector2 size = _fillRect.sizeDelta;
            size.x = _fillFullWidth * normalized;
            _fillRect.sizeDelta = size;
        }

        RefreshInfection();
        RefreshArmorClassText();
        RefreshPaintMark();
    }

    // Purpose: Refreshes the displayed or cached infection.
    private void RefreshInfection()
    {
        bool hasInfectionRuntime = _infection != null;

        if (_infectionRoot != null &&
            _infectionRoot.gameObject.activeSelf != hasInfectionRuntime)
        {
            _infectionRoot.gameObject.SetActive(hasInfectionRuntime);
        }

        if (_infectionFillImage == null)
            return;

        EnsureInfectionImageMode();

        float infectionAxisMaximum = hasInfectionRuntime && _damageable != null
            ? Mathf.Max(0f, _damageable.UninfectedMaxHealth)
            : 0f;
        float infection01 = infectionAxisMaximum > 0f
            ? Mathf.Clamp01(_infection.CurrentInfection / infectionAxisMaximum)
            : 0f;

        _infectionFillImage.fillAmount = infection01;
    }

    // Purpose: Performs the log infection debug workflow for this component.
    private void LogInfectionDebug(string reason, bool force)
    {
        if (!_debugInfectionLogs ||
            (!force && Time.unscaledTime < _nextInfectionDebugLogTime))
        {
            return;
        }

        _nextInfectionDebugLogTime = Time.unscaledTime + _debugInfectionLogInterval;

        float currentHealth = _damageable != null ? _damageable.CurrentHealth : -1f;
        float maxHealth = _damageable != null ? _damageable.MaxHealth : -1f;
        float currentInfection = _infection != null ? _infection.CurrentInfection : -1f;
        float infectionLimit = _infection != null ? _infection.InfectionLimit : -1f;
        float infectionAxisMaximum = _damageable != null ? _damageable.UninfectedMaxHealth : -1f;
        float expectedFill = infectionAxisMaximum > 0f
            ? Mathf.Clamp01(currentInfection / infectionAxisMaximum)
            : 0f;
        float appliedFill = _infectionFillImage != null
            ? _infectionFillImage.fillAmount
            : -1f;
        Color imageColor = _infectionFillImage != null
            ? _infectionFillImage.color
            : Color.clear;
        Rect imageRect = _infectionFillImage != null
            ? _infectionFillImage.rectTransform.rect
            : default;
        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();

        Debug.Log(
            $"[EnemyHealthBarWidget][InfectionDebug] reason={reason}, frame={Time.frameCount}, " +
            $"anchor={GetHierarchyPath(_anchor != null ? _anchor.transform : null)}, " +
            $"damageable={GetHierarchyPath(_damageable != null ? _damageable.transform : null)}, " +
            $"infectionRuntime={GetHierarchyPath(_infection != null ? _infection.transform : null)}, " +
            $"sameDamageable={(_infection != null && _infection.Damageable == _damageable)}, " +
            $"currentHealth={currentHealth:0.###}, maxHealth={maxHealth:0.###}, " +
            $"currentInfection={currentInfection:0.###}, infectionLimit={infectionLimit:0.###}, " +
            $"uiAxisMaximum={infectionAxisMaximum:0.###}, " +
            $"expectedFill={expectedFill:0.###}, appliedFill={appliedFill:0.###}, " +
            $"lethalByRuntimeValues={(_infection != null && _damageable != null && maxHealth <= 0f)}, " +
            $"damageableDead={(_damageable != null && _damageable.IsDead)}, " +
            $"infectionDead={(_infection != null && _infection.IsDead)}, " +
            $"rootActive={(_infectionRoot != null && _infectionRoot.gameObject.activeInHierarchy)}, " +
            $"imageActive={(_infectionFillImage != null && _infectionFillImage.gameObject.activeInHierarchy)}, " +
            $"imageEnabled={(_infectionFillImage != null && _infectionFillImage.enabled)}, " +
            $"imageType={(_infectionFillImage != null ? _infectionFillImage.type.ToString() : "null")}, " +
            $"imageSprite={(_infectionFillImage != null && _infectionFillImage.sprite != null ? _infectionFillImage.sprite.name : "null")}, " +
            $"imageColor=({imageColor.r:0.###},{imageColor.g:0.###},{imageColor.b:0.###},{imageColor.a:0.###}), " +
            $"rect=({imageRect.width:0.##},{imageRect.height:0.##}), " +
            $"canvasAlpha={(canvasGroup != null ? canvasGroup.alpha : -1f):0.###}, " +
            $"managerVisible={_managerVisible}, positioned={_hasValidScreenPosition}",
            this);
    }

    // Purpose: Returns the current hierarchy path.
    private static string GetHierarchyPath(Transform current)
    {
        if (current == null)
            return "null";

        string path = current.name;

        while (current.parent != null)
        {
            current = current.parent;
            path = current.name + "/" + path;
        }

        return path;
    }

    // Purpose: Refreshes the displayed or cached armor class text.
    private void RefreshArmorClassText()
    {
        if (_finalArmorClassText == null)
            return;

        float armorClass = _damageable != null
            ? _damageable.EffectiveArmorClass
            : 0f;

        _finalArmorClassText.text = string.Format(
            _finalArmorClassFormat,
            Mathf.Max(0f, armorClass).ToString("0.#"));
    }

    // Purpose: Refreshes the displayed or cached paint mark.
    private void RefreshPaintMark()
    {
        if (_paintMarkRoot == null)
            return;

        if (_paintMark == null || !_paintMark.IsMarkEnabled)
        {
            LogPaintMarkDebug("root hidden: missing controller or mark disabled");
            _paintMarkRoot.gameObject.SetActive(false);
            return;
        }

        _paintMarkRoot.gameObject.SetActive(true);
        EnsurePaintMarkImageMode();

        PaintMarkFaction faction = _paintMark.DisplayFaction;
        bool hasFaction = faction != PaintMarkFaction.None;
        bool isActive = _paintMark.IsActive;

        Color baseColor = faction switch
        {
            PaintMarkFaction.Vaccine => _vaccineMarkColor,
            PaintMarkFaction.Virus => _virusMarkColor,
            _ => _emptyMarkColor
        };

        if (_paintMarkBackgroundImage != null)
        {
            _paintMarkBackgroundImage.color =
                hasFaction ? baseColor : _emptyMarkColor;
            _paintMarkBackgroundImage.enabled = true;
        }

        if (_paintMarkFillImage != null)
        {
            _paintMarkFillImage.color =
                hasFaction ? baseColor : Color.clear;
            _paintMarkFillImage.fillAmount =
                hasFaction ? _paintMark.DisplayNormalized : 0f;
            _paintMarkFillImage.enabled = hasFaction;
        }

        if (_paintMarkOutlineImage != null)
        {
            _paintMarkOutlineImage.color =
                isActive ? _activeOutlineColor : Color.white;
            _paintMarkOutlineImage.enabled = hasFaction || isActive;
        }

        if (_paintMarkFactionIconImage != null)
        {
            Sprite icon = ResolvePaintMarkIcon(faction);

            _paintMarkFactionIconImage.sprite = icon;
            _paintMarkFactionIconImage.enabled = hasFaction && icon != null;
            _paintMarkFactionIconImage.color = Color.white;
        }

        if (_paintMarkStackText != null)
        {
            _paintMarkStackText.text = string.Format(
                _paintMarkStackFormat,
                _paintMark.DisplayStacks);
        }
    }

    // Purpose: Resolves the paint mark icon needed by this component.
    private Sprite ResolvePaintMarkIcon(PaintMarkFaction faction)
    {
        return faction switch
        {
            PaintMarkFaction.Vaccine => _vaccineMarkIcon,
            PaintMarkFaction.Virus => _virusMarkIcon,
            _ => null
        };
    }

    // Purpose: Resolves the paint mark controller needed by this component.
    private EntityPaintMarkController ResolvePaintMarkController(
        EnemyScreenSpaceHPUIAnchor anchor)
    {
        if (anchor == null)
            return null;

        EntityPaintMarkController direct =
            EntityComponentResolver.Resolve<EntityPaintMarkController>(anchor);

        if (direct != null)
            return direct;

        Damageable damageable = anchor.Damageable;

        return damageable != null
            ? EntityComponentResolver.Resolve<EntityPaintMarkController>(damageable)
            : null;
    }

    // Purpose: Resolves the status effects needed by this component.
    private EntityStatusEffectRuntime ResolveStatusEffects(
        EnemyScreenSpaceHPUIAnchor anchor)
    {
        if (anchor == null)
            return null;

        EntityStatusEffectRuntime direct =
            EntityComponentResolver.Resolve<EntityStatusEffectRuntime>(anchor);

        if (direct != null)
            return direct;

        ShooterInfection infection =
            EntityComponentResolver.Resolve<ShooterInfection>(anchor);

        return infection != null ? infection.StatusEffects : null;
    }

    // Purpose: Ensures status effect icons is ready before the workflow continues.
    private void EnsureStatusEffectIcons()
    {
        if (_statusEffectIcons != null)
            return;

        if (_statusEffectIconRoot != null)
        {
            _statusEffectIcons = _statusEffectIconRoot.GetComponent<StatusEffectIconStripUI>();

            if (_statusEffectIcons == null)
                _statusEffectIcons = _statusEffectIconRoot.gameObject.AddComponent<StatusEffectIconStripUI>();
        }

        if (_statusEffectIcons != null)
            return;

        _statusEffectIcons = StatusEffectIconStripUI.Create(
            Root,
            "StatusEffectIcons",
            new Vector2(0f, 0f),
            new Vector2(0f, -16f),
            18f);
        _statusEffectIconRoot = _statusEffectIcons != null
            ? _statusEffectIcons.transform as RectTransform
            : null;
    }

    // Purpose: Resolves the infection needed by this component.
    private ShooterInfection ResolveInfection(EnemyScreenSpaceHPUIAnchor anchor)
    {
        if (anchor == null)
            return null;

        ShooterInfection direct =
            EntityComponentResolver.Resolve<ShooterInfection>(anchor);

        if (direct != null)
            return direct;

        Damageable damageable = anchor.Damageable;

        return damageable != null
            ? EntityComponentResolver.Resolve<ShooterInfection>(damageable)
            : null;
    }

    // Purpose: Performs the log paint mark debug workflow for this component.
    private void LogPaintMarkDebug(string message)
    {
        if (!_debugPaintMarkLogs ||
            Time.unscaledTime < _nextPaintMarkDebugLogTime)
        {
            return;
        }

        _nextPaintMarkDebugLogTime =
            Time.unscaledTime + _debugPaintMarkLogInterval;

        string owner = _damageable != null ? _damageable.name : "none";
        string markState = _paintMark != null
            ? $"markEnabled={_paintMark.IsMarkEnabled}, " +
              $"activeFaction={_paintMark.ActiveFaction}, " +
              $"displayFaction={_paintMark.DisplayFaction}"
            : "mark=null";

        Debug.Log(
            $"[EnemyHealthBarWidget] PaintMark {message}. " +
            $"owner={owner}, {markState}",
            this);
    }

    // Purpose: Performs the force hidden until positioned workflow for this component.
    private void ForceHiddenUntilPositioned()
    {
        _uicanvasgroupopacity?.Hide();
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        Unbind();
    }

    // Purpose: Ensures fill image mode is ready before the workflow continues.
    private void EnsureFillImageMode()
    {
        if (_fillImage == null)
            return;

        if (_fillImage.type != Image.Type.Filled)
            _fillImage.type = Image.Type.Filled;

        if (_fillImage.fillMethod != Image.FillMethod.Horizontal)
            _fillImage.fillMethod = Image.FillMethod.Horizontal;

        if (_fillImage.fillOrigin != (int)Image.OriginHorizontal.Left)
            _fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
    }

    // Purpose: Ensures paint mark image mode is ready before the workflow continues.
    private void EnsurePaintMarkImageMode()
    {
        if (_paintMarkFillImage == null)
            return;

        if (_paintMarkFillImage.type != Image.Type.Filled)
            _paintMarkFillImage.type = Image.Type.Filled;

        if (_paintMarkFillImage.fillMethod != Image.FillMethod.Vertical)
            _paintMarkFillImage.fillMethod = Image.FillMethod.Vertical;

        if (_paintMarkFillImage.fillOrigin != (int)Image.OriginVertical.Bottom)
            _paintMarkFillImage.fillOrigin =
                (int)Image.OriginVertical.Bottom;
    }

    // Purpose: Ensures infection image mode is ready before the workflow continues.
    private void EnsureInfectionImageMode()
    {
        if (_infectionFillImage == null)
            return;

        if (_infectionFillImage.type != Image.Type.Filled)
            _infectionFillImage.type = Image.Type.Filled;

        if (_infectionFillImage.fillMethod != Image.FillMethod.Horizontal)
            _infectionFillImage.fillMethod = Image.FillMethod.Horizontal;

        if (_infectionFillImage.fillOrigin != (int)Image.OriginHorizontal.Left)
            _infectionFillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
    }
}
