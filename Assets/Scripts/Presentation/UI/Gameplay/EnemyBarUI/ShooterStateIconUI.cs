/*
 * Role hierarchy: Presentation > UI > Gameplay > EnemyBarUI
 * Primary responsibility: Presents ShooterStateIconUI state and handles its user-facing UI interaction.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using UnityEngine.UI;
using TMPro;

[DisallowMultipleComponent]
public class ShooterStateIconUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private EnemyShooterCombatController _combatController;
    [Tooltip("Optional semi-transparent bubble background. Leave it empty only for legacy icon-only widgets.")]
    [SerializeField] private Image _bubbleBackground;
    [SerializeField] private Image _iconImage;
    [Tooltip("Optional text fallback. Useful when no investigate/combat sprite has been authored yet.")]
    [SerializeField] private TextMeshProUGUI _statusText;
    [SerializeField] private CanvasGroup _group;

    [Header("Sprites")]
    [SerializeField] private Sprite _investigateSprite;
    [SerializeField] private Sprite _combatSprite;

    [Header("Text Fallback")]
    [SerializeField] private string _investigateText = "?";
    [SerializeField] private string _combatText = "!";
    [SerializeField] private Color _investigateColor = new Color(1f, 0.85f, 0.15f, 1f);
    [SerializeField] private Color _combatColor = new Color(1f, 0.2f, 0.15f, 1f);
    [SerializeField] private Color _bubbleBackgroundColor = new Color(0.02f, 0.08f, 0.1f, 0.52f);

    [Header("Display")]
    [SerializeField, Min(0.01f)] private float _investigateVisibleSeconds = 1.5f;
    [SerializeField, Min(0.01f)] private float _combatVisibleSeconds = 1.5f;
    [SerializeField] private bool _keepCombatVisibleWhileTargeted;
    [SerializeField, Min(0f)] private float _fadeOutSpeed = 12f;

    private EnemyShooterGoal _lastGoal;
    private bool _wasDirectSight;
    private float _hideTime;

    public bool IsDisplaying => _group != null && _group.alpha > 0.001f;

    // Purpose: Binds bind to the active runtime or UI target.
    public void Bind(EnemyScreenSpaceHPUIAnchor anchor)
    {
        _combatController = anchor != null
            ? EntityComponentResolver.Resolve<EnemyShooterCombatController>(anchor)
            : null;

        _lastGoal = _combatController != null
            ? _combatController.CurrentGoal
            : EnemyShooterGoal.Patrol;
        _wasDirectSight = _combatController != null && _combatController.CanSeeTarget;

        HideImmediate();
        RefreshForGoal(_lastGoal, true);
    }

    // Purpose: Removes the unbind binding.
    public void Unbind()
    {
        _combatController = null;
        _lastGoal = EnemyShooterGoal.Patrol;
        _wasDirectSight = false;
        HideImmediate();
    }

    // Purpose: Initializes Inspector defaults for this component.
    private void Reset()
    {
        _combatController = GetComponentInParent<EnemyShooterCombatController>();
        _bubbleBackground = GetComponent<Image>();
        _iconImage = GetComponentInChildren<Image>(true);
        _statusText = GetComponentInChildren<TextMeshProUGUI>(true);
        _group = GetComponent<CanvasGroup>();
    }

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        ResolveRefs();
        HideImmediate();
    }

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    private void OnEnable()
    {
        ResolveRefs();
        _lastGoal = _combatController != null
            ? _combatController.CurrentGoal
            : EnemyShooterGoal.Patrol;
        _wasDirectSight = _combatController != null && _combatController.CanSeeTarget;

        RefreshForGoal(_lastGoal, true);
    }

    // Purpose: Processes this component's per-frame runtime work.
    private void Update()
    {
        ResolveRefs();

        if (_combatController == null)
        {
            FadeOrHide();
            return;
        }

        EnemyShooterGoal currentGoal = _combatController.CurrentGoal;
        bool hasDirectSight = _combatController.CanSeeTarget;

        // The combat goal can update a frame after vision is acquired. Show the
        // alert immediately on that edge so the bubble reads as a reaction,
        // rather than as a delayed state-machine label.
        if (hasDirectSight && !_wasDirectSight)
        {
            Show(
                _combatSprite,
                _combatText,
                _combatColor,
                _combatVisibleSeconds);
        }

        _wasDirectSight = hasDirectSight;

        if (currentGoal != _lastGoal)
        {
            _lastGoal = currentGoal;
            RefreshForGoal(currentGoal, false);
        }

        if (_keepCombatVisibleWhileTargeted &&
            (currentGoal == EnemyShooterGoal.ChaseTarget ||
             currentGoal == EnemyShooterGoal.AttackTarget))
        {
            _hideTime = Time.unscaledTime + _combatVisibleSeconds;
        }

        FadeOrHide();
    }

    // Purpose: Resolves the refs needed by this component.
    private void ResolveRefs()
    {
        if (_combatController == null)
        {
            _combatController =
                GetComponentInParent<EnemyShooterCombatController>() ??
                GetComponentInChildren<EnemyShooterCombatController>(true);
        }

        if (_bubbleBackground == null)
            _bubbleBackground = GetComponent<Image>();

        if (_iconImage == null)
            _iconImage = GetComponentInChildren<Image>(true);

        if (_iconImage == _bubbleBackground)
            _iconImage = null;

        if (_statusText == null)
            _statusText = GetComponentInChildren<TextMeshProUGUI>(true);

        if (_group == null)
            _group = GetComponent<CanvasGroup>();

        if (_group == null)
            _group = gameObject.AddComponent<CanvasGroup>();
    }

    // Purpose: Refreshes the displayed or cached for goal.
    private void RefreshForGoal(EnemyShooterGoal goal, bool allowHidden)
    {
        switch (goal)
        {
            case EnemyShooterGoal.Investigate:
                Show(
                    _investigateSprite,
                    _investigateText,
                    _investigateColor,
                    _investigateVisibleSeconds);
                break;

            case EnemyShooterGoal.ChaseTarget:
            case EnemyShooterGoal.AttackTarget:
                Show(
                    _combatSprite,
                    _combatText,
                    _combatColor,
                    _combatVisibleSeconds);
                break;

            default:
                if (!allowHidden)
                    _hideTime = Mathf.Min(_hideTime, Time.unscaledTime);
                break;
        }
    }

    // Purpose: Shows the show UI or visual state.
    private void Show(
        Sprite sprite,
        string fallbackText,
        Color color,
        float visibleSeconds)
    {
        bool showSprite = sprite != null;
        bool showText = !showSprite &&
                        _statusText != null &&
                        !string.IsNullOrWhiteSpace(fallbackText);

        if (_iconImage != null)
        {
            _iconImage.sprite = sprite;
            _iconImage.color = color;
            _iconImage.enabled = showSprite;
        }

        if (_bubbleBackground != null)
        {
            _bubbleBackground.color = _bubbleBackgroundColor;
            _bubbleBackground.enabled = showSprite || showText;
        }

        if (_statusText != null)
        {
            _statusText.text = showText ? fallbackText : string.Empty;
            _statusText.color = color;
            _statusText.enabled = showText;
        }

        if (_group != null)
        {
            _group.alpha = showSprite || showText ? 1f : 0f;
            _group.interactable = false;
            _group.blocksRaycasts = false;
        }

        _hideTime = Time.unscaledTime + Mathf.Max(0.01f, visibleSeconds);
    }

    // Purpose: Performs the fade or hide workflow for this component.
    private void FadeOrHide()
    {
        if (_group == null || Time.unscaledTime < _hideTime)
            return;

        if (_fadeOutSpeed <= 0f)
        {
            HideImmediate();
            return;
        }

        _group.alpha = Mathf.MoveTowards(
            _group.alpha,
            0f,
            _fadeOutSpeed * Time.unscaledDeltaTime);

        if (_group.alpha > 0f)
            return;

        if (_iconImage != null)
            _iconImage.enabled = false;

        if (_bubbleBackground != null)
            _bubbleBackground.enabled = false;

        if (_statusText != null)
            _statusText.enabled = false;
    }

    // Purpose: Hides the immediate UI or visual state.
    private void HideImmediate()
    {
        if (_group != null)
        {
            _group.alpha = 0f;
            _group.interactable = false;
            _group.blocksRaycasts = false;
        }

        if (_iconImage != null)
            _iconImage.enabled = false;

        if (_bubbleBackground != null)
            _bubbleBackground.enabled = false;

        if (_statusText != null)
        {
            _statusText.text = string.Empty;
            _statusText.enabled = false;
        }
    }
}
