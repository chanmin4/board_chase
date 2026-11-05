using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public sealed class SectorHatchTests
{
    private Scene _preview;
    private readonly List<Object> _assets = new();
    private GameObject _root, _closed, _open, _actor;
    private SectorHatch _hatch;
    private RaidStation _station;
    private RaidRuntime _raid;
    private SectorRuntime _sector;
    private RaidRunSaveData _state;
    private InteractionManager _manager;
    private InteractionPromptSnapshot _prompt;
    private Transform _anchor;

    [SetUp] public void Setup()
    {
        _preview = EditorSceneManager.NewPreviewScene();
        var sectorGo = Go("Temporary sector");
        _sector = sectorGo.AddComponent<SectorRuntime>(); _sector.coord = new Vector2Int(7, 7);
        var states = Go("Temporary sector state").AddComponent<SectorStateManager>();
        Set(states, "_isInitialized", true);
        Set(states, "<CurrentSector>k__BackingField", _sector);
        ((Dictionary<SectorRuntime, Vector2Int>)Get(states, "_coordBySector")).Add(_sector, _sector.coord);
        _raid = Go("Temporary raid").AddComponent<RaidRuntime>();
        _state = new RaidRunSaveData { active = true, layout = new StageMapLayout
            { hatchRoomCoord = _sector.coord, generatorRoomCoord = _sector.coord, generationVersion = 3 } };
        Set(_raid, "<State>k__BackingField", _state); Set(_raid, "_sectors", states); Set(_raid, "_ready", true);
        Set(_raid, "<Definition>k__BackingField", Asset<StageDefinitionSO>());
        _root = Go("Temporary hatch", _sector.transform);
        _closed = Go("Closed", _root.transform); _open = Go("Open", _root.transform);
        _hatch = _root.AddComponent<SectorHatch>(); _station = _root.GetComponent<RaidStation>();
        Set(_hatch, "_closedVisual", _closed); Set(_hatch, "_openedVisual", _open);
        var trigger = Go("Pre-placed interaction trigger", _root.transform).AddComponent<BoxCollider>();
        trigger.gameObject.layer = LayerMask.NameToLayer("ObjectItem");
        trigger.isTrigger = true; trigger.center = new Vector3(0, .6f, 0); trigger.size = new Vector3(4, 2, 4);
        Set(_hatch, "_interactionTrigger", trigger);
        var anchorComponent = _root.AddComponent<InteractionPromptAnchor>();
        _anchor = Go("Anchor", _root.transform).transform; Set(anchorComponent, "_anchor", _anchor);
        Set(anchorComponent, "_interactDot", Go("Dot", _root.transform));
        Call(_hatch, "Awake"); _station.Configure(_raid, _sector, RaidStationKind.Hatch);
        _actor = Go("Temporary actor"); _actor.AddComponent<PlayerInventoryRuntime>();
        _manager = _actor.AddComponent<InteractionManager>();
        var channel = Asset<InteractionPromptEventChannelSO>(); channel.OnEventRaised += p => _prompt = p;
        Set(_manager, "_interactionPromptChannel", channel); Set(_manager, "_interactionKeyLabel", "F");
        Call(_manager, "OnEnable");
    }
    [TearDown] public void Cleanup()
    {
        if (_manager != null) Call(_manager, "OnDisable");
        if (_hatch != null) Call(_hatch, "OnDisable");
        if (_preview.IsValid()) EditorSceneManager.ClosePreviewScene(_preview);
        foreach (var asset in _assets) if (asset != null) Object.DestroyImmediate(asset);
        _assets.Clear();
    }

    [Test] public void OneComponentAddsTheExistingInteractionStation()
    { Assert.NotNull(_station); Assert.That(_station.Kind, Is.EqualTo(RaidStationKind.Hatch)); }

    [Test] public void UnpoweredHatchShowsClosedVisualAndPowerPrompt()
    {
        Assert.IsFalse(_hatch.IsOpen); Assert.IsTrue(_closed.activeSelf); Assert.IsFalse(_open.activeSelf);
        Assert.That(_station.PromptLabel, Is.EqualTo("전력이 필요합니다"));
        Assert.IsTrue(_station.CanInteract, "Locked hatches must still publish an interaction hint.");
    }

    [Test] public void PowerEventSwitchesVisualsAndLabel()
    {
        Power(true);
        Assert.IsTrue(_hatch.IsOpen); Assert.IsFalse(_closed.activeSelf); Assert.IsTrue(_open.activeSelf);
        Assert.That(_station.PromptLabel, Is.EqualTo("탈출"));
        Assert.IsTrue(_root.activeSelf); Assert.IsTrue(_hatch.InteractionTrigger.gameObject.activeInHierarchy);
    }

    [Test] public void RestoredPowerIsAppliedDuringConfigureWithoutWaitingForANewEvent()
    {
        _state.generatorPowered = true; _station.Configure(_raid, _sector, RaidStationKind.Hatch);
        Assert.IsTrue(_open.activeSelf); Assert.IsFalse(_closed.activeSelf);
    }

    [Test] public void ExistingAnchorAndConfiguredKeyAreUsed()
    {
        Enter();
        Assert.IsTrue(_prompt.Visible); Assert.That(_prompt.ActionLabel, Is.EqualTo("전력이 필요합니다"));
        Assert.That(_prompt.KeyLabel, Is.EqualTo("F")); Assert.AreSame(_anchor, _prompt.Anchor);
    }

    [Test] public void PowerChangeRefreshesSelectedPromptWithoutLeavingTheTrigger()
    {
        Enter(); Power(true);
        Assert.IsTrue(_prompt.Visible); Assert.That(_prompt.ActionLabel, Is.EqualTo("탈출"));
        Assert.That(_prompt.KeyLabel, Is.EqualTo("F")); Assert.AreSame(_anchor, _prompt.Anchor);
    }

    [Test] public void LockedKeyPressDoesNotExtractOrHideTheHint()
    {
        Enter(); Call(_manager, "ExecuteInteraction", new Interaction(InteractionType.RaidStation, _root));
        Assert.IsFalse(_state.extractionCommitted); Assert.IsTrue(_prompt.Visible);
        Assert.That(_prompt.ActionLabel, Is.EqualTo("전력이 필요합니다"));
    }

    [Test] public void HatchExtractionRequiresPowerAndCorrectCoordinateAndRejectsDuplicates()
    {
        Assert.IsFalse(RaidRuntime.AllowsExtraction(_state, _sector.coord, RaidStationKind.Hatch));
        Power(true); Assert.IsTrue(RaidRuntime.AllowsExtraction(_state, _sector.coord, RaidStationKind.Hatch));
        Assert.IsFalse(RaidRuntime.AllowsExtraction(_state, _sector.coord + Vector2Int.right, RaidStationKind.Hatch));
        _state.extractionCommitted = true;
        Assert.IsFalse(RaidRuntime.AllowsExtraction(_state, _sector.coord, RaidStationKind.Hatch));
    }

    [Test] public void RejectedGeneratorDoesNotPublishPowerOrOpenTheHatch()
    {
        int changed = 0; _raid.StateChanged += () => changed++;
        Assert.IsFalse(_raid.TryPowerGenerator(_actor.GetComponent<PlayerInventoryRuntime>()));
        Assert.AreEqual(0, changed); Assert.IsFalse(_state.generatorPowered); Assert.IsFalse(_hatch.IsOpen);
    }

    [Test] public void ActualGeneratorTransactionConsumesBatteryAndOpensExistingHatchObjectsOnce()
    {
        Assert.IsFalse(Application.isPlaying, "Preview checks must never touch an active real run.");
        var realSave = SaveSystem.Active;
        string originalRun = realSave != null ? JsonUtility.ToJson(realSave.RunData) : null;
        var definition = (StageDefinitionSO)Get(_raid, "<Definition>k__BackingField");
        var battery = Asset<ValuableItemSO>(); definition.RaidRuntime.generatorBattery = battery;
        var stack = new PlayerInventoryItemStack(); stack.Set(battery, 2);
        var inventory = _actor.GetComponent<PlayerInventoryRuntime>();
        Set(inventory, "_items", new List<PlayerInventoryItemStack> { stack });
        int children = _root.transform.childCount;
        Assert.IsTrue(_raid.TryPowerGenerator(inventory));
        Assert.IsTrue(_state.generatorPowered); Assert.IsTrue(_state.hatchRevealed);
        Assert.IsTrue(_hatch.IsOpen); Assert.IsFalse(_closed.activeSelf); Assert.IsTrue(_open.activeSelf);
        Assert.IsTrue(_hatch.InteractionTrigger.gameObject.activeSelf);
        Assert.AreEqual(1, inventory.GetInventoryItemAmount(battery));
        Assert.IsFalse(_raid.TryPowerGenerator(inventory));
        Assert.AreEqual(1, inventory.GetInventoryItemAmount(battery)); Assert.AreEqual(children, _root.transform.childCount);
        Assert.AreSame(realSave, SaveSystem.Active);
        if (realSave != null) Assert.AreEqual(originalRun, JsonUtility.ToJson(realSave.RunData));
    }

    [Test] public void AuthoredTriggerIsSeparateFromVisualsAndUsesObjectItemLayer()
    {
        var trigger = _hatch.InteractionTrigger;
        Assert.IsTrue(trigger.isTrigger); Assert.AreEqual(LayerMask.NameToLayer("ObjectItem"), trigger.gameObject.layer);
        Assert.IsFalse(trigger.transform.IsChildOf(_closed.transform)); Assert.IsFalse(trigger.transform.IsChildOf(_open.transform));
    }

    [Test] public void DisableUnsubscribesAndReenableSynchronizesLatestState()
    {
        Call(_hatch, "OnDisable"); _state.generatorPowered = true; Call(_raid, "NotifyStateChanged");
        Assert.IsFalse(_hatch.IsOpen); Call(_hatch, "OnEnable"); Assert.IsTrue(_hatch.IsOpen);
    }

    [Test] public void ProjectObjectsAreNotInstantiatedOrModified()
    {
        var extra = Go("Prefab-backed hatch", _sector.transform); var hatch = extra.AddComponent<SectorHatch>();
        var closed = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath("ce9471baa61d1f246b40065ab83e82f8"));
        var opened = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath("3f85e7916aa2b4847855903335a153d8"));
        Assert.NotNull(closed); Assert.NotNull(opened);
        bool beforeClosed = closed.activeSelf, beforeOpen = opened.activeSelf;
        var trigger = Go("Prefab hatch trigger", extra.transform).AddComponent<BoxCollider>();
        trigger.isTrigger = true; Set(hatch, "_interactionTrigger", trigger);
        Set(hatch, "_closedVisual", closed); Set(hatch, "_openedVisual", opened);
        int children = extra.transform.childCount;
        Assert.IsFalse(hatch.ValidateVisuals(out string problem));
        Assert.That(problem, Does.Contain("already placed"));
        extra.GetComponent<RaidStation>().Configure(_raid, _sector, RaidStationKind.Hatch);
        Power(true);
        Assert.IsTrue(trigger.enabled); Assert.IsTrue(trigger.gameObject.activeSelf);
        Call(hatch, "OnDisable"); Call(hatch, "OnEnable"); hatch.RefreshPresentation();
        Assert.AreEqual(children, extra.transform.childCount);
        Assert.AreEqual(beforeClosed, closed.activeSelf); Assert.AreEqual(beforeOpen, opened.activeSelf);
        Call(hatch, "OnDisable");
    }

    [Test] public void FactoryPrefersAuthoredHatchOverLegacyPrototypeStation()
    {
        var legacy = Go("Legacy terminal", _sector.transform).AddComponent<RaidStation>();
        _root.transform.SetAsLastSibling();
        RaidStationFactory.Attach(_raid, _sector, new StageRoomNode(_sector.coord, StageRoomType.Normal, StageRoomConnectionFlags.None) { raidRole = RaidRoomRole.Hatch },
            new RaidRuntimeSettings());
        Assert.AreSame(_raid, _station.Runtime); Assert.IsNull(legacy.Runtime);
    }

    [Test] public void BoxGeometryAndTriggerReferenceSurviveClosedOpenSwitches()
    {
        var box = (BoxCollider)_hatch.InteractionTrigger;
        box.center = new Vector3(.25f, .8f, -.2f); box.size = new Vector3(5, 2, 6);
        Vector3 center = box.center, size = box.size; int children = _root.transform.childCount;
        Power(true); Power(false); Power(true);
        Assert.AreSame(box, _hatch.InteractionTrigger);
        Assert.AreEqual(center, box.center); Assert.AreEqual(size, box.size);
        Assert.AreEqual(children, _root.transform.childCount); Assert.IsTrue(box.gameObject.activeSelf);
    }

    [Test] public void SphereUsesOnlyItsAuthoredRadiusAndCenter()
    {
        var sphere = Go("Authored sphere", _root.transform).AddComponent<SphereCollider>();
        sphere.isTrigger = true; sphere.radius = 4.5f; sphere.center = new Vector3(.4f, 1, -.7f);
        Set(_hatch, "_interactionTrigger", sphere);
        Power(true); Power(false);
        Assert.IsTrue(_hatch.HasValidInteractionTrigger); Assert.AreEqual(4.5f, sphere.radius);
        Assert.AreEqual(new Vector3(.4f, 1, -.7f), sphere.center);
    }

    [Test] public void MissingTriggerHasNoRuntimeFallbackAndCannotInteract()
    {
        Set(_hatch, "_interactionTrigger", null); int children = _root.transform.childCount;
        Assert.IsFalse(_hatch.ValidateInteractionTrigger(out string problem));
        Assert.That(problem, Does.Contain("not assigned"));
        _hatch.RefreshPresentation(); Power(true);
        Assert.IsNull(_hatch.InteractionTrigger); Assert.AreEqual(children, _root.transform.childCount);
        Assert.IsFalse(_station.CanInteract);
    }

    [Test] public void VisualChildColliderCannotBeTheInteractionTrigger()
    {
        var trigger = Go("Wrong trigger inside visual", _closed.transform).AddComponent<BoxCollider>();
        trigger.isTrigger = true; Set(_hatch, "_interactionTrigger", trigger);
        Assert.IsFalse(_hatch.HasValidInteractionTrigger); Assert.IsFalse(_station.CanInteract);
        Assert.IsFalse(_hatch.ValidateInteractionTrigger(out string problem));
        Assert.That(problem, Does.Contain("visual roots"));
    }

    [Test] public void NonTriggerDisabledAndInactiveCollidersAreRejectedWithoutMutation()
    {
        var trigger = _hatch.InteractionTrigger;
        trigger.isTrigger = false; Assert.IsFalse(_hatch.HasValidInteractionTrigger);
        trigger.isTrigger = true; trigger.enabled = false; Assert.IsFalse(_hatch.HasValidInteractionTrigger);
        trigger.enabled = true; trigger.gameObject.SetActive(false); Assert.IsFalse(_hatch.HasValidInteractionTrigger);
        Assert.IsFalse(trigger.gameObject.activeSelf);
        trigger.gameObject.SetActive(true); Assert.IsTrue(_hatch.HasValidInteractionTrigger);
        var external = Go("External collider").AddComponent<BoxCollider>();
        external.isTrigger = true; Set(_hatch, "_interactionTrigger", external);
        Assert.IsFalse(_hatch.HasValidInteractionTrigger);
    }

    [Test] public void RootTriggerIsAllowedAndNeverToggledByVisualSwitches()
    {
        var trigger = _root.AddComponent<BoxCollider>(); trigger.isTrigger = true;
        Set(_hatch, "_interactionTrigger", trigger);
        Assert.IsTrue(_hatch.HasValidInteractionTrigger); Power(true);
        Assert.IsTrue(_root.activeSelf); Assert.IsTrue(trigger.enabled); Assert.IsTrue(_station.CanInteract);
    }

    [Test] public void InspectorUsesColliderGeometryWithoutDuplicateRadiusFields()
    {
        var data = new SerializedObject(_hatch);
        Assert.IsNull(data.FindProperty("_interactionRadius")); Assert.IsNull(data.FindProperty("_interactionCenter"));
        Assert.AreSame(_hatch.InteractionTrigger, data.FindProperty("_interactionTrigger").objectReferenceValue);
        var editor = Editor.CreateEditor(_hatch);
        try { Assert.That(editor, Is.InstanceOf<SectorHatchEditor>()); }
        finally { Object.DestroyImmediate(editor); }
        var colliderData = new SerializedObject(_hatch.InteractionTrigger);
        colliderData.FindProperty("m_Center").vector3Value = new Vector3(1, 2, 3);
        colliderData.FindProperty("m_Size").vector3Value = new Vector3(5, 6, 7);
        colliderData.ApplyModifiedPropertiesWithoutUndo();
        Assert.AreEqual(new Vector3(1, 2, 3), ((BoxCollider)_hatch.InteractionTrigger).center);
        Assert.AreEqual(new Vector3(5, 6, 7), ((BoxCollider)_hatch.InteractionTrigger).size);
    }

    [Test] public void AlreadyPlacedPrefabReferencesAreReusedWithoutSpawningDuplicates()
    {
        int children = _root.transform.childCount;
        Assert.IsTrue(_hatch.ValidateVisuals(out string problem), problem);
        Power(true); Power(false); Call(_hatch, "OnDisable"); Call(_hatch, "OnEnable");
        Assert.AreSame(_closed, Get(_hatch, "_closedVisual")); Assert.AreSame(_open, Get(_hatch, "_openedVisual"));
        Assert.IsTrue(_closed.activeSelf); Assert.IsFalse(_open.activeSelf);
        Assert.IsNull(typeof(SectorHatch).GetMethod("ResolveVisual", BindingFlags.Instance | BindingFlags.NonPublic));
        Assert.IsNull(typeof(SectorHatch).GetField("_closedInstance", BindingFlags.Instance | BindingFlags.NonPublic));
        Assert.AreEqual(children, _root.transform.childCount);
    }

    [Test] public void SameSelfAndNestedVisualReferencesAreRejectedButPlacedObjectsAreAllowed()
    {
        Set(_hatch, "_openedVisual", _closed); Assert.IsFalse(_hatch.ValidateVisuals(out _));
        Set(_hatch, "_openedVisual", _open); Set(_hatch, "_closedVisual", _root);
        Assert.IsFalse(_hatch.ValidateVisuals(out _));
        var foreign = Go("Another room visual"); Set(_hatch, "_closedVisual", foreign);
        Assert.IsTrue(_hatch.ValidateVisuals(out _), "Any explicitly assigned, already-placed object can be toggled; it need not be a child.");
        Set(_hatch, "_closedVisual", _closed); _open.transform.SetParent(_closed.transform, false);
        Assert.IsFalse(_hatch.ValidateVisuals(out _));
    }

    [Test] public void PromptComponentsInsideVisualsAreRejected()
    {
        _closed.AddComponent<InteractionPromptAnchor>(); Assert.IsFalse(_hatch.ValidateVisuals(out string anchorProblem));
        Assert.That(anchorProblem, Does.Contain("InteractionPromptAnchor"));
    }

    private void Enter() => _manager.OnTriggerChangeDetected(true, _hatch.InteractionTrigger.gameObject);
    private void Power(bool value) { _state.generatorPowered = value; Call(_raid, "NotifyStateChanged"); }
    private GameObject Go(string name, Transform parent = null)
    {
        var go = EditorUtility.CreateGameObjectWithHideFlags(name, HideFlags.HideAndDontSave);
        SceneManager.MoveGameObjectToScene(go, _preview);
        if (parent != null) go.transform.SetParent(parent, false); return go;
    }
    private T Asset<T>() where T : ScriptableObject
    { var asset = ScriptableObject.CreateInstance<T>(); asset.hideFlags = HideFlags.HideAndDontSave; _assets.Add(asset); return asset; }
    private static object Get(object target, string field) => target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
    private static void Set(object target, string field, object value) => target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    private static object Call(object target, string method, params object[] args) =>
        target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, args);
}
