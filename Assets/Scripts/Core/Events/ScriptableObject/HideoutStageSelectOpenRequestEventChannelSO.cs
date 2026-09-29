/*
 * Role hierarchy: Core > Events > Hideout
 * Primary responsibility: Broadcasts a request to open the Hideout stage-selection interface.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using UnityEngine;

public readonly struct HideoutStageSelectOpenRequest
{
    public HideoutStageSelectStation Station { get; }
    public Component Actor { get; }

    public HideoutStageSelectOpenRequest(
        HideoutStageSelectStation station,
        Component actor)
    {
        Station = station;
        Actor = actor;
    }
}

[CreateAssetMenu(
    fileName = "HideoutStageSelectOpenRequestEventChannel",
    menuName = "Events/Hideout/Stage Select Open Request Event Channel")]
public sealed class HideoutStageSelectOpenRequestEventChannelSO : ScriptableObject
{
    public event Action<HideoutStageSelectOpenRequest> OnEventRaised;

    // Purpose: Publishes the event notification.
    public void RaiseEvent(HideoutStageSelectOpenRequest request)
    {
        OnEventRaised?.Invoke(request);
    }
}
