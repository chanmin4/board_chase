using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Shared module icon geometry. Item Maker and gameplay use the same tier order and colors.</summary>
public static class ModuleRuneStyle
{
    public const float IconLeft = .22f;
    public const float IconRight = .78f;
    public const float IconBottom = .33f;
    public const float IconTop = .81f;
    private const int Size = 96;
    private sealed class Frame
    {
        public Texture2D texture;
        public Sprite sprite;
        public Color32 tint;
    }
    private static readonly Dictionary<PlayerShopItemRarity, Frame> Frames = new();

    public static int Tier(PlayerShopItemRarity rarity) => rarity switch
    {
        PlayerShopItemRarity.Rare => 2,
        PlayerShopItemRarity.Unique => 3,
        PlayerShopItemRarity.Legendary => 4,
        PlayerShopItemRarity.Mythic => 5,
        _ => 1
    };

    public static string Numeral(PlayerShopItemRarity rarity) => Tier(rarity) switch
    {
        2 => "Ⅱ", 3 => "Ⅲ", 4 => "Ⅳ", 5 => "Ⅴ", _ => "Ⅰ"
    };

    public static Sprite GetFrame(PlayerShopItemRarity rarity)
    {
        Color32 tint = ItemRarityStyleSO.Active?.RarityColor(rarity) ?? Color.gray;
        if (!Frames.TryGetValue(rarity, out var frame) || frame.texture == null || frame.sprite == null)
        {
            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
            {
                name = "Module Rune " + Numeral(rarity), filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave
            };
            frame = new Frame { texture = texture };
            Frames[rarity] = frame;
        }
        else if (frame.tint.Equals(tint)) return frame.sprite;

        var pixels = new Color32[Size * Size];
        int tier = Tier(rarity);
        Color edge = (Color)tint;
        edge.a = 1f;
        for (int py = 0; py < Size; py++)
        for (int px = 0; px < Size; px++)
        {
            float x = (px + .5f) / Size * 2f - 1f;
            float y = (py + .5f) / Size * 2f - 1f;
            float d = Mathf.Max(Mathf.Abs(x), Mathf.Abs(x) * .5f + Mathf.Abs(y) * .8660254f);
            if (d >= .87f) continue;
            Color c = Color.Lerp(new Color(.025f, .045f, .066f), edge, .22f + (y + 1f) * .12f);
            if (d > .82f) c = Color.Lerp(edge, Color.white, .46f);
            else if (d > .76f) c = edge * .67f;
            else if (d > .73f) c = Color.Lerp(edge, Color.white, .18f);
            if (Mathf.Abs(x) < .29f && y > -.63f && y < -.37f)
                c = Color.Lerp(new Color(.02f, .025f, .036f), edge, .18f);
            if (IsNumeralStroke(x, y, tier))
                c = Color.Lerp(Color.white, edge, .24f);
            c.a = Mathf.Clamp01((.87f - d) * 60f);
            pixels[py * Size + px] = c;
        }
        frame.texture.SetPixels32(pixels);
        frame.texture.Apply();
        if (frame.sprite == null)
        {
            frame.sprite = Sprite.Create(frame.texture, new Rect(0, 0, Size, Size), new Vector2(.5f, .5f), 100f);
            frame.sprite.name = frame.texture.name;
            frame.sprite.hideFlags = HideFlags.HideAndDontSave;
        }
        frame.tint = tint;
        return frame.sprite;
    }

    private static bool IsNumeralStroke(float x, float y, int tier)
    {
        if (y < -.582f || y > -.409f) return false;
        if (tier <= 3)
        {
            for (int i = 0; i < tier; i++)
                if (Mathf.Abs(x - (i - (tier - 1) * .5f) * .11f) < .021f) return true;
            return false;
        }
        float center = tier == 4 ? .057f : 0f;
        if (tier == 4 && Mathf.Abs(x + .151f) < .021f) return true;
        return Mathf.Abs(Mathf.Abs(x - center) - (y + .582f) * .60f) < .026f;
    }
}

/// <summary>Decorates only module icons; preserves the original Sprite for drag/drop.</summary>
[DisallowMultipleComponent]
public sealed class ModuleRuneView : MonoBehaviour
{
    private Image _frame;
    private Image _effect;

    public static void Bind(Image host, ItemSO item)
    {
        if (host == null) return;
        host.TryGetComponent<ModuleRuneView>(out var view);
        if (item is not PassiveItemSO module)
        {
            if (view != null && view._frame != null) view._frame.gameObject.SetActive(false);
            return;
        }
        if (view == null) view = host.gameObject.AddComponent<ModuleRuneView>();
        view.Show(host, module);
    }

    private void Show(Image host, PassiveItemSO module)
    {
        if (_frame == null)
        {
            _frame = CreateImage("Module Rune", transform, Vector2.zero, Vector2.one);
            var fitter = _frame.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = 1f;
            _effect = CreateImage("Effect Sprite", _frame.transform,
                new Vector2(ModuleRuneStyle.IconLeft, ModuleRuneStyle.IconBottom),
                new Vector2(ModuleRuneStyle.IconRight, ModuleRuneStyle.IconTop));
        }
        _frame.sprite = ModuleRuneStyle.GetFrame(module.Rarity);
        _frame.raycastTarget = host.raycastTarget;
        _frame.gameObject.SetActive(true);
        _effect.sprite = module.PreviewImage;
        _effect.enabled = _effect.sprite != null;
        host.enabled = false;
    }

    private static Image CreateImage(string name, Transform parent, Vector2 min, Vector2 max)
    {
        var child = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rect = (RectTransform)child.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        var image = child.GetComponent<Image>();
        image.preserveAspect = true;
        image.raycastTarget = false;
        return image;
    }
}
