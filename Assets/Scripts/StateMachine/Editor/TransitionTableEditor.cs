/*
 * Role hierarchy: StateMachine > Editor
 * Primary responsibility: Provides the TransitionTableEditor authoring tool and its editor workflow.
 * Execution boundary: Editor only (VSplatter.StateMachine.Editor)
 */

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VSplatter.StateMachine.ScriptableObjects;
using static UnityEditor.EditorGUILayout;
using Object = UnityEngine.Object;

namespace VSplatter.StateMachine.Editor
{
	[CustomEditor(typeof(TransitionTableSO))]
	internal class TransitionTableEditor : UnityEditor.Editor
	{
		// Property with all the transitions.
		private SerializedProperty _transitions;

		// _fromStates and _transitionsByFromStates form a State->Transitions dictionary.
		private List<Object> _fromStates;
		private List<List<TransitionDisplayHelper>> _transitionsByFromStates;

		// Index of the state currently toggled on, -1 if none is.
		internal int _toggledIndex = -1;

		// Helper class to add new transitions.
		private AddTransitionHelper _addTransitionHelper;

		// Editor to display the StateSO inspector.
		private UnityEditor.Editor _cachedStateEditor;
		private bool _displayStateEditor;

		// Purpose: Activates runtime subscriptions and refreshes the enabled state.
		private void OnEnable()
		{
			_addTransitionHelper = new AddTransitionHelper(this);
			Undo.undoRedoPerformed += Reset;
			Reset();
		}

		// Purpose: Releases active subscriptions and transient state while disabled.
		private void OnDisable()
		{
			Undo.undoRedoPerformed -= Reset;
			_addTransitionHelper?.Dispose();
		}

		/// <summary>
		/// Method to fully reset the editor. Used whenever adding, removing and reordering transitions.
		/// </summary>
		internal void Reset()
		{
			serializedObject.Update();
			if (((TransitionTableSO)target).Transitions.Any(r => r.UseSharedRule))
			{
				// Old editor owns inline fields only. Do not display/edit stale snapshots or auto-delete usages.
				_fromStates = new List<Object>(); _transitionsByFromStates = new List<List<TransitionDisplayHelper>>(); _toggledIndex = -1;
				return;
			}
			var toggledState = _toggledIndex > -1 ? _fromStates[_toggledIndex] : null;
			_transitions = serializedObject.FindProperty("_transitions");
			GroupByFromState();
			_toggledIndex = toggledState ? _fromStates.IndexOf(toggledState) : -1;
		}

		// Purpose: Responds to the inspector gui callback.
		public override void OnInspectorGUI()
		{
			if (((TransitionTableSO)target).Transitions.Any(r => r.UseSharedRule))
			{
				EditorGUILayout.HelpBox("이 테이블은 공유 전환 규칙을 사용합니다. HFSM 창에서 공유 규칙·적용 상태·실제 검사 순서를 함께 편집하세요. 기존 직접 작성 필드는 이전 스냅샷이며 공유 중에는 사용하지 않습니다.", MessageType.Info);
				if (GUILayout.Button("HFSM에서 규칙 / 검사 순서 편집")) HFSMTransitionGraphWindow.OpenTable((TransitionTableSO)target);
				return;
			}
			if (!_displayStateEditor)
				TransitionTableGUI();
			else
				StateEditorGUI();
		}

		// Purpose: Performs the state editor gui workflow for this component.
		private void StateEditorGUI()
		{
			Separator();

			// Back button
			if (GUILayout.Button(EditorGUIUtility.IconContent("scrollleft"), GUILayout.Width(35), GUILayout.Height(20))
				|| _cachedStateEditor.serializedObject == null)
			{
				_displayStateEditor = false;
				return;
			}


			Separator();
			EditorGUILayout.HelpBox("Edit the Actions that a State performs per frame. The order represent the order of execution.", MessageType.Info);
			Separator();

			// State name
			EditorGUILayout.LabelField(_cachedStateEditor.target.name, EditorStyles.boldLabel);
			Separator();
			_cachedStateEditor.OnInspectorGUI();
		}

		// Purpose: Performs the transition table gui workflow for this component.
		private void TransitionTableGUI()
		{
			serializedObject.Update();
			EditorGUI.BeginChangeCheck();
			EditorGUILayout.PropertyField(serializedObject.FindProperty("_initialState"), new GUIContent("Explicit Entry"));
			if (EditorGUI.EndChangeCheck()) serializedObject.ApplyModifiedProperties();
			EditorGUILayout.HelpBox("For hierarchy, priorities and exit reasons use Tools > Authoring > HFSM Transition Graph. Equal priorities use authored row order. Empty conditions do not fire unless Always is enabled.", MessageType.None);
			Separator();
			EditorGUILayout.HelpBox("Click on any State's name to see the Transitions it contains, or click the Pencil/Wrench icon to see its Actions.", MessageType.Info);
			Separator();

			// For each fromState
			for (int i = 0; i < _fromStates.Count; i++)
			{
				var stateRect = BeginVertical(ContentStyle.WithPaddingAndMargins);
				EditorGUI.DrawRect(stateRect, ContentStyle.LightGray);

				var transitions = _transitionsByFromStates[i];

				// State Header
				var headerRect = BeginHorizontal();
				{
					BeginVertical();
					string label = transitions[0].SerializedTransition.FromState.objectReferenceValue.name;
					if (((TransitionTableSO)target).InitialState == transitions[0].SerializedTransition.FromState.objectReferenceValue)
						label += " (Initial State)";

					headerRect.height = EditorGUIUtility.singleLineHeight;
					GUILayoutUtility.GetRect(headerRect.width, headerRect.height);
					headerRect.x += 5;

					// Toggle
					{
						var toggleRect = headerRect;
						toggleRect.width -= 140;
						_toggledIndex =
							EditorGUI.BeginFoldoutHeaderGroup(toggleRect, _toggledIndex == i, label, ContentStyle.StateListStyle) ?
							i : _toggledIndex == i ? -1 : _toggledIndex;
					}

					Separator();
					EndVertical();

					// State Header Buttons
					{
						bool Button(Rect position, string icon) => GUI.Button(position, EditorGUIUtility.IconContent(icon));

						var buttonRect = new Rect(x: headerRect.width - 25, y: headerRect.y, width: 35, height: 20);

						// Move state down
						if (i < _fromStates.Count - 1)
						{
							if (Button(buttonRect, "scrolldown"))
							{
								ReorderState(i, false);
								EarlyOut();
								return;
							}
							buttonRect.x -= 40;
						}

						// Move state up
						if (i > 0)
						{
							if (Button(buttonRect, "scrollup"))
							{
								ReorderState(i, true);
								EarlyOut();
								return;
							}
							buttonRect.x -= 40;
						}

						// Switch to state editor
						if (Button(buttonRect, "SceneViewTools"))
						{
							DisplayStateEditor(transitions[0].SerializedTransition.FromState.objectReferenceValue);
							EarlyOut();
							return;
						}

						void EarlyOut()
						{
							EndHorizontal();
							EndFoldoutHeaderGroup();
							EndVertical();
							EndHorizontal();
						}
					}
				}
				EndHorizontal();

				if (_toggledIndex == i)
				{
					EditorGUI.BeginChangeCheck();
					stateRect.y += EditorGUIUtility.singleLineHeight * 2;

					foreach (var transition in transitions) // Display all the transitions in the state
					{
						if (transition.Display(ref stateRect)) // Return if there were changes
						{
							EditorGUI.EndChangeCheck();
							EndFoldoutHeaderGroup();
							EndVertical();
							EndHorizontal();
							return;
						}
						Separator();
					}
					if (EditorGUI.EndChangeCheck())
						serializedObject.ApplyModifiedProperties();
				}

				EndFoldoutHeaderGroup();
				EndVertical();
				Separator();
			}

			var rect = BeginHorizontal();
			Space(rect.width - 55);

			// Display add transition button
			_addTransitionHelper.Display(rect);

			EndHorizontal();
		}

		// Purpose: Performs the display state editor workflow for this component.
		internal void DisplayStateEditor(Object state)
		{
			if (_cachedStateEditor == null)
				_cachedStateEditor = CreateEditor(state, typeof(StateEditor));
			else
				CreateCachedEditor(state, typeof(StateEditor), ref _cachedStateEditor);

			_displayStateEditor = true;
		}

		/// <summary>
		/// Move a state up or down
		/// </summary>
		/// <param name="index">Index of the state in _fromStates</param>
		/// <param name="up">Moving up(true) or down(true)</param>
		internal void ReorderState(int index, bool up)
		{
			var toggledState = _toggledIndex > -1 ? _fromStates[_toggledIndex] : null;

			if (!up)
				index++;

			var transitions = _transitionsByFromStates[index];
			int transitionIndex = transitions[0].SerializedTransition.Index;
			int targetIndex = _transitionsByFromStates[index - 1][0].SerializedTransition.Index;
			_transitions.MoveArrayElement(transitionIndex, targetIndex);

			ApplyModifications($"Moved {_fromStates[index].name} State {(up ? "up" : "down")}");

			if (toggledState)
				_toggledIndex = _fromStates.IndexOf(toggledState);
		}

		/// <summary>
		/// Add a new transition. If a transition with the same from and to states is found,
		/// the conditions in the new transition are added to it.
		/// </summary>
		/// <param name="source">Source Transition</param>
		internal void AddTransition(SerializedTransition source)
		{
			SerializedTransition transition;
			if (TryGetExistingTransition(source.FromState, source.ToState, out int fromIndex, out int toIndex))
			{
				transition = _transitionsByFromStates[fromIndex][toIndex].SerializedTransition;
			}
			else
			{
				int count = _transitions.arraySize;
				_transitions.InsertArrayElementAtIndex(count);
				transition = new SerializedTransition(_transitions.GetArrayElementAtIndex(count));
				transition.ClearProperties();
				transition.FromState.objectReferenceValue = source.FromState.objectReferenceValue;
				transition.ToState.objectReferenceValue = source.ToState.objectReferenceValue;
			}

			CopyConditions(transition.Conditions, source.Conditions);

			ApplyModifications($"Added transition from {transition.FromState} to {transition.ToState}");

			_toggledIndex = fromIndex >= 0 ? fromIndex : _fromStates.Count - 1;
		}

		/// <summary>
		/// Move a transition up or down
		/// </summary>
		/// <param name="serializedTransition">The transition to move</param>
		/// <param name="up">Move up(true) or down(false)</param>
		internal void ReorderTransition(SerializedTransition serializedTransition, bool up)
		{
			int stateIndex = _fromStates.IndexOf(serializedTransition.FromState.objectReferenceValue);
			var stateTransitions = _transitionsByFromStates[stateIndex];
			int index = stateTransitions.FindIndex(t => t.SerializedTransition.Index == serializedTransition.Index);

			(int currentIndex, int targetIndex) = up ?
				(serializedTransition.Index, stateTransitions[index - 1].SerializedTransition.Index) :
				(stateTransitions[index + 1].SerializedTransition.Index, serializedTransition.Index);

			_transitions.MoveArrayElement(currentIndex, targetIndex);

			ApplyModifications($"Moved transition to {serializedTransition.ToState.objectReferenceValue.name} {(up ? "up" : "down")}");

			_toggledIndex = stateIndex;
		}

		/// <summary>
		/// Remove a transition.
		/// </summary>
		/// <param name="serializedTransition">Transition to delete.</param>
		internal void RemoveTransition(SerializedTransition serializedTransition)
		{
			int stateIndex = _fromStates.IndexOf(serializedTransition.FromState.objectReferenceValue);
			var stateTransitions = _transitionsByFromStates[stateIndex];
			int count = stateTransitions.Count;
			int index = stateTransitions.FindIndex(t => t.SerializedTransition.Index == serializedTransition.Index);
			int deleteIndex = serializedTransition.Index;
			string fromStateName = serializedTransition.FromState.objectReferenceValue.name;

			if (index == 0 && count > 1)
				_transitions.MoveArrayElement(stateTransitions[1].SerializedTransition.Index, deleteIndex++);

			_transitions.DeleteArrayElementAtIndex(deleteIndex);

			ApplyModifications($"Deleted transition from {fromStateName} " +
				"to {serializedTransition.ToState.objectReferenceValue.name}");

			if (count > 1)
				_toggledIndex = stateIndex;
		}

		// Purpose: Returns the current state transitions.
		internal List<SerializedTransition> GetStateTransitions(Object state)
		{
			return _transitionsByFromStates[_fromStates.IndexOf(state)].Select(t => t.SerializedTransition).ToList();
		}

		// Purpose: Copies conditions into the destination state.
		private void CopyConditions(SerializedProperty copyTo, SerializedProperty copyFrom)
		{
			for (int i = 0, j = copyTo.arraySize; i < copyFrom.arraySize; i++, j++)
			{
				copyTo.InsertArrayElementAtIndex(j);
				var cond = copyTo.GetArrayElementAtIndex(j);
				var srcCond = copyFrom.GetArrayElementAtIndex(i);
				cond.FindPropertyRelative("ExpectedResult").enumValueIndex = srcCond.FindPropertyRelative("ExpectedResult").enumValueIndex;
				cond.FindPropertyRelative("Operator").enumValueIndex = srcCond.FindPropertyRelative("Operator").enumValueIndex;
				cond.FindPropertyRelative("Condition").objectReferenceValue = srcCond.FindPropertyRelative("Condition").objectReferenceValue;
			}
		}

		// Purpose: Attempts to get existing transition and reports whether the request succeeded.
		private bool TryGetExistingTransition(SerializedProperty from, SerializedProperty to, out int fromIndex, out int toIndex)
		{
			fromIndex = _fromStates.IndexOf(from.objectReferenceValue);
			toIndex = -1;
			if (fromIndex < 0)
				return false;

			toIndex = _transitionsByFromStates[fromIndex].FindIndex(
				transitionHelper => transitionHelper.SerializedTransition.ToState.objectReferenceValue == to.objectReferenceValue);

			return toIndex >= 0;
		}

		// Purpose: Performs the group by from state workflow for this component.
		private void GroupByFromState()
		{
			var groupedTransitions = new Dictionary<Object, List<TransitionDisplayHelper>>();
			int count = _transitions.arraySize;
			for (int i = 0; i < count; i++)
			{
				var serializedTransition = new SerializedTransition(_transitions, i);
				if (serializedTransition.FromState.objectReferenceValue == null)
				{
					Debug.LogError("Transition with invalid \"From State\" found in table " + serializedObject.targetObject.name + ", deleting...");
					_transitions.DeleteArrayElementAtIndex(i);
					ApplyModifications("Invalid transition deleted");
					return;
				}
				if (serializedTransition.ToState.objectReferenceValue == null)
				{
					Debug.LogError("Transition with invalid \"Target State\" found in table " + serializedObject.targetObject.name + ", deleting...");
					_transitions.DeleteArrayElementAtIndex(i);
					ApplyModifications("Invalid transition deleted");
					return;
				}

				if (!groupedTransitions.TryGetValue(serializedTransition.FromState.objectReferenceValue, out var groupedProps))
				{
					groupedProps = new List<TransitionDisplayHelper>();
					groupedTransitions.Add(serializedTransition.FromState.objectReferenceValue, groupedProps);
				}
				groupedProps.Add(new TransitionDisplayHelper(serializedTransition, this));
			}

			_fromStates = groupedTransitions.Keys.ToList();
			_transitionsByFromStates = new List<List<TransitionDisplayHelper>>();
			foreach (var fromState in _fromStates)
				_transitionsByFromStates.Add(groupedTransitions[fromState]);
		}

		// Purpose: Applies the requested modifications change.
		private void ApplyModifications(string msg)
		{
			Undo.RecordObject(serializedObject.targetObject, msg);
			serializedObject.ApplyModifiedProperties();
			Reset();
		}
	}
}
