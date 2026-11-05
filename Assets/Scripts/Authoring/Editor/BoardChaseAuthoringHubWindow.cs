/*
 * Role hierarchy: Authoring > Editor > Hub
 * Primary responsibility: Gives contributors one clear entry point to the
 * project authoring tools and their ownership boundaries.
 * Execution boundary: Unity Editor only (Assembly-CSharp-Editor)
 */

using System;
using UnityEditor;
using UnityEngine;

public sealed class BoardChaseAuthoringHubWindow : EditorWindow
{
    private Vector2 _scrollPosition;

    [MenuItem("Tools/Authoring/Authoring Hub")]
    // Purpose: Opens the open UI or workflow.
    public static void Open()
    {
        BoardChaseAuthoringHubWindow window = GetWindow<BoardChaseAuthoringHubWindow>("Authoring Hub");
        window.minSize = new Vector2(720f, 470f);
        window.Show();
    }

    // Purpose: Responds to the gui callback.
    private void OnGUI()
    {
        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

        EditorGUILayout.Space(8f);
        AuthoringWorkspaceUi.DrawHeader(
            "Authoring Hub",
            "Owner: All Contributors",
            "Choose the tool that owns the asset you are editing, then save and play test its authored result.",
            "1 Choose Tool",
            "2 Author Asset",
            "3 Save",
            "4 Play Test");

        DrawToolCard(
            "Audio Cue Library",
            "Audio / music contributor",
            "Create one AudioCueSO per reusable sound intent, assign clips and playback rules, then sync the shared library.",
            AudioCueLibraryWindow.Open);
        DrawToolCard(
            "Quest Maker",
            "Quest / narrative contributor",
            "Create quests, configure objectives and rewards, then connect only forward prerequisite links.",
            QuestDesignTreeWindow.Open);
        DrawToolCard(
            "Loot Maker",
            "Loot / encounter contributor",
            "Bind an enemy config or loot object and author its shared rarity pool. Automatic shares rebalance themselves.",
            LootProgressionEditorWindow.Open);
        DrawToolCard(
            "Save Maker",
            "Save / persistence contributor",
            "Inspect device settings, permanent progress and the current run; configure save paths or explicitly back up, restore and reset local saves.",
            SaveInspectorWindow.Open);
        DrawToolCard(
            "Game System Tool",
            "캐릭터 능력치 / 상태이상 데이터 편집",
            "플레이어·적 스탯, 상태이상, 경험치·레벨·사용 제한, 감염·이동·분해 규칙을 한 곳에서 직접 편집합니다.",
            CharacterStatusMakerWindow.Open);
        DrawToolCard(
            "Shooter Customize Station",
            "Character visual contributor",
            "Choose Player, NPC, enemy or Boss; edit visual presets in 3D and save a fixed look or a weighted spawn appearance pool. Gameplay equipment stays separate.",
            ShooterCustomizeStationWindow.Open);

        EditorGUILayout.Space(8f);
        if (GUILayout.Button("Open Debug Monitor", GUILayout.Height(28f)))
            DebugMonitorWindow.OpenWindow();

        EditorGUILayout.HelpBox(
            "Debug Monitor is for live Play Mode testing only; it does not save authored content.",
            MessageType.None);

        EditorGUILayout.EndScrollView();
    }

    // Purpose: Draws the tool card authoring UI.
    private static void DrawToolCard(string title, string owner, string description, Action openAction)
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Open", GUILayout.Width(88f), GUILayout.Height(24f)))
                    openAction?.Invoke();
            }

            EditorGUILayout.LabelField($"Best for: {owner}", EditorStyles.miniBoldLabel);
            EditorGUILayout.LabelField(description, EditorStyles.wordWrappedMiniLabel);
        }
    }
}
