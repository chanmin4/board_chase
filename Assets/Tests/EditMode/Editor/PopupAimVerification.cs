using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class PopupAimVerification
{
    private const string Output = "Library/StableAimVerification";
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
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
    private static void Set(object owner, string name, object value) => owner.GetType().GetField(name, Flags).SetValue(owner,value);
    private static object Call(object owner, string name) => owner.GetType().GetMethod(name, Flags).Invoke(owner,null);
    [MenuItem("Tools/Verification/Popup mouse aim freeze (read-only)")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        Directory.CreateDirectory(Output);
        bool blockedBefore = GameplayActionInputBlocker.IsCombatInputBlocked;
        var sourceBefore = (Object)typeof(GameplayActionInputBlocker).GetField("_source",Flags).GetValue(null);
        var scene = EditorSceneManager.NewPreviewScene();
        var passes = new List<string>();
        PlayerAimController aim = null;
        PlayerShooterVisionController vision = null;
        try
        {
            var root = new GameObject("Temporary popup aim test");
            SceneManager.MoveGameObjectToScene(root,scene);
            aim = root.AddComponent<PlayerAimController>();
            vision = root.AddComponent<PlayerShooterVisionController>();
            var character = root.AddComponent<PlayerCharacter>();
            var group = root.AddComponent<CanvasGroup>();
            Set(vision,"_aimAction",aim); Set(vision,"_presentationForward",Vector3.right);
            Set(vision,"_presentationFrame",-1); Set(vision,"_presentationAim01",.75f);
            Vector3 savedAim = new Vector3(10,0,20);
            Set(aim,"_aimWorldPoint",savedAim); Set(aim,"_hasAimPoint",true);
            GameplayActionInputBlocker.SetBlocked(false,null);
            bool mainOpen = true, childOpen = true;
            UIEscapeRouter.Register(aim,()=>mainOpen,()=>mainOpen=false);
            UIEscapeRouter.Register(vision,()=>childOpen,()=>childOpen=false,100);
            Assert.IsTrue(UIEscapeRouter.HasOpenPopup); Assert.IsTrue(GameplayActionInputBlocker.IsAimBlocked);
            passes.Add("PASS visible registered popup blocks gameplay aiming without switching UI input maps");
            Assert.IsFalse(aim.TryGetRawAimPoint(out var raw)); Assert.AreEqual(savedAim,raw);
            Assert.IsFalse(aim.TryGetAimPoint(out _)); Call(aim,"UpdateAimPoint");
            Assert.IsTrue(aim.HasAimPoint); Assert.AreEqual(savedAim,aim.AimWorldPoint);
            passes.Add("PASS live mouse sampling stops while the last gameplay aim point stays intact");
            vision.RefreshPresentationAimPoint(); Assert.AreEqual(Vector3.right,vision.ForwardDirection);
            Assert.AreEqual(.75f,(float)Call(vision,"ResolveAim01"));
            Set(vision,"_presentationFrame",-1);
            Assert.AreEqual(Vector3.right,vision.ForwardDirection);
            passes.Add("PASS presentation vision keeps its last direction and aim shape across frames");
            var rotate = new ShooterRotateAction(); Set(rotate,"_transform",root.transform);
            Set(rotate,"_character",character); Set(rotate,"_turnSmoothSpeed",5f);
            Quaternion before = root.transform.rotation; rotate.OnUpdate();
            Assert.AreEqual(before,root.transform.rotation);
            Assert.AreEqual(0f,(float)typeof(ShooterRotateAction).GetField("_turnSmoothSpeed",Flags).GetValue(rotate));
            passes.Add("PASS player body and weapon rotation return before mouse or smoothing updates");
            mainOpen = false; Assert.IsTrue(GameplayActionInputBlocker.IsAimBlocked);
            childOpen = false; Assert.IsFalse(GameplayActionInputBlocker.IsAimBlocked);
            passes.Add("PASS remaining nested popup keeps aim locked and closing the last popup releases it");
            mainOpen = true; group.alpha = 0;
            Assert.IsFalse(UIEscapeRouter.HasOpenPopup); group.alpha = 1;
            root.SetActive(false); Assert.IsFalse(UIEscapeRouter.HasOpenPopup); root.SetActive(true);
            Assert.IsTrue(UIEscapeRouter.HasOpenPopup);
            passes.Add("PASS hidden or inactive popup owners do not leave an aim lock behind");
            mainOpen = false; Set(aim,"_aimWorldPoint",Vector3.forward*30);
            Assert.AreEqual(Vector3.forward,vision.ForwardDirection);
            Assert.AreEqual(0f,(float)Call(vision,"ResolveAim01"));
            passes.Add("PASS normal gameplay direction and aim shape resume after all popups close");
            TestStationaryDirection(scene,passes);
            File.WriteAllLines(Output+"/PASSED.txt",passes);
            if (File.Exists(Output+"/FAILED.txt")) File.Delete(Output+"/FAILED.txt");
            Debug.Log($"[PopupAim] {passes.Count} isolated checks passed; no authored scene or save changes.");
        }
        catch (Exception error)
        {
            File.WriteAllLines(Output+"/FAILED.txt",passes);
            File.AppendAllText(Output+"/FAILED.txt",error.ToString());
            if (File.Exists(Output+"/PASSED.txt")) File.Delete(Output+"/PASSED.txt");
            Debug.LogException(error);
        }
        finally
        {
            if (aim != null) UIEscapeRouter.Unregister(aim);
            if (vision != null) UIEscapeRouter.Unregister(vision);
            GameplayActionInputBlocker.SetBlocked(blockedBefore,sourceBefore);
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }
    private static Vector3 Sample(PlayerAimController aim,Vector2 pointer)
    {
        object[] args = { pointer, Vector3.zero };
        Assert.IsTrue((bool)typeof(PlayerAimController).GetMethod("TrySampleAimPointer",Flags).Invoke(aim,args));
        return (Vector3)args[1];
    }
    private static void TestStationaryDirection(Scene scene,List<string> passes)
    {
        var player = new GameObject("Temporary stable direction player"); player.SetActive(false);
        SceneManager.MoveGameObjectToScene(player,scene); player.transform.position = new Vector3(21000,0,21000);
        var aim = player.AddComponent<PlayerAimController>();
        var vision = player.AddComponent<PlayerShooterVisionController>(); Set(vision,"_aimAction",aim);
        var cameraObject = new GameObject("Temporary stable direction camera"); cameraObject.SetActive(false);
        SceneManager.MoveGameObjectToScene(cameraObject,scene);
        var camera = cameraObject.AddComponent<Camera>(); camera.pixelRect = new Rect(0,0,800,600);
        camera.aspect = 4f/3; camera.fieldOfView = 30;
        camera.transform.SetPositionAndRotation(new Vector3(21000,100,20960),Quaternion.Euler(70,0,0));
        Set(aim,"_aimCamera",camera); Set(aim,"_aimRaycastMask",(LayerMask)0); Set(aim,"_allowFallbackPlane",true);
        Vector2 pointer = new Vector2(150,300);
        Vector3 initial = Sample(aim,pointer), offset = initial-player.transform.position;
        Assert.IsTrue(aim.TryGetFlatAimDirection(player.transform.position,out var heldDirection));
        for (int i=0;i<60;i++)
        {
            player.transform.position += new Vector3(.2f,0,.3f);
            camera.transform.position += new Vector3(.1f,0,.25f);
            var point = Sample(aim,pointer);
            Assert.Less(Vector3.Distance(player.transform.position+offset,point),.001f);
            Assert.IsTrue(aim.TryGetFlatAimDirection(player.transform.position+Vector3.right*2,out var direction));
            Assert.Less(Vector3.Angle(heldDirection,direction),.001f);
        }
        passes.Add("PASS 60 moving camera/player samples retain exactly the same direction with a stationary pointer");
        passes.Add("PASS held shoot/throw target translates with the player and ignores changing origin offsets");
        Sample(aim,pointer+new Vector2(200,80));
        Assert.IsTrue(aim.TryGetFlatAimDirection(player.transform.position,out var movedDirection));
        Assert.Greater(Vector3.Angle(heldDirection,movedDirection),1);
        passes.Add("PASS actual pointer movement updates the shared direction");
        Set(aim,"_wasAimPointerBlocked",true);
        Sample(aim,pointer+new Vector2(350,150));
        Assert.IsTrue(aim.TryGetFlatAimDirection(player.transform.position,out var afterPopup));
        Assert.Less(Vector3.Angle(movedDirection,afterPopup),.001f);
        passes.Add("PASS pointer movement inside a popup is not replayed when the popup closes");
        // Presentation uses the same heading, even with a different current camera pose.
        Set(aim,"_lastAimPointer",(Vector2)Input.mousePosition);
        vision.RefreshPresentationAimPoint();
        Assert.Less(Vector3.Angle(movedDirection,vision.ForwardDirection),.001f);
        passes.Add("PASS rendered vision shares cached gameplay heading rather than recasting after camera follow");
    }
}
