using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VSplatter.StateMachine.Editor;

// Temporary explicit verification request. Remove after observing the report.
[InitializeOnLoad]
internal static class HfsmVerificationOnce
{
    private const string Report = "Library/HfsmRefactor-20260929/ui-verification-v2.txt";
    private static EditorWindow _window, _previous;
    private static Type _type;
    private static int _frames, _phase;
    private static double _started;
    private static readonly Dictionary<string, Rect> Controls = new();
    private static readonly List<string> Lines = new();
    private static string _foldKey;
    private static bool _oldFold, _hadFold;
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    static HfsmVerificationOnce() { EditorApplication.delayCall += Start; }
    private static void Start()
    {
        if (File.Exists(Report)) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode || PrefabStageUtility.GetCurrentPrefabStage() != null)
        { EditorApplication.delayCall += Start; return; }
        try
        {
            HfsmVerificationRunner.Run();
            _previous = EditorWindow.focusedWindow;
            _type = typeof(HfsmGraphAuthoring).Assembly.GetType("VSplatter.StateMachine.Editor.HFSMTransitionGraphWindow");
            _window = (EditorWindow)ScriptableObject.CreateInstance(_type);
            _window.titleContent = new GUIContent("HFSM UI verification");
            _window.position = new Rect(30, 40, 1500, 900);
            _type.GetField("ObserveControl", Flags).SetValue(_window, (Action<string, Rect>)((id, rect) => Controls[id] = rect));
            _type.GetMethod("SetOwner", Flags).Invoke(_window, new object[] { AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/GameScene/Shooter/EnemyShooter/EnemyBasePrefab.prefab") });
            _window.ShowUtility(); _started = EditorApplication.timeSinceStartup;
            EditorApplication.update += Tick;
        }
        catch (Exception ex) { Finish("FAIL " + ex); }
    }
    private static void Tick()
    {
        try
        {
            if (_window == null) throw new Exception("Verification window closed");
            if (EditorApplication.timeSinceStartup - _started > 70) throw new Exception("UI verification timed out");
            _window.Repaint(); if (++_frames < 10 || !Controls.ContainsKey("State/Alive_Shooter")) return; _frames = 0;
            switch (_phase++)
            {
                case 0:
                    Check(Controls.ContainsKey("Fold/Alive_Shooter"), "Real Alive coordinator binding renders as a composite");
                    // Resolve preference key before toggling so this test leaves user's layout preferences intact.
                    var model = _type.GetField("_model", Flags).GetValue(_window);
                    var root = model.GetType().GetProperty("Root", Flags).GetValue(model);
                    var nodes = (System.Collections.IEnumerable)root.GetType().GetField("Nodes", Flags).GetValue(root);
                    foreach (var node in nodes)
                    {
                        var state = (UnityEngine.Object)node.GetType().GetField("State", Flags).GetValue(node);
                        if (state.name != "Alive_Shooter") continue;
                        _foldKey = (string)model.GetType().GetMethod("LayoutKey", BindingFlags.Static | BindingFlags.Public).Invoke(null, new[] { node }) + ".expanded";
                        _hadFold = EditorPrefs.HasKey(_foldKey); _oldFold = EditorPrefs.GetBool(_foldKey, true);
                    }
                    Click("Fold/Alive_Shooter"); break;
                case 1:
                    Check(_foldKey != null && EditorPrefs.GetBool(_foldKey) != _oldFold, "Composite fold click changes and persists presentation state");
                    Click("Fold/Alive_Shooter"); break;
                case 2:
                    Click("State/Spawning_Shooter"); break;
                case 3:
                    Check(_type.GetField("_selectedNode", Flags).GetValue(_window) != null, "Graph state click opens the state inspector");
                    _type.GetField("_dirty", Flags).SetValue(_window, true); break;
                case 4:
                    var selected = _type.GetField("_selectedNode", Flags).GetValue(_window);
                    Check(selected != null && ((UnityEngine.Object)selected.GetType().GetField("State", Flags).GetValue(selected)).name == "Spawning_Shooter", "Rebuilding graph preserves the selected state inspector");
                    Finish("COMPLETE: memory fixtures + separate editor window only; no production prefab/scene saves."); break;
            }
        }
        catch (Exception ex) { Finish("FAIL " + ex); }
    }
    private static void Click(string id)
    {
        if (!Controls.TryGetValue(id, out var rect)) throw new Exception("Missing control " + id);
        _window.SendEvent(new Event { type = EventType.MouseDown, button = 0, mousePosition = rect.center });
        _window.SendEvent(new Event { type = EventType.MouseUp, button = 0, mousePosition = rect.center });
    }
    private static void Check(bool ok, string label) { if (!ok) throw new Exception(label); Lines.Add("PASS " + label); }
    private static void Finish(string result)
    {
        EditorApplication.update -= Tick;
        if (_foldKey != null) { if (_hadFold) EditorPrefs.SetBool(_foldKey, _oldFold); else EditorPrefs.DeleteKey(_foldKey); }
        if (_window != null) _window.Close(); if (_previous != null) _previous.Focus();
        Lines.Add(result); Directory.CreateDirectory(Path.GetDirectoryName(Report)); File.WriteAllLines(Report, Lines);
        Debug.Log("[HFSM UI verification] " + result);
    }
}
