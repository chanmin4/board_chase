/*
 * Role hierarchy: Gameplay > Entities > Shooter > Common > Visuals
 * Primary responsibility: Defines one reusable cosmetic attachment and its socket-relative placement.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

[CreateAssetMenu(
    fileName = "ShooterVisualAttachment",
    menuName = "Game/Visual Customization/Shooter Attachment")]
public sealed class ShooterVisualAttachmentSO : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string _attachmentId = "accessory";

    [Header("Visual")]
    [SerializeField] private GameObject _visualPrefab;
    [SerializeField] private ShooterVisualSocketId _socket = ShooterVisualSocketId.Hair;
    [SerializeField] private ShooterVisualLocalTransform _localTransform = new ShooterVisualLocalTransform(
        Vector3.zero,
        Vector3.zero,
        Vector3.one);

    [Header("Visual Occupancy")]
    [Tooltip("Visible body regions this cosmetic claims. Equipment with an overlapping region temporarily hides this cosmetic.")]
    [SerializeField] private ShooterVisualOccupancy _occupiedRegions;

    public string AttachmentId => string.IsNullOrWhiteSpace(_attachmentId)
        ? name
        : _attachmentId;
    public GameObject VisualPrefab => _visualPrefab;
    public ShooterVisualSocketId Socket => _socket;
    public ShooterVisualLocalTransform LocalTransform => _localTransform;
    public ShooterVisualOccupancy OccupiedRegions => _occupiedRegions;

    // Purpose: Applies the requested local transform change.
    public void ApplyLocalTransform(Transform target)
    {
        _localTransform.ApplyTo(target);
    }

    // Purpose: Updates the placement state.
    public void SetPlacement(
        ShooterVisualSocketId socket,
        ShooterVisualLocalTransform localTransform)
    {
        _socket = socket;
        _localTransform = localTransform;
    }

    // Purpose: Updates the visual body regions this reusable cosmetic claims.
    public void SetOccupiedRegions(ShooterVisualOccupancy occupiedRegions)
    {
        _occupiedRegions = occupiedRegions;
    }
}
