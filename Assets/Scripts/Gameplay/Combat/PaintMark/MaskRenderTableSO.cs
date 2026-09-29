/*
 * Role hierarchy: Gameplay > Combat > PaintMark
 * Primary responsibility: Defines the MaskRenderTableSO data asset used to configure runtime rules in the Inspector.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "MaskRenderTable",
    menuName = "Game/Paint/Mask Render Table")]
public sealed class MaskRenderTableSO : DescriptionBaseSO
{
    [Serializable]
    private struct Entry
    {
        [Tooltip("Gameplay mask channel requested by bullets or systems.")]
        [SerializeField] private PaintChannel _channel;

        [Tooltip("Render profile used for this channel. The profile must contain the visible overlay material.")]
        [SerializeField] private MaskRenderSO _maskRender;

        public PaintChannel Channel => _channel;
        public MaskRenderSO MaskRender => _maskRender;
    }

    [Header("Mask Render Entries")]
    [Tooltip("One entry per PaintChannel. Missing entries intentionally fail instead of using hidden fallback materials.")]
    [SerializeField] private List<Entry> _entries = new();

    // Purpose: Attempts to get and reports whether the request succeeded.
    public bool TryGet(PaintChannel channel, out MaskRenderSO maskRender)
    {
        for (int i = 0; i < _entries.Count; i++)
        {
            if (_entries[i].Channel != channel)
                continue;

            maskRender = _entries[i].MaskRender;
            return maskRender != null;
        }

        maskRender = null;
        return false;
    }

#if UNITY_EDITOR
    // Purpose: Normalizes Inspector-authored data after it changes in the Editor.
    private void OnValidate()
    {
        HashSet<PaintChannel> usedChannels = new();

        for (int i = 0; i < _entries.Count; i++)
        {
            PaintChannel channel = _entries[i].Channel;

            if (!usedChannels.Add(channel))
            {
                Debug.LogWarning(
                    $"[MaskRenderTableSO] Duplicate channel entry found: {channel}",
                    this);
            }
        }
    }
#endif
}
