using System.IO;
using UnityEditor;

[InitializeOnLoad]
internal static class ShopQuestUnlockVerificationOnce
{
    static ShopQuestUnlockVerificationOnce() { EditorApplication.delayCall += TryRun; }
    private static void TryRun()
    {
        if (File.Exists(ShopQuestUnlockVerificationRunner.Report)) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
        { EditorApplication.delayCall += TryRun; return; }
        ShopQuestUnlockVerificationRunner.Run();
    }
}
