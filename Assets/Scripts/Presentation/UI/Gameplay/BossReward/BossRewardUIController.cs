/*
 * Role hierarchy: Presentation > UI > Gameplay > BossReward
 * Primary responsibility: Coordinates the BossRewardUIController subsystem lifecycle and collaborating objects.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

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

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        SetVisible(false);
    }

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    private void OnEnable()
    {
        if (_rewardRequestChannel != null)
            _rewardRequestChannel.OnEventRaised += HandleRewardRequested;
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        if (_rewardRequestChannel != null)
            _rewardRequestChannel.OnEventRaised -= HandleRewardRequested;

        if (_showRoutine != null)
            StopCoroutine(_showRoutine);

        UnlockInput();
    }

    // Purpose: Handles the incoming reward requested request or event.
    private void HandleRewardRequested(BossRewardRequest request)
    {
        _currentRequest = request;

        if (_showRoutine != null)
            StopCoroutine(_showRoutine);

        _showRoutine = StartCoroutine(ShowAfterDelayRoutine());
    }

    // Purpose: Shows the after delay routine UI or visual state.
    private IEnumerator ShowAfterDelayRoutine()
    {
        LockInput();
        SetVisible(false);

        yield return new WaitForSecondsRealtime(_showDelaySeconds);

        BindChoices();
        SetVisible(true);
    }

    // Purpose: Binds choices to the active runtime or UI target.
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

    // Purpose: Handles the incoming reward clicked request or event.
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

    // Purpose: Updates the visible state.
    private void SetVisible(bool visible)
    {
        _isOpen = visible;

        if (_rootCanvasGroup == null)
            return;

        _rootCanvasGroup.alpha = visible ? 1f : 0f;
        _rootCanvasGroup.interactable = visible;
        _rootCanvasGroup.blocksRaycasts = visible;
    }

    // Purpose: Performs the lock input workflow for this component.
    private void LockInput()
    {
        if (_inputReader != null)
            _inputReader.EnableMenuInput();
    }

    // Purpose: Performs the unlock input workflow for this component.
    private void UnlockInput()
    {
        if (_inputReader != null)
            _inputReader.EnableGameplayInput();
    }
}
