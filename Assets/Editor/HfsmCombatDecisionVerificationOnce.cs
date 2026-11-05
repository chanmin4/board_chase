using System;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>임시 1회 검사 진입점. 결과 파일 생성 후 재실행하지 않으며 검사가 끝나면 이 파일을 제거합니다.</summary>
[InitializeOnLoad]
internal static class HfsmCombatDecisionVerificationOnce
{
    static HfsmCombatDecisionVerificationOnce() => EditorApplication.delayCall += WaitForImport;
    private static void WaitForImport()
    {
        if (File.Exists(HfsmCombatDecisionVerificationRunner.Report)) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
        { EditorApplication.delayCall += WaitForImport; return; }
        try { HfsmCombatDecisionVerificationRunner.Run(); }
        catch (Exception ex) { Debug.LogException(ex); }
    }
}
