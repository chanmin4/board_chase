/*
 * Role hierarchy: StateMachine > Editor
 * Primary responsibility: Provides the TransitionTableEditorWindow authoring tool and its editor workflow.
 * Execution boundary: Editor only (VSplatter.StateMachine.Editor)
 */

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using VSplatter.StateMachine.ScriptableObjects;

namespace VSplatter.StateMachine.Editor
{
	internal class TransitionTableEditorWindow : EditorWindow
	{
		private static TransitionTableEditorWindow _window;
		private static readonly string _uxmlPath = "Assets/Scripts/StateMachine/Editor/TransitionTableEditorWindow.uxml";
		private static readonly string _ussPath = "Assets/Scripts/StateMachine/Editor/TransitionTableEditorWindow.uss";
		private bool _doRefresh;

		private UnityEditor.Editor _transitionTableEditor;
		private MakerSelectionUiState _selectionUi;
		private MakerSelectionUiState SelectionUi => _selectionUi ??= new MakerSelectionUiState();

		[MenuItem("State Machine Editor", menuItem = "ChopChop/State Machine Editor")]
		// Purpose: Performs the display workflow for this component.
		internal static void Display()
		{
			if (_window == null)
				_window = GetWindow<TransitionTableEditorWindow>("State Machine Editor");

			_window.Show();
		}

		// Purpose: Activates runtime subscriptions and refreshes the enabled state.
		private void OnEnable()
		{
			var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(_uxmlPath);
			var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(_ussPath);

			rootVisualElement.Add(visualTree.CloneTree());

			string labelClass = $"label-{(EditorGUIUtility.isProSkin ? "pro" : "personal")}";
			rootVisualElement.Query<Label>().Build().ForEach(label => label.AddToClassList(labelClass));

			rootVisualElement.styleSheets.Add(styleSheet);

			minSize = new Vector2(480, 360);

			EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
		}

		// Purpose: Releases active subscriptions and transient state while disabled.
		private void OnDisable()
		{
			EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
		}

		// Purpose: Responds to the play mode state changed callback.
		private void OnPlayModeStateChanged(PlayModeStateChange obj)
		{
			if (obj == PlayModeStateChange.EnteredPlayMode)
				_doRefresh = true;
		}

		/// <summary>
		/// Update list every time we gain focus
		/// </summary>
		private void OnFocus()
		{
			// Calling CreateListView() from here when the window is docked
			// throws a NullReferenceException in UnityEditor.DockArea:OnEnable().
			if (_doRefresh == false)
				_doRefresh = true;
		}

		// Purpose: Responds to the lost focus callback.
		private void OnLostFocus()
		{
			ListView listView = rootVisualElement.Q<ListView>(className: "table-list");
			listView.selectionChanged -= OnListSelectionChanged;
		}

		// Purpose: Processes this component's per-frame runtime work.
		private void Update()
		{
			if (!_doRefresh)
				return;

			CreateListView();
			_doRefresh = false;
		}

		// Purpose: Creates the list view owned by this workflow.
		private void CreateListView()
		{
			var assets = FindAssets();
			ListView listView = rootVisualElement.Q<ListView>(className: "table-list");

			listView.makeItem = null;
			listView.bindItem = null;

			listView.itemsSource = assets;
			listView.fixedItemHeight = 16;
			string labelClass = $"label-{(EditorGUIUtility.isProSkin ? "pro" : "personal")}";
			listView.makeItem = () =>
			{
				var label = new Label();
				label.AddToClassList(labelClass);
				return label;
			};
			listView.bindItem = (element, i) => ((Label)element).text = assets[i].name;
			listView.selectionType = SelectionType.Single;

			listView.selectionChanged -= OnListSelectionChanged;
			listView.selectionChanged += OnListSelectionChanged;

			if (_transitionTableEditor && _transitionTableEditor.target)
				listView.selectedIndex = System.Array.IndexOf(assets, _transitionTableEditor.target);
		}

		// Purpose: Responds to the list selection changed callback.
		private void OnListSelectionChanged(IEnumerable<object> enumerable)
		{
			SelectionUi.SelectionClicked();
			IMGUIContainer editor = rootVisualElement.Q<IMGUIContainer>(className: "table-editor");
			editor.onGUIHandler = null;

			List<object> list = (List<object>)enumerable;

			if (list.Count == 0)
				return;

			var table = (TransitionTableSO)list[0];
			if (table == null)
				return;

			if (_transitionTableEditor == null)
				_transitionTableEditor = UnityEditor.Editor.CreateEditor(table, typeof(TransitionTableEditor));
			else if (_transitionTableEditor.target != table)
				UnityEditor.Editor.CreateCachedEditor(table, typeof(TransitionTableEditor), ref _transitionTableEditor);

			editor.onGUIHandler = () =>
			{
				if (!_transitionTableEditor.target)
				{
					editor.onGUIHandler = null;
					return;
				}

				ListView listView = rootVisualElement.Q<ListView>(className: "table-list");
				if ((Object)listView.selectedItem != _transitionTableEditor.target)
				{
					var i = listView.itemsSource.IndexOf(_transitionTableEditor.target);
					listView.selectedIndex = i;
					if (i < 0)
					{
						editor.onGUIHandler = null;
						return;
					}
				}

				SelectionUi.Synchronize(_transitionTableEditor.target);
				_transitionTableEditor.OnInspectorGUI();
			};
		}


		// Purpose: Finds the requested assets.
		private TransitionTableSO[] FindAssets()
		{
			var guids = AssetDatabase.FindAssets($"t:{nameof(TransitionTableSO)}");
			var assets = new TransitionTableSO[guids.Length];
			for (int i = 0; i < guids.Length; i++)
				assets[i] = AssetDatabase.LoadAssetAtPath<TransitionTableSO>(AssetDatabase.GUIDToAssetPath(guids[i]));

			return assets;
		}
	}
}
