using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class MakerSelectionUiStateTests
{
    private ShooterCustomizeSO _a, _b;
    [SetUp] public void Setup()
    {
        _a = ScriptableObject.CreateInstance<ShooterCustomizeSO>(); _a.name = "Raider_AngryEye1";
        _b = ScriptableObject.CreateInstance<ShooterCustomizeSO>(); _b.name = "Raider_EyeClosed";
    }
    [TearDown] public void Cleanup()
    {
        Object.DestroyImmediate(_a); Object.DestroyImmediate(_b);
    }

    [Test] public void FirstDrawUsesSelectedObjectNameInsteadOfAnOldDraft()
    {
        Assert.That(new MakerSelectionUiState().NameFor(_b, "Old name"), Is.EqualTo(_b.name));
    }
    [Test] public void SwitchingObjectsResetsNameEvenWhenTheirOriginalNamesMatch()
    {
        var state = new MakerSelectionUiState(); _b.name = _a.name;
        state.NameFor(_a, "");
        Assert.That(state.NameFor(_b, "Pending rename"), Is.EqualTo(_b.name));
    }
    [Test] public void SameObjectKeepsTypingIncludingAnEmptyDraft()
    {
        var state = new MakerSelectionUiState(); state.NameFor(_a, "");
        Assert.That(state.NameFor(_a, "Pending rename"), Is.EqualTo("Pending rename"));
        Assert.That(state.NameFor(_a, ""), Is.Empty);
    }
    [Test] public void ReclickingSameObjectRestoresItsCurrentName()
    {
        var state = new MakerSelectionUiState(); state.NameFor(_a, ""); state.SelectionClicked();
        Assert.That(state.NameFor(_a, "Unapplied draft"), Is.EqualTo(_a.name));
    }
    [Test] public void ExternalRenameRefreshesTheNameField()
    {
        var state = new MakerSelectionUiState(); state.NameFor(_a, ""); _a.name = "Renamed externally";
        Assert.That(state.NameFor(_a, "Old cached input"), Is.EqualTo(_a.name));
    }
    [Test] public void MissingSelectionClearsTheOldName()
    {
        var state = new MakerSelectionUiState(); state.NameFor(_a, "");
        Assert.That(state.NameFor(null, "Old input"), Is.Empty);
    }
    [Test] public void DestroyedSelectionIsHandledAsMissing()
    {
        var state = new MakerSelectionUiState(); state.NameFor(_a, ""); Object.DestroyImmediate(_a);
        Assert.That(state.NameFor(_a, "Old input"), Is.Empty);
    }
    [Test] public void AuthoredNameCanDifferFromTheOwnerAssetFileName()
    {
        var state = new MakerSelectionUiState();
        Assert.That(state.NameFor(_a, "Old input", "Loot_Box_Prefab"), Is.EqualTo("Loot_Box_Prefab"));
        Assert.That(state.NameFor(_a, "Typed prefab name", "Loot_Box_Prefab"), Is.EqualTo("Typed prefab name"));
        Assert.That(state.NameFor(_a, "Typed prefab name", "Renamed_Prefab"), Is.EqualTo("Renamed_Prefab"));
    }
    [Test] public void DifferentRecordsInOneAssetHaveIndependentSelectionContexts()
    {
        var state = new MakerSelectionUiState();
        Assert.That(state.Synchronize(_a, "group:1"), Is.True);
        Assert.That(state.Synchronize(_a, "group:1"), Is.False);
        Assert.That(state.Synchronize(_a, "group:2"), Is.True);
        Assert.That(state.Synchronize(_b, "group:2"), Is.True);
    }
    [Test] public void PlainRecordOwnersDoNotResetOnEveryDraw()
    {
        var state = new MakerSelectionUiState(); var record = new object();
        Assert.That(state.Synchronize(record), Is.True);
        Assert.That(state.Synchronize(record), Is.False);
        Assert.That(state.Synchronize(new object()), Is.True);
    }
    [Test] public void SeparateWindowsDoNotShareDraftOrSelectionState()
    {
        var item = new MakerSelectionUiState(); var loot = new MakerSelectionUiState();
        item.NameFor(_a, ""); loot.NameFor(_b, ""); loot.SelectionClicked();
        Assert.That(item.NameFor(_a, "Item draft"), Is.EqualTo("Item draft"));
        Assert.That(loot.NameFor(_b, "Loot draft"), Is.EqualTo(_b.name));
    }
    [Test] public void NameSynchronizationNeverModifiesTheOwnerData()
    {
        string a = EditorJsonUtility.ToJson(_a), b = EditorJsonUtility.ToJson(_b);
        var state = new MakerSelectionUiState(); state.NameFor(_a, "Old input");
        state.NameFor(_b, "New draft"); state.SelectionClicked(); state.NameFor(_b, "Another draft");
        Assert.That(EditorJsonUtility.ToJson(_a), Is.EqualTo(a)); Assert.That(EditorJsonUtility.ToJson(_b), Is.EqualTo(b));
    }
    [Test] public void SelectionCallbacksOutsideOnGuiDeferTheFocusReset()
    {
        if (Event.current != null) Assert.Ignore("This test must run outside OnGUI.");
        var state = new MakerSelectionUiState(); state.SelectionClicked();
        Assert.That(state.NameFor(_b, "Old draft"), Is.EqualTo(_b.name));
        var pending = typeof(MakerSelectionUiState).GetField("_pendingFocusReset", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Assert.That(pending.GetValue(state), Is.True);
    }

    [Test] public void PreviousDelayedCommitIsDiscardedAfterSelectionChanges()
    {
        var state = StateWithStaleDelayedInput();
        Assert.That(DiscardDelayed(state, new Event { type = EventType.ExecuteCommand, commandName = "DelayedControlShouldCommit" }), Is.True);
        Assert.That(DiscardDelayed(state, new Event { type = EventType.ExecuteCommand, commandName = "DelayedControlShouldCommit" }), Is.False);
    }

    [Test] public void NewInputAllowsNormalDelayedCommitsOnTheSelectedObject()
    {
        var state = StateWithStaleDelayedInput();
        Assert.That(DiscardDelayed(state, new Event { type = EventType.KeyDown, keyCode = KeyCode.Return }), Is.False);
        Assert.That(DiscardDelayed(state, new Event { type = EventType.ExecuteCommand, commandName = "DelayedControlShouldCommit" }), Is.False);
    }

    private static MakerSelectionUiState StateWithStaleDelayedInput()
    {
        var state = new MakerSelectionUiState();
        typeof(MakerSelectionUiState).GetField("_discardDelayedCommit", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(state, true);
        return state;
    }
    private static bool DiscardDelayed(MakerSelectionUiState state, Event evt) => (bool)typeof(MakerSelectionUiState)
        .GetMethod("ShouldDiscardDelayedCommit", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(state, new object[] { evt, false });
}
