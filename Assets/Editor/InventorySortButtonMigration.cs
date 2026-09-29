using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Removes only the recognisable, unbound buttons left by the old OnValidate generator.</summary>
internal static class InventorySortButtonMigration
{
    [InitializeOnLoadMethod]
    private static void Initialize()
    {
        EditorApplication.delayCall += CleanupLoadedScenes;
        EditorSceneManager.sceneOpened -= OnSceneOpened;
        EditorSceneManager.sceneOpened += OnSceneOpened;
    }

    private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
        EditorApplication.delayCall -= CleanupLoadedScenes;
        EditorApplication.delayCall += CleanupLoadedScenes;
    }

    private static void CleanupLoadedScenes()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += CleanupLoadedScenes;
            return;
        }

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded || (scene.path != "Assets/Scenes/RUNScene.unity" &&
                                    scene.path != "Assets/Scenes/Manager/GamePlay.unity"))
                continue;

            GameObject[] roots = scene.GetRootGameObjects();
            var obsolete = roots.Where(IsLegacyGeneratedButton).ToList();

            // A still-open UI scene may retain the old, unsaved preview children
            // alongside the newly imported, persistently wired prefab children.
            foreach (PlayerInventoryUI panel in roots.SelectMany(root => root.GetComponentsInChildren<PlayerInventoryUI>(true)))
            {
                foreach (Button button in panel.GetComponentsInChildren<Button>(true))
                {
                    if (!IsLegacyGeneratedButton(button.gameObject)) continue;
                    Transform header = button.transform.parent;
                    if (header == null || header.name != "Header") continue;
                    bool hasAuthoredReplacement = header.Cast<Transform>().Any(child =>
                        child != button.transform && child.name == button.name &&
                        child.TryGetComponent(out Button replacement) &&
                        replacement.onClick.GetPersistentEventCount() > 0 &&
                        replacement.onClick.GetPersistentTarget(0) == panel &&
                        replacement.onClick.GetPersistentMethodName(0) == nameof(PlayerInventoryUI.SortPlayerInventory));
                    if (hasAuthoredReplacement) obsolete.Add(button.gameObject);
                }
            }

            if (obsolete.Count == 0) continue;
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Remove orphaned inventory sort buttons");
            foreach (GameObject go in obsolete.Distinct())
                Undo.DestroyObjectImmediate(go);
            Undo.CollapseUndoOperations(undoGroup);
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"[Inventory Sort] Removed {obsolete.Count} obsolete preview buttons from {scene.name}. " +
                      "GamePlay inventory prefab buttons are preserved. Undo is available; the scene was not auto-saved.");
        }
    }

    // Name alone is not sufficient authority to delete an object. Match the
    // old generator's dimensions, colour, components and lack of authored calls.
    internal static bool IsLegacyGeneratedButton(GameObject go)
    {
        if (go == null || (go.name != "SortCompact" && go.name != "SortRarity") ||
            EditorUtility.IsPersistent(go) || PrefabUtility.GetCorrespondingObjectFromSource(go) != null ||
            !(go.transform is RectTransform rect) || rect.sizeDelta != new Vector2(72, 22) ||
            !go.TryGetComponent(out Button button) || button.onClick.GetPersistentEventCount() != 0 ||
            !go.TryGetComponent(out Image image) || image.color != new Color(.08f, .25f, .3f, 1) ||
            rect.childCount > 1)
            return false;

        if (go.GetComponents<Component>().Any(component => !(component is RectTransform) &&
            !(component is CanvasRenderer) && !(component is Image) && !(component is Button)))
            return false;

        if (rect.childCount == 0) return true;
        Transform label = rect.GetChild(0);
        return label.name == "Label" && label.childCount == 0 && label.GetComponent<TextMeshProUGUI>() != null &&
               label.GetComponents<Component>().All(component => component is RectTransform ||
                   component is CanvasRenderer || component is TextMeshProUGUI);
    }
}
