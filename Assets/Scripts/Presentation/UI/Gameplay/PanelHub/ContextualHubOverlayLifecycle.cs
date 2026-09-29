/*
 * Role hierarchy: Presentation > UI > Gameplay > PanelHub
 * Primary responsibility: Implements the focused ContextualHubOverlayLifecycle responsibility and collaborates with its role group.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

[DisallowMultipleComponent]
public sealed class ContextualHubOverlayLifecycle : MonoBehaviour, IUIOverlayLifecycle
{
    private PlayerPanelHub _owner;
    private PlayerPanelHubMode _mode;

    // Purpose: Configures this component''s configure.
    public void Configure(PlayerPanelHub owner, PlayerPanelHubMode mode)
    {
        _owner = owner;
        _mode = mode;
    }

    // Purpose: Responds to the overlay shown callback.
    public void OnOverlayShown()
    {
        _owner?.HandleContextHubShown(_mode);
    }

    // Purpose: Responds to the overlay hidden callback.
    public void OnOverlayHidden()
    {
        _owner?.HandleContextHubHidden(_mode);
    }
}
