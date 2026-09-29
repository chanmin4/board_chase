/*
 * Role hierarchy: Gameplay > Entities > Shooter > Common > Visuals
 * Primary responsibility: Combines a body pose, tint, and reusable cosmetic attachments into one shooter appearance.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "ShooterVisualAppearance",
    menuName = "Game/Visual Customization/Shooter Appearance")]
public sealed class ShooterVisualAppearanceProfileSO : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string _appearanceId = "shooter_appearance";

    [Header("Body")]
    [SerializeField] private ShooterVisualBodyProfileSO _bodyProfile;
    [Tooltip("Use this for enemy-category distinction. Equipment and accessories retain their own authored materials.")]
    [SerializeField] private Color _bodyTint = Color.white;

    [Header("Cosmetic Accessories")]
    [Tooltip("These are visual-only attachments. Weapon and armor gameplay equipment remain owned by their existing Item SOs.")]
    [SerializeField] private List<ShooterVisualAttachmentSO> _accessories = new List<ShooterVisualAttachmentSO>();

    public string AppearanceId => string.IsNullOrWhiteSpace(_appearanceId)
        ? name
        : _appearanceId;
    public ShooterVisualBodyProfileSO BodyProfile => _bodyProfile;
    public Color BodyTint => _bodyTint;
    public IReadOnlyList<ShooterVisualAttachmentSO> Accessories => _accessories;
}
