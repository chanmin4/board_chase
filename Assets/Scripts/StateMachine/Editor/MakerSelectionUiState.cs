using System;
using UnityEditor;
using UnityEngine;

/// <summary>Editor-only selection/input state. Never renames, saves or modifies the selected data.</summary>
public sealed class MakerSelectionUiState
{
    private object _owner;
    private string _key, _sourceName;
    private bool _initialized, _selectionClicked, _pendingFocusReset;
    private bool _discardDelayedCommit;

    // Selection callbacks may run outside OnGUI. Defer GUI work until the selected fields are drawn.
    public void SelectionClicked() => _selectionClicked = _pendingFocusReset = true;

    public bool Synchronize(object owner, string key = "")
    {
        bool changed = SynchronizeState(owner, key);
        FlushPendingFocus();
        return changed;
    }

    private bool SynchronizeState(object owner, string key)
    {
        if (owner is UnityEngine.Object unityOwner && unityOwner == null) owner = null;
        bool sameOwner = _owner is UnityEngine.Object previous && owner is UnityEngine.Object current
            ? previous == current : ReferenceEquals(_owner, owner);
        bool changed = !_initialized || _selectionClicked || !sameOwner || _key != key;
        if (changed)
        {
            _owner = owner; _key = key; _initialized = true; _selectionClicked = false;
            _pendingFocusReset = true;
        }
        return changed;
    }

    // Preserve typing on the same object. Reset on selection/re-selection or an actual source rename.
    public string NameFor(UnityEngine.Object selected, string draft, string authoredName = null)
    {
        string currentName = selected != null ? authoredName ?? selected.name : string.Empty;
        bool changed = SynchronizeState(selected, "");
        if (changed || !string.Equals(_sourceName, currentName, StringComparison.Ordinal))
        {
            draft = currentName; _sourceName = currentName; _pendingFocusReset = true;
        }
        FlushPendingFocus();
        return draft ?? string.Empty;
    }

    private void FlushPendingFocus()
    {
        var evt = Event.current;
        if (evt == null) return;
        bool reset = _pendingFocusReset;
        if (reset)
        {
            // IMGUI reuses text-control IDs. Otherwise its active text editor can write the old
            // object's text back into the newly selected object's field, even after a draft reset.
            GUI.FocusControl(null);
            GUIUtility.keyboardControl = 0;
            EditorGUIUtility.editingTextField = false;
            _pendingFocusReset = false;
            _discardDelayedCommit = true;
        }
        if (ShouldDiscardDelayedCommit(evt, reset)) evt.Use();
    }

    private bool ShouldDiscardDelayedCommit(Event evt, bool reset)
    {
        // Unity's delayed editor sends this command after losing focus, including at EndGUI.
        // The old control ID can now belong to the new object. Consume that stale commit only;
        // ordinary Enter/click commits remain enabled once the new object receives input.
        if (_discardDelayedCommit && evt.type == EventType.ExecuteCommand && evt.commandName == "DelayedControlShouldCommit")
        {
            _discardDelayedCommit = false;
            return true;
        }
        else if (!reset && (evt.type == EventType.MouseDown || evt.type == EventType.KeyDown))
            _discardDelayedCommit = false;
        return false;
    }
}
