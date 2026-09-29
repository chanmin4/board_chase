/*
 * Role hierarchy: Gameplay > World > Sectors > ScriptableObject
 * Primary responsibility: Defines encounter, portal, and timer behaviour for a Normal Sector without changing its room identity.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

public enum NormalSectorEncounterPolicy
{
    Required = 0,
    Optional = 1,
    None = 2
}

public enum NormalSectorPortalPolicy
{
    ClearEncounter = 0,
    FreePass = 1
}

public enum NormalSectorTimerPolicy
{
    UseStageDefault = 0,
    Disabled = 1
}

[CreateAssetMenu(
    fileName = "NormalSectorPolicy",
    menuName = "Game/Sector/Normal Sector Policy")]
public sealed class NormalSectorPolicySO : ScriptableObject
{
    [Header("Encounter")]
    [Tooltip("Required rooms use the selected Stage encounter as their completion requirement. Optional rooms spawn the encounter but may be left through a Free Pass portal. None rooms do not configure or spawn a Stage encounter.")]
    [SerializeField] private NormalSectorEncounterPolicy _encounterPolicy =
        NormalSectorEncounterPolicy.Required;

    [Header("Progression")]
    [Tooltip("Clear Encounter keeps the current gate behaviour. Free Pass allows leaving before the room is cleared. Clear Encounter is automatically treated as Free Pass when Encounter is None so a room cannot soft-lock itself.")]
    [SerializeField] private NormalSectorPortalPolicy _portalPolicy =
        NormalSectorPortalPolicy.ClearEncounter;

    [Header("Timer")]
    [Tooltip("Use Stage Default keeps the StageDefinition normal-sector timer. Timers only run for Required + Clear Encounter rooms.")]
    [SerializeField] private NormalSectorTimerPolicy _timerPolicy =
        NormalSectorTimerPolicy.UseStageDefault;

    public NormalSectorEncounterPolicy EncounterPolicy => _encounterPolicy;
    public NormalSectorPortalPolicy PortalPolicy => _portalPolicy;
    public NormalSectorTimerPolicy TimerPolicy => _timerPolicy;

    public bool UsesEnemyEncounter => _encounterPolicy != NormalSectorEncounterPolicy.None;
    public bool RequiresEnemyClearForProgression =>
        _encounterPolicy == NormalSectorEncounterPolicy.Required &&
        _portalPolicy == NormalSectorPortalPolicy.ClearEncounter;
    public bool AllowsPortalTravelBeforeClear => !RequiresEnemyClearForProgression;
    public bool UsesStageDefaultTimer =>
        _timerPolicy == NormalSectorTimerPolicy.UseStageDefault &&
        RequiresEnemyClearForProgression;

    /// <summary>
    /// Existing sector presets have no policy asset. Keep their historical
    /// behaviour until an author deliberately assigns one.
    /// </summary>
    public static bool ResolveUsesEnemyEncounter(NormalSectorPolicySO policy)
    {
        return policy == null || policy.UsesEnemyEncounter;
    }

    // Purpose: Resolves the requires enemy clear for progression needed by this component.
    public static bool ResolveRequiresEnemyClearForProgression(NormalSectorPolicySO policy)
    {
        return policy == null || policy.RequiresEnemyClearForProgression;
    }

    // Purpose: Resolves the allows portal travel before clear needed by this component.
    public static bool ResolveAllowsPortalTravelBeforeClear(NormalSectorPolicySO policy)
    {
        return policy != null && policy.AllowsPortalTravelBeforeClear;
    }

    // Purpose: Resolves the uses stage default timer needed by this component.
    public static bool ResolveUsesStageDefaultTimer(NormalSectorPolicySO policy)
    {
        return policy == null || policy.UsesStageDefaultTimer;
    }
}
