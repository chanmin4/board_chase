using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUpgradeTrackColumnUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private TextMeshProUGUI _titleText;
    [SerializeField] private ScrollRect _scrollRect;
    [SerializeField] private Transform _contentRoot;
    [SerializeField] private UpgradeNodeButtonUI _nodePrefab;

    private readonly List<UpgradeNodeButtonUI> _nodes = new();

    public void Bind(
        PlayerUpgradeTrackViewData data,
        Action<PlayerUpgradeTrack, int> nodeClicked,
        Func<PlayerUpgradeTooltipUI> tooltipGetter)
    {
        if (_titleText != null)
            _titleText.text = $"{data.title}  {data.currentLevel}/{data.maxLevel}";

        EnsureNodeCount(data.nodes != null ? data.nodes.Length : 0);

        for (int i = 0; i < _nodes.Count; i++)
        {
            bool active = data.nodes != null && i < data.nodes.Length;
            _nodes[i].gameObject.SetActive(active);

            if (active)
                _nodes[i].Bind(data.nodes[i], nodeClicked, tooltipGetter);
        }

        ScrollToLevel(data.scrollFocusLevel, data.maxLevel);
    }

    private void EnsureNodeCount(int count)
    {
        if (_nodePrefab == null || _contentRoot == null)
            return;

        while (_nodes.Count < count)
        {
            UpgradeNodeButtonUI node = Instantiate(_nodePrefab, _contentRoot);
            _nodes.Add(node);
        }
    }

    private void ScrollToLevel(int level, int maxLevel)
    {
        if (_scrollRect == null)
            return;

        Canvas.ForceUpdateCanvases();

        level = Mathf.Clamp(level, 1, maxLevel);

        if (level <= 1)
        {
            _scrollRect.verticalNormalizedPosition = 1f;
            return;
        }

        float t = (level - 1f) / (maxLevel - 1f);
        _scrollRect.verticalNormalizedPosition = 1f - t;
    }
}
