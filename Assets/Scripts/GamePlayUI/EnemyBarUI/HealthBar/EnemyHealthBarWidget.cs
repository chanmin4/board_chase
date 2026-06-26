using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EnemyHealthBarWidget : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RectTransform _root;
    [SerializeField] private RectTransform _fillRect;
    [SerializeField] private Image _fillImage;
    [SerializeField] private UICanvasGroupOpacity _uicanvasgroupopacity;

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
    [SerializeField] private Color _vaccineMarkColor = new Color(0.2f, 0.85f, 1f, 1f);
    [SerializeField] private Color _virusMarkColor = new Color(0.65f, 0.2f, 1f, 1f);
    [SerializeField] private Color _emptyMarkColor = new Color(1f, 1f, 1f, 0.18f);
    [SerializeField] private Color _activeOutlineColor = Color.white;

    [Header("Paint Mark Format")]
    [SerializeField] private string _paintMarkStackFormat = "{0}";

    [Header("Multiplier")]
    [SerializeField] private TextMeshProUGUI _multiplierText;
    [SerializeField] private string _multiplierFormat = "x{0:0.00}";
    [SerializeField] private Color _multiplierNormalColor = Color.white;
    [SerializeField] private Color _multiplierEmphasisColor = new Color(1f, 0.92f, 0.35f, 1f);

    private EnemyScreenSpaceHPUIAnchor _anchor;
    private Damageable _damageable;
    private EntityPaintMarkController _paintMark;
    private EnemyHealthBarSettingsSO _settings;
    private float _emphasisUntilTime;
    private float _fillFullWidth;
    private bool _managerVisible = false;
    private bool _hasValidScreenPosition;

    public RectTransform Root => _root != null ? _root : (RectTransform)transform;

    private void Awake()
    {
        if (_root == null)
            _root = (RectTransform)transform;

       if (_fillRect != null && _fillImage == null)
        {
            _fillFullWidth = _fillRect.sizeDelta.x;
            _fillRect.pivot = new Vector2(0f, 0.5f);
            _fillRect.anchorMin = new Vector2(0f, 0.5f);
            _fillRect.anchorMax = new Vector2(0f, 0.5f);
        }

        EnsureFillImageMode();
        EnsurePaintMarkImageMode();
        ForceHiddenUntilPositioned();
        _uicanvasgroupopacity?.HideImmediate();
    }

    public void Bind(EnemyScreenSpaceHPUIAnchor anchor)
    {
        Unbind();

        _anchor = anchor;
        _damageable = anchor != null ? anchor.Damageable : null;
        _paintMark = ResolvePaintMarkController(anchor);
        _settings = anchor != null ? anchor.HealthBarSettings : null;
        _managerVisible = false;
        _hasValidScreenPosition = false;
        _uicanvasgroupopacity?.HideImmediate();

        if (_damageable != null)
        {
            _damageable.OnHealthChanged += OnHealthChanged;
            _damageable.OnDamageMultiplierChanged += OnDamageMultiplierChanged;
        }

        if (_paintMark != null)
            _paintMark.OnChanged += OnPaintMarkChanged;

        EnsureFillImageMode();
        EnsurePaintMarkImageMode();
        RefreshImmediate();
        ForceHiddenUntilPositioned();
    }

    public void Unbind()
    {
        if (_damageable != null)
        {
            _damageable.OnHealthChanged -= OnHealthChanged;
            _damageable.OnDamageMultiplierChanged -= OnDamageMultiplierChanged;
        }

        if (_paintMark != null)
            _paintMark.OnChanged -= OnPaintMarkChanged;

        _anchor = null;
        _damageable = null;
        _paintMark = null;
        _settings = null;
        _managerVisible = false;
        _hasValidScreenPosition = false;
    }

    public void TickVisualState()
    {
        if (_damageable == null || _uicanvasgroupopacity == null)
            return;

        RefreshImmediate();

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

        if (Time.time <= _emphasisUntilTime)
        {
            _uicanvasgroupopacity.SetEmphasis();
            return;
        }

        if (_settings != null &&
            _settings.HideWhenFull &&
            Mathf.Approximately(_damageable.HealthNormalized, 1f))
        {
            _uicanvasgroupopacity.Hide();
            return;
        }

        _uicanvasgroupopacity.SetDefault();
    }

    public void SetScreenPosition(Vector2 anchoredPosition)
    {
        Root.anchoredPosition = anchoredPosition;
        _hasValidScreenPosition = true;
    }

    public void SetManagerVisible(bool visible)
    {
        _managerVisible = visible;

        if (_uicanvasgroupopacity == null)
            return;

        if (!visible)
            _uicanvasgroupopacity.Hide();
    }

    private void OnHealthChanged(Damageable damageable)
    {
        RefreshImmediate();

        if (_settings != null)
            _emphasisUntilTime = Time.time + _settings.EmphasisDuration;
    }

    private void OnDamageMultiplierChanged(Damageable damageable)
    {
        RefreshImmediate();
    }

    private void OnPaintMarkChanged(EntityPaintMarkController controller)
    {
        RefreshPaintMark();
    }

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

        RefreshMultiplierText();
        RefreshPaintMark();
    }

    private void RefreshMultiplierText()
    {
        if (_multiplierText == null || _damageable == null)
            return;

        float multiplier = _damageable.FinalDamageTakenMultiplier;
        _multiplierText.text = string.Format(_multiplierFormat, multiplier);
        _multiplierText.color = Mathf.Approximately(multiplier, 1f)
            ? _multiplierNormalColor
            : _multiplierEmphasisColor;
    }

    private void RefreshPaintMark()
    {
        if (_paintMarkRoot == null)
            return;

        if (_paintMark == null || !_paintMark.IsMarkEnabled)
        {
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
            _paintMarkBackgroundImage.color = hasFaction ? baseColor : _emptyMarkColor;
            _paintMarkBackgroundImage.enabled = true;
        }

        if (_paintMarkFillImage != null)
        {
            _paintMarkFillImage.color = hasFaction ? baseColor : Color.clear;
            _paintMarkFillImage.fillAmount = hasFaction ? _paintMark.DisplayNormalized : 0f;
            _paintMarkFillImage.enabled = hasFaction;
        }

        if (_paintMarkOutlineImage != null)
        {
            _paintMarkOutlineImage.color = isActive ? _activeOutlineColor : Color.white;
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
            _paintMarkStackText.text = string.Format(_paintMarkStackFormat, _paintMark.DisplayStacks);
    }

    private Sprite ResolvePaintMarkIcon(PaintMarkFaction faction)
    {
        return faction switch
        {
            PaintMarkFaction.Vaccine => _vaccineMarkIcon,
            PaintMarkFaction.Virus => _virusMarkIcon,
            _ => null
        };
    }

    private EntityPaintMarkController ResolvePaintMarkController(EnemyScreenSpaceHPUIAnchor anchor)
    {
        if (anchor == null)
            return null;

        if (anchor.TryGetComponent(out EntityPaintMarkController direct))
            return direct;

        EntityPaintMarkController parent =
            anchor.GetComponentInParent<EntityPaintMarkController>();

        if (parent != null)
            return parent;

        Damageable damageable = anchor.Damageable;

        if (damageable == null)
            return null;

        if (damageable.TryGetComponent(out EntityPaintMarkController sameObject))
            return sameObject;

        parent = damageable.GetComponentInParent<EntityPaintMarkController>();

        if (parent != null)
            return parent;

        return damageable.GetComponentInChildren<EntityPaintMarkController>(true);
    }

    private void ForceHiddenUntilPositioned()
    {
        if (_uicanvasgroupopacity != null)
            _uicanvasgroupopacity.Hide();
    }

    private void OnDisable()
    {
        Unbind();
    }

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

    private void EnsurePaintMarkImageMode()
    {
        if (_paintMarkFillImage == null)
            return;

        if (_paintMarkFillImage.type != Image.Type.Filled)
            _paintMarkFillImage.type = Image.Type.Filled;

        if (_paintMarkFillImage.fillMethod != Image.FillMethod.Vertical)
            _paintMarkFillImage.fillMethod = Image.FillMethod.Vertical;

        if (_paintMarkFillImage.fillOrigin != (int)Image.OriginVertical.Bottom)
            _paintMarkFillImage.fillOrigin = (int)Image.OriginVertical.Bottom;
    }
}
