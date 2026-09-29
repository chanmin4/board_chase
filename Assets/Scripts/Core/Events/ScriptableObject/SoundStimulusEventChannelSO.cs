/*
 * Role hierarchy: Core > Events > ScriptableObject
 * Primary responsibility: Defines the SoundStimulusEventChannelSO publish-subscribe contract to reduce direct system coupling.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(menuName = "Events/Combat/Sound Stimulus Event Channel")]
public class SoundStimulusEventChannelSO : DescriptionBaseSO
{
    public UnityAction<SoundStimulus> OnEventRaised;

    // Purpose: Publishes the event notification.
    public void RaiseEvent(SoundStimulus stimulus)
    {
        if (!stimulus.IsValid)
            return;

        OnEventRaised?.Invoke(stimulus);
    }
}
