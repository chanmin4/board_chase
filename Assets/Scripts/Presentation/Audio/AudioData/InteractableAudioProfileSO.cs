/*
 * Role hierarchy: Presentation > Audio > AudioData
 * Primary responsibility: Defines the authored audio handoff for world interactables.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

/// <summary>
/// Reusable sound handoff for world objects. Individual interactable scripts
/// consume only the fields that match their actions, so a chest, shop terminal
/// and breakable obstacle can share the same predictable authoring surface.
/// </summary>
[CreateAssetMenu(fileName = "InteractableAudioProfile", menuName = "Audio/Profiles/Interactable Audio Profile")]
public class InteractableAudioProfileSO : DescriptionBaseSO
{
    [Header("Interaction")]
    [SerializeField] private AudioCueSO _interactCue;
    [SerializeField] private AudioCueSO _openCue;
    [SerializeField] private AudioCueSO _closeCue;
    [SerializeField] private AudioCueSO _lockedOrRejectedCue;
    [SerializeField] private AudioCueSO _pickupCue;

    [Header("World Response")]
    [SerializeField] private AudioCueSO _hitCue;

    public AudioCueSO InteractCue => _interactCue;
    public AudioCueSO OpenCue => _openCue;
    public AudioCueSO CloseCue => _closeCue;
    public AudioCueSO LockedOrRejectedCue => _lockedOrRejectedCue;
    public AudioCueSO PickupCue => _pickupCue;
    public AudioCueSO HitCue => _hitCue;
}
