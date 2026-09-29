/*
 * Role hierarchy: Presentation > UI > Gameplay > Overlay
 * Primary responsibility: Defines the shared contract implemented by the IUIOverlayLifecycle role group.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

public interface IUIOverlayLifecycle
{
    void OnOverlayShown();
    void OnOverlayHidden();
}
