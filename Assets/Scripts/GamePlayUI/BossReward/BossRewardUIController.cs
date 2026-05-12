using System.Collections;
using UnityEngine;

public class BossRewardUIController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private InputReader _inputReader;
    [SerializeField] private BossRewardCatalogSO _catalog;
    [SerializeField] private CanvasGroup _rootCanvasGroup;
    [SerializeField] private BossRewardChoiceButtonUI[] _choiceButtons;

    [Header("Listening")]
    [SerializeField] private BossRewardRequestEventChannelSO _rewardRequestChannel;

    [Header("Broadcasting")]
    [SerializeField] private BossRewardSelectedEventChannelSO _rewardSelectedChannel;

    [Header("Options")]
    [SerializeField, Min(0f)] private float _showDelaySeconds = 1f;

    private BossRewardRequest _currentRequest;
    private Coroutine _showRoutine;
    private bool _isOpen;

    private void Awake()
    {
        SetVisible(false);
    }

    private void OnEnable()
    {
        if (_rewardRequestChannel != null)
            _rewardRequestChannel.OnEventRaised += HandleRewardRequested;
    }

    private void OnDisable()
    {
        if (_rewardRequestChannel != null)
            _rewardRequestChannel.OnEventRaised -= HandleRewardRequested;

        if (_showRoutine != null)
            StopCoroutine(_showRoutine);

        UnlockInput();
    }

    private void HandleRewardRequested(BossRewardRequest request)
    {
        _currentRequest = request;

        if (_showRoutine != null)
            StopCoroutine(_showRoutine);

        _showRoutine = StartCoroutine(ShowAfterDelayRoutine());
    }

    private IEnumerator ShowAfterDelayRoutine()
    {
        LockInput();
        SetVisible(false);

        yield return new WaitForSecondsRealtime(_showDelaySeconds);

        BindChoices();
        SetVisible(true);
    }

    private void BindChoices()
    {
        BossRewardOptionSO[] choices = _catalog != null
            ? _catalog.PickChoices()
            : null;

        for (int i = 0; i < _choiceButtons.Length; i++)
        {
            BossRewardOptionSO reward = choices != null && i < choices.Length
                ? choices[i]
                : null;

            if (_choiceButtons[i] != null)
            {
                _choiceButtons[i].gameObject.SetActive(reward != null);
                _choiceButtons[i].Bind(reward, HandleRewardClicked);
            }
        }
    }

    private void HandleRewardClicked(BossRewardOptionSO reward)
    {
        if (!_isOpen || reward == null)
            return;

        SetVisible(false);
        UnlockInput();

        _rewardSelectedChannel?.RaiseEvent(new BossRewardSelection(
            _currentRequest.sourceSector,
            _currentRequest.namedEnemy,
            reward
        ));
    }

    private void SetVisible(bool visible)
    {
        _isOpen = visible;

        if (_rootCanvasGroup == null)
            return;

        _rootCanvasGroup.alpha = visible ? 1f : 0f;
        _rootCanvasGroup.interactable = visible;
        _rootCanvasGroup.blocksRaycasts = visible;
    }

    private void LockInput()
    {
        if (_inputReader != null)
            _inputReader.EnableMenuInput();
    }

    private void UnlockInput()
    {
        if (_inputReader != null)
            _inputReader.EnableGameplayInput();
    }
}
