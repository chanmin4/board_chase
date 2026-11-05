using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>Real UI components in a disposable Preview Scene; never changes a real inventory or input map.</summary>
public sealed class UIEscapeTests
{
    private Scene _preview;
    private UIOverlayManager _manager;
    private EscPauseMenuController _pause;
    private GameStateSO _state;
    private readonly Dictionary<FieldInfo, object> _globals = new();
    private object[] _popups;
    private float _timeScale;

    [SetUp] public void Setup()
    {
        _timeScale = Time.timeScale;
        foreach (var type in new[] { typeof(UIEscapeRouter), typeof(GamePause), typeof(GameplayActionInputBlocker), typeof(InventoryDragContext), typeof(InventoryDragGhostUI) })
            foreach (var field in type.GetFields(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public))
                if (!field.IsLiteral && !field.IsInitOnly) _globals[field] = field.GetValue(null);
        var registry = (IList)StaticGet(typeof(UIEscapeRouter), "Popups");
        _popups = new object[registry.Count]; registry.CopyTo(_popups, 0); registry.Clear();
        StaticSet(typeof(InventoryDragGhostUI), "_instance", null);
        StaticSet(typeof(GamePause), "_isPaused", false);
        Time.timeScale = 1f;
        NextPress();
        _preview = EditorSceneManager.NewPreviewScene();
        _manager = Go("Temporary overlay manager").AddComponent<UIOverlayManager>();
        Set(_manager, "_debugLogs", false); Set(_manager, "_playOverlayAudio", false);
        Call(_manager, "OnEnable");
        _pause = Go("Temporary ESC menu").AddComponent<EscPauseMenuController>();
        Set(_pause, "_overlayManager", _manager);
        Set(_pause, "_rootGroup", Go("ESC root").GetComponent<CanvasGroup>());
        Set(_pause, "_pauseMenuGroup", Go("Pause view").GetComponent<CanvasGroup>());
        Set(_pause, "_settingsGroup", Go("Settings view").GetComponent<CanvasGroup>());
        _state = ScriptableObject.CreateInstance<GameStateSO>(); _state.hideFlags = HideFlags.HideAndDontSave;
        Set(_pause, "_gameState", _state);
    }

    [TearDown] public void Cleanup()
    {
        try
        {
            if (_manager != null) Call(_manager, "OnDisable");
            if (_preview.IsValid()) EditorSceneManager.ClosePreviewScene(_preview);
            if (_state != null) Object.DestroyImmediate(_state);
        }
        finally
        {
            var registry = (IList)StaticGet(typeof(UIEscapeRouter), "Popups");
            registry.Clear(); if (_popups != null) foreach (var popup in _popups) registry.Add(popup);
            foreach (var pair in _globals) pair.Key.SetValue(null, pair.Value);
            _globals.Clear(); Time.timeScale = _timeScale;
        }
    }

    [TestCase(UIOverlayId.StageSelect)]
    [TestCase(UIOverlayId.PlayerPanelHub)] // Inventory, missions, stats and map share this hub.
    [TestCase(UIOverlayId.ShopHub)]
    [TestCase(UIOverlayId.LootHub)]
    [TestCase(UIOverlayId.LockedLootKey)]
    [TestCase(UIOverlayId.BossReward)]
    [TestCase(UIOverlayId.MetaUpgrade)]
    public void EscapeClosesOnlyPopupAndDoesNotPauseOrChangeGameState(UIOverlayId id)
    {
        var panel = Open(id);
        _state.UpdateGameState(GameState.Combat);
        int hidden = 0; var evt = new UnityEvent(); evt.AddListener(() => hidden++); Set(panel, "_onHidden", evt);
        _pause.HandleEscapePressed();
        Assert.AreEqual(0f, panel.GetComponent<CanvasGroup>().alpha);
        Assert.IsFalse(_manager.HasOpenOverlays); Assert.AreEqual(1, hidden);
        AssertNotPaused(); Assert.AreEqual(GameState.Combat, _state.CurrentGameState);
        // A second reader seeing the same physical press must not open Pause or repeat Hide.
        _pause.HandleEscapePressed(); Assert.IsTrue(UIEscapeRouter.TryCloseTopPopup());
        AssertNotPaused(); Assert.AreEqual(1, hidden);
    }

    [Test] public void EmptyScreenEscapeOpensPauseAndNextEscapeResumesOriginalTimeAndState()
    {
        Time.timeScale = .75f; _state.UpdateGameState(GameState.Combat);
        _pause.HandleEscapePressed();
        Assert.IsTrue(GamePause.IsPaused); Assert.AreEqual(0f, Time.timeScale);
        Assert.AreEqual(GameState.Pause, _state.CurrentGameState);
        _pause.HandleEscapePressed(); Assert.IsTrue(GamePause.IsPaused);
        Press(); AssertNotPaused(); Assert.AreEqual(.75f, Time.timeScale);
        Assert.AreEqual(GameState.Combat, _state.CurrentGameState);
    }

    [Test] public void StackedPopupsCloseOneAtATimeThenPauseOpens()
    {
        var inventory = Open(UIOverlayId.PlayerPanelHub); var shop = Open(UIOverlayId.ShopHub);
        _pause.HandleEscapePressed();
        Assert.AreEqual(UIOverlayId.PlayerPanelHub, _manager.TopOverlayId);
        Assert.AreEqual(1f, inventory.GetComponent<CanvasGroup>().alpha);
        Assert.AreEqual(0f, shop.GetComponent<CanvasGroup>().alpha); AssertNotPaused();
        Press(); Assert.IsFalse(_manager.HasOpenOverlays); AssertNotPaused();
        Press(); Assert.IsTrue(GamePause.IsPaused);
    }

    [Test] public void BringingExistingOverlayToFrontChangesEscapeTarget()
    {
        var inventory = Open(UIOverlayId.PlayerPanelHub); var shop = Open(UIOverlayId.ShopHub);
        _manager.Open(UIOverlayId.PlayerPanelHub); _pause.HandleEscapePressed();
        Assert.AreEqual(0f, inventory.GetComponent<CanvasGroup>().alpha);
        Assert.AreEqual(1f, shop.GetComponent<CanvasGroup>().alpha);
        Assert.AreEqual(UIOverlayId.ShopHub, _manager.TopOverlayId); AssertNotPaused();
    }

    [Test] public void NestedKeyToolClosesBeforeInventoryWithoutClosingParent()
    {
        var parent = Open(UIOverlayId.PlayerPanelHub);
        var child = Go("Temporary key tool", parent.transform).AddComponent<KeyToolContainerPanel>();
        Set(child, "_group", child.GetComponent<CanvasGroup>()); Set(child, "_isOpen", true);
        Call(child, "RegisterEscapeHandler");
        _pause.HandleEscapePressed();
        Assert.IsFalse((bool)Get(child, "_isOpen")); Assert.AreEqual(0f, child.GetComponent<CanvasGroup>().alpha);
        Assert.AreEqual(UIOverlayId.PlayerPanelHub, _manager.TopOverlayId); AssertNotPaused();
        Press(); Assert.IsFalse(_manager.HasOpenOverlays); AssertNotPaused();
    }

    [Test] public void PurchaseConfirmationCancelsBeforeShopAndDoesNotSpendItems()
    {
        var parent = Open(UIOverlayId.ShopHub);
        var child = Go("Temporary purchase", parent.transform).AddComponent<ShopPurchaseTradePanel>();
        Set(child, "_isOpen", true); Call(child, "RegisterEscapeHandler");
        _pause.HandleEscapePressed();
        Assert.IsFalse((bool)Get(child, "_isOpen")); Assert.IsFalse(child.gameObject.activeSelf);
        Assert.AreEqual(UIOverlayId.ShopHub, _manager.TopOverlayId); AssertNotPaused();
        Press(); Assert.IsFalse(_manager.HasOpenOverlays); AssertNotPaused();
    }

    [TestCase(false)] [TestCase(true)]
    public void StageSelectCanCloseWithoutActiveOverlayManager(bool disabledManager)
    {
        var panel = Panel(UIOverlayId.StageSelect); panel.Show();
        var stage = panel.gameObject.AddComponent<HideoutStageSelectPanel>();
        Set(stage, "_overlay", panel); Set(stage, "_isOpen", true);
        Set(stage, "_overlayManager", disabledManager ? _manager : null);
        _manager.enabled = !disabledManager; Call(stage, "RegisterEscapeHandler");
        _pause.HandleEscapePressed();
        Assert.IsFalse((bool)Get(stage, "_isOpen")); Assert.AreEqual(0f, panel.GetComponent<CanvasGroup>().alpha);
        AssertNotPaused();
    }

    [Test] public void UnmanagedLockedLootCanCloseWithoutConsumingAKey()
    {
        var panel = Panel(UIOverlayId.LockedLootKey); panel.Show();
        var locked = panel.gameObject.AddComponent<LockedLootKeyPanel>();
        Set(locked, "_overlay", panel); Set(locked, "_isOpen", true);
        Call(locked, "RegisterEscapeHandler"); _pause.HandleEscapePressed();
        Assert.IsFalse((bool)Get(locked, "_isOpen")); Assert.AreEqual(0f, panel.GetComponent<CanvasGroup>().alpha);
        AssertNotPaused();
    }

    [Test] public void HiddenNestedPopupDoesNotBlockPause()
    {
        var hiddenParent = Go("Hidden inventory"); hiddenParent.GetComponent<CanvasGroup>().alpha = 0;
        var child = Go("Hidden key tool", hiddenParent.transform).AddComponent<KeyToolContainerPanel>();
        Set(child, "_isOpen", true); Call(child, "RegisterEscapeHandler");
        _pause.HandleEscapePressed(); Assert.IsTrue(GamePause.IsPaused);
        Assert.IsTrue((bool)Get(child, "_isOpen"));
    }

    [Test] public void KeyToolInCoveredInventoryDoesNotStealEscapeFromFrontShop()
    {
        var inventory = Open(UIOverlayId.PlayerPanelHub);
        var child = Go("Covered key tool", inventory.transform).AddComponent<KeyToolContainerPanel>();
        Set(child, "_group", child.GetComponent<CanvasGroup>()); Set(child, "_isOpen", true);
        Call(child, "RegisterEscapeHandler"); var shop = Open(UIOverlayId.ShopHub);
        _pause.HandleEscapePressed();
        Assert.AreEqual(0f, shop.GetComponent<CanvasGroup>().alpha);
        Assert.IsTrue((bool)Get(child, "_isOpen")); Assert.AreEqual(UIOverlayId.PlayerPanelHub, _manager.TopOverlayId);
        Press(); Assert.IsFalse((bool)Get(child, "_isOpen"));
        Assert.AreEqual(UIOverlayId.PlayerPanelHub, _manager.TopOverlayId); AssertNotPaused();
    }

    [Test] public void DisabledAndDestroyedPopupRegistrationsAreIgnored()
    {
        var disabled = Go("Disabled popup").AddComponent<KeyToolContainerPanel>(); disabled.enabled = false;
        var destroyed = Go("Destroyed popup").AddComponent<KeyToolContainerPanel>();
        UIEscapeRouter.Register(disabled, () => true, () => Assert.Fail("Disabled popup was closed"), 100);
        UIEscapeRouter.Register(destroyed, () => true, () => Assert.Fail("Destroyed popup was closed"), 100);
        Object.DestroyImmediate(destroyed.gameObject);
        _pause.HandleEscapePressed(); Assert.IsTrue(GamePause.IsPaused);
    }

    [TestCase(false)] [TestCase(true)]
    public void ReentrantHideCallbackCannotOpenPauseEvenWhenManagerRegistrationIsMissing(bool missingRegistration)
    {
        var panel = Open(UIOverlayId.StageSelect);
        if (missingRegistration) UIEscapeRouter.Unregister(_manager);
        var evt = new UnityEvent(); evt.AddListener(_pause.HandleEscapePressed); Set(panel, "_onHidden", evt);
        _pause.HandleEscapePressed(); Assert.IsFalse(_manager.HasOpenOverlays); AssertNotPaused();
    }

    [Test] public void PauseConfirmationThenSettingsThenPauseMenuCloseInOrder()
    {
        _pause.HandleEscapePressed(); Call(_pause, "OpenSettings"); Set(_pause, "_confirmationOpen", true);
        Press(); Assert.IsFalse((bool)Get(_pause, "_confirmationOpen")); Assert.IsTrue((bool)Get(_pause, "_settingsOpen"));
        Assert.IsTrue(GamePause.IsPaused);
        Press(); Assert.IsFalse((bool)Get(_pause, "_settingsOpen")); Assert.IsTrue(GamePause.IsPaused);
        Press(); AssertNotPaused();
    }

    private void AssertNotPaused() { Assert.IsFalse(GamePause.IsPaused); Assert.IsFalse((bool)Get(_pause, "_isOpen")); }
    private void Press() { NextPress(); _pause.HandleEscapePressed(); }
    private static void NextPress() => StaticSet(typeof(UIEscapeRouter), "_consumedFrame", -1);
    private UIOverlayPanel Open(UIOverlayId id) { var panel = Panel(id); _manager.RegisterPanel(panel); _manager.Open(id); return panel; }
    private UIOverlayPanel Panel(UIOverlayId id)
    {
        var go = Go("Temporary " + id); var panel = go.AddComponent<UIOverlayPanel>();
        panel.ConfigureRuntime(id, go.GetComponent<CanvasGroup>(), true, true); return panel;
    }
    private GameObject Go(string name, Transform parent = null)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup)) { hideFlags = HideFlags.HideAndDontSave };
        SceneManager.MoveGameObjectToScene(go, _preview); if (parent != null) go.transform.SetParent(parent, false); return go;
    }
    private static FieldInfo Field(Type type, string name) => type.GetField(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
        ?? throw new MissingFieldException(type.Name, name);
    private static object Get(object obj, string name) => Field(obj.GetType(), name).GetValue(obj);
    private static void Set(object obj, string name, object value) => Field(obj.GetType(), name).SetValue(obj, value);
    private static object StaticGet(Type type, string name) => Field(type, name).GetValue(null);
    private static void StaticSet(Type type, string name, object value) => Field(type, name).SetValue(null, value);
    private static void Call(object obj, string name) => obj.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(obj, null);
}
