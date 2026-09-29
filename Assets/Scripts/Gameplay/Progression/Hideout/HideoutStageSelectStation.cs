/*
 * Role hierarchy: Gameplay > Progression > Hideout
 * Primary responsibility: Receives Hideout station interactions and raises a stage-selection request without placing portals.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class HideoutStageSelectStation : MonoBehaviour
{
    [Header("Stage Select")]
    [SerializeField] private bool _interactable = true;
    [SerializeField] private string _promptLabel = "Stage Select";
    [SerializeField] private HideoutStageSelectOpenRequestEventChannelSO _openRequestChannel;

    public static event Action<HideoutStageSelectOpenRequest> Requested;

    public bool CanInteract => isActiveAndEnabled && _interactable;

    public string PromptLabel => string.IsNullOrWhiteSpace(_promptLabel)
        ? "Stage Select"
        : _promptLabel;

    // Purpose: Attempts to interact and reports whether the request succeeded.
    public bool TryInteract(Component actor)
    {
        if (!CanInteract)
            return false;

        // The station may be placed directly in a user-authored HomeSector prefab,
        // so guarantee that its runtime UI endpoint exists before the request fires.
        HideoutStageSelectPanel.EnsureInstance();

        HideoutStageSelectOpenRequest request =
            new HideoutStageSelectOpenRequest(this, actor);

        _openRequestChannel?.RaiseEvent(request);
        Requested?.Invoke(request);

        Debug.Log(
            $"[HideoutStageSelectStation] Stage select requested. " +
            $"station={name}, actor={(actor != null ? actor.name : "null")}",
            this);
        return true;
    }
}
