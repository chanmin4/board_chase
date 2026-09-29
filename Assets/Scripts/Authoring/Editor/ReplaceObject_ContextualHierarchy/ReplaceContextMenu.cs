/*
 * Role hierarchy: Authoring > Editor > ReplaceObject_ContextualHierarchy
 * Primary responsibility: Provides the ReplaceContextMenu authoring tool and its editor workflow.
 * Execution boundary: Editor only (Assembly-CSharp-Editor)
 */

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UOP1.EditorTools.Replacer
{
	internal class ReplaceContextMenu
	{
		private static Type hierarchyType;

		private static EditorWindow focusedWindow;
		private static IMGUIContainer hierarchyGUI;

		private static Vector2 mousePosition;
		private static bool hasExecuted;

		[InitializeOnLoadMethod]
		// Purpose: Responds to the initialize callback.
		private static void OnInitialize()
		{
			hierarchyType = typeof(Editor).Assembly.GetType("UnityEditor.SceneHierarchyWindow");

			EditorApplication.update += TrackFocusedHierarchy;
		}

		// Purpose: Performs the track focused hierarchy workflow for this component.
		private static void TrackFocusedHierarchy()
		{
			if (focusedWindow != EditorWindow.focusedWindow)
			{
				focusedWindow = EditorWindow.focusedWindow;

				if (focusedWindow?.GetType() == hierarchyType)
				{
					if (hierarchyGUI != null)
						hierarchyGUI.onGUIHandler -= OnFocusedHierarchyGUI;

					hierarchyGUI = focusedWindow.rootVisualElement.parent.Query<IMGUIContainer>();
					hierarchyGUI.onGUIHandler += OnFocusedHierarchyGUI;
				}
			}
		}

		// Purpose: Responds to the focused hierarchy gui callback.
		private static void OnFocusedHierarchyGUI()
		{
			// As Event.current is null during context-menu callback, we need to track mouse position on hierarchy GUI
			mousePosition = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);
		}

		[MenuItem("GameObject/Replace", true, priority = 0)]
		// Purpose: Performs the replace selection validate workflow for this component.
		private static bool ReplaceSelectionValidate()
		{
			return Selection.gameObjects.Length > 0;
		}

		[MenuItem("GameObject/Replace", priority = 0)]
		// Purpose: Performs the replace selection workflow for this component.
		private static void ReplaceSelection()
		{
			if (hasExecuted)
				return;

			var rect = new Rect(mousePosition, new Vector2(240, 360));

			ReplacePrefabSearchPopup.Show(rect);

			EditorApplication.delayCall += () => hasExecuted = false;
		}
	}
}
