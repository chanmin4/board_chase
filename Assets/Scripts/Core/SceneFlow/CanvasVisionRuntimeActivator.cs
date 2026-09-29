/*
 * Role hierarchy: Core > SceneFlow
 * Primary responsibility: Restores the author-hidden Canvas-Vision whenever a playable scene is loaded.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Canvas-Vision stays visible in the hierarchy but transparent in edit mode for
/// comfortable UI authoring. This PersistentManager-owned bridge restores its
/// CanvasGroup alpha before gameplay starts and after each additive scene load.
/// </summary>
[DefaultExecutionOrder(-10010)]
[DisallowMultipleComponent]
public sealed class CanvasVisionRuntimeActivator : MonoBehaviour
{
    [SerializeField] private string _canvasObjectName = "Canvas-Vision";

    // Purpose: Subscribes before scene Start calls and restores an already-loaded Canvas-Vision.
    private void Awake()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
        RestoreCanvasVisionInLoadedScenes();
    }

    // Purpose: Releases the global scene-load subscription when PersistentManager is destroyed.
    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    // Purpose: Restores Canvas-Vision after a newly loaded scene has created its authored objects.
    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RestoreCanvasVisionInLoadedScenes();
    }

    // Purpose: Restores only the authored Canvas-Vision CanvasGroups that belong to loaded scenes.
    private void RestoreCanvasVisionInLoadedScenes()
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (canvas == null ||
                canvas.gameObject.name != _canvasObjectName ||
                !canvas.gameObject.scene.IsValid() ||
                !canvas.gameObject.scene.isLoaded)
            {
                continue;
            }

            CanvasGroup group = canvas.GetComponent<CanvasGroup>();
            if (group == null)
                continue;

            group.alpha = 1f;
            group.interactable = false;
            group.blocksRaycasts = false;
        }
    }
}
