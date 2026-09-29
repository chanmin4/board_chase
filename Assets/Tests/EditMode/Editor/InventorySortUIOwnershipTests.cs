using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public sealed class InventorySortUIOwnershipTests
{
    private const string PanelPath = "Assets/Prefabs/UI/GamePlay/PanelHub/Inventory/PlayerInventoryPanel.prefab";
    private readonly List<GameObject> _created = new();

    [TearDown]
    public void Cleanup()
    {
        foreach (GameObject go in _created) if (go != null) Object.DestroyImmediate(go);
        _created.Clear();
    }

    [Test]
    public void PrefabOwnsTwoButtonPairsWithCorrectPersistentSortArguments()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PanelPath);
        Assert.NotNull(prefab);
        PlayerInventoryUI panel = prefab.GetComponent<PlayerInventoryUI>();
        Button[] buttons = prefab.GetComponentsInChildren<Button>(true)
            .Where(button => button.name == "SortCompact" || button.name == "SortRarity").ToArray();
        Assert.AreEqual(4, buttons.Length);
        foreach (string section in new[] { "PocketSection", "InventorySection" })
        foreach (string name in new[] { "SortCompact", "SortRarity" })
        {
            Button button = buttons.Single(candidate => candidate.name == name &&
                candidate.transform.parent.name == "Header" && candidate.transform.parent.parent.name == section);
            Assert.AreEqual(1, button.onClick.GetPersistentEventCount());
            Assert.AreSame(panel, button.onClick.GetPersistentTarget(0));
            Assert.AreEqual(nameof(PlayerInventoryUI.SortPlayerInventory), button.onClick.GetPersistentMethodName(0));
            var serialized = new SerializedObject(button);
            Assert.AreEqual(name == "SortRarity", serialized.FindProperty(
                "m_OnClick.m_PersistentCalls.m_Calls.Array.data[0].m_Arguments.m_BoolArgument").boolValue);
            Assert.IsFalse(button.GetComponentInChildren<TMP_Text>(true).raycastTarget);
            Assert.IsFalse(InventorySortButtonMigration.IsLegacyGeneratedButton(button.gameObject));
        }
    }

    [Test]
    public void GamePlayCanvasInheritsFourButtonsFromInventoryPrefab()
    {
        GameObject canvas = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/GamePlay/Canvas-GamePlay.prefab");
        PlayerInventoryUI panel = canvas.GetComponentInChildren<PlayerInventoryUI>(true);
        Assert.NotNull(panel);
        Button[] buttons = panel.GetComponentsInChildren<Button>(true)
            .Where(button => button.name == "SortCompact" || button.name == "SortRarity").ToArray();
        Assert.AreEqual(4, buttons.Length);
        foreach (Button button in buttons)
            Assert.AreSame(panel, button.onClick.GetPersistentTarget(0));
        string scene = File.ReadAllText("Assets/Scenes/Manager/GamePlay.unity");
        StringAssert.Contains(AssetDatabase.AssetPathToGUID("Assets/Prefabs/UI/GamePlay/Canvas-GamePlay.prefab"), scene);
    }

    [Test]
    public void RunSceneContainsNoSerializedSortButtonsOrInventoryPanel()
    {
        string scene = File.ReadAllText("Assets/Scenes/RUNScene.unity");
        StringAssert.DoesNotContain("m_Name: SortCompact", scene);
        StringAssert.DoesNotContain("m_Name: SortRarity", scene);
        StringAssert.DoesNotContain(AssetDatabase.AssetPathToGUID(PanelPath), scene);
    }

    [Test]
    public void MissingButtonsAreNeverCreatedByRefreshOrValidation()
    {
        GameObject root = Go("InventoryTest");
        PlayerInventoryUI panel = root.AddComponent<PlayerInventoryUI>();
        RectTransform section = (RectTransform)Go("PocketSection", root.transform).transform;
        Transform header = Go("Header", section).transform;
        for (int i = 0; i < 5; i++)
        {
            Invoke(panel, "RefreshSortButtons", section);
            Invoke(panel, "OnValidate");
        }
        Assert.AreEqual(0, header.childCount);
        Assert.AreEqual(0, root.GetComponentsInChildren<Button>(true).Length);
    }

    [Test]
    public void EditorPreviewDoesNotModifyPersistentPrefabHierarchy()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PanelPath);
        PlayerInventoryUI panel = prefab.GetComponent<PlayerInventoryUI>();
        int before = prefab.GetComponentsInChildren<Transform>(true).Length;
        Invoke(panel, "OnValidate");
        Invoke(panel, "ApplyEditorPreview");
        Assert.AreEqual(before, prefab.GetComponentsInChildren<Transform>(true).Length);
    }

    [Test]
    public void SortRefreshDoesNotTouchAnotherPanelsSection()
    {
        PlayerInventoryUI panel = Go("InventoryTest").AddComponent<PlayerInventoryUI>();
        GameObject foreign = Go("ForeignSection");
        GameObject header = Go("Header", foreign.transform);
        GameObject buttonRoot = Go("SortCompact", header.transform);
        Button button = buttonRoot.AddComponent<Button>();
        button.interactable = false;
        Invoke(panel, "RefreshSortButtons", (RectTransform)foreign.transform);
        Assert.IsFalse(button.interactable);
    }

    [Test]
    public void CleanupRecognizesOnlyExactLegacyButtonShape()
    {
        GameObject root = Go("SortCompact");
        root.AddComponent<Image>().color = new Color(.08f, .25f, .3f, 1);
        root.AddComponent<Button>();
        ((RectTransform)root.transform).sizeDelta = new Vector2(72, 22);
        Assert.IsTrue(InventorySortButtonMigration.IsLegacyGeneratedButton(root));
        root.name = "MyOtherButton";
        Assert.IsFalse(InventorySortButtonMigration.IsLegacyGeneratedButton(root));
        root.name = "SortCompact";
        root.AddComponent<CanvasGroup>();
        Assert.IsFalse(InventorySortButtonMigration.IsLegacyGeneratedButton(root));
    }

    private GameObject Go(string name, Transform parent = null)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.SetActive(false);
        if (parent != null) go.transform.SetParent(parent, false);
        _created.Add(go);
        return go;
    }

    private static void Invoke(PlayerInventoryUI panel, string method, params object[] arguments) =>
        typeof(PlayerInventoryUI).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(panel, arguments);
}
