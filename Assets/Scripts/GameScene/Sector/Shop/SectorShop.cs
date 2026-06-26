using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class SectorShop : MonoBehaviour
{
    [Header("Rules")]
    [Tooltip("Stage-specific Shop room generation and inventory settings.")]
    [SerializeField] private StageShopSettingsSO _shopSettings;

    [Header("Refs")]
    [Tooltip("SectorRuntime that owns this shop. Empty uses GetComponentInParent<SectorRuntime>().")]
    [SerializeField] private SectorRuntime _sector;

    [Tooltip("Shop UI panel. Empty searches the scene, including inactive objects.")]
    [SerializeField] private PlayerShopPanelUI _shopPanel;

    [Header("Overlay")]
    [Tooltip("Optional. If assigned, opens UIOverlayId.Shop before binding offers to ShopPanel.")]
    [SerializeField] private UIOverlayRequestEventChannelSO _overlayRequestChannel;

    [Header("Listening")]
    [Tooltip("Receives SectorStateManager when it is ready.")]
    [SerializeField] private SectorStateManagerReadyEventChannelSO _sectorStateManagerReadyChannel;

    [Header("Room Completion")]
    [Tooltip("If true, Shop room becomes cleared when the player first opens the shop.")]
    [SerializeField] private bool _completeRoomOnFirstInteract = true;

    [Header("Seed")]
    [Tooltip("Extra deterministic salt used when rolling this shop inventory.")]
    [SerializeField] private int _offerSeedSalt = 9419;

    [Header("Debug")]
    [SerializeField] private bool _logWarnings = true;

    private readonly List<PlayerShopOffer> _offers = new();
    private SectorStateManager _sectorStateManager;
    private ShopRoomDropTableSO _activeDropTable;
    private bool _offersRolled;
    private bool _roomCompletedByShop;
    private bool _activeAllowReroll = true;

    public bool CanInteract => IsShopRoom();

    private void OnEnable()
    {
        ResolveRefs();

        if (_sectorStateManagerReadyChannel != null)
        {
            _sectorStateManagerReadyChannel.OnEventRaised += HandleSectorStateManagerReady;

            if (_sectorStateManagerReadyChannel.HasCurrent)
                HandleSectorStateManagerReady(_sectorStateManagerReadyChannel.Current);
        }
    }

    private void OnDisable()
    {
        if (_sectorStateManagerReadyChannel != null)
            _sectorStateManagerReadyChannel.OnEventRaised -= HandleSectorStateManagerReady;
    }

    public bool TryInteract(Component actor)
    {
        ResolveRefs();

        if (!CanInteract)
            return false;

        if (!TryEnsureOffers())
            return false;

        if (_shopPanel == null)
        {
            LogWarning("PlayerShopPanelUI is missing.");
            return false;
        }

        _overlayRequestChannel?.Open(UIOverlayId.Shop);
        _shopPanel.OpenWithOffers(_offers, _activeDropTable, _activeAllowReroll);
        CompleteRoomIfConfigured();
        return true;
    }

    private void HandleSectorStateManagerReady(SectorStateManager manager)
    {
        if (manager != null)
            _sectorStateManager = manager;
    }

    private bool TryEnsureOffers()
    {
        if (_offersRolled)
            return _offers.Count > 0;

        int stageIndex = _sectorStateManager != null
            ? _sectorStateManager.CurrentStage
            : 0;

        int offerCountOverride = 0;
        _activeAllowReroll = true;
        _activeDropTable = null;

        if (_shopSettings != null &&
            _shopSettings.TryGetRule(stageIndex, out StageShopSettingsSO.StageShopRule rule))
        {
            _activeDropTable = rule.dropTable;
            _activeAllowReroll = rule.allowReroll;
        }

        if (_activeDropTable == null)
        {
            LogWarning($"ShopRoomDropTableSO is missing. stage={stageIndex}");
            return false;
        }

        _offers.Clear();
        _offers.AddRange(_activeDropTable.CreateOffers(
            ResolveOfferSeed(stageIndex, ResolveSectorCoord()),
            offerCountOverride));

        _offersRolled = true;

        if (_offers.Count <= 0)
            LogWarning($"Failed to roll shop offers. stage={stageIndex}");

        return _offers.Count > 0;
    }

    private bool IsShopRoom()
    {
        ResolveRefs();

        if (_sector == null)
            return false;

        if (_sectorStateManager == null)
            return true;

        if (!_sectorStateManager.TryGetStageRoomType(_sector, out StageRoomType roomType))
            return true;

        return roomType == StageRoomType.Shop;
    }

    private void CompleteRoomIfConfigured()
    {
        if (!_completeRoomOnFirstInteract || _roomCompletedByShop || _sector == null)
            return;

        _roomCompletedByShop = true;

        if (_sectorStateManager != null)
        {
            _sectorStateManager.CompleteSector(_sector);
            return;
        }

        _sector.SetCleared(true);
    }

    private Vector2Int ResolveSectorCoord()
    {
        if (_sectorStateManager != null &&
            _sectorStateManager.TryGetSectorCoord(_sector, out Vector2Int coord))
        {
            return coord;
        }

        return _sector != null ? _sector.Coord : default;
    }

    private int ResolveOfferSeed(int stageIndex, Vector2Int coord)
    {
        int stageSeed = stageIndex;

        if (_sectorStateManager != null &&
            _sectorStateManager.CurrentStageMapLayout != null)
        {
            stageSeed = _sectorStateManager.CurrentStageMapLayout.stageSeed;
        }

        unchecked
        {
            int hash = 17;
            hash = hash * 31 + stageSeed;
            hash = hash * 31 + stageIndex;
            hash = hash * 31 + coord.x;
            hash = hash * 31 + coord.y;
            hash = hash * 31 + _offerSeedSalt;
            return hash;
        }
    }

    private void ResolveRefs()
    {
        if (_sector == null)
            _sector = GetComponentInParent<SectorRuntime>();

        if (_sectorStateManager == null &&
            _sectorStateManagerReadyChannel != null &&
            _sectorStateManagerReadyChannel.HasCurrent)
        {
            _sectorStateManager = _sectorStateManagerReadyChannel.Current;
        }

        if (_sectorStateManager == null)
            _sectorStateManager = FindAnyObjectByType<SectorStateManager>();

        if (_shopPanel == null)
            _shopPanel = FindAnyObjectByType<PlayerShopPanelUI>(FindObjectsInactive.Include);
    }

    private void LogWarning(string message)
    {
        if (!_logWarnings)
            return;

        Debug.LogWarning($"[SectorShop] {message}", this);
    }
}
