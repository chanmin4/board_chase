using System;
using System.Reflection;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>테스트 오브젝트는 생성 순간부터 미리보기 씬에 둔다. 생성 후 이동하면 사용자 씬에 dirty 표시가 남는다.</summary>
internal static class ShooterCustomizeTestObjects
{
    // Unity's PreviewRenderUtility uses this same internal editor hook. Keep this version-specific
    // dependency in tests, not in production authoring code, and restore the default target immediately.
    private static readonly MethodInfo Target = typeof(EditorSceneManager).GetMethod("SetTargetSceneForNewGameObjects",
        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(int) }, null) ??
        typeof(EditorSceneManager).GetMethod("SetTargetSceneForNewGameObjects", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
            null, new[] { typeof(Scene) }, null);

    private static T InScene<T>(Scene scene, Func<T> create)
    {
        if (Target == null) throw new InvalidOperationException("Unity preview-scene creation hook is unavailable; do not create test objects in the user scene.");
        bool handle = Target.GetParameters()[0].ParameterType == typeof(int);
        Target.Invoke(null, new[] { handle ? (object)scene.handle : scene });
        try { return create(); }
        finally { Target.Invoke(null, new[] { handle ? (object)0 : default(Scene) }); }
    }

    public static GameObject Create(string name, Scene scene) => InScene(scene, () => new GameObject(name));
    public static GameObject Primitive(PrimitiveType type, Scene scene) => InScene(scene, () => GameObject.CreatePrimitive(type));
}
