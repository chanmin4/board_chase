/*
 * Role hierarchy: Authoring > Editor
 * Primary responsibility: Provides the ShooterLoadoutSOEditor authoring tool and its editor workflow.
 * Execution boundary: Editor only (Assembly-CSharp-Editor)
 */

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ShooterLoadoutSO))]
public sealed class ShooterLoadoutSOEditor : Editor
{
    private SerializedProperty _script;
    private SerializedProperty _weaponEntries;
    private SerializedProperty _ammoPools;
    private SerializedProperty _armorEntries;
    private SerializedProperty _inventoryEntries;

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    private void OnEnable()
    {
        _script = serializedObject.FindProperty("m_Script");
        _weaponEntries = serializedObject.FindProperty("_weaponEntries");
        _ammoPools = serializedObject.FindProperty("_ammoPools");
        _armorEntries = serializedObject.FindProperty("_armorEntries");
        _inventoryEntries = serializedObject.FindProperty("_inventoryEntries");
    }

    // Purpose: Responds to the inspector gui callback.
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        using (new EditorGUI.DisabledScope(true))
            EditorGUILayout.PropertyField(_script);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Weapon Sets", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Exactly one enabled Weapon Set is selected by relative Weight. Set ID must be unique because top-level Ammo Pools reference it.",
            MessageType.Info);
        EditorGUILayout.PropertyField(_weaponEntries, GUIContent.none, true);
        DrawWeaponSetValidation();

        EditorGUILayout.Space();
        DrawAmmoPools();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Armor Equipment", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_armorEntries, GUIContent.none, true);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Inventory", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_inventoryEntries, GUIContent.none, true);
        DrawInventoryStackUsage();

        serializedObject.ApplyModifiedProperties();
    }

    // Purpose: Draws the ammo pools authoring UI.
    private void DrawAmmoPools()
    {
        EditorGUILayout.LabelField("Ammo Pools", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Ammo Pools are top-level entries beside Weapon Sets. A pool selects one compatible starting-ammo stack and stores it in the carried inventory. Ammo is not equipped into a separate equipment slot.",
            MessageType.Info);

        int requestedSize = Mathf.Max(0, EditorGUILayout.IntField("Size", _ammoPools.arraySize));
        if (requestedSize != _ammoPools.arraySize)
            _ammoPools.arraySize = requestedSize;

        for (int i = 0; i < _ammoPools.arraySize; i++)
        {
            SerializedProperty pool = _ammoPools.GetArrayElementAtIndex(i);
            SerializedProperty setId = pool.FindPropertyRelative("weaponSetId");
            SerializedProperty sourceSlot = pool.FindPropertyRelative("sourceWeaponSlotIndex");
            SerializedProperty destinationSlot = pool.FindPropertyRelative("destinationAmmoSlotIndex");
            SerializedProperty ammoType = pool.FindPropertyRelative("ammoType");
            SerializedProperty candidates = pool.FindPropertyRelative("candidates");

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            pool.isExpanded = EditorGUILayout.Foldout(
                pool.isExpanded,
                BuildPoolLabel(i, setId.stringValue, destinationSlot.intValue),
                true);

            if (GUILayout.Button("Remove", GUILayout.Width(64f)))
            {
                _ammoPools.DeleteArrayElementAtIndex(i);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                break;
            }

            EditorGUILayout.EndHorizontal();

            if (pool.isExpanded)
            {
                DrawWeaponSetSelector(setId);
                EditorGUILayout.PropertyField(sourceSlot, new GUIContent("Source Weapon Slot"));
                EditorGUILayout.PropertyField(
                    destinationSlot,
                    new GUIContent(
                        "Inventory Order",
                        "Stable ordering key for generated starting-ammo stacks. This is not an equipment slot."));
                EditorGUILayout.PropertyField(ammoType, new GUIContent("Ammo Type"));

                RangedWeaponSO weapon = ResolveWeapon(setId.stringValue, sourceSlot.intValue);
                DrawResolvedWeaponInfo(weapon, ammoType);
                EditorGUILayout.PropertyField(candidates, new GUIContent("Weighted Candidates"), true);
                DrawCandidateValidation(candidates, weapon, (BulletAmmoType)ammoType.enumValueIndex);

                using (new EditorGUI.DisabledScope(weapon == null))
                {
                    if (GUILayout.Button("Add All Compatible Bullets"))
                        AddCompatibleBullets(candidates, weapon, (BulletAmmoType)ammoType.enumValueIndex);
                }
            }

            EditorGUILayout.EndVertical();
        }

        if (GUILayout.Button("Add Ammo Pool"))
        {
            int index = _ammoPools.arraySize;
            _ammoPools.InsertArrayElementAtIndex(index);
            SerializedProperty pool = _ammoPools.GetArrayElementAtIndex(index);
            pool.FindPropertyRelative("weaponSetId").stringValue = GetFirstWeaponSetId();
            pool.FindPropertyRelative("sourceWeaponSlotIndex").intValue = 0;
            pool.FindPropertyRelative("destinationAmmoSlotIndex").intValue = index;
            pool.FindPropertyRelative("ammoType").enumValueIndex = (int)BulletAmmoType.Basic;
            pool.FindPropertyRelative("candidates").arraySize = 0;
            pool.isExpanded = true;
        }
    }

    // Purpose: Draws the weapon set selector authoring UI.
    private void DrawWeaponSetSelector(SerializedProperty setId)
    {
        List<string> ids = GetWeaponSetIds();

        if (ids.Count == 0)
        {
            EditorGUILayout.PropertyField(setId, new GUIContent("Weapon Set ID"));
            EditorGUILayout.HelpBox("Create at least one Weapon Set with a unique Set ID first.", MessageType.Error);
            return;
        }

        int selectedIndex = Mathf.Max(0, ids.IndexOf(setId.stringValue));
        int newIndex = EditorGUILayout.Popup("Weapon Set", selectedIndex, ids.ToArray());
        setId.stringValue = ids[Mathf.Clamp(newIndex, 0, ids.Count - 1)];
    }

    // Purpose: Draws the resolved weapon info authoring UI.
    private void DrawResolvedWeaponInfo(RangedWeaponSO weapon, SerializedProperty ammoType)
    {
        if (weapon == null)
        {
            EditorGUILayout.HelpBox(
                "The referenced source slot is empty or does not contain a RangedWeaponSO.",
                MessageType.Error);
            return;
        }

        EditorGUILayout.HelpBox(
            $"Weapon: {weapon.name} | Type: {weapon.WeaponType} | Required Family: {weapon.RequiredBulletFamily} | Ammo Type: {(BulletAmmoType)ammoType.enumValueIndex}",
            MessageType.None);
    }

    // Purpose: Draws the candidate validation authoring UI.
    private void DrawCandidateValidation(
        SerializedProperty candidates,
        RangedWeaponSO weapon,
        BulletAmmoType ammoType)
    {
        if (weapon == null)
            return;

        int enabledCompatibleCount = 0;
        var invalidNames = new List<string>();

        for (int i = 0; i < candidates.arraySize; i++)
        {
            SerializedProperty candidate = candidates.GetArrayElementAtIndex(i);
            BulletSO bullet = candidate.FindPropertyRelative("bullet").objectReferenceValue as BulletSO;
            float weight = candidate.FindPropertyRelative("weight").floatValue;

            if (bullet == null || weight <= 0f)
                continue;

            if (bullet.AmmoType == ammoType && weapon.IsCompatibleWith(bullet))
                enabledCompatibleCount++;
            else
                invalidNames.Add($"{bullet.name} ({bullet.Family}/{bullet.AmmoType})");
        }

        if (invalidNames.Count > 0)
        {
            EditorGUILayout.HelpBox(
                "These candidates are incompatible and will never be rolled: " + string.Join(", ", invalidNames),
                MessageType.Error);
        }

        if (enabledCompatibleCount == 0)
        {
            EditorGUILayout.HelpBox(
                "This pool has no enabled compatible candidate. The selected ranged weapon would spawn without combat ammunition.",
                MessageType.Error);
        }
    }

    // Purpose: Draws the weapon set validation authoring UI.
    private void DrawWeaponSetValidation()
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);

        for (int i = 0; i < _weaponEntries.arraySize; i++)
        {
            SerializedProperty entry = _weaponEntries.GetArrayElementAtIndex(i);
            string id = entry.FindPropertyRelative("setId").stringValue.Trim();

            if (string.IsNullOrEmpty(id))
            {
                EditorGUILayout.HelpBox($"Weapon Set {i} has no Set ID.", MessageType.Error);
                continue;
            }

            if (!seen.Add(id))
                EditorGUILayout.HelpBox($"Duplicate Weapon Set ID: {id}", MessageType.Error);
        }
    }

    // Purpose: Adds the requested compatible bullets to this component.
    private void AddCompatibleBullets(
        SerializedProperty candidates,
        RangedWeaponSO weapon,
        BulletAmmoType ammoType)
    {
        var existing = new HashSet<BulletSO>();

        for (int i = 0; i < candidates.arraySize; i++)
        {
            BulletSO bullet = candidates
                .GetArrayElementAtIndex(i)
                .FindPropertyRelative("bullet")
                .objectReferenceValue as BulletSO;

            if (bullet != null)
                existing.Add(bullet);
        }

        string[] guids = AssetDatabase.FindAssets("t:BulletSO");
        var compatible = new List<BulletSO>();

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            BulletSO bullet = AssetDatabase.LoadAssetAtPath<BulletSO>(path);

            if (bullet == null || existing.Contains(bullet))
                continue;

            if (bullet.AmmoType == ammoType && weapon.IsCompatibleWith(bullet))
                compatible.Add(bullet);
        }

        compatible.Sort((left, right) => string.Compare(left.name, right.name, StringComparison.OrdinalIgnoreCase));

        for (int i = 0; i < compatible.Count; i++)
        {
            int index = candidates.arraySize;
            candidates.InsertArrayElementAtIndex(index);
            SerializedProperty candidate = candidates.GetArrayElementAtIndex(index);
            candidate.FindPropertyRelative("bullet").objectReferenceValue = compatible[i];
            candidate.FindPropertyRelative("weight").floatValue = 1f;
            candidate.FindPropertyRelative("minAmount").intValue = 15;
            candidate.FindPropertyRelative("maxAmount").intValue = 20;
        }
    }

    // Purpose: Resolves the weapon needed by this component.
    private RangedWeaponSO ResolveWeapon(string setId, int sourceWeaponSlotIndex)
    {
        for (int i = 0; i < _weaponEntries.arraySize; i++)
        {
            SerializedProperty entry = _weaponEntries.GetArrayElementAtIndex(i);

            if (!string.Equals(
                    entry.FindPropertyRelative("setId").stringValue.Trim(),
                    setId,
                    StringComparison.Ordinal))
            {
                continue;
            }

            string weaponPropertyName = sourceWeaponSlotIndex switch
            {
                0 => "weaponSlot0",
                1 => "weaponSlot1",
                _ => string.Empty
            };

            return string.IsNullOrEmpty(weaponPropertyName)
                ? null
                : entry.FindPropertyRelative(weaponPropertyName).objectReferenceValue as RangedWeaponSO;
        }

        return null;
    }

    // Purpose: Returns the current weapon set ids.
    private List<string> GetWeaponSetIds()
    {
        var ids = new List<string>();

        for (int i = 0; i < _weaponEntries.arraySize; i++)
        {
            string id = _weaponEntries
                .GetArrayElementAtIndex(i)
                .FindPropertyRelative("setId")
                .stringValue
                .Trim();

            if (!string.IsNullOrEmpty(id) && !ids.Contains(id))
                ids.Add(id);
        }

        return ids;
    }

    // Purpose: Returns the current first weapon set id.
    private string GetFirstWeaponSetId()
    {
        List<string> ids = GetWeaponSetIds();
        return ids.Count > 0 ? ids[0] : string.Empty;
    }

    // Purpose: Builds the pool label from the current authored data.
    private static string BuildPoolLabel(int index, string setId, int destinationAmmoSlotIndex)
    {
        string resolvedSetId = string.IsNullOrWhiteSpace(setId) ? "Unassigned" : setId;
        return $"Pool {index}: {resolvedSetId} -> Inventory Order {destinationAmmoSlotIndex}";
    }

    // Purpose: Draws the inventory stack usage authoring UI.
    private void DrawInventoryStackUsage()
    {
        if (_inventoryEntries == null || !_inventoryEntries.isArray)
            return;

        for (int i = 0; i < _inventoryEntries.arraySize; i++)
        {
            SerializedProperty entry =
                _inventoryEntries.GetArrayElementAtIndex(i);
            ItemSO item =
                entry.FindPropertyRelative("item").objectReferenceValue as ItemSO;

            if (item == null)
                continue;

            int minAmount = Mathf.Max(
                0,
                entry.FindPropertyRelative("minAmount").intValue);
            int maxAmount = Mathf.Max(
                minAmount,
                entry.FindPropertyRelative("maxAmount").intValue);
            int maxStack = Mathf.Max(1, item.MaxStack);
            int minSlots = minAmount <= 0
                ? 0
                : Mathf.CeilToInt(minAmount / (float)maxStack);
            int maxSlots = maxAmount <= 0
                ? 0
                : Mathf.CeilToInt(maxAmount / (float)maxStack);

            string slotText = minSlots == maxSlots
                ? $"{minSlots}"
                : $"{minSlots}-{maxSlots}";

            EditorGUILayout.HelpBox(
                $"Inventory Element {i}: {item.name} | amount {minAmount}-{maxAmount} | " +
                $"Max Stack {maxStack} | expected occupied slots {slotText}",
                maxSlots > 1 ? MessageType.Warning : MessageType.Info);
        }
    }
}
