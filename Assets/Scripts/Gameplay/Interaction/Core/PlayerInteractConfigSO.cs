/*
 * Role hierarchy: Gameplay > Interaction > Core
 * Primary responsibility: Handles InteractionCastTimeRule interaction, pickup, trade, loot, or storage flow.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using UnityEngine;

[Serializable]
public sealed class InteractionCastTimeRule
{
    [SerializeField] private InteractionType _interactionType = InteractionType.None;
    [SerializeField, Min(0f)] private float _castSeconds = 0.2f;

    public InteractionType InteractionType => _interactionType;
    public float CastSeconds => Mathf.Max(0f, _castSeconds);
}

[CreateAssetMenu(
    fileName = "PlayerInteractConfig",
    menuName = "Game/Interaction/Player Interact Config")]
public class PlayerInteractConfigSO : ScriptableObject
{
    [Header("Default")]
    [SerializeField, Min(0f)] public float _defaultCastSeconds = 0.2f;

    [Header("Interaction Type Overrides")]
    [SerializeField] private InteractionCastTimeRule[] _castTimeOverrides;

    public float DefaultCastSeconds => Mathf.Max(0f, _defaultCastSeconds);

    // Purpose: Resolves the cast seconds needed by this component.
    public float ResolveCastSeconds(InteractionType interactionType)
    {
        if (_castTimeOverrides != null)
        {
            for (int i = 0; i < _castTimeOverrides.Length; i++)
            {
                InteractionCastTimeRule rule = _castTimeOverrides[i];

                if (rule != null && rule.InteractionType == interactionType)
                    return rule.CastSeconds;
            }
        }

        return DefaultCastSeconds;
    }
}
