// SectorShop.cs
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class SectorShop : MonoBehaviour
{
    [Header("Rules")]
    [SerializeField] private StageShopSettingsSO _shopSettings;

    [Header("Refs")]
    [SerializeField] private SectorRuntime _sector;

    [Header("Overlay")]
    [SerializeField] private UIOverlayRequestEventChannelSO _overlayRequestChannel;
    [SerializeField] private SectorShopOpenRequestEventChannelSO _shopOpenRequestChannel;

    [Header("Listening")]
    [SerializeField] private SectorStateManagerReadyEventChannelSO _sectorStateManagerReadyChannel;

    [Header("Room Completion")]
    [SerializeField] private bool _completeRoomOnFirstInteract = true;

    [Header("Seed")]
    [SerializeField] private int _offerSeedSalt = 9419;

    [Header("Debug")]
    [SerializeField] private bool _logWarnings = true;

    private readonly List<PlayerShopOffer> _bulletOffers = new();
    private readonly List<PlayerShopOffer> _itemOffers = new();

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

        if (_shopOpenRequestChannel == null)
        {
            LogWarning("SectorShopOpenRequestEventChannelSO is missing.");
            return false;
        }

        _overlayRequestChannel?.Open(UIOverlayId.PlayerPanelHub);
        _shopOpenRequestChannel.RaiseEvent(
            new SectorShopOpenRequest(
                _bulletOffers,
                _itemOffers,
                _activeDropTable,
                _activeAllowReroll));

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
            return _bulletOffers.Count > 0 || _itemOffers.Count > 0;

        int stageIndex = _sectorStateManager != null
            ? _sectorStateManager.CurrentStage
            : 0;

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

        int seed = ResolveOfferSeed(stageIndex, ResolveSectorCoord());

        _bulletOffers.Clear();
        _itemOffers.Clear();

        _bulletOffers.AddRange(_activeDropTable.CreateBulletOffers(seed));
        _itemOffers.AddRange(_activeDropTable.CreateItemOffers(seed + 1009));

        _offersRolled = true;

        if (_bulletOffers.Count <= 0 && _itemOffers.Count <= 0)
            LogWarning($"Failed to roll shop offers. stage={stageIndex}");

        return _bulletOffers.Count > 0 || _itemOffers.Count > 0;
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
    }

    private void LogWarning(string message)
    {
        if (!_logWarnings)
            return;

        Debug.LogWarning($"[SectorShop] {message}", this);
    }
}