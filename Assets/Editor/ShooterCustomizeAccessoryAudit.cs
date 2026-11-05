using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>읽기 전용 실제 Unity 임포트 검사. FBX 정점/Importer/Prefab/외형 데이터를 저장하지 않는다.</summary>
public static class ShooterCustomizeAccessoryAudit
{
    public const string Report = "Library/ShooterCustomizeVerification/accessory-unity-audit.txt";

    [MenuItem("Tools/Authoring/Verification/Audit Shooter Accessories (Read Only)")]
    public static void Run()
    {
        var log = new StringBuilder("Accessory audit: positions and bounds are prefab-local, after Unity FBX import.\n");
        int count = 0, offCenter = 0, nonzeroRoot = 0;
        foreach (var path in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/GameScene/Shooter/Accesory" })
            .Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p))
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;
            count++; if (prefab.transform.localPosition.sqrMagnitude > .000001f) nonzeroRoot++;
            log.AppendLine(path);
            log.AppendLine(" root position=" + prefab.transform.localPosition.ToString("F4") + " rotation=" + prefab.transform.localEulerAngles.ToString("F4") + " scale=" + prefab.transform.localScale.ToString("F4"));
            if (ShooterCustomizeAccessoryPlacement.TryBounds(prefab.transform, prefab.transform, out var bounds))
            {
                log.AppendLine(" geometry center=" + bounds.center.ToString("F4") + " size=" + bounds.size.ToString("F4"));
                if (bounds.center.sqrMagnitude > .000001f) offCenter++;
            }
            foreach (var renderer in prefab.GetComponentsInChildren<Renderer>(true))
                log.AppendLine(" renderer " + AnimationUtility.CalculateTransformPath(renderer.transform, prefab.transform) +
                    " localPosition=" + renderer.transform.localPosition.ToString("F4") + " mesh bounds center=" + renderer.localBounds.center.ToString("F4"));
        }
        log.AppendLine($"{count} prefabs; {nonzeroRoot} nonzero root positions; {offCenter} off-center imported geometry bounds.");
        var visual = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/GameScene/Shooter/Visual_Shooter.prefab");
        if (visual != null)
            foreach (var socket in visual.GetComponentsInChildren<Transform>(true).Where(t => t.name.EndsWith("Socket")))
                log.AppendLine(socket.name + " in Visual=" + visual.transform.InverseTransformPoint(socket.position).ToString("F4"));
        Directory.CreateDirectory(Path.GetDirectoryName(Report)); File.WriteAllText(Report, log.ToString());
        Debug.Log("[Shooter accessory audit] " + count + " prefabs checked. " + Report);
    }
}
