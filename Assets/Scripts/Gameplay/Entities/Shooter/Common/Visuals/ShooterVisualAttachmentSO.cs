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
    [Tooltip("외형 장식의 기본 숨김 조건. 캐릭터에 추가할 때 복사되며 이후 캐릭터별 조정은 별도로 보존된다.")]
    [SerializeField] private ShooterVisualEquipmentKind _hideWhenEquipped;

    public string AttachmentId => string.IsNullOrWhiteSpace(_attachmentId)
        ? name
        : _attachmentId;
    public GameObject VisualPrefab => _visualPrefab;
    public ShooterVisualSocketId Socket => _socket;
    public ShooterVisualLocalTransform LocalTransform => _localTransform;
    public ShooterVisualOccupancy OccupiedRegions => _occupiedRegions;
    public ShooterVisualEquipmentKind HideWhenEquipped => _hideWhenEquipped;

    /// <summary>기본 정의를 작성한다. 기존 캐릭터 외형의 저장된 장착값은 변경하지 않는다.</summary>
    public void Configure(string id, GameObject prefab, ShooterVisualSocketId socket, ShooterVisualLocalTransform pose,
        ShooterVisualOccupancy regions, ShooterVisualEquipmentKind hiddenBy)
    {
        _attachmentId = id; _visualPrefab = prefab; _socket = socket; _localTransform = pose;
        _occupiedRegions = regions; _hideWhenEquipped = hiddenBy;
    }

    /// <summary>추가 시점의 기본값을 복사한다. 런타임은 이 복사본을 사용하므로 기본 SO 변경이 기존 외형을 바꾸지 않는다.</summary>
    public ShooterCustomizeAttachment CreatePlacement() => new ShooterCustomizeAttachment {
        definition = this, socket = _socket, visualPrefab = _visualPrefab, localTransform = _localTransform,
        occupiedRegions = _occupiedRegions, hideWhenEquipped = _hideWhenEquipped };

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
