using UnityEngine;

[DisallowMultipleComponent]
public class SectorPortal : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Collider _interactionCollider;
    [SerializeField] private Transform _arrivalPoint;
    [SerializeField] private GameObject _particleRoot;

    private SectorPortalManager _manager;
    private SectorRuntime _ownerSector;
    private SectorRuntime _targetSector;
    private SectorPortal _targetPortal;
    private SectorPortalDirection _direction;

    private bool _isAvailable;

    public SectorRuntime OwnerSector => _ownerSector;
    public SectorRuntime TargetSector => _targetSector;
    public SectorPortal TargetPortal => _targetPortal;
    public SectorPortalDirection Direction => _direction;
    public bool IsAvailable => _isAvailable;
    public bool CanInteract => _isAvailable && _manager != null && _targetSector != null && _targetPortal != null;

    public Vector3 ArrivalPosition =>
        _arrivalPoint != null ? _arrivalPoint.position : transform.position;

    private void Reset()
    {
        _interactionCollider = GetComponent<Collider>();
    }

    public void Initialize(
        SectorPortalManager manager,
        SectorRuntime ownerSector,
        SectorPortalDirection direction)
    {
        _manager = manager;
        _ownerSector = ownerSector;
        _direction = direction;
    }

    public void SetLink(SectorRuntime targetSector, SectorPortal targetPortal)
    {
        _targetSector = targetSector;
        _targetPortal = targetPortal;
    }

    public void SetAvailable(bool available)
    {
        _isAvailable = available;

        if (_interactionCollider != null)
            _interactionCollider.enabled = available;

        if (_particleRoot != null)
            _particleRoot.SetActive(available);
    }

    public bool TryInteract(Transform actor)
    {
        Debug.Log($"[SectorPortal] TryInteract portal={name}, CanInteract={CanInteract}, actor={actor}, manager={_manager}, target={_targetSector}, targetPortal={_targetPortal}");

        if (!CanInteract || actor == null)
            return false;

        bool result = _manager.TryMoveThroughPortal(this, actor);
        Debug.Log($"[SectorPortal] TryInteract result={result}");

        return result;
    }
}
