/*
 * Role hierarchy: Presentation > UI > Gameplay > Interaction
 * Primary responsibility: Handles InteractionPromptAnchor interaction, pickup, trade, loot, or storage flow.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

[DisallowMultipleComponent]
public class InteractionPromptAnchor : MonoBehaviour
{
    [Tooltip("Screen-space interaction prompt and cast bar follow this world position. When empty, this object's transform is used.")]
    [SerializeField] private Transform _anchor;

    [Tooltip("Optional child visual shown only while this object is the interaction currently selected by the player. Assign and style the circle/dot directly in the prefab; the InteractionManager only toggles it. Do not assign this component's own GameObject.")]
    [SerializeField] private GameObject _interactDot;

    public Transform Anchor => _anchor != null ? _anchor : transform;
    public GameObject InteractDot => _interactDot;

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        SetInteractDotVisible(false);
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        SetInteractDotVisible(false);
    }

    // Purpose: Updates the interact dot visible state.
    public void SetInteractDotVisible(bool visible)
    {
        if (_interactDot == null)
            return;

        if (_interactDot == gameObject)
        {
            Debug.LogError(
                "[InteractionPromptAnchor] Interact Dot must reference a separate child visual, not the GameObject containing InteractionPromptAnchor.",
                this);
            return;
        }

        if (_interactDot.activeSelf != visible)
            _interactDot.SetActive(visible);
    }
}
