using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class UIVisibilityHelper
{
    public static CanvasGroup EnsureCanvasGroup(GameObject target)
    {
        if (target == null)
            return null;

        target.SetActive(true);

        CanvasGroup group = target.GetComponent<CanvasGroup>();
        if (group == null)
            group = target.AddComponent<CanvasGroup>();

        return group;
    }

    public static CanvasGroup EnsureCanvasGroup(Component target)
    {
        return target != null ? EnsureCanvasGroup(target.gameObject) : null;
    }

    public static void SetVisible(CanvasGroup group, bool visible)
    {
        SetVisible(group, visible, 1f);
    }

    public static void SetVisible(CanvasGroup group, bool visible, float visibleAlpha)
    {
        if (group == null)
            return;

        group.gameObject.SetActive(true);
        group.alpha = visible ? Mathf.Clamp01(visibleAlpha) : 0f;
        group.interactable = visible;
        group.blocksRaycasts = visible;
    }

    public static void SetVisible(GameObject target, bool visible)
    {
        SetVisible(EnsureCanvasGroup(target), visible);
    }

    public static void SetVisible(Component target, bool visible)
    {
        SetVisible(EnsureCanvasGroup(target), visible);
    }

    public static void ForceActive(Component target)
    {
        if (target != null)
            target.gameObject.SetActive(true);
    }

    public static void SetImage(Image image, Sprite sprite, bool visible)
    {
        if (image == null)
            return;

        image.gameObject.SetActive(true);
        image.sprite = sprite;
        SetVisible(image, visible);
    }

    public static void SetText(TextMeshProUGUI text, string value, bool visible)
    {
        if (text == null)
            return;

        text.gameObject.SetActive(true);
        text.text = visible ? value : string.Empty;
        SetVisible(text, visible);
    }
}
