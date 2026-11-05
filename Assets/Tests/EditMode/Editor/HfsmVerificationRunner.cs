using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;

/// <summary>Explicit runner only. No InitializeOnLoad migration or automatic production asset edits.</summary>
public static class HfsmVerificationRunner
{
    [MenuItem("Tools/Authoring/Verification/Run HFSM Compatibility Tests")]
    public static void Run()
    {
        int passed = 0, failed = 0; var lines = new List<string>();
        foreach (var type in new[] { typeof(HfsmRuntimeTests), typeof(HfsmCompatibilityTests), typeof(HfsmDefinitionTests), typeof(HfsmGraphAuthoringTests) })
        foreach (var method in type.GetMethods().Where(m => m.IsDefined(typeof(TestAttribute))))
        {
            object fixture = Activator.CreateInstance(type);
            try { foreach (var setup in type.GetMethods().Where(m => m.IsDefined(typeof(SetUpAttribute)))) setup.Invoke(fixture, null); method.Invoke(fixture, null); lines.Add("PASS " + type.Name + "." + method.Name); passed++; }
            catch (Exception ex) { lines.Add("FAIL " + type.Name + "." + method.Name + " " + (ex.InnerException ?? ex)); failed++; }
            finally { foreach (var cleanup in type.GetMethods().Where(m => m.IsDefined(typeof(TearDownAttribute)))) cleanup.Invoke(fixture, null); }
        }
        lines.Add($"{passed} passed / {failed} failed. No production prefab/scene saves.");
        string directory = "Library/HfsmRefactor-20260929"; Directory.CreateDirectory(directory); File.WriteAllLines(directory + "/unity-verification.txt", lines);
        UnityEngine.Debug.Log("[HFSM verification] " + lines.Last());
        if (UnityEngine.Application.isBatchMode) EditorApplication.Exit(failed == 0 ? 0 : 1);
    }
}
