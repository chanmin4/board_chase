/*
 * Role hierarchy: Core > Persistence
 * Primary responsibility: Implements the focused SerializableScriptableObject responsibility and collaborates with its role group.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class SerializableScriptableObject : ScriptableObject
{
	[SerializeField, HideInInspector] private string _guid;
	public string Guid => _guid;
	protected void SetRuntimeGuid(string value) => _guid = value;

#if UNITY_EDITOR
	void OnValidate()
	{
		var path = AssetDatabase.GetAssetPath(this);
		if (!string.IsNullOrEmpty(path)) _guid = AssetDatabase.AssetPathToGUID(path);
	}
#endif
}
