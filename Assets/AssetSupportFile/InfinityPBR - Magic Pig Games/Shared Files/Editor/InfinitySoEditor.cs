using UnityEngine;

namespace InfinityPBR
{
    public abstract class InfinitySoEditor<T> : InfinityEditorScriptableObject<T> where T : ScriptableObject
    {
        public static bool ShowFullInspector(string scriptName)
        {
            Space();
            SetBool("Show full inspector " + scriptName,
                LeftCheck("Show full inspector", GetBool("Show full inspector " + scriptName)));
            return GetBool("Show full inspector " + scriptName);
        }
    }
}
