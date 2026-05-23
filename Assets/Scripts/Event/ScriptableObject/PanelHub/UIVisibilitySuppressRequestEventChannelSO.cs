using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct UIVisibilitySuppressRequest
{
    public string targetId;
    public string sourceId;
    public bool suppress;

    public UIVisibilitySuppressRequest(string targetId, string sourceId, bool suppress)
    {
        this.targetId = targetId;
        this.sourceId = sourceId;
        this.suppress = suppress;
    }
}

[CreateAssetMenu(
    fileName = "UIVisibilitySuppressRequestEventChannel",
    menuName = "Events/UI/UI Visibility Suppress Request Event Channel")]
public class UIVisibilitySuppressRequestEventChannelSO : ScriptableObject
{
    private readonly Dictionary<string, HashSet<string>> _activeSuppressorsByTarget = new();

    public event Action<UIVisibilitySuppressRequest> OnEventRaised;

    public void RaiseEvent(UIVisibilitySuppressRequest request)
    {
        string targetId = NormalizeId(request.targetId);
        string sourceId = NormalizeId(request.sourceId);

        if (!_activeSuppressorsByTarget.TryGetValue(targetId, out HashSet<string> sources))
        {
            sources = new HashSet<string>();
            _activeSuppressorsByTarget.Add(targetId, sources);
        }

        if (request.suppress)
            sources.Add(sourceId);
        else
            sources.Remove(sourceId);

        if (sources.Count == 0)
            _activeSuppressorsByTarget.Remove(targetId);

        OnEventRaised?.Invoke(new UIVisibilitySuppressRequest(
            targetId,
            sourceId,
            request.suppress));
    }

    public bool IsSuppressed(string targetId)
    {
        targetId = NormalizeId(targetId);

        return _activeSuppressorsByTarget.TryGetValue(targetId, out HashSet<string> sources) &&
               sources.Count > 0;
    }

    public void ClearSource(string targetId, string sourceId)
    {
        RaiseEvent(new UIVisibilitySuppressRequest(targetId, sourceId, false));
    }

    private static string NormalizeId(string id)
    {
        return string.IsNullOrWhiteSpace(id) ? "Default" : id.Trim();
    }
}