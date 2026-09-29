/*
 * Role hierarchy: WIP > Legacy > Authoring > Editor > SceneSelector
 * Primary responsibility: Isolates the legacy or deferred SceneSelector implementation from active production code.
 * Execution boundary: Editor only (Assembly-CSharp-Editor)
 */

/*
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public partial class SceneSelector : EditorWindow
{
	[Serializable]
	private class Item
	{
		public string guid;
		public int order = int.MaxValue;
		public bool isVisible = true;
		public Color color = Color.clear;

		[NonSerialized]
		public GameSceneSO gameSceneSO;
	}

	private class Styles
	{
		public GUIStyle item;
	}

	private class Textures
	{
		public Texture2D visibilityOn;
		public Texture2D visibilityOff;
	}

	[Serializable]
	private class Storage : ISerializationCallbackReceiver
	{
		[SerializeField]
		public List<Item> items = new List<Item>();

		[NonSerialized]
		public Dictionary<string, Item> itemsMap = new Dictionary<string, Item>();

		void ISerializationCallbackReceiver.OnBeforeSerialize()
		{
			for (int i = 0, count = items.Count; i < count; ++i)
			{
				var item = items[i];
				item.order = i;
			}
		}

		void ISerializationCallbackReceiver.OnAfterDeserialize()
		{
			items.OrderBy(x => x.order);
			foreach (var item in items)
			{
				itemsMap.Add(item.guid, item);
			}
		}
	}
}
*/