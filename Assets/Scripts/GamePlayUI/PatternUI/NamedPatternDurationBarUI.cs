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

    private void Awake()
    {
        SetVisible(false);
    }

    private void OnEnable()
    {
        if (_durationEventChannel != null)
            _durationEventChannel.OnEventRaised += HandleDurationChanged;
    }

    private void OnDisable()
    {
        if (_durationEventChannel != null)
            _durationEventChannel.OnEventRaised -= HandleDurationChanged;
    }

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
