/*
 * Role hierarchy: Presentation > UI > Gameplay > PatternUI
 * Primary responsibility: Presents NamedPatternDurationBarUI state and handles its user-facing UI interaction.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NamedPatternDurationBarUI : MonoBehaviour
{

    [Header("Listening")]
    [SerializeField] private NamedPatternDurationEventChannelSO _durationEventChannel;

    [Header("Refs")]
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private Image _fillImage;
    [SerializeField] private TMP_Text _patternNameText;
    [SerializeField] private TMP_Text _objectiveCountText;

    [Header("Options")]
    [SerializeField] private float _visibleAlpha = 1f;
    [SerializeField] private float _hiddenAlpha = 0f;

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        SetVisible(false);
    }

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    private void OnEnable()
    {
        if (_durationEventChannel != null)
            _durationEventChannel.OnEventRaised += HandleDurationChanged;
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        if (_durationEventChannel != null)
            _durationEventChannel.OnEventRaised -= HandleDurationChanged;
    }

    // Purpose: Handles the incoming duration changed request or event.
    private void HandleDurationChanged(NamedPatternDurationSnapshot snapshot)
    {
        if (!snapshot.visible)
        {
            SetVisible(false);
            return;
        }

        SetVisible(true);

        if (_patternNameText != null)
            _patternNameText.text = snapshot.patternName;

        if (_fillImage != null)
            _fillImage.fillAmount = snapshot.remaining01;

        if (_objectiveCountText != null)
        {
            _objectiveCountText.gameObject.SetActive(snapshot.objectiveVisible);
            if (snapshot.objectiveVisible)
                _objectiveCountText.text = snapshot.objectiveRemaining.ToString();
        }


    }

    // Purpose: Updates the visible state.
    private void SetVisible(bool visible)
    {
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = visible ? _visibleAlpha : _hiddenAlpha;
            _canvasGroup.interactable = visible;
            _canvasGroup.blocksRaycasts = visible;
        }
        else
        {
            gameObject.SetActive(visible);
        }
    }
}
