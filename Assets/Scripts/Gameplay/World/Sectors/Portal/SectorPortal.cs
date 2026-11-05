/*
 * Role hierarchy: Gameplay > World > Sectors > Portal
 * Primary responsibility: Manages SectorPortal stage, sector, navigation, spawn, or spatial progression state.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

[DisallowMultipleComponent]
public class SectorPortal : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Collider _interactionCollider;
    [SerializeField] private Transform _arrivalPoint;
    [SerializeField] private GameObject _particleRoot;

    [Tooltip("Optional root containing the entire visible/interactable portal. Boss portals fall back to this GameObject when this field is empty.")]
    [SerializeField] private GameObject _availabilityRoot;

    private SectorPortalManager _manager;
    private SectorRuntime _ownerSector;
    private SectorRuntime _targetSector;
    private SectorPortal _targetPortal;
    private SectorPortalDirection _direction;

    private bool _isAvailable;
    private bool _isExtraction;

    public SectorRuntime OwnerSector => _ownerSector;
    public SectorRuntime TargetSector => _targetSector;
    public SectorPortal TargetPortal => _targetPortal;
    public SectorPortalDirection Direction => _direction;
    public bool IsAvailable => _isAvailable;
    public bool IsExtraction => _isExtraction;
    public bool CanInteract => _isAvailable && _manager != null;
    public bool HasPendingKeyConfirmation => ShopPurchaseTradePanel.IsKeySubmissionOwnedBy(this);

    public Vector3 ArrivalPosition =>
        _arrivalPoint != null ? _arrivalPoint.position : transform.position;

    // Purpose: Initializes Inspector defaults for this component.
    private void Reset()
    {
        _interactionCollider = GetComponent<Collider>();
    }

    // Purpose: Initializes initialize for use by this component.
    public void Initialize(
        SectorPortalManager manager,
        SectorRuntime ownerSector,
        SectorPortalDirection direction)
    {
        _manager = manager;
        _ownerSector = ownerSector;
        _direction = direction;
    }

    // Purpose: Updates the link state.
    public void SetLink(SectorRuntime targetSector, SectorPortal targetPortal)
    {
        _targetSector = targetSector;
        _targetPortal = targetPortal;
    }

    // Purpose: Marks whether this portal is the generated early-extraction route.
    public void SetExtraction(bool isExtraction)
    {
        _isExtraction = isExtraction;
    }

    // Purpose: Updates the available state.
    public void SetAvailable(bool available)
    {
        SetAvailable(available, hideWholePortalWhenUnavailable: false);
    }

    // Purpose: Updates the available state.
    public void SetAvailable(bool available, bool hideWholePortalWhenUnavailable)
    {
        _isAvailable = available;

        if (_interactionCollider != null)
            _interactionCollider.enabled = available;

        if (_particleRoot != null)
            _particleRoot.SetActive(available);

        GameObject availabilityRoot = _availabilityRoot != null
            ? _availabilityRoot
            : hideWholePortalWhenUnavailable ? gameObject : null;

        if (availabilityRoot != null && availabilityRoot.activeSelf != available)
            availabilityRoot.SetActive(available);
    }

    // Purpose: Attempts to interact and reports whether the request succeeded.
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
