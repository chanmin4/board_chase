using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public sealed partial class LootProgressionEditorWindow
{
    private GameObject _registerPrefab;
    private LootTableSO _worldPoolClipboard;
    private string _worldPoolClipboardLabel;
    private bool _registeringPrefabs;
    private string _registrationStatus;
    private LootObjectSO _namingBox;
    private string _prefabNameDraft;
    private MakerSelectionUiState _containerNameUi;
    private MakerSelectionUiState ContainerNameUi => _containerNameUi ??= new MakerSelectionUiState();
    private string _namingPrefabPath;
    private bool _renamePending;
    private LootObjectSO _containerDragCandidate;
    private Vector2 _containerDragStart;
    private int _containerDragControl;
    private LootContainerCategory? _containerDropHover;
    private bool _categoryMovePending;
    private bool _deletePending;
    private LootObjectSO _deleteBox;
    private LootObjectSO _deleteReplacement;
    private LootMakerDeletion.Usage _deleteUsage;
    private Vector2 _deleteReferenceScroll;
    private string _deleteError;
    private Action _deleteWork;
    private double _deleteWorkAfter;
    private int _deleteRequest;

    private void ScheduleContainerDeleteWork(Action work)
    {
        _deleteWork = work;
        // Run outside IMGUI, without depending on a later inspector/delayCall pass.
        _deleteWorkAfter = EditorApplication.timeSinceStartup + .1;
        EditorApplication.update -= PumpContainerDeleteWork;
        EditorApplication.update += PumpContainerDeleteWork;
        Repaint();
    }

    private void PumpContainerDeleteWork()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating ||
            EditorApplication.timeSinceStartup < _deleteWorkAfter) return;
        EditorApplication.update -= PumpContainerDeleteWork;
        var work = _deleteWork;
        _deleteWork = null;
        if (this != null) work?.Invoke();
    }

    private void ClearContainerDeleteReview()
    {
        ++_deleteRequest;
        EditorApplication.update -= PumpContainerDeleteWork;
        _deleteWork = null;
        _deletePending = false;
        _deleteBox = null;
        _deleteUsage = null;
        _deleteReplacement = null;
        _deleteError = null;
    }

    private void QueueContainerDeleteCheck(LootObjectSO box)
    {
        if (_deletePending || box == null) return;
        ClearContainerDeleteReview();
        int request = _deleteRequest;
        // Show the review immediately. A scan failure/cancellation must not look like a dead button.
        _deleteBox = box;
        _deleteReferenceScroll = Vector2.zero;
        _worldLootTableScroll = Vector2.zero;
        _deletePending = true;
        ScheduleContainerDeleteWork(() =>
        {
            if (this == null) return;
            try
            {
                var usage = LootMakerDeletion.FindUsage(box, true);
                if (request != _deleteRequest) return; // Explicit navigation/cancel, not a hidden selection guard.
                _deleteUsage = usage;
                var linked = box.WorldPrefab != null ? box.WorldPrefab.GetComponentInChildren<LootableObject>(true)?.LootObject : null;
                // Suggest only; no reference or asset changes until explicit confirmation.
                _deleteReplacement = linked != box ? linked : null;
            }
            catch (OperationCanceledException)
            { if (request == _deleteRequest) _deleteError = "Check cancelled. Nothing changed. Retry or go back to editing."; }
            catch (Exception ex)
            { if (request == _deleteRequest) _deleteError = ex.Message; }
            finally
            {
                if (request == _deleteRequest) _deletePending = false;
                Repaint();
            }
        });
    }

    private bool DrawContainerDeletePanel()
    {
        if (_deleteBox == null) return false;
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("DELETE REVIEW - " + GetContainerTitle(_deleteBox), EditorStyles.boldLabel);
            ObserveGuiControl("DeletePanel", GUILayoutUtility.GetLastRect());
            EditorGUILayout.LabelField(AssetDatabase.GetAssetPath(_deleteBox), EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.HelpBox("Deletes only this LootObjectSO record. Its model prefab and loot table are kept.\nFile operation, not Ctrl+Z: Recycle Bin + Library/LootMakerDeletedRecords backup.", MessageType.Warning);
            if (_deletePending)
                EditorGUILayout.HelpBox("Checking uses / waiting for confirmation... No record is deleted until you confirm the final dialog.", MessageType.Info);
            if (!string.IsNullOrEmpty(_deleteError))
                EditorGUILayout.HelpBox(_deleteError, MessageType.Warning);
            if (_deleteUsage == null)
            {
                if (!_deletePending && GUILayout.Button("Retry reference check")) QueueContainerDeleteCheck(_deleteBox);
                if (GUILayout.Button("Back to editing")) ClearContainerDeleteReview();
                ObserveGuiControl("DeleteCancel", GUILayoutUtility.GetLastRect());
                return true;
            }
            bool used = _deleteUsage.References.Count > 0;
            EditorGUILayout.LabelField($"Check complete: {_deleteUsage.References.Count} use(s), {_deleteUsage.Blockers.Count} blocker(s).", EditorStyles.boldLabel);
            ObserveGuiControl("DeleteCheckComplete", GUILayoutUtility.GetLastRect());
            if (used)
            {
                EditorGUILayout.LabelField($"{_deleteUsage.References.Count} reference(s) must be redirected before deletion.", EditorStyles.wordWrappedMiniLabel);
                _deleteReplacement = (LootObjectSO)EditorGUILayout.ObjectField("Replace uses with", _deleteReplacement, typeof(LootObjectSO), false);
                var candidates = _containerCatalog.Where(b => b != null && b != _deleteBox).ToList();
                var labels = new[] { "Choose a replacement..." }.Concat(candidates.Select(b =>
                    GetContainerTitle(b) + " | " + b.name + (b.WorldPrefab == _deleteBox.WorldPrefab ? " [same prefab]" : ""))).ToArray();
                int selected = candidates.IndexOf(_deleteReplacement) + 1;
                int next = EditorGUILayout.Popup("Existing records", selected, labels);
                if (next != selected) _deleteReplacement = next > 0 ? candidates[next - 1] : null;
                if (_deleteReplacement != null)
                    EditorGUILayout.LabelField("Replacement: " + AssetDatabase.GetAssetPath(_deleteReplacement), EditorStyles.wordWrappedMiniLabel);
                EditorGUILayout.HelpBox("These users will use the replacement's loot items, capacity and rarity settings. The old table is NOT merged or copied.", MessageType.Warning);
            }
            if (used || _deleteUsage.Blockers.Count > 0)
            {
                _deleteReferenceScroll = EditorGUILayout.BeginScrollView(_deleteReferenceScroll, GUILayout.Height(120));
                foreach (string reference in _deleteUsage.References) EditorGUILayout.LabelField(reference, EditorStyles.wordWrappedMiniLabel);
                foreach (string blocker in _deleteUsage.Blockers) EditorGUILayout.HelpBox(blocker, MessageType.Error);
                EditorGUILayout.EndScrollView();
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(_deletePending || _deleteUsage.Blockers.Count > 0 ||
                    (used && (_deleteReplacement == null || _deleteReplacement == _deleteBox))))
                    if (GUILayout.Button(used ? "Replace Links + Delete..." : "Delete Record...")) QueueContainerDelete(_deleteBox, used ? _deleteReplacement : null);
                ObserveGuiControl("DeleteConfirm", GUILayoutUtility.GetLastRect());
                if (GUILayout.Button("Cancel", GUILayout.Width(64))) ClearContainerDeleteReview();
                ObserveGuiControl("DeleteCancel", GUILayoutUtility.GetLastRect());
            }
        }
        return true;
    }

    private void QueueContainerDelete(LootObjectSO box, LootObjectSO replacement)
    {
        if (_deletePending) return;
        int request = _deleteRequest;
        _deleteError = null;
        _deletePending = true;
        ScheduleContainerDeleteWork(() =>
        {
            if (this == null) return;
            try
            {
                if (box == null) return;
                string path = AssetDatabase.GetAssetPath(box);
                string message = "Delete this LootObjectSO record?\n\n" + path + "\n\n" +
                    (replacement != null ? "Redirect its references to:\n" + AssetDatabase.GetAssetPath(replacement) +
                        "\nUsers will use THAT record's loot settings. Old loot contents are not copied.\n\n" : "") +
                    "The prefab and loot table are NOT deleted.\nRecovery: Recycle Bin and Library/LootMakerDeletedRecords. Not Ctrl+Z.";
                if (!EditorUtility.DisplayDialog("Delete Loot Record", message, replacement != null ? "Replace Links + Delete" : "Delete Record", "Cancel")) return;
                string backup = LootMakerDeletion.DeleteRecord(box, replacement, true);
                SelectContainer(null);
                RefreshWorldCatalog();
                if (replacement != null)
                {
                    _worldCategoryFilter = replacement.Category;
                    _showUnassignedVariants = !replacement.HasCategoryAssignment;
                    _worldLootObjectSearch = string.Empty;
                    SelectContainer(replacement);
                }
                Debug.Log("[Loot Maker] Deleted catalog record only: " + path + ". Prefab/table kept. Recovery backup: " + backup);
                ShowNotification(new GUIContent("Record deleted. Prefab / table kept. Backup in Library/LootMakerDeletedRecords."));
            }
            catch (OperationCanceledException)
            { if (request == _deleteRequest) _deleteError = "Deletion cancelled. Nothing changed."; }
            catch (Exception ex)
            { if (request == _deleteRequest) _deleteError = ex.Message; }
            finally
            {
                if (request == _deleteRequest) _deletePending = false;
                Repaint();
            }
        });
    }

    private void ClearContainerDrag()
    {
        if (_containerDragControl != 0 && GUIUtility.hotControl == _containerDragControl)
            GUIUtility.hotControl = 0;
        _containerDragControl = 0;
        _containerDragCandidate = null;
        _containerDropHover = null;
    }

    private void OnLostFocus() => ClearContainerDrag();

    private void HandleContainerDragCancellation()
    {
        Event evt = Event.current;
        if (evt.type == EventType.DragUpdated) _containerDropHover = null;
        if (evt.type == EventType.DragExited ||
            (evt.type == EventType.KeyDown && evt.keyCode == KeyCode.Escape))
        {
            bool captured = _containerDragCandidate != null;
            ClearContainerDrag();
            if (captured && evt.type == EventType.KeyDown) evt.Use();
            Repaint();
        }
    }

    private void HandleContainerCardInput(Rect rect, LootObjectSO box)
    {
        Event evt = Event.current;
        int control = GUIUtility.GetControlID(box.GetInstanceID(), FocusType.Passive, rect);
        if (evt.type == EventType.MouseDown && evt.button == 0 && rect.Contains(evt.mousePosition))
        {
            ClearContainerDrag();
            _containerDragCandidate = box;
            _containerDragStart = evt.mousePosition;
            _containerDragControl = control;
            GUIUtility.hotControl = control;
            GUI.FocusControl(null);
            SelectContainer(box);
            Repaint();
            evt.Use();
        }
        else if (evt.type == EventType.MouseDrag && _containerDragCandidate == box &&
                 GUIUtility.hotControl == control && (evt.mousePosition - _containerDragStart).sqrMagnitude >= 25f)
        {
            DragAndDrop.PrepareStartDrag();
            DragAndDrop.objectReferences = new UnityEngine.Object[] { box };
            DragAndDrop.paths = new[] { AssetDatabase.GetAssetPath(box) };
            // Release IMGUI capture before Unity's drag system takes over, including outside the scroll view.
            ClearContainerDrag();
            DragAndDrop.StartDrag(GetContainerTitle(box));
            evt.Use();
        }
        else if (evt.type == EventType.MouseUp && GUIUtility.hotControl == control)
        {
            ClearContainerDrag();
            evt.Use();
        }
    }

    private void HandleContainerCategoryDrop(Rect rect, LootContainerCategory? category, bool unassigned)
    {
        Event evt = Event.current;
        if (!rect.Contains(evt.mousePosition) ||
            (evt.type != EventType.DragUpdated && evt.type != EventType.DragPerform)) return;
        var payload = DragAndDrop.objectReferences;
        var boxes = payload.OfType<LootObjectSO>().Distinct().ToArray();
        bool valid = category.HasValue && !unassigned && !_categoryMovePending && boxes.Length > 0 &&
            boxes.Length == payload.Length && boxes.All(b => b != null && AssetDatabase.IsMainAsset(b) && AssetDatabase.IsOpenForEdit(b)) &&
            !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling;
        DragAndDrop.visualMode = valid ? DragAndDropVisualMode.Move : DragAndDropVisualMode.Rejected;
        _containerDropHover = valid ? category : null;
        if (evt.type == EventType.DragPerform)
        {
            if (valid)
            {
                DragAndDrop.AcceptDrag();
                QueueContainerCategoryMove(boxes, category.Value);
            }
            else ShowNotification(new GUIContent("Drag a box card to Ammo, Medical, or another category. All / Unassigned are filters only."));
            ClearContainerDrag();
        }
        Repaint();
        evt.Use();
    }

    private void QueueContainerCategoryMove(LootObjectSO[] boxes, LootContainerCategory category)
    {
        _categoryMovePending = true;
        string[] guids = boxes.Select(b => AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(b))).ToArray();
        var tab = _worldTypeDetailTab;
        EditorApplication.delayCall += () =>
        {
            if (this == null) return;
            try
            {
                var targets = guids.Select(g => AssetDatabase.LoadAssetAtPath<LootObjectSO>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
                if (targets.Any(b => b == null || !AssetDatabase.IsOpenForEdit(b)))
                    throw new InvalidOperationException("A loot record is missing or read-only. Refresh and try again.");
                Undo.IncrementCurrentGroup();
                int group = Undo.GetCurrentGroup();
                Undo.SetCurrentGroupName("Move Loot Box Category");
                try
                {
                    foreach (var box in targets) LootMakerAuthoring.MoveContainerCategory(box, category);
                    Undo.FlushUndoRecordObjects();
                    foreach (var box in targets) AssetDatabase.SaveAssetIfDirty(box);
                }
                finally { Undo.CollapseUndoOperations(group); }
                RefreshWorldCatalog();
                _worldCategoryFilter = category;
                _showUnassignedVariants = false;
                _worldLootObjectSearch = string.Empty;
                _worldLootObjectScroll = Vector2.zero;
                SelectContainer(targets[0]);
                _worldTypeDetailTab = tab;
                ShowNotification(new GUIContent($"Moved to {CategoryLabel(category)}. Loot unchanged. Ctrl+Z to undo."));
            }
            catch (Exception ex) { EditorUtility.DisplayDialog("Move Loot Box Category", ex.Message, "OK"); }
            finally { _categoryMovePending = false; Repaint(); }
        };
    }

    private void DrawContainerNaming(LootObjectSO box)
    {
        string path = AssetDatabase.GetAssetPath(box.WorldPrefab);
        if (_namingBox != box || _namingPrefabPath != path)
        {
            _namingBox = box;
            _namingPrefabPath = path;
            _prefabNameDraft = System.IO.Path.GetFileNameWithoutExtension(path);
        }
        _prefabNameDraft = ContainerNameUi.NameFor(box, _prefabNameDraft, System.IO.Path.GetFileNameWithoutExtension(path));
        float previousWidth = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = 100f;
        try
        {
            using (new EditorGUI.DisabledScope(_renamePending || EditorApplication.isPlayingOrWillChangePlaymode))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    _prefabNameDraft = EditorGUILayout.TextField(new GUIContent("Prefab Name", path), _prefabNameDraft);
                    using (new EditorGUI.DisabledScope(box.WorldPrefab == null || string.IsNullOrWhiteSpace(_prefabNameDraft) ||
                        _prefabNameDraft == System.IO.Path.GetFileNameWithoutExtension(path)))
                        if (GUILayout.Button("Rename", GUILayout.Width(64))) QueueContainerRename(box, _prefabNameDraft);
                }
                using var data = new SerializedObject(box);
                string oldTitle = data.FindProperty("_displayName").stringValue;
                string title = EditorGUILayout.DelayedTextField(new GUIContent("Game Title", "Fallback title shown in-game. Localization takes priority. Press Enter to apply; Save persists it."), oldTitle);
                if (title != oldTitle) LootMakerAuthoring.SetContainerDisplayName(box, title);
            }
            EditorGUILayout.LabelField("Rename saves the prefab name immediately, keeping its GUID and links.\nGame Title is separate; localization takes priority. Use Save for title edits.", EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.LabelField("Category", box.HasCategoryAssignment ? CategoryLabel(box.Category) : "Unassigned",
                EditorStyles.miniLabel);
        }
        finally { EditorGUIUtility.labelWidth = previousWidth; }
    }

    private void QueueContainerRename(LootObjectSO box, string requestedName)
    {
        if (_renamePending) return;
        _renamePending = true;
        var tab = _worldTypeDetailTab;
        EditorApplication.delayCall += () =>
        {
            if (this == null) return;
            try
            {
                LootMakerAuthoring.RenameContainerPrefab(box, requestedName);
                _worldLootObjectSearch = string.Empty;
                RefreshWorldCatalog();
                SelectContainer(box);
                _worldTypeDetailTab = tab;
                ShowNotification(new GUIContent("Prefab renamed. Loot links preserved."));
            }
            catch (Exception ex) { EditorUtility.DisplayDialog("Rename Loot Prefab", ex.Message, "OK"); }
            finally { _renamePending = false; Repaint(); }
        };
    }

    private void OnDisable()
    {
        ClearContainerDeleteReview();
        ClearContainerDrag();
        Undo.undoRedoPerformed -= OnLootUndoRedo;
        if (_worldPoolClipboard != null) DestroyImmediate(_worldPoolClipboard);
        _worldPoolClipboard = null;
    }

    private void OnLootUndoRedo()
    {
        RefreshWorldCatalog();
        _focusedWorldTable = _selectedWorldLootObject?.LootTable;
        if (_selectedWorldLootObject != null)
        {
            _worldCategoryFilter = _selectedWorldLootObject.Category;
            _showUnassignedVariants = !_selectedWorldLootObject.HasCategoryAssignment;
        }
        Repaint();
    }

    private void DrawPrefabRegistration()
    {
        using (new EditorGUI.DisabledScope(_registeringPrefabs || EditorApplication.isPlayingOrWillChangePlaymode))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                _registerPrefab = (GameObject)EditorGUILayout.ObjectField(_registerPrefab, typeof(GameObject), false);
                using (new EditorGUI.DisabledScope(_registerPrefab == null))
                    if (GUILayout.Button("+ Register", GUILayout.Width(80))) QueuePrefabRegistration(new[] { _registerPrefab });
            }
            Rect drop = GUILayoutUtility.GetRect(GUIContent.none, GUI.skin.box, GUILayout.Height(43), GUILayout.ExpandWidth(true));
            GUI.Box(drop, "+ Drop loot-box prefabs here\nNew boxes get an independent copy of the selected loot list.",
                new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleCenter, fontSize = 10, wordWrap = true });
            Event current = Event.current;
            if (GUI.enabled && drop.Contains(current.mousePosition) &&
                (current.type == EventType.DragUpdated || current.type == EventType.DragPerform))
            {
                var candidates = DragAndDrop.objectReferences;
                bool valid = candidates.Length > 0 && candidates.All(c => LootMakerAuthoring.CanRegister(c, out _, out _));
                DragAndDrop.visualMode = valid ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;
                if (current.type == EventType.DragPerform)
                {
                    if (valid)
                    { DragAndDrop.AcceptDrag(); QueuePrefabRegistration(candidates.Cast<GameObject>().Distinct().ToArray()); }
                    else
                    {
                        var firstInvalid = candidates.FirstOrDefault(c => !LootMakerAuthoring.CanRegister(c, out _, out _));
                        LootMakerAuthoring.CanRegister(firstInvalid, out _, out _registrationStatus);
                    }
                }
                current.Use();
            }
        }
        if (_registeringPrefabs) EditorGUILayout.LabelField("Registering prefabs...", EditorStyles.miniLabel);
        MakerRegistrationUi.PrefabSummary(_registerPrefab);
        if (!string.IsNullOrEmpty(_registrationStatus))
            EditorGUILayout.LabelField(_registrationStatus, EditorStyles.wordWrappedMiniLabel);
    }

    private void QueuePrefabRegistration(GameObject[] prefabs)
    {
        if (_registeringPrefabs) return;
        _registeringPrefabs = true;
        var sourceTable = _selectedWorldLootObject?.LootTable;
        var category = _worldCategoryFilter ?? _selectedWorldLootObject?.Category ?? LootContainerCategory.Uncategorized;
        // Asset creation/import outside OnGUI keeps IMGUI layout and dragged references stable.
        EditorApplication.delayCall += () =>
        {
            if (this == null) return;
            int createdCount = 0, existingCount = 0;
            LootObjectSO last = null;
            var errors = new List<string>();
            foreach (var prefab in prefabs)
            {
                try
                {
                    last = LootMakerAuthoring.RegisterWithTable(prefab, sourceTable, category, out bool created);
                    if (created) createdCount++; else existingCount++;
                }
                catch (Exception ex) { errors.Add((prefab != null ? prefab.name : "Prefab") + ": " + ex.Message); }
            }
            RefreshWorldCatalog();
            if (last != null)
            {
                _worldCategoryFilter = last.Category;
                _showUnassignedVariants = !last.HasCategoryAssignment;
                _worldLootObjectSearch = string.Empty;
                SelectContainer(last);
            }
            _registrationStatus = $"{createdCount} registered / {existingCount} already registered. Existing boxes kept unchanged.";
            _registeringPrefabs = false;
            _registerPrefab = null;
            if (errors.Count > 0) EditorUtility.DisplayDialog("Prefab registration", string.Join("\n\n", errors), "OK");
            Repaint();
        };
    }

    private static void DrawBoxPreview(Rect rect, GameObject prefab)
    {
        if (prefab == null)
        {
            GUI.Label(rect, "Prefab not linked\nSelect Asset to check World Prefab", EditorStyles.centeredGreyMiniLabel);
            return;
        }
        Texture preview = AssetPreview.GetAssetPreview(prefab);
        if (preview != null) GUI.DrawTexture(rect, preview, ScaleMode.ScaleToFit);
        else GUI.Label(rect, AssetPreview.IsLoadingAssetPreview(prefab.GetInstanceID())
            ? "Loading model preview..." : "Preview unavailable\nUse Open Prefab to inspect", EditorStyles.centeredGreyMiniLabel);
    }

    private void DrawWorldTableActions(LootTableSO table, int users)
    {
        using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(table == null))
                    if (GUILayout.Button(new GUIContent("Copy Table", "Copy items, quantities, category weights and item shares. No live link.")))
                    {
                        if (_worldPoolClipboard != null) DestroyImmediate(_worldPoolClipboard);
                        _worldPoolClipboard = LootMakerAuthoring.Snapshot(table);
                        _worldPoolClipboardLabel = table.name;
                        ShowNotification(new GUIContent("Contents copied. Choose another box, then Paste Table."));
                    }
                ObserveGuiControl("CopyTable", GUILayoutUtility.GetLastRect());
                using (new EditorGUI.DisabledScope(_worldPoolClipboard == null || _selectedWorldLootObject == null))
                    if (GUILayout.Button(new GUIContent("Paste Table...", "Replace only this box's contents with a deep copy. Other boxes and Box Settings stay unchanged.")))
                    {
                        if (EditorUtility.DisplayDialog("Paste into this box?",
                            "Replace only " + GetContainerTitle(_selectedWorldLootObject) + "'s items, quantities, category weights and shares?\nCapacity, filled slots and rarity weights stay unchanged. No shared connection is created.",
                            "Paste Contents", "Cancel"))
                            RunWorldTableAction(() => LootMakerAuthoring.PasteToBox(_worldPoolClipboard, _selectedWorldLootObject));
                    }
                ObserveGuiControl("PasteTable", GUILayoutUtility.GetLastRect());
            }
            if (_selectedWorldLootObject != null && (table == null || users > 1 || _selectedWorldLootObject.LootObjectType != null))
                if (GUILayout.Button(table == null ? "Create Own Table" : "Separate This Box"))
                    RunWorldTableAction(() => LootMakerAuthoring.MakeIndependent(_selectedWorldLootObject, table));
            if (_worldPoolClipboard != null)
                EditorGUILayout.LabelField("Copied contents: " + _worldPoolClipboardLabel, EditorStyles.wordWrappedMiniLabel);
        }
        EditorGUILayout.Space(4);
    }

    private void RunWorldTableAction(Action action)
    {
        try
        {
            action();
            RefreshWorldCatalog();
            if (_selectedWorldLootObject != null)
            {
                _focusedWorldTable = _selectedWorldLootObject.LootTable;
                _worldCategoryFilter = _selectedWorldLootObject.Category;
                _showUnassignedVariants = !_selectedWorldLootObject.HasCategoryAssignment;
                _worldLootObjectSearch = string.Empty;
            }
            Repaint();
        }
        catch (Exception ex) { EditorUtility.DisplayDialog("Loot table", ex.Message, "OK"); }
    }
}
