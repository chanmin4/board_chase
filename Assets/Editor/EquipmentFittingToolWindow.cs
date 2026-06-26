using UnityEditor;
using UnityEngine;

public class EquipmentFittingToolWindow : EditorWindow
{
    private Object _targetItemSO;
    private Transform _previewTransform;

    [MenuItem("Tools/VSplatter/Equipment Fitting Tool")]
    private static void Open()
    {
        GetWindow<EquipmentFittingToolWindow>("Equipment Fitting");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Equipment Fitting Tool", EditorStyles.boldLabel);
        EditorGUILayout.Space(6f);

        _targetItemSO = EditorGUILayout.ObjectField(
            "Target Item SO",
            _targetItemSO,
            typeof(ScriptableObject),
            false);

        _previewTransform = (Transform)EditorGUILayout.ObjectField(
            "Preview Transform",
            _previewTransform,
            typeof(Transform),
            true);

        EditorGUILayout.Space(6f);

        if (GUILayout.Button("Use Selected Transform"))
        {
            if (Selection.activeTransform != null)
                _previewTransform = Selection.activeTransform;
        }

        EditorGUILayout.Space(6f);

        using (new EditorGUI.DisabledScope(_targetItemSO == null || _previewTransform == null))
        {
            if (GUILayout.Button("Copy Preview Transform To Target SO"))
                CopyPreviewTransformToTarget();

            if (GUILayout.Button("Apply Target SO Placement To Preview Transform"))
                ApplyTargetPlacementToPreview();
        }

        EditorGUILayout.Space(8f);

        if (_targetItemSO != null && !IsSupportedTarget(_targetItemSO))
        {
            EditorGUILayout.HelpBox(
                "Target Item SO must be WeaponSO or ArmorItemSO.",
                MessageType.Warning);
        }

        EditorGUILayout.HelpBox(
            "Workflow: put item prefab under the correct socket in a preview scene, adjust local transform, then copy it into the item SO.",
            MessageType.Info);
    }

    private void CopyPreviewTransformToTarget()
    {
        if (_targetItemSO == null || _previewTransform == null)
            return;

        SerializedObject serialized = new SerializedObject(_targetItemSO);

        if (!TryGetPlacementProperties(
                serialized,
                out SerializedProperty usePrefabLocalTransform,
                out SerializedProperty localPosition,
                out SerializedProperty localEulerAngles,
                out SerializedProperty localScale))
        {
            Debug.LogWarning("[EquipmentFittingTool] Unsupported target SO.", _targetItemSO);
            return;
        }

        Undo.RecordObject(_targetItemSO, "Copy Equipment Fitting Transform");

        usePrefabLocalTransform.boolValue = false;
        localPosition.vector3Value = _previewTransform.localPosition;
        localEulerAngles.vector3Value = _previewTransform.localEulerAngles;
        localScale.vector3Value = _previewTransform.localScale;

        serialized.ApplyModifiedProperties();

        EditorUtility.SetDirty(_targetItemSO);
        AssetDatabase.SaveAssets();

        Debug.Log(
            $"[EquipmentFittingTool] Copied local transform to {_targetItemSO.name}.",
            _targetItemSO);
    }

    private void ApplyTargetPlacementToPreview()
    {
        if (_targetItemSO == null || _previewTransform == null)
            return;

        SerializedObject serialized = new SerializedObject(_targetItemSO);

        if (!TryGetPlacementProperties(
                serialized,
                out _,
                out SerializedProperty localPosition,
                out SerializedProperty localEulerAngles,
                out SerializedProperty localScale))
        {
            Debug.LogWarning("[EquipmentFittingTool] Unsupported target SO.", _targetItemSO);
            return;
        }

        Undo.RecordObject(_previewTransform, "Apply Equipment Fitting Transform");

        _previewTransform.localPosition = localPosition.vector3Value;
        _previewTransform.localRotation = Quaternion.Euler(localEulerAngles.vector3Value);
        _previewTransform.localScale = localScale.vector3Value;

        EditorUtility.SetDirty(_previewTransform);

        Debug.Log(
            $"[EquipmentFittingTool] Applied {_targetItemSO.name} placement to {_previewTransform.name}.",
            _previewTransform);
    }

    private static bool TryGetPlacementProperties(
        SerializedObject serialized,
        out SerializedProperty usePrefabLocalTransform,
        out SerializedProperty localPosition,
        out SerializedProperty localEulerAngles,
        out SerializedProperty localScale)
    {
        usePrefabLocalTransform = null;
        localPosition = null;
        localEulerAngles = null;
        localScale = null;

        Object target = serialized.targetObject;

        if (target is WeaponSO)
        {
            usePrefabLocalTransform = serialized.FindProperty("_useWeaponViewPrefabLocalTransform");
            localPosition = serialized.FindProperty("_weaponViewLocalPosition");
            localEulerAngles = serialized.FindProperty("_weaponViewLocalEulerAngles");
            localScale = serialized.FindProperty("_weaponViewLocalScale");
        }
        else if (target is ArmorItemSO)
        {
            usePrefabLocalTransform = serialized.FindProperty("_useEquippedVisualPrefabLocalTransform");
            localPosition = serialized.FindProperty("_equippedVisualLocalPosition");
            localEulerAngles = serialized.FindProperty("_equippedVisualLocalEulerAngles");
            localScale = serialized.FindProperty("_equippedVisualLocalScale");
        }

        return usePrefabLocalTransform != null &&
               localPosition != null &&
               localEulerAngles != null &&
               localScale != null;
    }

    private static bool IsSupportedTarget(Object target)
    {
        return target is WeaponSO || target is ArmorItemSO;
    }
}