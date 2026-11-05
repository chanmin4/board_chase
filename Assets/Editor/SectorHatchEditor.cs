using UnityEditor;
using UnityEngine;

/// <summary>Edits the authored Collider directly; there is no second runtime radius setting.</summary>
[CustomEditor(typeof(SectorHatch))]
[CanEditMultipleObjects]
public sealed class SectorHatchEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        using (new EditorGUI.DisabledScope(true))
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));
        EditorGUILayout.LabelField("Visuals", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("_closedVisual"), new GUIContent("Closed Object"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("_openedVisual"), new GUIContent("Opened Object"));
        EditorGUILayout.HelpBox("이미 배치한 오브젝트 두 개만 연결하세요. 새로 생성하지 않고 SetActive로 켜고 끕니다. 기본은 Closed 켜기 / Opened 끄기. HatchRoot·트리거·Anchor는 계속 켜둡니다.", MessageType.Info);
        if (targets.Length == 1)
        {
            serializedObject.ApplyModifiedProperties();
            if (!((SectorHatch)target).ValidateVisuals(out string problem))
                EditorGUILayout.HelpBox(Explain(problem), MessageType.Warning);
            serializedObject.Update();
        }
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Interaction — 미리 배치한 Collider", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("_interactionTrigger"));
        serializedObject.ApplyModifiedProperties();

        if (targets.Length == 1) DrawTrigger((SectorHatch)target);
        else EditorGUILayout.HelpBox("콜라이더 범위를 편집하려면 해치 하나를 선택하세요.", MessageType.Info);

        serializedObject.Update();
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Prompt", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("_powerRequiredPrompt"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("_extractPrompt"));
        serializedObject.ApplyModifiedProperties();
    }

    private void DrawTrigger(SectorHatch hatch)
    {
        if (!hatch.ValidateInteractionTrigger(out string problem))
            EditorGUILayout.HelpBox(Explain(problem), MessageType.Warning);
        var trigger = hatch.InteractionTrigger;
        if (trigger == null)
        {
            using (new EditorGUI.DisabledScope(Application.isPlaying || EditorUtility.IsPersistent(hatch)))
                if (GUILayout.Button("상호작용 트리거 만들고 연결")) CreateTrigger(hatch);
            EditorGUILayout.HelpBox("실행 중 자동 생성하지 않습니다. 기존 Collider를 연결하거나 위 버튼으로 편집 모드에서 미리 만들어두세요.", MessageType.Info);
            return;
        }

        int itemLayer = LayerMask.NameToLayer("ObjectItem");
        if (itemLayer >= 0 && trigger.gameObject.layer != itemLayer)
        {
            EditorGUILayout.HelpBox("플레이어가 감지하도록 트리거 오브젝트의 Layer를 ObjectItem으로 설정하는 것을 권장합니다.", MessageType.Warning);
            if (GUILayout.Button("트리거 Layer를 ObjectItem으로 설정"))
            {
                Undo.RecordObject(trigger.gameObject, "Set hatch interaction layer");
                trigger.gameObject.layer = itemLayer;
                PrefabUtility.RecordPrefabInstancePropertyModifications(trigger.gameObject);
            }
        }
        EditorGUILayout.HelpBox("아래 값은 연결된 Collider 자체의 값입니다. HatchRoot를 선택하면 Scene에 녹색 범위가 표시됩니다. Box는 Size, Sphere는 Radius로 조절하세요.", MessageType.Info);
        var colliderData = new SerializedObject(trigger);
        colliderData.Update();
        EditorGUILayout.PropertyField(colliderData.FindProperty("m_Enabled"), new GUIContent("Collider Enabled"));
        EditorGUILayout.PropertyField(colliderData.FindProperty("m_IsTrigger"), new GUIContent("Is Trigger"));
        if (trigger is BoxCollider)
        {
            EditorGUILayout.PropertyField(colliderData.FindProperty("m_Center"), new GUIContent("Center"));
            EditorGUILayout.PropertyField(colliderData.FindProperty("m_Size"), new GUIContent("Size"));
        }
        else if (trigger is SphereCollider || trigger is CapsuleCollider)
        {
            EditorGUILayout.PropertyField(colliderData.FindProperty("m_Center"), new GUIContent("Center"));
            EditorGUILayout.PropertyField(colliderData.FindProperty("m_Radius"), new GUIContent("Radius"));
            if (trigger is CapsuleCollider)
            {
                EditorGUILayout.PropertyField(colliderData.FindProperty("m_Height"), new GUIContent("Height"));
                EditorGUILayout.PropertyField(colliderData.FindProperty("m_Direction"), new GUIContent("Direction"));
            }
        }
        else EditorGUILayout.HelpBox("이 Collider 형상의 상세 설정은 원래 Collider Inspector에서 편집하세요. Scene에는 Bounds가 표시됩니다.", MessageType.Info);
        if (colliderData.ApplyModifiedProperties()) SceneView.RepaintAll();
        if (GUILayout.Button("연결된 Collider 오브젝트 선택")) Selection.activeGameObject = trigger.gameObject;
    }

    private void CreateTrigger(SectorHatch hatch)
    {
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Create authored hatch interaction trigger");
        var child = new GameObject("InteractionTrigger");
        Undo.RegisterCreatedObjectUndo(child, "Create hatch interaction trigger");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(child, hatch.gameObject.scene);
        Undo.SetTransformParent(child.transform, hatch.transform, "Parent hatch interaction trigger");
        child.transform.localPosition = Vector3.zero;
        child.transform.localRotation = Quaternion.identity;
        child.transform.localScale = Vector3.one;
        int itemLayer = LayerMask.NameToLayer("ObjectItem");
        if (itemLayer >= 0) child.layer = itemLayer;
        var sphere = Undo.AddComponent<SphereCollider>(child);
        sphere.isTrigger = true; sphere.radius = 1.7f; sphere.center = new Vector3(0, .6f, 0);
        serializedObject.Update();
        serializedObject.FindProperty("_interactionTrigger").objectReferenceValue = sphere;
        serializedObject.ApplyModifiedProperties();
        Undo.CollapseUndoOperations(group);
        SceneView.RepaintAll();
    }

    private void OnSceneGUI()
    {
        foreach (Object selected in targets)
        {
            var trigger = ((SectorHatch)selected).InteractionTrigger;
            if (trigger == null) continue;
            if (trigger is BoxCollider box)
            {
                using (new Handles.DrawingScope(Color.green, box.transform.localToWorldMatrix))
                    Handles.DrawWireCube(box.center, box.size);
            }
            else if (trigger is SphereCollider sphere)
            {
                Vector3 scale = sphere.transform.lossyScale;
                float radius = sphere.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
                Vector3 center = sphere.transform.TransformPoint(sphere.center);
                using (new Handles.DrawingScope(Color.green))
                {
                    Handles.DrawWireDisc(center, Vector3.up, radius);
                    Handles.DrawWireDisc(center, Vector3.right, radius);
                    Handles.DrawWireDisc(center, Vector3.forward, radius);
                }
            }
            else
            {
                using (new Handles.DrawingScope(Color.green))
                    Handles.DrawWireCube(trigger.bounds.center, trigger.bounds.size);
            }
        }
    }

    private static string Explain(string problem) => problem switch
    {
        "Closed and Opened Visual must both be assigned." => "Closed Object / Opened Object에 이미 배치한 오브젝트 두 개를 연결하세요.",
        "Closed/Open visuals must be separate sibling roots, not the same or nested objects." => "Closed / Opened는 서로 포함되지 않는 별도 자식이어야 합니다.",
        "Visual objects must be already placed in a Scene, not Project prefab assets." => "이미 배치한 오브젝트를 연결하세요. Project 에셋은 생성하거나 켜고 끄지 않습니다.",
        "Visual objects must not include this component's own root or its parent objects." => "SectorHatch 자신이나 부모 전체를 외형으로 연결하면 상호작용도 꺼집니다. 별도의 닫힘·열림 오브젝트를 연결하세요.",
        "Keep InteractionPromptAnchor outside the visual roots so both hatch states retain it." => "InteractionPromptAnchor는 Closed / Opened에서 제거하고, 항상 켜진 HatchRoot 쪽에 두세요.",
        "Interaction Trigger is not assigned." => "Interaction Trigger를 미리 배치하고 연결하세요.",
        "Interaction Trigger must belong to this HatchRoot." => "트리거는 HatchRoot 자신 또는 그 자식에 있어야 합니다.",
        "Interaction Trigger must be outside the Closed/Open visual roots." => "트리거를 Closed/Open 안에 넣지 말고, 두 외형과 별개의 항상 켜진 자식으로 두세요.",
        "Interaction Trigger requires Is Trigger." => "연결된 Collider의 Is Trigger를 켜세요.",
        "Interaction Trigger Collider is disabled." => "연결된 Collider 컴포넌트를 켜세요.",
        "Interaction Trigger and its parent objects must remain active." => "트리거와 그 부모 오브젝트는 계속 켜두세요.",
        _ => problem
    };
}
