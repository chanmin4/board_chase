/*
 * Role hierarchy: Authoring > Editor > ReplaceObject_ContextualHierarchy
 * Primary responsibility: Provides the ReplacePrefabSearchPopup authoring tool and its editor workflow.
 * Execution boundary: Editor only (Assembly-CSharp-Editor)
 */

using System;
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using static UnityEditor.EditorGUIUtility;
using static UnityEditor.EditorJsonUtility;
using static UnityEngine.Application;

namespace UOP1.EditorTools.Replacer
{
	internal class ReplacePrefabSearchPopup : EditorWindow
	{
		private const float previewHeight = 128;

		private class ViewState : ScriptableObject
		{
			public TreeViewState treeViewState = new TreeViewState();
		}

		private static ReplacePrefabSearchPopup window;
		private static Styles styles;

		private static Event evt => Event.current;
		private static string assetPath => Path.Combine(dataPath.Remove(dataPath.Length - 7, 7), "Library", "ReplacePrefabTreeState.asset");

		private bool hasSelection => tree.state.selectedIDs.Count > 0;
		private int selectedId => tree.state.selectedIDs[0];
		private GameObject instance => EditorUtility.InstanceIDToObject(selectedId) as GameObject;

		private SearchField searchField;
		private PrefabSelectionTreeView tree;
		private ViewState viewState;

		private Vector2 startPos;
		private Vector2 startSize;
		private Vector2 lastSize;

		private GameObjectPreview selectionPreview = new GameObjectPreview();

		// Purpose: Shows the show UI or visual state.
		public static void Show(Rect rect)
		{
			var windows = Resources.FindObjectsOfTypeAll<ReplacePrefabSearchPopup>();
			window = windows.Length != 0 ? windows[0] : CreateInstance<ReplacePrefabSearchPopup>();

			window.Init();

			window.startPos = rect.position;
			window.startSize = rect.size;

			window.position = new Rect(rect.position, rect.size);
			// Need to predict start window size to avoid trash frame
			window.SetInitialSize();

			// This type of window supports resizing, but is also persistent, so we need to close it manually
			window.ShowPopup();

			//onSelectEntry += _ => window.Close();
		}

		// Purpose: Performs the init workflow for this component.
		private void Init()
		{
			viewState = CreateInstance<ViewState>();

			if (File.Exists(assetPath))
				FromJsonOverwrite(File.ReadAllText(assetPath), viewState);

			tree = new PrefabSelectionTreeView(viewState.treeViewState);
			tree.onSelectEntry += OnSelectEntry;

			AssetPreview.SetPreviewTextureCacheSize(tree.RowsCount);

			searchField = new SearchField();
			searchField.downOrUpArrowKeyPressed += tree.SetFocusAndEnsureSelectedItem;
			searchField.SetFocus();
		}

		// Purpose: Responds to the select entry callback.
		private void OnSelectEntry(GameObject prefab)
		{
			ReplaceTool.ReplaceSelectedObjects(Selection.gameObjects, prefab);
		}

		// Purpose: Activates runtime subscriptions and refreshes the enabled state.
		private void OnEnable()
		{
			Init();
		}

		// Purpose: Releases active subscriptions and transient state while disabled.
		private void OnDisable()
		{
			tree.Cleanup();
		}

		// Purpose: Closes the active close UI or workflow.
		public new void Close()
		{
			SaveState();
			base.Close();
		}

		// Purpose: Saves the current state state.
		private void SaveState()
		{
			File.WriteAllText(assetPath, ToJson(viewState));
		}

		// Purpose: Responds to the gui callback.
		private void OnGUI()
		{
			if (evt.type == EventType.KeyDown && evt.keyCode == KeyCode.Escape)
			{
				if (tree.hasSearch)
					tree.searchString = "";
				else
					Close();
			}

			if (focusedWindow != this)
				Close();

			if (styles == null)
				styles = new Styles();

			DoToolbar();
			DoTreeView();
			DoSelectionPreview();
		}

		void DoToolbar()
		{
			tree.searchString = searchField.OnToolbarGUI(tree.searchString);

			GUILayout.Label("Replace With...", styles.headerLabel);
		}

		void DoTreeView()
		{
			var rect = GUILayoutUtility.GetRect(0, 10000, 0, 10000);
			rect.x += 2;
			rect.width -= 4;

			rect.y += 2;
			rect.height -= 4;

			tree.OnGUI(rect);
		}

		void DoSelectionPreview()
		{
			if (hasSelection && tree.IsRenderable(selectedId))
			{
				SetSize(startSize.x, startSize.y + previewHeight);
				var previewRect = GUILayoutUtility.GetRect(position.width, previewHeight);

				selectionPreview.CreatePreviewForTarget(instance);
				selectionPreview.RenderInteractivePreview(previewRect);

				selectionPreview.DrawPreviewTexture(previewRect);
			}
			else
			{
				SetSize(startSize.x, startSize.y);
			}
		}

		// Purpose: Updates the initial size state.
		private void SetInitialSize()
		{
			if (hasSelection && tree.IsRenderable(selectedId))
				SetSize(startSize.x, startSize.y + previewHeight);
			else
				SetSize(startSize.x, startSize.y);
		}

		// Purpose: Updates the size state.
		private void SetSize(float width, float height)
		{
			var newSize = new Vector2(width, height);
			if (newSize != lastSize)
			{
				lastSize = newSize;
				position = new Rect(position.x, position.y, width, height);
			}
		}

		private class Styles
		{
			public GUIStyle headerLabel = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
			{
				fontSize = 11,
				fontStyle = FontStyle.Bold
			};
		}
	}
}
