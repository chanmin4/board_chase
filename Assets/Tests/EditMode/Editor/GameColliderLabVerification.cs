using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Opt-in, preview objects only. Never starts Play or saves an authored scene.
public static class GameColliderLabVerification
{
    private const string Output = "Library/GameColliderLabVerification";
    private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private static double _next;
    [InitializeOnLoadMethod] private static void Register() => EditorApplication.update += Check;
    private static void Check()
    {
        if (EditorApplication.timeSinceStartup < _next) return;
        _next = EditorApplication.timeSinceStartup + 1;
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating ||
            !File.Exists(Output + "/request")) return;
        File.Delete(Output + "/request"); Run();
    }
    [MenuItem("Tools/Verification/Game collider overlay and RUN lab camera (read-only)")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        Directory.CreateDirectory(Output);
        var passes = new List<string>();
        string runBefore = File.ReadAllText("Assets/Scenes/RUNScene.unity");
        var scene = EditorSceneManager.NewPreviewScene();
        DebugMonitorWindow monitor = null;
        try
        {
            GameObject Go(string name, bool ui = false)
            {
                var go = ui ? new GameObject(name, typeof(RectTransform)) : new GameObject(name);
                go.SetActive(false); SceneManager.MoveGameObjectToScene(go, scene); return go;
            }
            var camera = Go("Projection test camera").AddComponent<Camera>();
            camera.fieldOfView = 60; camera.nearClipPlane = .3f;
            camera.pixelRect = new Rect(0,0,800,600);
            var rect = (RectTransform)Go("Overlay rect",true).transform; rect.sizeDelta = new Vector2(800,600);
            Assert.IsTrue(GameColliderOverlay.ProjectSegment(camera,rect,new Vector3(-1,0,5),new Vector3(1,0,5),out var a,out var b));
            Assert.Greater(b.x,a.x); passes.Add("PASS Game wire projects through the selected camera");
            Assert.IsFalse(GameColliderOverlay.ProjectSegment(camera,rect,Vector3.back,Vector3.back*2,out _,out _));
            Assert.IsTrue(GameColliderOverlay.ProjectSegment(camera,rect,new Vector3(0,0,-1),new Vector3(0,1,5),out a,out b));
            Assert.IsFalse(float.IsNaN(a.x) || float.IsInfinity(b.y)); passes.Add("PASS behind-camera and near-plane crossing lines are handled safely");
            using (var vertices = new VertexHelper())
            {
                GameColliderOverlay.AddLine(vertices,Vector2.zero,Vector2.right*10,Color.cyan);
                Assert.AreEqual(4,vertices.currentVertCount); Assert.AreEqual(6,vertices.currentIndexCount);
                GameColliderOverlay.AddLine(vertices,Vector2.zero,Vector2.zero,Color.cyan);
                Assert.AreEqual(4,vertices.currentVertCount); passes.Add("PASS UI wire uses a bounded triangle batch and skips zero-length lines");
            }
            monitor = ScriptableObject.CreateInstance<DebugMonitorWindow>();
            Assert.IsTrue((bool)typeof(DebugMonitorWindow).GetField("_showGameColliders",Fields).GetValue(monitor));
            Assert.IsFalse((bool)typeof(DebugMonitorWindow).GetField("_showSceneColliders",Fields).GetValue(monitor));
            var box = Go("Box shape").AddComponent<BoxCollider>(); box.center = new Vector3(2,1,3); box.size = new Vector3(4,2,6);
            typeof(DebugMonitorWindow).GetMethod("BuildGameWire",Fields).Invoke(monitor,new object[] { box });
            var lines = (List<Vector3>)typeof(DebugMonitorWindow).GetField("_gameColliderLines",Fields).GetValue(monitor);
            Assert.AreEqual(24,lines.Count); passes.Add("PASS Scene overlay defaults off and box geometry follows actual center/size");
            var nativeBody = Go("Missing native component test").AddComponent<UnityEngine.AI.NavMeshAgent>(); nativeBody.enabled = false;
            var motor = ShooterBodyCollisionMotor.Ensure(nativeBody);
            Assert.IsNotNull(motor.GetComponent<CharacterController>()); Assert.IsFalse(nativeBody.updatePosition);
            Assert.AreSame(motor,ShooterBodyCollisionMotor.Ensure(nativeBody)); passes.Add("PASS missing CharacterController is created with Unity-null checks and reused");

            var driver = Go("Camera copy test").AddComponent<CombatLabRuntime>();
            driver.CameraRig = Go("Existing RUN camera rig").AddComponent<CameraManager>();
            var copyCamera = Go("Existing Main Camera").AddComponent<Camera>();
            copyCamera.transform.SetParent(driver.CameraRig.transform);
            typeof(CameraManager).GetField("mainCamera",Fields).SetValue(driver.CameraRig,copyCamera);
            typeof(CameraManager).GetField("cameraTarget",Fields).SetValue(driver.CameraRig,copyCamera.transform);
            var sourceScene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/RUNScene.unity");
            try
            {
                CameraManager source = null;
                foreach (var root in sourceScene.GetRootGameObjects())
                { source = root.GetComponentInChildren<CameraManager>(true); if (source != null) break; }
                Assert.IsNotNull(source);
                var sourceCamera = source.GetComponentInChildren<Camera>(true);
                Assert.IsNotNull(sourceCamera);
                Assert.That(File.ReadAllText("Assets/Scripts/Authoring/Editor/CombatLabSession.cs"), Does.Not.Contain("CopyRunCamera"));
                passes.Add("PASS live lab no longer clones the RUN camera");
            }
            finally { EditorSceneManager.ClosePreviewScene(sourceScene); }
            var player = Go("Follow position test"); player.transform.position = new Vector3(20,0,30);
            driver.CameraRig.SetFollowTarget(player.transform,true);
            Assert.AreSame(player.transform,typeof(CameraManager).GetField("followTarget",Fields).GetValue(driver.CameraRig));
            Assert.Greater(Vector3.Distance(copyCamera.transform.position,Vector3.zero),10);
            Assert.AreEqual(runBefore,File.ReadAllText("Assets/Scenes/RUNScene.unity"));
            passes.Add("PASS existing rig can rebind the borrowed player without changing RUNScene");
            File.WriteAllLines(Output+"/PASSED.txt",passes);
            if (File.Exists(Output+"/FAILED.txt")) File.Delete(Output+"/FAILED.txt");
            Debug.Log($"[GameColliderLab] {passes.Count} isolated checks passed; live Game rendering still needs a Play check.");
        }
        catch (Exception error)
        {
            File.WriteAllLines(Output+"/FAILED.txt",passes.ConvertAll(p=>p).ToArray());
            File.AppendAllText(Output+"/FAILED.txt",error.ToString());
            if (File.Exists(Output+"/PASSED.txt")) File.Delete(Output+"/PASSED.txt");
            Debug.LogException(error);
        }
        finally
        {
            if (monitor != null) Object.DestroyImmediate(monitor);
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }
}
