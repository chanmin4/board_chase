/*
 * Role hierarchy: Gameplay > Items > Weapons > Runtime
 * Primary responsibility: Implements the focused SpecialAreaPolarity responsibility and collaborates with its role group.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System.Collections.Generic;
using UnityEngine;

public enum SpecialAreaPolarity
{
    Harmful = 0,
    Beneficial = 1,
    Neutral = 2
}

/// <summary>
/// Shared lifetime/tick/AI-query shell for landed special areas. Concrete
/// specials keep their own gameplay fields and implement their own tick rules.
/// </summary>
public abstract class SpecialAreaRuntime : MonoBehaviour
{
    private static readonly List<SpecialAreaRuntime> ActiveAreas = new();

    [Header("Runtime - Don't Touch")]
    [ReadOnly] [SerializeField] private float _remainingSeconds;
    [ReadOnly] [SerializeField] private float _radius;
    [ReadOnly] [SerializeField] private SpecialAreaPolarity _polarity;
    [ReadOnly] [SerializeField] private float _aiThreat;

    private float _expiresAt;
    private float _nextTickAt;
    private float _tickInterval;
    private bool _configured;

    public float Radius => _radius;
    public float RemainingSeconds => _remainingSeconds;
    public SpecialAreaPolarity Polarity => _polarity;
    public float AIThreat => _aiThreat;

    // Purpose: Configures this component''s area.
    protected void ConfigureArea(
        float radius,
        float durationSeconds,
        float tickInterval,
        SpecialAreaPolarity polarity,
        float aiThreat)
    {
        _radius = Mathf.Max(0.01f, radius);
        _tickInterval = Mathf.Max(0.01f, tickInterval);
        _polarity = polarity;
        _aiThreat = Mathf.Max(0f, aiThreat);
        _expiresAt = Time.time + Mathf.Max(0.01f, durationSeconds);
        _nextTickAt = Time.time + _tickInterval;
        _remainingSeconds = Mathf.Max(0f, _expiresAt - Time.time);
        _configured = true;

        if (isActiveAndEnabled)
            RegisterActiveArea();
    }

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    private void OnEnable()
    {
        if (_configured)
            RegisterActiveArea();
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        ActiveAreas.Remove(this);
    }

    // Purpose: Processes this component's per-frame runtime work.
    private void Update()
    {
        if (!_configured)
            return;

        float now = Time.time;
        _remainingSeconds = Mathf.Max(0f, _expiresAt - now);

        int safety = 0;
        while (now >= _nextTickAt && _nextTickAt <= _expiresAt && safety < 4)
        {
            TickArea();
            _nextTickAt += _tickInterval;
            safety++;
        }

        if (now >= _expiresAt)
            Destroy(gameObject);
    }

    // Purpose: Processes one update step for area.
    protected abstract void TickArea();

    // Purpose: Calculates threat from the current inputs.
    public virtual float EvaluateThreat(GameObject subject, Vector3 worldPosition)
    {
        if (!_configured ||
            _polarity != SpecialAreaPolarity.Harmful ||
            _aiThreat <= 0f ||
            !IsInside(worldPosition))
        {
            return 0f;
        }

        if (!IsHarmfulTo(subject))
            return 0f;

        Vector3 offset = worldPosition - transform.position;
        offset.y = 0f;
        float normalizedDistance = Mathf.Clamp01(offset.magnitude / Mathf.Max(0.01f, _radius));
        return _aiThreat * (2f - normalizedDistance);
    }

    // Purpose: Checks whether harmful to is currently true.
    protected virtual bool IsHarmfulTo(GameObject subject)
    {
        return true;
    }

    // Purpose: Checks whether inside is currently true.
    protected bool IsInside(Vector3 worldPosition)
    {
        Vector3 offset = worldPosition - transform.position;
        offset.y = 0f;
        return offset.sqrMagnitude <= _radius * _radius;
    }

    // Purpose: Calculates combined threat from the current inputs.
    public static float EvaluateCombinedThreat(GameObject subject, Vector3 worldPosition)
    {
        float result = 0f;

        for (int i = ActiveAreas.Count - 1; i >= 0; i--)
        {
            SpecialAreaRuntime area = ActiveAreas[i];

            if (area == null)
            {
                ActiveAreas.RemoveAt(i);
                continue;
            }

            result += area.EvaluateThreat(subject, worldPosition);
        }

        return Mathf.Max(0f, result);
    }

    // Purpose: Registers active area with its owning runtime service.
    private void RegisterActiveArea()
    {
        if (!ActiveAreas.Contains(this))
            ActiveAreas.Add(this);
    }
}
