using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>One Escape press dismisses one visible popup, never its parent or Pause as well.</summary>
public static class UIEscapeRouter
{
    private sealed class Popup
    {
        public MonoBehaviour owner;
        public Func<bool> isOpen;
        public Action close;
        public int priority;
    }
    private static readonly List<Popup> Popups = new();
    private static int _consumedFrame = -1;
    public static bool IsConsumed => _consumedFrame == Time.frameCount;
    // Read current visibility, not a frame cache: a popup may open after player Update.
    public static bool HasOpenPopup
    {
        get
        {
            for (int i = Popups.Count - 1; i >= 0; i--)
            {
                var popup = Popups[i];
                if (popup.owner != null && IsVisible(popup.owner) && popup.isOpen()) return true;
            }
            return false;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() { Popups.Clear(); _consumedFrame = -1; }

    // Main overlay stacks use priority 0; nested purchase/key windows use 100.
    public static void Register(MonoBehaviour owner, Func<bool> isOpen, Action close, int priority = 0)
    {
        Unregister(owner);
        if (owner != null && isOpen != null && close != null)
            Popups.Add(new Popup { owner = owner, isOpen = isOpen, close = close, priority = priority });
    }
    public static void Unregister(MonoBehaviour owner) => Popups.RemoveAll(popup => popup.owner == owner);
    public static bool TryConsume()
    {
        if (IsConsumed) return false;
        _consumedFrame = Time.frameCount; return true;
    }
    public static bool TryCloseTopPopup()
    {
        if (IsConsumed) return true;
        Popup top = null;
        for (int i = Popups.Count - 1; i >= 0; i--)
        {
            var popup = Popups[i];
            if (popup.owner == null) { Popups.RemoveAt(i); continue; }
            if (!IsVisible(popup.owner) || !popup.isOpen()) continue;
            if (top == null || popup.priority > top.priority) top = popup;
        }
        if (top == null) return false;
        // Consume before hiding: callbacks/input-map changes must not reuse the same key press.
        TryConsume(); top.close(); return true;
    }
    private static bool IsVisible(MonoBehaviour owner)
    {
        if (!owner.isActiveAndEnabled) return false;
        for (Transform current = owner.transform; current != null; current = current.parent)
        {
            var group = current.GetComponent<CanvasGroup>();
            if (group != null && group.alpha <= .001f) return false;
            // A nested popup in an older overlay must not steal Escape from the front overlay.
            var panel = current.GetComponent<UIOverlayPanel>();
            if (panel != null)
                foreach (var popup in Popups)
                    if (popup.owner is UIOverlayManager manager && manager.isActiveAndEnabled &&
                        manager.IsCoveredByAnotherOverlay(panel)) return false;
        }
        return true;
    }
}
