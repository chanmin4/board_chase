using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(menuName = "Events/Combat/Sound Stimulus Event Channel")]
public class SoundStimulusEventChannelSO : DescriptionBaseSO
{
    public UnityAction<SoundStimulus> OnEventRaised;

    public void RaiseEvent(SoundStimulus stimulus)
    {
        if (!stimulus.IsValid)
            return;

        OnEventRaised?.Invoke(stimulus);
    }
}
