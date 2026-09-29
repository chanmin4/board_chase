/*
 * Role hierarchy: Core > SceneFlow
 * Primary responsibility: Implements the focused GameplayScreenTransitionRunner responsibility and collaborates with its role group.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using System.Collections;
using PixeLadder.EasyTransition;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class GameplayScreenTransitionRunner : MonoBehaviour
{
    // Purpose: Plays the requested transition action or effect.
    public IEnumerator PlayTransition(
        ScreenTransitionRequestEventChannelSO transitionRequestChannel,
        InputReader inputReader,
        TransitionEffect effect,
        float coveredHoldSeconds,
        float transitionTimeoutSeconds,
        Action onCovered,
        Action onComplete)
    {
        bool coveredInvoked = false;
        bool completed = false;

        Action invokeCovered = () =>
        {
            if (coveredInvoked)
                return;

            coveredInvoked = true;
            onCovered?.Invoke();
        };

        Action invokeComplete = () =>
        {
            if (completed)
                return;

            completed = true;
            inputReader?.EnableGameplayInput();
            onComplete?.Invoke();
        };

        if (transitionRequestChannel == null)
        {
            Debug.LogError(
                "[GameplayScreenTransitionRunner] Transition Request Channel is missing. " +
                "The covered and complete callbacks will run immediately so the caller does not remain locked.",
                this);

            invokeCovered();
            invokeComplete();
            yield break;
        }

        if (inputReader == null)
        {
            Debug.LogError(
                "[GameplayScreenTransitionRunner] InputReader is missing. " +
                "The transition will continue, but gameplay input cannot be locked.",
                this);
        }
        else
        {
            inputReader.DisableAllInput();
        }

        ScreenTransitionRequest request = new ScreenTransitionRequest(
            effect,
            invokeCovered,
            invokeComplete,
            coveredHoldSeconds);

        if (!transitionRequestChannel.RaiseEvent(request))
        {
            Debug.LogError(
                "[GameplayScreenTransitionRunner] No TransitionInterfaceController is listening to the transition channel. " +
                "The covered and complete callbacks will run immediately so the caller does not remain locked.",
                this);

            invokeCovered();
            invokeComplete();
            yield break;
        }

        float timeoutAt =
            Time.unscaledTime + Mathf.Max(0.5f, transitionTimeoutSeconds);

        while (!completed)
        {
            if (Time.unscaledTime >= timeoutAt)
            {
                Debug.LogError(
                    "[GameplayScreenTransitionRunner] Screen transition timed out. " +
                    "Forcing the remaining callbacks and restoring gameplay input.",
                    this);

                invokeCovered();
                invokeComplete();
                yield break;
            }

            yield return null;
        }
    }
}
