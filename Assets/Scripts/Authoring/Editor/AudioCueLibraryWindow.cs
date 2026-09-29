/*
 * Role hierarchy: Authoring > Editor > Audio
 * Primary responsibility: Maintains the project AudioCueSO catalogue and assists audio handoff.
 * Execution boundary: Unity Editor only (Assembly-CSharp-Editor)
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public sealed class AudioCueLibraryWindow : EditorWindow
{
    private const string DefaultLibraryPath = "Assets/ScriptableObject/Audio/AudioCueLibrary.asset";
    private const string DefaultCueRootDirectory = "Assets/ScriptableObject/Audio/AudioCue";

    private readonly List<AudioCueSO> _allCues = new();
    private readonly List<AudioCueSO> _filteredCues = new();
    private readonly List<string> _usagePaths = new();
    private readonly List<string> _validationIssues = new();

    private AudioCueLibrarySO _library;
    private AudioCueSO _selectedCue;
    private Vector2 _browserScroll;
    private Vector2 _cueScroll;
    private Vector2 _detailScroll;
    private string _search = string.Empty;
    private int _categoryIndex;
    private bool _missingClipOnly;
    private bool _notRegisteredOnly;
    private bool _hasSearchedUsage;
    private bool _showCreateCuePanel;
    private string _newCueName = "NewAudioCue";
    private AudioCueCategory _newCueCategory = AudioCueCategory.Uncategorized;
    private AudioConfigurationSO _newCuePlaybackRule;
    private bool _newCueLooping;

    [MenuItem("Tools/Authoring/Audio Cue Library")]
    // Purpose: Opens the open UI or workflow.
    public static void Open()
    {
        AudioCueLibraryWindow window = GetWindow<AudioCueLibraryWindow>("Audio Cue Library");
        window.minSize = new Vector2(1180f, 640f);
        window.RefreshCatalog();
        window.Show();
    }

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    private void OnEnable()
    {
        ResolveDefaultLibrary();
        RefreshCatalog();
    }

    // Purpose: Resolves the default library needed by this component.
    private void ResolveDefaultLibrary()
    {
        if (_library != null)
            return;

        string[] guids = AssetDatabase.FindAssets("t:AudioCueLibrarySO");
        if (guids.Length != 1)
            return;

        _library = AssetDatabase.LoadAssetAtPath<AudioCueLibrarySO>(AssetDatabase.GUIDToAssetPath(guids[0]));
    }

    // Purpose: Responds to the gui callback.
    private void OnGUI()
    {
        AuthoringWorkspaceUi.DrawHeader(
            "Audio Cue Library",
            "Owner: Audio",
            "Create reusable sound intents here; gameplay and UI reference the finished AudioCueSO.",
            "1 Library",
            "2 Create Cue",
            "3 Add Clips",
            "4 Routing",
            "5 Sync / Validate");
        DrawToolbar();
        DrawValidationSummary();

        float workspaceHeight = Mathf.Max(400f, position.height - 154f);
        EditorGUILayout.BeginHorizontal(GUILayout.Height(workspaceHeight));
        DrawCategoryBrowser(workspaceHeight);
        DrawCueList(workspaceHeight);
        DrawSelectedCue(workspaceHeight);
        EditorGUILayout.EndHorizontal();
    }

    // Purpose: Draws the new cue inspector authoring UI.
    private void DrawNewCueInspector()
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("New Audio Cue", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "Choose a sound intent first. The Cue is saved into its category folder automatically; then assign clips, playback, and handoff notes here.",
                EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.Space(4f);

            EditorGUILayout.LabelField("Templates", EditorStyles.miniBoldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("UI One-Shot", GUILayout.Height(24f)))
                    ApplyCueTemplate("UI_NewCue", AudioCueCategory.Ui, false);
                if (GUILayout.Button("Music Loop", GUILayout.Height(24f)))
                    ApplyCueTemplate("BGM_NewTrack", AudioCueCategory.Music, true);
                if (GUILayout.Button("World SFX", GUILayout.Height(24f)))
                    ApplyCueTemplate("SFX_World_NewCue", AudioCueCategory.World, false);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                _newCueName = EditorGUILayout.TextField("Cue Name", _newCueName);
                _newCueCategory = (AudioCueCategory)EditorGUILayout.EnumPopup(
                    "Category",
                    _newCueCategory,
                    GUILayout.Width(230f));
            }

            _newCuePlaybackRule = (AudioConfigurationSO)EditorGUILayout.ObjectField(
                "Playback Rule (optional)",
                _newCuePlaybackRule,
                typeof(AudioConfigurationSO),
                false);
            _newCueLooping = EditorGUILayout.Toggle("Loop", _newCueLooping);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Cancel", GUILayout.Width(86f), GUILayout.Height(24f)))
                    _showCreateCuePanel = false;

                using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(_newCueName)))
                {
                    if (GUILayout.Button("Create Cue", GUILayout.Width(112f), GUILayout.Height(24f)))
                        CreateCue();
                }
            }
        }
    }

    // Purpose: Applies the requested cue template change.
    private void ApplyCueTemplate(string name, AudioCueCategory category, bool looping)
    {
        _newCueName = name;
        _newCueCategory = category;
        _newCueLooping = looping;
    }

    // Purpose: Draws the toolbar authoring UI.
    private void DrawToolbar()
    {
        EditorGUILayout.Space(4f);
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        EditorGUI.BeginChangeCheck();
        _library = (AudioCueLibrarySO)EditorGUILayout.ObjectField(_library, typeof(AudioCueLibrarySO), false, GUILayout.Width(260f));
        if (EditorGUI.EndChangeCheck())
        {
            _hasSearchedUsage = false;
            ValidateCatalog();
        }

        if (_library == null)
        {
            if (GUILayout.Button("Create Library", EditorStyles.toolbarButton, GUILayout.Width(100f)))
                CreateLibrary();
        }
        else
        {
            if (GUILayout.Button("Sync Cues", EditorStyles.toolbarButton, GUILayout.Width(78f)))
                SyncLibrary();

            if (GUILayout.Button("Select Library", EditorStyles.toolbarButton, GUILayout.Width(90f)))
                Selection.activeObject = _library;
        }

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("New Cue", EditorStyles.toolbarButton, GUILayout.Width(76f)))
            BeginCreateCue();

        if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(62f)))
            RefreshCatalog();

        if (GUILayout.Button("Validate", EditorStyles.toolbarButton, GUILayout.Width(62f)))
        {
            ValidateCatalog();
            ShowNotification(new GUIContent(
                _validationIssues.Count == 0
                    ? "Audio Cue Library has no basic issues."
                    : $"Found {_validationIssues.Count} Audio Cue issue(s)."));
        }

        EditorGUILayout.EndHorizontal();
    }

    // Purpose: Draws the validation summary authoring UI.
    private void DrawValidationSummary()
    {
        if (_validationIssues.Count == 0)
        {
            EditorGUILayout.LabelField($"Cue catalogue check: {_allCues.Count} cue assets, no basic issues found.", EditorStyles.miniLabel);
            return;
        }

        EditorGUILayout.HelpBox($"Cue catalogue check found {_validationIssues.Count} issue(s). Select a cue to inspect it.", MessageType.Warning);
    }

    // Purpose: Draws the category browser authoring UI.
    private void DrawCategoryBrowser(float workspaceHeight)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(210f), GUILayout.Height(workspaceHeight));
        EditorGUILayout.LabelField("1. CATEGORY", EditorStyles.boldLabel);
        EditorGUILayout.LabelField(
            "Choose a category to filter the cue cards.",
            EditorStyles.wordWrappedMiniLabel);

        _browserScroll = EditorGUILayout.BeginScrollView(_browserScroll);
        DrawCategoryBrowserRow("All Cues", 0, _allCues.Count);

        EditorGUILayout.Space(5f);
        EditorGUILayout.LabelField("CATEGORIES", EditorStyles.miniBoldLabel);
        Array categories = Enum.GetValues(typeof(AudioCueCategory));
        for (int i = 0; i < categories.Length; i++)
        {
            AudioCueCategory category = (AudioCueCategory)categories.GetValue(i);
            DrawCategoryBrowserRow(category.ToString(), i + 1, CountCues(category));
        }

        EditorGUILayout.Space(5f);
        EditorGUILayout.LabelField("QUALITY FILTERS", EditorStyles.miniBoldLabel);
        EditorGUI.BeginChangeCheck();
        _missingClipOnly = EditorGUILayout.ToggleLeft(
            $"Needs clips ({CountCuesWithoutPlayableClips()})",
            _missingClipOnly);
        _notRegisteredOnly = EditorGUILayout.ToggleLeft(
            _library == null
                ? "Not in Library (select library)"
                : $"Not in Library ({CountUnregisteredCues()})",
            _notRegisteredOnly);
        if (EditorGUI.EndChangeCheck())
            RebuildFilteredCues();

        if (GUILayout.Button("Clear Browser Filters", EditorStyles.miniButton))
        {
            _categoryIndex = 0;
            _missingClipOnly = false;
            _notRegisteredOnly = false;
            RebuildFilteredCues();
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    // Purpose: Draws the category browser row authoring UI.
    private void DrawCategoryBrowserRow(string label, int categoryIndex, int count)
    {
        GUIStyle style = _categoryIndex == categoryIndex
            ? EditorStyles.toolbarButton
            : EditorStyles.miniButton;

        if (!GUILayout.Button($"{label} ({count})", style, GUILayout.Height(22f)))
            return;

        _categoryIndex = categoryIndex;
        RebuildFilteredCues();
    }

    // Purpose: Draws the cue list authoring UI.
    private void DrawCueList(float workspaceHeight)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.ExpandWidth(true), GUILayout.Height(workspaceHeight));
        EditorGUILayout.LabelField("2. CUE CATALOG", EditorStyles.boldLabel);
        EditorGUILayout.LabelField(
            "Asset-style cue cards. Select one card, then edit its clips and playback rule in the inspector.",
            EditorStyles.wordWrappedMiniLabel);

        EditorGUI.BeginChangeCheck();
        _search = EditorGUILayout.TextField("Search", _search);
        if (EditorGUI.EndChangeCheck())
            RebuildFilteredCues();

        EditorGUILayout.LabelField(
            $"{_filteredCues.Count} matching cue(s)  |  {GetActiveBrowserLabel()}",
            EditorStyles.miniLabel);
        _cueScroll = EditorGUILayout.BeginScrollView(_cueScroll);
        DrawCueCardGrid();

        if (_filteredCues.Count == 0)
            EditorGUILayout.LabelField("No cues match this browser selection.", EditorStyles.centeredGreyMiniLabel);

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    // Purpose: Draws filtered audio assets as fixed-size cards so titles and status never overlap in a narrow list.
    private void DrawCueCardGrid()
    {
        const float cardWidth = 172f;
        int columnCount = Mathf.Max(1, Mathf.FloorToInt(Mathf.Max(250f, position.width - 640f) / cardWidth));

        for (int rowStart = 0; rowStart < _filteredCues.Count; rowStart += columnCount)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                int rowEnd = Mathf.Min(_filteredCues.Count, rowStart + columnCount);
                for (int index = rowStart; index < rowEnd; index++)
                    DrawCueCard(_filteredCues[index], cardWidth);

                GUILayout.FlexibleSpace();
            }
        }
    }

    // Purpose: Draws one AudioCueSO as an asset-style card with fixed title, category, clip, and library-status areas.
    private void DrawCueCard(AudioCueSO cue, float cardWidth)
    {
        if (cue == null)
            return;

        Rect card = GUILayoutUtility.GetRect(cardWidth, 116f, GUILayout.Width(cardWidth), GUILayout.Height(116f));
        bool isSelected = cue == _selectedCue && !_showCreateCuePanel;
        bool needsClips = !cue.HasPlayableClip;
        bool isUnregistered = _library != null && !_library.Contains(cue);
        Color background = isSelected
            ? new Color(0.08f, 0.38f, 0.48f, 1f)
            : needsClips
                ? new Color(0.36f, 0.24f, 0.08f, 1f)
                : isUnregistered
                    ? new Color(0.23f, 0.18f, 0.28f, 1f)
                    : new Color(0.07f, 0.19f, 0.23f, 1f);

        EditorGUI.DrawRect(card, background);
        GUI.Box(card, GUIContent.none, EditorStyles.helpBox);

        Texture preview = AssetPreview.GetMiniThumbnail(cue) ?? Texture2D.grayTexture;
        Rect previewRect = new Rect(card.x + 9f, card.y + 9f, 42f, 42f);
        Rect titleRect = new Rect(card.x + 58f, card.y + 10f, card.width - 67f, 18f);
        Rect categoryRect = new Rect(card.x + 58f, card.y + 30f, card.width - 67f, 16f);
        Rect summaryRect = new Rect(card.x + 10f, card.y + 59f, card.width - 20f, 16f);
        Rect statusRect = new Rect(card.x + 10f, card.y + 79f, card.width - 20f, 17f);
        string clipSummary = needsClips ? "Needs playable clip" : $"{cue.ClipCount} playable clip(s)";
        string registration = _library == null
            ? "No library selected"
            : isUnregistered ? "Not registered" : "Registered";

        GUI.DrawTexture(previewRect, preview, ScaleMode.ScaleToFit, true);
        GUI.Label(titleRect, cue.name, EditorStyles.boldLabel);
        GUI.Label(categoryRect, cue.Category.ToString(), EditorStyles.miniLabel);
        GUI.Label(summaryRect, clipSummary, EditorStyles.miniLabel);
        EditorGUI.DrawRect(statusRect, isUnregistered || needsClips
            ? new Color(0.42f, 0.20f, 0.08f, 0.72f)
            : new Color(0.08f, 0.34f, 0.27f, 0.72f));
        GUI.Label(statusRect, $"Library: {registration}", EditorStyles.miniLabel);

        if (GUI.Button(card, GUIContent.none, GUIStyle.none))
        {
            _selectedCue = cue;
            _hasSearchedUsage = false;
            _showCreateCuePanel = false;
        }
    }

    // Purpose: Draws the selected cue authoring UI.
    private void DrawSelectedCue(float workspaceHeight)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(390f), GUILayout.Height(workspaceHeight));
        EditorGUILayout.LabelField(_showCreateCuePanel ? "3. CUE INSPECTOR  |  NEW" : "3. CUE INSPECTOR", EditorStyles.boldLabel);

        if (_showCreateCuePanel)
        {
            DrawNewCueInspector();
            EditorGUILayout.EndVertical();
            return;
        }

        if (_selectedCue == null)
        {
            EditorGUILayout.HelpBox("Select a cue from the browser, or create a new reusable sound intent.", MessageType.None);
            if (GUILayout.Button("Create New Cue", GUILayout.Height(26f)))
                BeginCreateCue();
            EditorGUILayout.EndVertical();
            return;
        }

        _detailScroll = EditorGUILayout.BeginScrollView(_detailScroll, GUI.skin.box);
        EditorGUILayout.ObjectField("Cue", _selectedCue, typeof(AudioCueSO), false);
        EditorGUILayout.LabelField("Cue ID", string.IsNullOrWhiteSpace(_selectedCue.CueId) ? "Missing" : _selectedCue.CueId);
        EditorGUILayout.LabelField("Category", _selectedCue.Category.ToString());
        EditorGUILayout.LabelField("Playable Clips", _selectedCue.ClipCount.ToString());
        EditorGUILayout.LabelField("Library Registration", _library == null ? "No library selected" : _library.Contains(_selectedCue) ? "Registered" : "Not registered");

        EditorGUILayout.Space(6f);
        DrawSelectedCueAuthoringFields();

        EditorGUILayout.Space(6f);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Duplicate as New"))
            DuplicateSelectedCue();

        if (GUILayout.Button("Open Inspector"))
        {
            Selection.activeObject = _selectedCue;
            EditorGUIUtility.PingObject(_selectedCue);
        }

        GUI.enabled = _selectedCue.HasPlayableClip;
        if (GUILayout.Button("Preview Random Clip"))
            PreviewCue(_selectedCue);
        GUI.enabled = true;

        if (GUILayout.Button("Stop Preview"))
            StopPreview();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("Usage Search", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Finds assets, prefabs and scenes that reference this Cue directly. This is an editor-only reference audit; runtime calls through channels are not inferred.", MessageType.None);

        if (GUILayout.Button("Find Direct Asset Usages"))
            FindUsagePaths();

        if (_hasSearchedUsage)
        {
            if (_usagePaths.Count == 0)
                EditorGUILayout.LabelField("No direct serialized asset references found.", EditorStyles.miniLabel);
            else
            {
                EditorGUILayout.LabelField($"Direct references ({_usagePaths.Count})", EditorStyles.miniLabel);
                for (int i = 0; i < _usagePaths.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.SelectableLabel(_usagePaths[i], EditorStyles.miniLabel, GUILayout.Height(EditorGUIUtility.singleLineHeight));
                    if (GUILayout.Button("Select", GUILayout.Width(54f)))
                    {
                        UnityEngine.Object asset = AssetDatabase.LoadMainAssetAtPath(_usagePaths[i]);
                        if (asset != null)
                            Selection.activeObject = asset;
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
        }

        DrawSelectedCueIssues();
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    // Purpose: Draws the selected cue authoring fields authoring UI.
    private void DrawSelectedCueAuthoringFields()
    {
        EditorGUILayout.LabelField("Cue Setup", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Drag AudioClip assets into Clip Groups below. Keep variations that can be randomly selected in the same group. Use a separate cue when gameplay needs a different sound intent.",
            MessageType.None);

        SerializedObject serializedCue = new(_selectedCue);
        serializedCue.Update();

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(serializedCue.FindProperty("_category"), new GUIContent("Content Category"));
        EditorGUILayout.PropertyField(serializedCue.FindProperty("_authoringNotes"), new GUIContent("Handoff Notes"));
        EditorGUILayout.PropertyField(serializedCue.FindProperty("_defaultPlaybackConfiguration"), new GUIContent("Default Playback Rule"));
        EditorGUILayout.PropertyField(serializedCue.FindProperty("looping"), new GUIContent("Loop"));
        EditorGUILayout.PropertyField(serializedCue.FindProperty("_gainDb"), new GUIContent("Gain (dB)"));
        EditorGUILayout.PropertyField(serializedCue.FindProperty("_audioClipGroups"), new GUIContent("Clip Groups"), true);

        if (!EditorGUI.EndChangeCheck())
            return;

        serializedCue.ApplyModifiedProperties();
        EditorUtility.SetDirty(_selectedCue);
        AssetDatabase.SaveAssets();
        RefreshCatalog();
    }

    // Purpose: Draws the selected cue issues authoring UI.
    private void DrawSelectedCueIssues()
    {
        List<string> issues = GetIssuesFor(_selectedCue);
        if (issues.Count == 0)
            return;

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("Validation", EditorStyles.boldLabel);
        for (int i = 0; i < issues.Count; i++)
            EditorGUILayout.HelpBox(issues[i], MessageType.Warning);
    }

    // Purpose: Refreshes the displayed or cached catalog.
    private void RefreshCatalog()
    {
        _allCues.Clear();
        string[] guids = AssetDatabase.FindAssets("t:AudioCueSO");

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            AudioCueSO cue = AssetDatabase.LoadAssetAtPath<AudioCueSO>(path);
            if (cue != null)
                _allCues.Add(cue);
        }

        _allCues.Sort((left, right) => string.Compare(left.name, right.name, StringComparison.OrdinalIgnoreCase));
        RebuildFilteredCues();
        ValidateCatalog();
        Repaint();
    }

    // Purpose: Repairs or rebuilds filtered cues from the current source data.
    private void RebuildFilteredCues()
    {
        _filteredCues.Clear();
        AudioCueCategory? selectedCategory = _categoryIndex > 0 ? (AudioCueCategory)(_categoryIndex - 1) : null;
        string search = _search ?? string.Empty;

        for (int i = 0; i < _allCues.Count; i++)
        {
            AudioCueSO cue = _allCues[i];
            if (cue == null)
                continue;

            if (selectedCategory.HasValue && cue.Category != selectedCategory.Value)
                continue;

            if (_missingClipOnly && cue.HasPlayableClip)
                continue;

            if (_notRegisteredOnly && (_library == null || _library.Contains(cue)))
                continue;

            if (!string.IsNullOrWhiteSpace(search) &&
                cue.name.IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0 &&
                (cue.CueId ?? string.Empty).IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0 &&
                (cue.AuthoringNotes ?? string.Empty).IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0)
                continue;

            _filteredCues.Add(cue);
        }
    }

    // Purpose: Counts the available cues.
    private int CountCues(AudioCueCategory category)
    {
        int count = 0;
        for (int i = 0; i < _allCues.Count; i++)
        {
            if (_allCues[i] != null && _allCues[i].Category == category)
                count++;
        }

        return count;
    }

    // Purpose: Counts the available cues without playable clips.
    private int CountCuesWithoutPlayableClips()
    {
        int count = 0;
        for (int i = 0; i < _allCues.Count; i++)
        {
            if (_allCues[i] != null && !_allCues[i].HasPlayableClip)
                count++;
        }

        return count;
    }

    // Purpose: Counts the available unregistered cues.
    private int CountUnregisteredCues()
    {
        if (_library == null)
            return 0;

        int count = 0;
        for (int i = 0; i < _allCues.Count; i++)
        {
            if (_allCues[i] != null && !_library.Contains(_allCues[i]))
                count++;
        }

        return count;
    }

    // Purpose: Returns the current active browser label.
    private string GetActiveBrowserLabel()
    {
        string category = _categoryIndex == 0
            ? "All Cues"
            : ((AudioCueCategory)(_categoryIndex - 1)).ToString();

        if (_missingClipOnly && _notRegisteredOnly)
            return category + " + Needs Clips + Not in Library";
        if (_missingClipOnly)
            return category + " + Needs Clips";
        if (_notRegisteredOnly)
            return category + " + Not in Library";

        return category;
    }

    // Purpose: Validates catalog and reports invalid authored data.
    private void ValidateCatalog()
    {
        _validationIssues.Clear();
        Dictionary<string, AudioCueSO> cueIds = new(StringComparer.Ordinal);

        for (int i = 0; i < _allCues.Count; i++)
        {
            AudioCueSO cue = _allCues[i];
            if (cue == null)
                continue;

            if (!cue.HasPlayableClip)
                _validationIssues.Add($"{cue.name}: no playable AudioClip is assigned.");

            if (string.IsNullOrWhiteSpace(cue.CueId))
                _validationIssues.Add($"{cue.name}: Cue ID is missing. Re-select or edit the asset to run OnValidate.");
            else if (cueIds.TryGetValue(cue.CueId, out AudioCueSO existing))
                _validationIssues.Add($"Duplicate Cue ID: {existing.name} and {cue.name}.");
            else
                cueIds.Add(cue.CueId, cue);

            if (_library != null && !_library.Contains(cue))
                _validationIssues.Add($"{cue.name}: not registered in the selected Audio Cue Library.");
        }
    }

    // Purpose: Returns the current issues for.
    private List<string> GetIssuesFor(AudioCueSO target)
    {
        List<string> results = new();
        if (target == null)
            return results;

        for (int i = 0; i < _validationIssues.Count; i++)
        {
            string issue = _validationIssues[i];
            if (issue.IndexOf(target.name, StringComparison.Ordinal) >= 0 ||
                (!string.IsNullOrWhiteSpace(target.CueId) && issue.IndexOf(target.CueId, StringComparison.Ordinal) >= 0))
                results.Add(issue);
        }

        return results;
    }

    // Purpose: Creates the library owned by this workflow.
    private void CreateLibrary()
    {
        EnsureDirectory(DefaultLibraryPath);
        string path = AssetDatabase.GenerateUniqueAssetPath(DefaultLibraryPath);
        _library = CreateInstance<AudioCueLibrarySO>();
        AssetDatabase.CreateAsset(_library, path);
        AssetDatabase.SaveAssets();
        Selection.activeObject = _library;
        SyncLibrary();
    }

    // Purpose: Creates the cue owned by this workflow.
    private void CreateCue()
    {
        string directory = GetCueDirectory(_newCueCategory);
        EnsureDirectory(directory + "/AudioCue.asset");
        string assetName = SanitizeAssetName(_newCueName, "NewAudioCue");
        string path = AssetDatabase.GenerateUniqueAssetPath($"{directory}/{assetName}.asset");
        AudioCueSO cue = CreateInstance<AudioCueSO>();
        SerializedObject serializedCue = new(cue);
        serializedCue.FindProperty("_cueId").stringValue = Guid.NewGuid().ToString("N");
        serializedCue.FindProperty("_category").enumValueIndex = (int)_newCueCategory;
        serializedCue.FindProperty("_defaultPlaybackConfiguration").objectReferenceValue = _newCuePlaybackRule;
        serializedCue.FindProperty("looping").boolValue = _newCueLooping;
        serializedCue.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.CreateAsset(cue, path);
        AssetDatabase.SaveAssets();
        _selectedCue = cue;
        _categoryIndex = (int)_newCueCategory + 1;
        _missingClipOnly = false;
        _notRegisteredOnly = false;
        Selection.activeObject = cue;
        RefreshCatalog();
        if (_library != null)
            SyncLibrary();

        _showCreateCuePanel = false;
    }

    // Purpose: Begins the create cue workflow.
    private void BeginCreateCue()
    {
        _showCreateCuePanel = true;
        _selectedCue = null;
        _hasSearchedUsage = false;
    }

    // Purpose: Performs the duplicate selected cue workflow for this component.
    private void DuplicateSelectedCue()
    {
        if (_selectedCue == null)
            return;

        string sourcePath = AssetDatabase.GetAssetPath(_selectedCue);
        string directory = Path.GetDirectoryName(sourcePath)?.Replace('\\', '/') ?? GetCueDirectory(_selectedCue.Category);
        string path = AssetDatabase.GenerateUniqueAssetPath($"{directory}/{_selectedCue.name}_Copy.asset");
        if (!AssetDatabase.CopyAsset(sourcePath, path))
        {
            ShowNotification(new GUIContent("Could not duplicate the selected Audio Cue."));
            return;
        }

        AudioCueSO duplicate = AssetDatabase.LoadAssetAtPath<AudioCueSO>(path);
        if (duplicate == null)
            return;

        SerializedObject serializedCue = new(duplicate);
        serializedCue.FindProperty("_cueId").stringValue = Guid.NewGuid().ToString("N");
        serializedCue.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(duplicate);
        AssetDatabase.SaveAssets();

        _selectedCue = duplicate;
        _showCreateCuePanel = false;
        _newCueName = duplicate.name;
        RefreshCatalog();
        if (_library != null)
            SyncLibrary();

        Selection.activeObject = duplicate;
        EditorGUIUtility.PingObject(duplicate);
    }

    // Purpose: Normalizes asset name to a safe authored value.
    private static string SanitizeAssetName(string value, string fallback)
    {
        string result = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        char[] invalidCharacters = Path.GetInvalidFileNameChars();
        for (int i = 0; i < invalidCharacters.Length; i++)
            result = result.Replace(invalidCharacters[i], '_');

        return string.IsNullOrWhiteSpace(result) ? fallback : result;
    }

    // Purpose: Returns the consistent project folder that owns a new Cue category.
    private static string GetCueDirectory(AudioCueCategory category)
    {
        return category switch
        {
            AudioCueCategory.Ui => DefaultCueRootDirectory + "/SFX/UI",
            AudioCueCategory.Music => DefaultCueRootDirectory + "/Music",
            AudioCueCategory.Player => DefaultCueRootDirectory + "/SFX/Character/Player",
            AudioCueCategory.Weapon => DefaultCueRootDirectory + "/SFX/GameItem/Weapon",
            AudioCueCategory.Enemy => DefaultCueRootDirectory + "/SFX/Character/Enemy",
            AudioCueCategory.Creature => DefaultCueRootDirectory + "/SFX/Creature",
            AudioCueCategory.Interactable => DefaultCueRootDirectory + "/SFX/Interactable",
            AudioCueCategory.World => DefaultCueRootDirectory + "/SFX/World",
            AudioCueCategory.Ambience => DefaultCueRootDirectory + "/SFX/Ambience",
            AudioCueCategory.Voice => DefaultCueRootDirectory + "/SFX/Voice",
            _ => DefaultCueRootDirectory + "/SFX/Uncategorized"
        };
    }

    // Purpose: Synchronizes library with the current source state.
    private void SyncLibrary()
    {
        if (_library == null)
            return;

        SerializedObject serializedLibrary = new(_library);
        SerializedProperty cues = serializedLibrary.FindProperty("_cues");
        cues.arraySize = _allCues.Count;
        for (int i = 0; i < _allCues.Count; i++)
            cues.GetArrayElementAtIndex(i).objectReferenceValue = _allCues[i];

        serializedLibrary.ApplyModifiedProperties();
        EditorUtility.SetDirty(_library);
        AssetDatabase.SaveAssets();
        RebuildFilteredCues();
        ValidateCatalog();
    }

    // Purpose: Finds the requested usage paths.
    private void FindUsagePaths()
    {
        _usagePaths.Clear();
        _hasSearchedUsage = true;
        string cuePath = AssetDatabase.GetAssetPath(_selectedCue);
        if (string.IsNullOrWhiteSpace(cuePath))
            return;

        string[] allAssetPaths = AssetDatabase.GetAllAssetPaths();
        for (int i = 0; i < allAssetPaths.Length; i++)
        {
            string path = allAssetPaths[i];
            if (path == cuePath || !IsReferenceSearchPath(path))
                continue;

            string[] dependencies = AssetDatabase.GetDependencies(path, false);
            for (int j = 0; j < dependencies.Length; j++)
            {
                if (dependencies[j] == cuePath)
                {
                    _usagePaths.Add(path);
                    break;
                }
            }
        }
    }

    // Purpose: Checks whether reference search path is currently true.
    private static bool IsReferenceSearchPath(string path)
    {
        return path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase) ||
               path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase) ||
               path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase);
    }

    // Purpose: Ensures directory is ready before the workflow continues.
    private static void EnsureDirectory(string assetPath)
    {
        string directory = Path.GetDirectoryName(assetPath);
        if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);
    }

    // Purpose: Performs the preview cue workflow for this component.
    private static void PreviewCue(AudioCueSO cue)
    {
        AudioClip[] clips = cue.GetClips();
        for (int i = 0; i < clips.Length; i++)
        {
            if (clips[i] == null)
                continue;

            Type audioUtil = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");
            MethodInfo method = audioUtil?.GetMethod("PlayPreviewClip", BindingFlags.Static | BindingFlags.Public, null, new[] { typeof(AudioClip), typeof(int), typeof(bool) }, null);
            method?.Invoke(null, new object[] { clips[i], 0, false });
            return;
        }
    }

    // Purpose: Stops the active preview behavior.
    private static void StopPreview()
    {
        Type audioUtil = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");
        MethodInfo method = audioUtil?.GetMethod("StopAllPreviewClips", BindingFlags.Static | BindingFlags.Public);
        method?.Invoke(null, null);
    }

}
