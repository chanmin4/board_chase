/*
 * Role hierarchy: Gameplay > Entities > Shooter > Common > Visuals
 * Primary responsibility: Stores one complete, directly-authored shooter visual customization.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct ShooterCustomizeAttachment
{
    [Tooltip("장식 기본 정의의 출처. 아래 장착값은 캐릭터별 복사본이며 기본 SO를 수정해도 자동으로 바뀌지 않는다.")]
    public ShooterVisualAttachmentSO definition;
    public ShooterVisualSocketId socket;
    public GameObject visualPrefab;
    public ShooterVisualLocalTransform localTransform;
    public ShooterVisualOccupancy occupiedRegions;
    [Tooltip("Hide this cosmetic while any of these equipment visuals is worn. Removing the equipment restores it. None preserves existing looks.")]
    public ShooterVisualEquipmentKind hideWhenEquipped;
}

/// <summary>
/// The single authoring asset for one shooter look.
/// It intentionally owns only visual data: source prefab, body/socket poses,
/// body tint, and cosmetic prefab placements. Gameplay weapon and armor data
/// stays on the existing Item SOs.
/// </summary>
[CreateAssetMenu(
    fileName = "ShooterCustomize",
    menuName = "Game/Visual Customization/Shooter Customize")]
public sealed class ShooterCustomizeSO : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string _customizeId = "shooter_customize";

    [Header("Source")]
    [Tooltip("Compatible Visual_Shooter-style prefab used as the base for this look.")]
    [SerializeField] private GameObject _visualPrefab;

    [Header("Visual Body")]
    [Tooltip("Applied to the complete base character visual, including sibling visual parts such as Feet. Runtime equipment and saved cosmetic attachments keep their own colors.")]
    [SerializeField] private Color _bodyTint = Color.white;
    [Tooltip("Applied only to ShooterVisualRig.BodyVisualRoot, never to gameplay colliders.")]
    [SerializeField] private ShooterVisualLocalTransform _bodyVisualTransform = new ShooterVisualLocalTransform(
        Vector3.zero,
        Vector3.zero,
        Vector3.one);

    [Header("Socket Poses")]
    [SerializeField] private List<ShooterVisualSocketPose> _socketPoses = new List<ShooterVisualSocketPose>();

    [Header("Cosmetic Placements")]
    [Tooltip("Direct children added under visual sockets in the Customize Station preview.")]
    [SerializeField] private List<ShooterCustomizeAttachment> _attachments = new List<ShooterCustomizeAttachment>();

    public string CustomizeId => string.IsNullOrWhiteSpace(_customizeId) ? name : _customizeId;
    public GameObject VisualPrefab => _visualPrefab;
    public Color BodyTint => _bodyTint;
    public ShooterVisualLocalTransform BodyVisualTransform => _bodyVisualTransform;
    public IReadOnlyList<ShooterVisualSocketPose> SocketPoses => _socketPoses;
    public IReadOnlyList<ShooterCustomizeAttachment> Attachments => _attachments;

    // Purpose: Performs the capture workflow for this component.
    public void Capture(
        GameObject visualPrefab,
        Color bodyTint,
        ShooterVisualLocalTransform bodyVisualTransform,
        IList<ShooterVisualSocketPose> socketPoses,
        IList<ShooterCustomizeAttachment> attachments)
    {
        _visualPrefab = visualPrefab;
        _bodyTint = bodyTint;
        _bodyVisualTransform = bodyVisualTransform;

        _socketPoses.Clear();

        if (socketPoses != null)
        {
            for (int i = 0; i < socketPoses.Count; i++)
                _socketPoses.Add(socketPoses[i]);
        }

        _attachments.Clear();

        if (attachments != null)
        {
            for (int i = 0; i < attachments.Count; i++)
                _attachments.Add(attachments[i]);
        }
    }
}
