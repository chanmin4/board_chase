using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class ShooterMeleeAnimationSetup
{
    private const string ClipPath = "Assets/AnimationClips/Shooter_MeleeAttack.anim";
    private const string ControllerPath = "Assets/AnimatorController/Shooter.controller";
    private const string WeaponSocketPath = "Root/RightHand_WeaponSocket";
    private const string TriggerName = "MeleeAttack";
    private const string LocomotionStateName = "IdleMove";
    private const string MeleeStateName = "MeleeAttack";
    private const float ClipDuration = 0.55f;

    // Intentionally not exposed as a Tools menu item. This rebuild helper is
    // for controlled maintenance of the shared test animation only.
    // Purpose: Repairs or rebuilds rebuild from the current source data.
    public static void Rebuild()
    {
        AnimationClip clip = CreateOrUpdateClip();
        AnimatorController controller =
            AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);

        if (controller == null)
        {
            Debug.LogError(
                $"[ShooterMeleeAnimationSetup] Shooter Animator Controller was not found at '{ControllerPath}'.");
            return;
        }

        ConfigureController(controller, clip);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorGUIUtility.PingObject(clip);

        Debug.Log(
            $"[ShooterMeleeAnimationSetup] Rebuilt '{ClipPath}' and connected Animator trigger '{TriggerName}'.",
            clip);
    }

    // Purpose: Creates the or update clip owned by this workflow.
    private static AnimationClip CreateOrUpdateClip()
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath);

        if (clip == null)
        {
            clip = new AnimationClip
            {
                name = "Shooter_MeleeAttack",
                frameRate = 30f,
                wrapMode = WrapMode.Once
            };

            AssetDatabase.CreateAsset(clip, ClipPath);
        }

        Undo.RecordObject(clip, "Rebuild Shooter Melee Attack Animation");

        EditorCurveBinding[] oldBindings = AnimationUtility.GetCurveBindings(clip);

        for (int i = 0; i < oldBindings.Length; i++)
            AnimationUtility.SetEditorCurve(clip, oldBindings[i], null);

        float[] times = { 0f, 0.08f, 0.16f, 0.28f, 0.38f, ClipDuration };

        SetTransformCurve(
            clip,
            "m_LocalPosition.x",
            times,
            new[] { 0.6f, 0.82f, 0.86f, -0.55f, -0.35f, 0.6f });
        SetTransformCurve(
            clip,
            "m_LocalPosition.y",
            times,
            new[] { 1f, 0.74f, 0.72f, 0.70f, 0.82f, 1f });
        SetTransformCurve(
            clip,
            "m_LocalPosition.z",
            times,
            new[] { 0.2f, -0.05f, 0.05f, 0.46f, 0.35f, 0.2f });

        // Wind up on the right, cut across the front, then return to the shared
        // socket rest pose. The visual prefabs share this exact transform path.
        SetTransformCurve(
            clip,
            "localEulerAnglesRaw.x",
            times,
            new[] { 0f, 15f, 10f, -10f, -20f, 0f });
        SetTransformCurve(
            clip,
            "localEulerAnglesRaw.y",
            times,
            new[] { 0f, -70f, -80f, 30f, 80f, 0f });
        SetTransformCurve(
            clip,
            "localEulerAnglesRaw.z",
            times,
            new[] { 0f, -35f, -40f, 10f, 35f, 0f });

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = false;
        settings.startTime = 0f;
        settings.stopTime = ClipDuration;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        EditorUtility.SetDirty(clip);
        return clip;
    }

    // Purpose: Configures this component''s controller.
    private static void ConfigureController(
        AnimatorController controller,
        AnimationClip clip)
    {
        Undo.RecordObject(controller, "Configure Shooter Melee Attack Animator");

        if (!HasParameter(controller, TriggerName, AnimatorControllerParameterType.Trigger))
            controller.AddParameter(TriggerName, AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        AnimatorState locomotionState = FindState(stateMachine, LocomotionStateName);

        if (locomotionState == null)
        {
            Debug.LogError(
                $"[ShooterMeleeAnimationSetup] Locomotion state '{LocomotionStateName}' is missing.",
                controller);
            return;
        }

        AnimatorState meleeState = FindState(stateMachine, MeleeStateName);

        if (meleeState == null)
            meleeState = stateMachine.AddState(MeleeStateName, new Vector3(360f, 260f));

        meleeState.motion = clip;
        meleeState.speed = 1f;
        meleeState.writeDefaultValues = true;

        if (!HasAnyStateTriggerTransition(stateMachine, meleeState, TriggerName))
        {
            AnimatorStateTransition enterTransition =
                stateMachine.AddAnyStateTransition(meleeState);
            enterTransition.hasExitTime = false;
            enterTransition.duration = 0.03f;
            enterTransition.canTransitionToSelf = false;
            enterTransition.AddCondition(AnimatorConditionMode.If, 0f, TriggerName);
        }

        if (!HasTransition(meleeState, locomotionState))
        {
            AnimatorStateTransition exitTransition =
                meleeState.AddTransition(locomotionState);
            exitTransition.hasExitTime = true;
            exitTransition.exitTime = 0.95f;
            exitTransition.duration = 0.05f;
            exitTransition.hasFixedDuration = true;
        }

        EditorUtility.SetDirty(controller);
        EditorUtility.SetDirty(stateMachine);
    }

    // Purpose: Updates the transform curve state.
    private static void SetTransformCurve(
        AnimationClip clip,
        string propertyName,
        float[] times,
        float[] values)
    {
        Keyframe[] keys = new Keyframe[times.Length];

        for (int i = 0; i < keys.Length; i++)
            keys[i] = new Keyframe(times[i], values[i]);

        AnimationCurve curve = new AnimationCurve(keys);

        for (int i = 0; i < curve.length; i++)
        {
            AnimationUtility.SetKeyLeftTangentMode(
                curve,
                i,
                AnimationUtility.TangentMode.Linear);
            AnimationUtility.SetKeyRightTangentMode(
                curve,
                i,
                AnimationUtility.TangentMode.Linear);
        }

        AnimationUtility.SetEditorCurve(
            clip,
            EditorCurveBinding.FloatCurve(
                WeaponSocketPath,
                typeof(Transform),
                propertyName),
            curve);
    }

    // Purpose: Checks whether this component has parameter.
    private static bool HasParameter(
        AnimatorController controller,
        string parameterName,
        AnimatorControllerParameterType type)
    {
        AnimatorControllerParameter[] parameters = controller.parameters;

        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].name == parameterName && parameters[i].type == type)
                return true;
        }

        return false;
    }

    // Purpose: Finds the requested state.
    private static AnimatorState FindState(
        AnimatorStateMachine stateMachine,
        string stateName)
    {
        ChildAnimatorState[] states = stateMachine.states;

        for (int i = 0; i < states.Length; i++)
        {
            if (states[i].state != null && states[i].state.name == stateName)
                return states[i].state;
        }

        return null;
    }

    // Purpose: Checks whether this component has any state trigger transition.
    private static bool HasAnyStateTriggerTransition(
        AnimatorStateMachine stateMachine,
        AnimatorState destination,
        string triggerName)
    {
        AnimatorStateTransition[] transitions = stateMachine.anyStateTransitions;

        for (int i = 0; i < transitions.Length; i++)
        {
            AnimatorStateTransition transition = transitions[i];

            if (transition.destinationState != destination)
                continue;

            AnimatorCondition[] conditions = transition.conditions;

            for (int j = 0; j < conditions.Length; j++)
            {
                if (conditions[j].parameter == triggerName)
                    return true;
            }
        }

        return false;
    }

    // Purpose: Checks whether this component has transition.
    private static bool HasTransition(
        AnimatorState source,
        AnimatorState destination)
    {
        AnimatorStateTransition[] transitions = source.transitions;

        for (int i = 0; i < transitions.Length; i++)
        {
            if (transitions[i].destinationState == destination)
                return true;
        }

        return false;
    }
}
