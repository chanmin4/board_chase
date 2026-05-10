using System;
using UnityEngine;
using PixeLadder.EasyTransition;

public sealed class ScreenTransitionRequest
{
    public readonly TransitionEffect Effect;
    public readonly Action OnCovered;
    public readonly Action OnComplete;
    public readonly float CoveredHoldSeconds;

    public ScreenTransitionRequest(
        TransitionEffect effect,
        Action onCovered,
        Action onComplete,
        float coveredHoldSeconds = 0f)
    {
        Effect = effect;
        OnCovered = onCovered;
        OnComplete = onComplete;
        CoveredHoldSeconds = Mathf.Max(0f, coveredHoldSeconds);
    }
}

[CreateAssetMenu(
    fileName = "ScreenTransitionRequestEventChannel",
    menuName = "Events/Screen Transition Request EventChannel")]
public class ScreenTransitionRequestEventChannelSO : ScriptableObject
{
    public event Action<ScreenTransitionRequest> OnEventRaised;

    public bool RaiseEvent(ScreenTransitionRequest request)
    {
        if (OnEventRaised == null)
            return false;

        OnEventRaised.Invoke(request);
        return true;
    }
}
