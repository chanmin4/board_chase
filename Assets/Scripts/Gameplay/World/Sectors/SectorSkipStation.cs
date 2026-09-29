/*
 * Role hierarchy: Gameplay > World > Sectors
 * Primary responsibility: Lets an authored sector terminal mark the currently active sector as skipped/cleared.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

[DisallowMultipleComponent]
public sealed class SectorSkipStation : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private SectorRuntime _sector;
    [SerializeField] private StageProgressionManager _stageProgressionManager;

    [Header("Interaction")]
    [SerializeField] private string _promptLabel = "Skip Sector";
    [SerializeField] private bool _allowBossSectorSkip = true;

    public string PromptLabel => string.IsNullOrWhiteSpace(_promptLabel) ? "Skip Sector" : _promptLabel;

    public bool CanInteract
    {
        get
        {
            ResolveRefs();

            if (!isActiveAndEnabled || _sector == null || _sector.IsCleared || _stageProgressionManager == null)
                return false;

            if (!_allowBossSectorSkip &&
                TryGetRoomType(out StageRoomType roomType) &&
                roomType == StageRoomType.Boss)
            {
                return false;
            }

            return true;
        }
    }

    // Purpose: Initializes Inspector defaults for this component.
    private void Reset()
    {
        ResolveRefs();
    }

    // Purpose: Attempts to interact and reports whether the request succeeded.
    public bool TryInteract(Component actor)
    {
        if (actor == null || !CanInteract)
            return false;

        bool skipped = _stageProgressionManager.TrySkipActiveSector(_sector);
        if (!skipped)
        {
            Debug.LogWarning(
                $"[SectorSkipStation] Skip rejected. station={name}, sector={(_sector != null ? _sector.name : "None")}",
                this);
        }

        return skipped;
    }

    // Purpose: Resolves the refs needed by this component.
    private void ResolveRefs()
    {
        if (_sector == null)
            _sector = GetComponentInParent<SectorRuntime>();

        if (_stageProgressionManager == null)
            _stageProgressionManager = FindAnyObjectByType<StageProgressionManager>();
    }

    // Purpose: Attempts to get room type and reports whether the request succeeded.
    private bool TryGetRoomType(out StageRoomType roomType)
    {
        roomType = StageRoomType.Empty;
        SectorStateManager stateManager = FindAnyObjectByType<SectorStateManager>();

        return stateManager != null &&
               _sector != null &&
               stateManager.TryGetStageRoomType(_sector, out roomType);
    }
}
