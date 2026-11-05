using UnityEngine;

/// <summary>Authored room device. Replace only Visual; the root trigger and component own interaction.</summary>
[DisallowMultipleComponent]
public sealed class SectorPortalActivator : MonoBehaviour
{
    [SerializeField] private Transform _visual;
    private SectorRuntime _sector;
    private SectorRuntime Sector => _sector != null ? _sector : (_sector = GetComponentInParent<SectorRuntime>());
    private RaidRuntime Raid => RaidRuntime.Active;

    public bool CanInteract => isActiveAndEnabled && Sector != null && Raid != null &&
        (SectorTravelRulesSO.Active?.travel.requirePortalActivation ?? true) &&
        Raid.IsInSector(Sector) && !Raid.HasActivatedPortals(Sector);
    public string PromptLabel => "포탈 개방";

    public bool TryInteract(Transform actor)
    {
        if (!CanInteract || actor == null) return false;
        var interactions = EntityComponentResolver.Resolve<InteractionManager>(actor);
        var damageable = EntityComponentResolver.Resolve<Damageable>(actor);
        if (interactions == null || !interactions.CanInteractWithTarget(gameObject) ||
            (damageable != null && damageable.IsDead)) return false;
        return Raid.TryActivatePortals(Sector);
    }

    private void LateUpdate()
    {
        // The base prefab is also inherited by HOME. Never expose a raid device there.
        if (_visual == null) return;
        bool visible = Sector != null && Raid != null && Raid.IsInSector(Sector) &&
            (SectorTravelRulesSO.Active?.travel.requirePortalActivation ?? true);
        if (_visual.gameObject.activeSelf != visible) _visual.gameObject.SetActive(visible);
    }
}
