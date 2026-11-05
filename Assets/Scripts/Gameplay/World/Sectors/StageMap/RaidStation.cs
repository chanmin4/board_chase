using UnityEngine;

public enum RaidStationKind { NormalExtraction, StartReturn, Hatch, Generator, SpecialExtractionWip, GeneratorShortcut }

/// <summary>Interior interaction point; never replaces one of the four travel portals.</summary>
[DisallowMultipleComponent]
public sealed class RaidStation : MonoBehaviour
{
    [SerializeField] private RaidStationKind _kind;
    [SerializeField] private SectorRuntime _sector;
    private RaidRuntime _raid;
    private SectorHatch _hatch;
    public event System.Action PromptChanged;
    private TMPro.TMP_Text _prototypeLabel;
    public void SetPrototypeLabel(TMPro.TMP_Text label) => _prototypeLabel = label;
    private void Update()
    {
        if (_prototypeLabel != null && _raid != null)
        {
            _prototypeLabel.text = PromptLabel;
            if (_kind == RaidStationKind.GeneratorShortcut) _prototypeLabel.gameObject.SetActive(_raid.State.generatorPowered);
        }
    }
    public RaidStationKind Kind => _kind;
    public SectorRuntime Sector => _sector;
    public RaidRuntime Runtime => _raid;
    public bool CanInteract => isActiveAndEnabled && _raid != null && _raid.IsInSector(_sector) &&
                               (_kind != RaidStationKind.NormalExtraction || (_sector.IsCleared &&
                                RaidRuntime.AllowsExtraction(_raid.State, _sector.Coord, _kind))) &&
                               _kind != RaidStationKind.SpecialExtractionWip &&
                               (_kind != RaidStationKind.Hatch || _hatch == null || (_hatch.isActiveAndEnabled && _hatch.HasValidInteractionTrigger)) &&
                               (_kind != RaidStationKind.GeneratorShortcut || _raid.State.generatorPowered);
    public string PromptLabel => _kind == RaidStationKind.Hatch && _hatch != null
        ? _hatch.PromptLabel : UIRuntimeLocalization.Get("UI_Map", _kind switch
    {
        RaidStationKind.GeneratorShortcut => "Raid_GeneratorShortcut",
        RaidStationKind.Generator => _raid != null && _raid.State.generatorPowered ? "Raid_Powered" : "Raid_PowerGenerator",
        RaidStationKind.Hatch => _raid != null && _raid.State.generatorPowered ? "Raid_HatchExtract" : "Raid_HatchLocked",
        RaidStationKind.StartReturn => "Raid_ReturnHome",
        _ => "Map_Extract"
    }, _kind == RaidStationKind.GeneratorShortcut ? "중앙 해치로 이동" : _kind.ToString());

    public void Configure(RaidRuntime raid, SectorRuntime sector, RaidStationKind kind)
    {
        _raid = raid; _sector = sector; _kind = kind;
        _hatch = GetComponent<SectorHatch>();
        if (_hatch != null) _hatch.Configure(kind == RaidStationKind.Hatch ? raid : null);
        NotifyPromptChanged();
    }
    internal void NotifyPromptChanged() => PromptChanged?.Invoke();

    public bool TryInteract(Component actor)
    {
        if (!CanInteract || actor == null) return false;
        PlayerInventoryRuntime inventory = EntityComponentResolver.Resolve<PlayerInventoryRuntime>(actor.transform);
        if (_kind == RaidStationKind.GeneratorShortcut) return _raid.TryReturnThroughShortcut(inventory);
        return _kind == RaidStationKind.Generator
            ? _raid.TryPowerGenerator(inventory)
            : _raid.TryExtract(_sector, _kind, inventory);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = _kind == RaidStationKind.Generator ? Color.yellow : Color.green;
        Gizmos.DrawWireCube(transform.position + Vector3.up * .6f, new Vector3(1f, 1.2f, 1f));
    }
}
