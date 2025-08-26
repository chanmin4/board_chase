using System.IO;
using UnityEngine;

public static class SaveSystem
{
    static string Path => System.IO.Path.Combine(Application.persistentDataPath, "save.json");

    public static SaveData Load()
    {
        try
        {
            if (!File.Exists(Path)) return new SaveData();
            var json = File.ReadAllText(Path);
            return JsonUtility.FromJson<SaveData>(json) ?? new SaveData();
        }
        catch { return new SaveData(); }
    }

    public static void Save(SaveData data)
    {
        try
        {
            var json = JsonUtility.ToJson(data, true);
            File.WriteAllText(Path, json);
#if UNITY_EDITOR
            Debug.Log($"[Save] {Path}\n{json}");
#endif
        }
        catch (System.Exception e) { Debug.LogError(e); }
    }
     public static bool Delete()
    {
        try
        {
            if (File.Exists(Path)) { File.Delete(Path); return true; }
        }
        catch (System.Exception e) { Debug.LogError($"[Save] Delete failed: {e}"); }
        return false;
    }
}
