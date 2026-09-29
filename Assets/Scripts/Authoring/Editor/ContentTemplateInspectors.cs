/*
 * Role hierarchy: Authoring > Editor > Content Templates
 * Primary responsibility: Presents the reusable ContentTemplates assets as
 * small, ordered authoring forms without changing their runtime data model.
 * Execution boundary: Unity Editor only (Assembly-CSharp-Editor)
 */

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Shared, deliberately narrow Inspector helpers. They only customize assets
/// stored under ContentTemplates; all live content keeps Unity's normal
/// Inspector so this tool cannot hide production-only fields.
/// </summary>
internal static class ContentTemplateInspectorUi
{
    private const string TemplateFolderSegment = "/ContentTemplates/";

    // Purpose: Checks whether the selected asset is one of the safe copy-first templates.
    public static bool IsTemplateAsset(UnityEngine.Object asset)
    {
        string path = asset != null ? AssetDatabase.GetAssetPath(asset) : string.Empty;
        return !string.IsNullOrWhiteSpace(path) &&
               path.Replace('\\', '/').IndexOf(TemplateFolderSegment, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    // Purpose: Draws the shared template workflow and exposes a safe duplicate action.
    public static void DrawHeader(
        UnityEngine.Object asset,
        string title,
        string owner,
        string purpose,
        params string[] workflow)
    {
        AuthoringWorkspaceUi.DrawHeader(title, owner, purpose, workflow);

        using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField(
                "TEMPLATE — duplicate this asset before assigning it to live content.",
                EditorStyles.wordWrappedMiniLabel);

            if (GUILayout.Button("Duplicate…", GUILayout.Width(104f)))
                DuplicateTemplate(asset);
        }

        EditorGUILayout.Space(4f);
    }

    // Purpose: Draws the disabled script field at the top of a custom inspector.
    public static void DrawScript(SerializedObject serializedObject)
    {
        SerializedProperty script = serializedObject.FindProperty("m_Script");
        if (script == null)
            return;

        using (new EditorGUI.DisabledScope(true))
            EditorGUILayout.PropertyField(script);
    }

    // Purpose: Draws an ordered section label inside a template Inspector.
    public static void DrawSection(string title, string message = null)
    {
        EditorGUILayout.Space(7f);
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

        if (!string.IsNullOrWhiteSpace(message))
            EditorGUILayout.HelpBox(message, MessageType.None);
    }

    // Purpose: Draws a serialized property when it exists and remembers that it was drawn.
    public static void DrawProperty(
        SerializedObject serializedObject,
        ISet<string> drawnProperties,
        string propertyName,
        string label = null,
        string tooltip = null,
        bool includeChildren = true)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
            return;

        drawnProperties.Add(propertyName);
        GUIContent content = string.IsNullOrWhiteSpace(label)
            ? new GUIContent(property.displayName, tooltip)
            : new GUIContent(label, tooltip);
        EditorGUILayout.PropertyField(property, content, includeChildren);
    }

    // Purpose: Draws fields introduced after this editor was written so template data remains forward-compatible.
    public static void DrawRemainingProperties(
        SerializedObject serializedObject,
        ISet<string> drawnProperties)
    {
        SerializedProperty iterator = serializedObject.GetIterator();
        bool enterChildren = true;
        bool foundAny = false;

        while (iterator.NextVisible(enterChildren))
        {
            enterChildren = false;
            // Ordered array/object fields were already rendered with children.
            // Only append new top-level fields here; otherwise those nested
            // fields would be drawn a second time below their parent array.
            if (iterator.depth != 0 ||
                iterator.name == "m_Script" ||
                drawnProperties.Contains(iterator.name))
                continue;

            if (!foundAny)
            {
                DrawSection(
                    "Additional / Newly Added Fields",
                    "These fields were not part of the original template layout. They remain visible here so no serialized data is hidden by the authoring UI.");
                foundAny = true;
            }

            EditorGUILayout.PropertyField(iterator, true);
        }
    }

    // Purpose: Duplicates the selected template without modifying the template source asset.
    private static void DuplicateTemplate(UnityEngine.Object sourceAsset)
    {
        string sourcePath = sourceAsset != null
            ? AssetDatabase.GetAssetPath(sourceAsset)
            : string.Empty;
        if (string.IsNullOrWhiteSpace(sourcePath))
            return;

        string directory = Path.GetDirectoryName(sourcePath)?.Replace('\\', '/');
        string filename = Path.GetFileNameWithoutExtension(sourcePath)
            .Replace("_Template", string.Empty) + "_New";
        string destination = EditorUtility.SaveFilePanelInProject(
            "Duplicate Content Template",
            filename,
            "asset",
            "Save a duplicate in the destination content folder. The source template is never edited.",
            directory);

        if (string.IsNullOrWhiteSpace(destination))
            return;

        if (!AssetDatabase.CopyAsset(sourcePath, destination))
        {
            EditorUtility.DisplayDialog(
                "Duplicate Failed",
                "Unity could not duplicate this template at the selected path.",
                "OK");
            return;
        }

        AssetDatabase.ImportAsset(destination);
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(destination);
    }
}

[CustomEditor(typeof(ItemSO), true)]
internal sealed class ContentTemplateItemInspector : Editor
{
    // Purpose: Draws a guided, type-aware Inspector only for item templates.
    public override void OnInspectorGUI()
    {
        if (!ContentTemplateInspectorUi.IsTemplateAsset(target))
        {
            DrawDefaultInspector();
            return;
        }

        serializedObject.Update();
        var drawn = new HashSet<string>(StringComparer.Ordinal);

        ContentTemplateInspectorUi.DrawHeader(
            target,
            "ITEM TEMPLATE",
            "Owner: Design / Economy",
            "Fill identity and inventory values first. Then assign visuals and gameplay references before placing the copied asset in loot, shop, quest, or loadout data.",
            "1 Duplicate", "2 Identity", "3 Gameplay", "4 Visual / Audio", "5 Register");
        ContentTemplateInspectorUi.DrawScript(serializedObject);

        ContentTemplateInspectorUi.DrawSection("1. Identity and Economy");
        DrawBaseItemProperties(drawn);

        if (target is GasMaskItemSO)
            DrawGasMaskProperties(drawn);
        else if (target is BackpackItemSO)
            DrawBackpackProperties(drawn);
        else if (target is ArmorItemSO)
            DrawArmorProperties(drawn);
        else if (target is BulletSO)
            DrawBulletProperties(drawn);
        else if (target is WeaponSO)
            DrawWeaponProperties(drawn);

        ContentTemplateInspectorUi.DrawRemainingProperties(serializedObject, drawn);
        serializedObject.ApplyModifiedProperties();
    }

    // Purpose: Draws the ItemSO fields shared by every item template.
    private void DrawBaseItemProperties(ISet<string> drawn)
    {
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "_guid", "Stable Item ID");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "_name", "Localized Name");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "_displayName", "Fallback Display Name");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "_description", "Localized Description");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "_previewImage", "Inventory Icon");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "_rarity", "Rarity");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "_purchasePrice", "Purchase Price");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "_maxStack", "Stack Limit");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "_maxOwnedAmount", "Ownership Limit");
        ContentTemplateInspectorUi.DrawProperty(
            serializedObject,
            drawn,
            "_worldVisualPrefab",
            "World Visual",
            "Visual-only child placed below the shared ItemPickup wrapper.");
        ContentTemplateInspectorUi.DrawProperty(
            serializedObject,
            drawn,
            "_worldItemPrefab",
            "World Pickup Prefab",
            "Legacy weapon field. Prefer the shared ItemPickup wrapper when creating new content.");
    }

    // Purpose: Draws the gas-mask fields in use order.
    private void DrawGasMaskProperties(ISet<string> drawn)
    {
        ContentTemplateInspectorUi.DrawSection(
            "2. Gas Protection",
            "Lower Gas Infection Multiplier is stronger. Filter durability is consumed by raw gas exposure while the mask is equipped.");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "_gasMaskId", "Gas Mask ID");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "_tier", "Tier");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "_gasInfectionMultiplier", "Gas Infection Multiplier");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "_maxFilterDurability", "Maximum Filter Durability");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "_filterDurabilityLossPerRawInfection", "Filter Loss per Raw Infection");

        ContentTemplateInspectorUi.DrawSection(
            "3. Equipped Face Visual",
            "Full gas masks use Face and occupy Eyes + Mouth. Respirators use Mouth only. These are visual regions, not gameplay hit boxes.");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "_equippedVisualPrefab", "Equipped Visual");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "_equippedVisualSocket", "Attach To");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "_visualCoverage", "Occupies");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "_equippedVisualTransform", "Local Placement");
    }

    // Purpose: Draws backpack capacity and mobility fields.
    private void DrawBackpackProperties(ISet<string> drawn)
    {
        ContentTemplateInspectorUi.DrawSection(
            "2. Carry Capacity and Mobility",
            "Pocket slots remain separate. This capacity appears only while the backpack is worn.");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "_backpackId", "Backpack ID");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "_inventorySlotCapacity", "Inventory Slots");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "_moveSpeedMultiplier", "Worn Move Speed Multiplier");
    }

    // Purpose: Draws armor defense and equipped-visual fields.
    private void DrawArmorProperties(ISet<string> drawn)
    {
        ContentTemplateInspectorUi.DrawSection("2. Defense and Durability");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "_armorId", "Armor ID");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "_armorClass", "Armor Class");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "_maxDurability", "Maximum Durability");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "_sameOrHigherPenetrationHealthDamageMultiplier", "Full-Penetration Damage Multiplier");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "_sameOrHigherPenetrationMarkDamageMultiplier", "Full-Penetration Mark Multiplier");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "_markDamageMultiplierPerClassGap", "Mark Multiplier per Class Gap");

        ContentTemplateInspectorUi.DrawSection("3. Equipped Visual and Stat Changes");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "_equippedVisualPrefab", "Equipped Visual");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "_equippedVisualLocalPosition", "Local Position");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "_equippedVisualLocalEulerAngles", "Local Rotation");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "_equippedVisualLocalScale", "Local Scale");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "_visualCoverage", "Occupies");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "_statModifiers", "Stat Modifiers");
    }

    // Purpose: Draws projectile setup before the combat values that rely on it.
    private void DrawBulletProperties(ISet<string> drawn)
    {
        ContentTemplateInspectorUi.DrawSection(
            "2. Projectile Setup",
            "Projectile Prefab is the fired gameplay projectile, not the pickup world visual.");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "bulletId", "Bullet ID");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "displayName", "Display Name");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "projectilePrefab", "Projectile Prefab");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "projectileTrailPrefab", "Projectile Trail");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "speed", "Speed");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "castRadius", "Collision Radius");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "maxLifetime", "Maximum Lifetime");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "spawnOffset", "Spawn Offset");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "projectileCollisionMask", "Collision Mask");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "projectileTriggerInteraction", "Trigger Interaction");

        ContentTemplateInspectorUi.DrawSection("3. Combat and Sound Stimuli");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "penetrationClass", "Penetration Class");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "_tier", "Loot Tier");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "_healthDamage", "Health Damage");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "_projectileFlybySoundRadius", "Flyby Sound Radius");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "_projectileFlybyEmitDistance", "Flyby Emit Distance");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "_projectileFlybySoundIntensity", "Flyby Sound Intensity");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "_projectileImpactSoundRadius", "Impact Sound Radius");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "_projectileImpactSoundIntensity", "Impact Sound Intensity");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "_projectileSoundInvestigateDelaySeconds", "Sound Investigation Delay");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "statModifiers", "Stat Modifiers");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "playerDamageableTargetMask", "Player-fired Target Mask");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "enemyDamageableTargetMask", "Enemy-fired Target Mask");
    }

    // Purpose: Draws common weapon data while allowing concrete types to expose their own fields below.
    private void DrawWeaponProperties(ISet<string> drawn)
    {
        ContentTemplateInspectorUi.DrawSection("2. Weapon Identity and View");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "_weaponId", "Weapon ID");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "_weaponViewPrefab", "Hand View Prefab");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "_weaponViewLocalPosition", "Hand Local Position");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "_weaponViewLocalEulerAngles", "Hand Local Rotation");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "_weaponViewLocalScale", "Hand Local Scale");

        ContentTemplateInspectorUi.DrawSection("3. Audio and Stat Changes");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "_audioProfile", "Weapon Audio Profile");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "_statModifiers", "Stat Modifiers");
    }
}

[CustomEditor(typeof(QuestDefinitionSO))]
internal sealed class ContentTemplateQuestInspector : Editor
{
    // Purpose: Draws the quest template in the same order used by Quest Maker.
    public override void OnInspectorGUI()
    {
        if (!ContentTemplateInspectorUi.IsTemplateAsset(target))
        {
            DrawDefaultInspector();
            return;
        }

        serializedObject.Update();
        var drawn = new HashSet<string>(StringComparer.Ordinal);
        ContentTemplateInspectorUi.DrawHeader(
            target,
            "QUEST TEMPLATE",
            "Owner: Design / Narrative",
            "Design objectives, rewards and prerequisites in Quest Maker. Quest Sets organize the design; Home NPC offered lists and the runtime catalog are configured separately.",
            "1 Duplicate", "2 Objective", "3 Reward", "4 Link in Tree");
        ContentTemplateInspectorUi.DrawScript(serializedObject);

        ContentTemplateInspectorUi.DrawSection("1. Identity");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "_questId", "Stable Quest ID");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "_displayName", "Quest Name");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "_description", "Player Description");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "_branch", "Home NPC Branch");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "_prerequisites", "Prerequisites");

        ContentTemplateInspectorUi.DrawSection("2. Objective");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "_objectives", "Objectives");

        ContentTemplateInspectorUi.DrawSection("3. Completion Reward");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "_rewards", "Linked Rewards");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "_persistentRewardSummary", "Fallback Reward Summary");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "_completionAudioCue", "Completion Audio Cue");

        ContentTemplateInspectorUi.DrawRemainingProperties(serializedObject, drawn);
        serializedObject.ApplyModifiedProperties();
    }
}

[CustomEditor(typeof(StatusEffectDefinitionSO))]
internal sealed class ContentTemplateStatusEffectInspector : Editor
{
    // Purpose: Draws status-effect template data in player-facing authoring order.
    public override void OnInspectorGUI()
    {
        if (!ContentTemplateInspectorUi.IsTemplateAsset(target))
        {
            DrawDefaultInspector();
            return;
        }

        serializedObject.Update();
        var drawn = new HashSet<string>(StringComparer.Ordinal);
        ContentTemplateInspectorUi.DrawHeader(
            target,
            "STATUS EFFECT TEMPLATE",
            "Owner: Combat / UX",
            "The player HUD can use either the assigned icon or its procedural fallback. Define duration and modifiers before connecting the copied effect to an item, room, or projectile.",
            "1 Duplicate", "2 Identity", "3 Duration", "4 Modifiers", "5 Connect");
        ContentTemplateInspectorUi.DrawScript(serializedObject);

        ContentTemplateInspectorUi.DrawSection("1. Player-facing Identity");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "_id", "Stable Effect ID");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "_polarity", "Polarity");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "_icon", "Optional Icon");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "_iconKind", "Fallback Icon Kind");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "_iconColor", "Icon Color");

        ContentTemplateInspectorUi.DrawSection("2. Duration and Runtime Behaviour");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "_durationSeconds", "Duration Seconds");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "_removeOnFriendlyPaint", "Remove on Friendly Paint");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "_blocksActions", "Blocks Actions");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "_suppressesSoundIndicators", "Suppresses Sound Indicators");

        ContentTemplateInspectorUi.DrawSection("3. Stat Effects and VFX");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "_modifiers", "Stat Modifiers");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "_armorClassDelta", "Armor Class Delta");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "_worldVfxPrefab", "World VFX");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "_isInjury", "Counts as Injury");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "_suppressesInjuryApplications", "Suppresses Injury Applications");

        ContentTemplateInspectorUi.DrawRemainingProperties(serializedObject, drawn);
        serializedObject.ApplyModifiedProperties();
    }
}

[CustomEditor(typeof(AudioCueSO))]
internal sealed class ContentTemplateAudioCueInspector : Editor
{
    // Purpose: Draws audio cue template data in the handoff order used by audio contributors.
    public override void OnInspectorGUI()
    {
        if (!ContentTemplateInspectorUi.IsTemplateAsset(target))
        {
            DrawDefaultInspector();
            return;
        }

        serializedObject.Update();
        var drawn = new HashSet<string>(StringComparer.Ordinal);
        ContentTemplateInspectorUi.DrawHeader(
            target,
            "AUDIO CUE TEMPLATE",
            "Owner: Audio",
            "Duplicate, choose ownership and routing, replace the sample clip, then register the finished copy in Audio Cue Library.",
            "1 Duplicate", "2 Category", "3 Clips", "4 Routing", "5 Register");
        ContentTemplateInspectorUi.DrawScript(serializedObject);

        ContentTemplateInspectorUi.DrawSection("1. Ownership and Handoff");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "_cueId", "Stable Cue ID");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "_category", "Owner Category");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "_authoringNotes", "Handoff Notes");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "description", "In-game Description");

        ContentTemplateInspectorUi.DrawSection("2. Clips and Playback");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "_audioClipGroups", "Clip Groups");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "looping", "Looping");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "_gainDb", "Additional Gain (dB)");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "_defaultPlaybackConfiguration", "Default Playback Routing");

        ContentTemplateInspectorUi.DrawRemainingProperties(serializedObject, drawn);
        serializedObject.ApplyModifiedProperties();
    }
}

[CustomEditor(typeof(SectorPresetSO), true)]
internal sealed class ContentTemplateSectorPresetInspector : Editor
{
    // Purpose: Draws normal and boss sector template data in a safe, explicit setup order.
    public override void OnInspectorGUI()
    {
        if (!ContentTemplateInspectorUi.IsTemplateAsset(target))
        {
            DrawDefaultInspector();
            return;
        }

        serializedObject.Update();
        var drawn = new HashSet<string>(StringComparer.Ordinal);
        string roomLabel = target is SectorBossBattlePresetSO ? "BOSS" : "NORMAL";
        ContentTemplateInspectorUi.DrawHeader(
            target,
            roomLabel + " SECTOR TEMPLATE",
            "Owner: Level / Encounter",
            "Copy this preset, assign the authored SectorRuntime prefab, then pair the copy with an Encounter in the Stage Definition. Normal rooms may also receive a content profile.",
            "1 Duplicate", "2 Sector Prefab", "3 Content", "4 Encounter", "5 Add to Stage");
        ContentTemplateInspectorUi.DrawScript(serializedObject);

        ContentTemplateInspectorUi.DrawSection("1. Room Identity");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "_displayName", "Designer Label");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "_roomType", "Room Type");

        ContentTemplateInspectorUi.DrawSection("2. Authored Room Prefab");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "_sectorPrefab", "Sector Runtime Prefab");

        if (target is SectorNormalBattlePresetSO)
        {
            ContentTemplateInspectorUi.DrawSection("3. Normal-room Content");
            ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "_normalSectorPolicy", "Normal Sector Policy");
            ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "_contentProfile", "Loot / Interactable Content Profile");
        }
        else
        {
            ContentTemplateInspectorUi.DrawSection(
                "3. Boss Pairing",
                "Boss presets do not own an encounter. Add this copied preset and its copied Encounter together as a Boss Battle pair in StageDefinition.");
        }

        ContentTemplateInspectorUi.DrawRemainingProperties(serializedObject, drawn);
        serializedObject.ApplyModifiedProperties();
    }
}

[CustomEditor(typeof(StageBattleEncounterPresetSO))]
internal sealed class ContentTemplateEncounterInspector : Editor
{
    // Purpose: Draws encounter template data before it is paired with a sector preset.
    public override void OnInspectorGUI()
    {
        if (!ContentTemplateInspectorUi.IsTemplateAsset(target))
        {
            DrawDefaultInspector();
            return;
        }

        serializedObject.Update();
        var drawn = new HashSet<string>(StringComparer.Ordinal);
        ContentTemplateInspectorUi.DrawHeader(
            target,
            "ENCOUNTER TEMPLATE",
            "Owner: Encounter Design",
            "Build waves from reusable Enemy Wave Presets. Then pair this copied encounter with a copied Normal or Boss Sector preset in the target Stage Definition.",
            "1 Duplicate", "2 Alive Cap", "3 Waves", "4 Pair with Sector", "5 Add to Stage");
        ContentTemplateInspectorUi.DrawScript(serializedObject);

        ContentTemplateInspectorUi.DrawSection("1. Encounter Identity");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "_displayName", "Designer Label");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "_sectorMaxAlive", "Maximum Enemies Alive");

        ContentTemplateInspectorUi.DrawSection(
            "2. Timed Waves",
            "Each wave picks one valid Enemy Wave Preset by weight. Keep at least one enabled candidate in every wave.");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "_waves", "Encounter Waves");

        ContentTemplateInspectorUi.DrawRemainingProperties(serializedObject, drawn);
        serializedObject.ApplyModifiedProperties();
    }
}

[CustomEditor(typeof(QuestLineSO))]
internal sealed class ContentTemplateQuestLineInspector : Editor
{
    // Purpose: Draws the quest-line template as a concise authoring checklist.
    public override void OnInspectorGUI()
    {
        if (!ContentTemplateInspectorUi.IsTemplateAsset(target))
        {
            DrawDefaultInspector();
            return;
        }

        serializedObject.Update();
        var drawn = new HashSet<string>(StringComparer.Ordinal);
        ContentTemplateInspectorUi.DrawHeader(
            target,
            "QUEST LINE TEMPLATE",
            "Owner: Design / Narrative",
            "This asset controls which quests belong to the line. Use Quest Maker to set the prerequisite flow between those copied quests.",
            "1 Duplicate", "2 Add Quests", "3 Link Flow", "4 Validate");
        ContentTemplateInspectorUi.DrawScript(serializedObject);

        ContentTemplateInspectorUi.DrawSection("1. Line Identity and Ordered Contents");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "_displayName", "Line Name");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "_quests", "Quest List");
        ContentTemplateInspectorUi.DrawRemainingProperties(serializedObject, drawn);
        serializedObject.ApplyModifiedProperties();
    }
}

[CustomEditor(typeof(SectorContentProfileSO))]
internal sealed class ContentTemplateSectorContentProfileInspector : Editor
{
    // Purpose: Draws the normal-room loot and interactable profile template in anchor-first order.
    public override void OnInspectorGUI()
    {
        if (!ContentTemplateInspectorUi.IsTemplateAsset(target))
        {
            DrawDefaultInspector();
            return;
        }

        serializedObject.Update();
        var drawn = new HashSet<string>(StringComparer.Ordinal);
        ContentTemplateInspectorUi.DrawHeader(
            target,
            "SECTOR CONTENT PROFILE TEMPLATE",
            "Owner: Level / Loot",
            "Each row must match one SectorContentSlotAnchor ID on the copied Normal Sector prefab. Add the anchor in Sector Maker first, then define its candidate pool here.",
            "1 Duplicate", "2 Paint Anchor", "3 Match Slot ID", "4 Add Candidates", "5 Assign to Preset");
        ContentTemplateInspectorUi.DrawScript(serializedObject);

        ContentTemplateInspectorUi.DrawSection(
            "1. Anchor-to-content Plan",
            "Loot Container, Rare Cache, and Special Interactable are independent slot kinds. An enabled row without a valid candidate intentionally does not spawn anything.");
        ContentTemplateInspectorUi.DrawProperty(serializedObject, drawn, "_slotDefinitions", "Content Slot Definitions");
        ContentTemplateInspectorUi.DrawRemainingProperties(serializedObject, drawn);
        serializedObject.ApplyModifiedProperties();
    }
}
