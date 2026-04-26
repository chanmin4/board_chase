using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class SectorPortalManager : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private SectorStateManager _sectorStateManager;
    [Header("Events")]
    [SerializeField] private SectorRuntimeEventChannelSO _moveSectorCameraEvent;
    [SerializeField] private SectorRuntimeEventChannelSO _currentSectorChangedEvent;
    [SerializeField] private SectorRuntimeEventChannelSO _sectorOpenedEvent;

    private readonly Dictionary<Vector2Int, SectorRuntime> _sectorByCoord = new();

    private bool _startSectorConsumed;

    private void Awake()
    {
        if (_sectorStateManager == null)
            _sectorStateManager = FindAnyObjectByType<SectorStateManager>();

        if (_sectorStateManager != null)
            _sectorStateManager.EnsureInitialized();
    }

    private void OnEnable()
    {
        if (_sectorOpenedEvent != null)
            _sectorOpenedEvent.OnEventRaised += OnSectorOpened;
    }

    private void OnDisable()
    {
        if (_sectorOpenedEvent != null)
            _sectorOpenedEvent.OnEventRaised -= OnSectorOpened;
    }

    private void Start()
    {
        CacheSectors();
        BindPortals();
        RefreshAllPortals();
    }

    private void OnSectorOpened(SectorRuntime sector)
    {
        RefreshAllPortals();
    }

    private void CacheSectors()
    {
        _sectorByCoord.Clear();

        if (_sectorStateManager == null)
        {
            Debug.LogWarning("[SectorPortalManager] SectorStateManager is missing.");
            return;
        }

        SectorRuntime[] sectors = FindObjectsByType<SectorRuntime>(FindObjectsSortMode.None);
        for (int i = 0; i < sectors.Length; i++)
        {
            SectorRuntime sector = sectors[i];
            if (sector == null)
                continue;

            if (!_sectorStateManager.TryGetSectorCoord(sector, out Vector2Int coord))
                continue;

            if (!_sectorByCoord.ContainsKey(coord))
                _sectorByCoord.Add(coord, sector);
        }
    }

    private void BindPortals()
    {
        foreach (var pair in _sectorByCoord)
        {
            SectorRuntime sector = pair.Value;
            BindPortal(sector, SectorPortalDirection.XMin);
            BindPortal(sector, SectorPortalDirection.XMax);
            BindPortal(sector, SectorPortalDirection.ZMin);
            BindPortal(sector, SectorPortalDirection.ZMax);
        }
    }

    private void BindPortal(SectorRuntime sector, SectorPortalDirection direction)
    {
        SectorPortal portal = GetPortal(sector, direction);
        if (portal == null)
        {
            return;
        }

        portal.Initialize(this, sector, direction);

        Vector2Int ownerCoord = _sectorStateManager.GetSectorCoord(sector);
        Vector2Int targetCoord = ownerCoord + SectorPortalDirectionUtility.ToCoordOffset(direction);

        if (!_sectorByCoord.TryGetValue(targetCoord, out SectorRuntime targetSector))
        {
            portal.SetLink(null, null);
            return;
        }

        SectorPortalDirection opposite = SectorPortalDirectionUtility.Opposite(direction);
        SectorPortal targetPortal = GetPortal(targetSector, opposite);


        portal.SetLink(targetSector, targetPortal);
    }
    private void RefreshAllPortals()
    {
        foreach (var pair in _sectorByCoord)
        {
            SectorRuntime sector = pair.Value;

            RefreshPortal(sector, SectorPortalDirection.XMin);
            RefreshPortal(sector, SectorPortalDirection.XMax);
            RefreshPortal(sector, SectorPortalDirection.ZMin);
            RefreshPortal(sector, SectorPortalDirection.ZMax);
        }
    }

    private void RefreshPortal(SectorRuntime sector, SectorPortalDirection direction)
    {
        SectorPortal portal = GetPortal(sector, direction);
        if (portal == null)
            return;

        portal.SetAvailable(CanUsePortal(portal));
    }

    private bool CanUsePortal(SectorPortal portal)
    {
        if (portal == null)
        {
            Debug.LogWarning("[SectorPortalManager] CanUsePortal false: portal null");
            return false;
        }

        SectorRuntime source = portal.OwnerSector;
        SectorRuntime target = portal.TargetSector;

        if (source == null)
        {
           
            return false;
        }

        if (target == null)
        {
           
            return false;
        }

        if (portal.TargetPortal == null)
        {
          
            return false;
        }

        if (!source.isOpened)
        {
        
            return false;
        }

        if (!target.isOpened)
        {
        
            return false;
        }

        if (_sectorStateManager != null &&
            _startSectorConsumed &&
            (_sectorStateManager.IsStartSector(source) || _sectorStateManager.IsStartSector(target)))
        {
            Debug.LogWarning($"[SectorPortalManager] CanUsePortal false: start sector consumed. source={source.name}, target={target.name}");
            return false;
        }

        Debug.Log($"[SectorPortalManager] CanUsePortal true. portal={portal.name}, source={source.name}, target={target.name}");
        return true;
    }

    public bool TryMoveThroughPortal(SectorPortal sourcePortal, Transform player)
    {
        

        if (!CanUsePortal(sourcePortal) || player == null)
        {
        
            return false;
        }

        SectorPortal targetPortal = sourcePortal.TargetPortal;
        SectorRuntime targetSector = sourcePortal.TargetSector;

        if (targetPortal == null || targetSector == null)
        {
           
            return false;
        }


        CharacterController controller = player.GetComponent<CharacterController>();
        if (controller != null)
            controller.enabled = false;

        player.position = targetPortal.ArrivalPosition;

        if (controller != null)
            controller.enabled = true;

        if (_moveSectorCameraEvent != null)
            _moveSectorCameraEvent.RaiseEvent(targetSector);

        if (_currentSectorChangedEvent != null)
            _currentSectorChangedEvent.RaiseEvent(targetSector);

        if (_sectorStateManager != null &&
            sourcePortal.OwnerSector != null &&
            _sectorStateManager.IsStartSector(sourcePortal.OwnerSector))
        {
            _startSectorConsumed = true;
            RefreshAllPortals();
        }

        return true;
    }

    private SectorPortal GetPortal(SectorRuntime sector, SectorPortalDirection direction)
    {
        if (sector == null)
            return null;

        SectorEdge edge = GetEdge(sector, direction);
        if (edge == null || edge.portal == null)
            return null;

        return edge.portal.GetComponentInChildren<SectorPortal>(true);
    }

    private SectorEdge GetEdge(SectorRuntime sector, SectorPortalDirection direction)
    {
        switch (direction)
        {
            case SectorPortalDirection.XMin:
                return sector.XMin;

            case SectorPortalDirection.XMax:
                return sector.XMax;

            case SectorPortalDirection.ZMin:
                return sector.ZMin;

            case SectorPortalDirection.ZMax:
                return sector.ZMax;

            default:
                return null;
        }
    }
}
