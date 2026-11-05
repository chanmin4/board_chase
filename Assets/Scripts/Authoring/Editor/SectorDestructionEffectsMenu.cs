using UnityEditor;
using UnityEngine;

public static class SectorDestructionEffectsMenu
{
    [MenuItem("Tools/Effects/엄폐 파괴 연기 설정")]
    private static void Open()
    {
        var settings = AssetDatabase.LoadAssetAtPath<SectorDestructionEffectsSO>("Assets/Resources/SectorDestructionEffects.asset");
        if (settings == null)
        {
            Debug.LogWarning("Assets/Resources/SectorDestructionEffects.asset 설정을 찾을 수 없습니다.");
            return;
        }
        Selection.activeObject = settings;
        EditorGUIUtility.PingObject(settings);
    }
}
