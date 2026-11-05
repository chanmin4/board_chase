using UnityEngine;

/// <summary>Four physical interaction points delegate to the same sector lock transaction.</summary>
[DisallowMultipleComponent]
public sealed class SectorLockPanel : MonoBehaviour
{
    [SerializeField] private SectorLockedCombatArea _area;
    public SectorLockedCombatArea Area => _area;
    public string PromptLabel => "잠금 전투 구역 해제 · 열쇠 제출";
    public bool CanInteract => isActiveAndEnabled && _area != null && _area.IsBound && _area.IsLocked &&
        RaidRuntime.Active != null && RaidRuntime.Active.IsInSector(_area.Sector);
    public bool HasPendingKeyConfirmation => ShopPurchaseTradePanel.IsKeySubmissionOwnedBy(this);
    public void Configure(SectorLockedCombatArea area) => _area = area;
    public bool TryInteract(Component actor) => CanInteract && _area.TryOpenPanel(this, actor);
    public void DisableInteraction()
    {
        foreach (Collider trigger in GetComponentsInChildren<Collider>(true))
            if (trigger.isTrigger) trigger.enabled = false;
    }
}
