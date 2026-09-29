/*
 * Role hierarchy: Gameplay > Progression > Quests > Data
 * Primary responsibility: Groups ordered QuestDefinitionSO entries into an authorable quest line.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using UnityEngine;

[CreateAssetMenu(fileName = "QuestLine", menuName = "VSplatter/Progression/Quest Line")]
public sealed class QuestLineSO : ScriptableObject
{
    [SerializeField] private string _displayName = "Quest Line";
    [Tooltip("The list is displayed in this authored order. Prerequisites still decide acceptance eligibility.")]
    [SerializeField] private QuestDefinitionSO[] _quests = Array.Empty<QuestDefinitionSO>();

    public string DisplayName => string.IsNullOrWhiteSpace(_displayName) ? name : _displayName;
    public QuestDefinitionSO[] Quests => _quests ?? Array.Empty<QuestDefinitionSO>();
}
