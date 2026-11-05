using UnityEngine;

public enum SectorMapIconKind
{
    None = 0, NormalCombat = 1, ThemedCombat = 2, Office = 3, Warehouse = 4,
    Armory = 5, Medical = 6, Vault = 7, Server = 8, Hatch = 9,
    Generator = 10, Extraction = 11, Merchant = 12, Boss = 13, Unknown = 14, Named = 15
}

[CreateAssetMenu(fileName = "SectorMapIcons", menuName = "Game/UI/Shared Sector Map Icons")]
public sealed class SectorMapIconSetSO : ScriptableObject
{
    [SerializeField] private Sprite _normalCombat;
    [SerializeField] private Sprite _themedCombat;
    [SerializeField] private Sprite _office;
    [SerializeField] private Sprite _warehouse;
    [SerializeField] private Sprite _armory;
    [SerializeField] private Sprite _medical;
    [SerializeField] private Sprite _vault;
    [SerializeField] private Sprite _server;
    [SerializeField] private Sprite _hatch;
    [SerializeField] private Sprite _generator;
    [SerializeField] private Sprite _extraction;
    [SerializeField] private Sprite _merchant;
    [SerializeField] private Sprite _boss;
    [SerializeField] private Sprite _unknown;
    [SerializeField] private Sprite _named;

    public Sprite GetIcon(SectorMapIconKind kind) => kind switch
    {
        SectorMapIconKind.NormalCombat => _normalCombat,
        SectorMapIconKind.ThemedCombat => _themedCombat != null ? _themedCombat : _normalCombat,
        SectorMapIconKind.Office => _office != null ? _office : _normalCombat,
        SectorMapIconKind.Warehouse => _warehouse != null ? _warehouse : _normalCombat,
        SectorMapIconKind.Armory => _armory,
        SectorMapIconKind.Medical => _medical,
        SectorMapIconKind.Vault => _vault,
        SectorMapIconKind.Server => _server != null ? _server : _normalCombat,
        SectorMapIconKind.Hatch => _hatch,
        SectorMapIconKind.Generator => _generator,
        SectorMapIconKind.Extraction => _extraction,
        SectorMapIconKind.Merchant => _merchant,
        SectorMapIconKind.Boss => _boss,
        SectorMapIconKind.Unknown => _unknown,
        SectorMapIconKind.Named => _named,
        _ => null
    };
}
