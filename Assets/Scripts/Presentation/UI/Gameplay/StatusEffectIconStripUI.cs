/*
 * Role hierarchy: Presentation > UI > Gameplay
 * Primary responsibility: Presents StatusEffectIconStripUI state and handles its user-facing UI interaction.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class StatusEffectIconStripUI : MonoBehaviour
{
    private enum LayoutMode
    {
        Horizontal = 0,
        PlayerHealthGrid = 1
    }

    [Tooltip("Size of each status icon in screen-space pixels.")]
    [SerializeField, Min(8f)] private float _iconSize = 24f;
    [SerializeField, Min(0f)] private float _spacing = 3f;
    [SerializeField] private LayoutMode _layoutMode = LayoutMode.Horizontal;
    [SerializeField, Min(1)] private int _iconsPerRow = 4;

    private static readonly Dictionary<StatusEffectDefinitionSO, Sprite> GeneratedIcons = new();
    private readonly List<GameObject> _spawnedIcons = new();
    private EntityStatusEffectRuntime _runtime;

    // Purpose: Creates the create owned by this workflow.
    public static StatusEffectIconStripUI Create(
        RectTransform parent,
        string objectName,
        Vector2 anchor,
        Vector2 anchoredPosition,
        float iconSize)
    {
        if (parent == null)
            return null;

        GameObject rootObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(HorizontalLayoutGroup),
            typeof(StatusEffectIconStripUI));
        RectTransform root = rootObject.GetComponent<RectTransform>();
        root.SetParent(parent, false);
        root.anchorMin = anchor;
        root.anchorMax = anchor;
        root.pivot = new Vector2(0f, 0.5f);
        root.anchoredPosition = anchoredPosition;
        root.sizeDelta = new Vector2(240f, iconSize);

        StatusEffectIconStripUI strip = rootObject.GetComponent<StatusEffectIconStripUI>();
        strip._iconSize = Mathf.Max(8f, iconSize);
        strip.ConfigureLayout();
        return strip;
    }

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        ConfigureLayout();
    }

    // Purpose: Releases owned resources before this component is destroyed.
    private void OnDestroy()
    {
        Bind(null);
    }

    /// <summary>
    /// Configures this already-authored root as the player's compact status sector:
    /// four icons per row, with additional rows growing upward from the HP bar.
    /// Enemy widgets retain the default horizontal strip configuration.
    /// </summary>
    public void ConfigureAsPlayerHealthGrid()
    {
        _layoutMode = LayoutMode.PlayerHealthGrid;

        ConfigureLayout();
    }

    // Purpose: Binds bind to the active runtime or UI target.
    public void Bind(EntityStatusEffectRuntime runtime)
    {
        if (_runtime == runtime)
        {
            Refresh();
            return;
        }

        if (_runtime != null)
            _runtime.OnChanged -= HandleChanged;

        _runtime = runtime;

        if (_runtime != null)
            _runtime.OnChanged += HandleChanged;

        Refresh();
    }

    // Purpose: Handles the incoming changed request or event.
    private void HandleChanged(EntityStatusEffectRuntime runtime)
    {
        Refresh();
    }

    // Purpose: Configures this component''s layout.
    private void ConfigureLayout()
    {
        if (_layoutMode == LayoutMode.PlayerHealthGrid)
        {
            HorizontalLayoutGroup horizontalLayout = GetComponent<HorizontalLayoutGroup>();
            if (horizontalLayout != null)
                horizontalLayout.enabled = false;

            GridLayoutGroup grid = GetComponent<GridLayoutGroup>();
            if (grid == null)
                grid = gameObject.AddComponent<GridLayoutGroup>();

            grid.enabled = true;
            grid.cellSize = new Vector2(_iconSize, _iconSize);
            grid.spacing = new Vector2(_spacing, _spacing);
            grid.startCorner = GridLayoutGroup.Corner.LowerLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.LowerLeft;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = Mathf.Max(1, _iconsPerRow);
            grid.padding = new RectOffset();

            ContentSizeFitter fitter = GetComponent<ContentSizeFitter>();
            if (fitter == null)
                fitter = gameObject.AddComponent<ContentSizeFitter>();

            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return;
        }

        GridLayoutGroup gridLayout = GetComponent<GridLayoutGroup>();
        if (gridLayout != null)
            gridLayout.enabled = false;

        ContentSizeFitter gridFitter = GetComponent<ContentSizeFitter>();
        if (gridFitter != null)
            gridFitter.enabled = false;

        HorizontalLayoutGroup layout = GetComponent<HorizontalLayoutGroup>();

        if (layout == null)
            layout = gameObject.AddComponent<HorizontalLayoutGroup>();

        layout.enabled = true;
        layout.spacing = _spacing;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
    }

    // Purpose: Refreshes the displayed or cached refresh.
    private void Refresh()
    {
        ClearIcons();

        if (_runtime == null)
            return;

        IReadOnlyList<EntityStatusEffectRuntime.ActiveStatusEffect> effects = _runtime.ActiveEffects;

        for (int i = 0; i < effects.Count; i++)
        {
            StatusEffectDefinitionSO definition = effects[i].Definition;

            if (definition == null)
                continue;

            SpawnIcon(definition);
        }

    }

    // Purpose: Spawns icon for the current runtime context.
    private void SpawnIcon(StatusEffectDefinitionSO definition)
    {
        GameObject iconObject = new GameObject(
            definition.Id,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(LayoutElement));
        RectTransform rect = iconObject.GetComponent<RectTransform>();
        rect.SetParent(transform, false);
        rect.sizeDelta = new Vector2(_iconSize, _iconSize);

        LayoutElement layout = iconObject.GetComponent<LayoutElement>();
        layout.preferredWidth = _iconSize;
        layout.preferredHeight = _iconSize;

        Image image = iconObject.GetComponent<Image>();
        image.sprite = ResolveIcon(definition);
        image.color = definition.Icon != null ? definition.IconColor : Color.white;
        image.preserveAspect = true;
        image.raycastTarget = false;

        _spawnedIcons.Add(iconObject);
    }

    // Purpose: Clears the current icons state.
    private void ClearIcons()
    {
        for (int i = 0; i < _spawnedIcons.Count; i++)
        {
            if (_spawnedIcons[i] != null)
                Destroy(_spawnedIcons[i]);
        }

        _spawnedIcons.Clear();
    }

    /// <summary>
    /// Returns the authored icon when available, otherwise creates the shared procedural fallback.
    /// World-space and health-bar presentations intentionally use the same visual language.
    /// </summary>
    public static Sprite GetIcon(StatusEffectDefinitionSO definition)
    {
        return definition != null ? ResolveIcon(definition) : null;
    }

    // Purpose: Resolves the icon needed by this component.
    private static Sprite ResolveIcon(StatusEffectDefinitionSO definition)
    {
        if (definition.Icon != null)
            return definition.Icon;

        if (GeneratedIcons.TryGetValue(definition, out Sprite cached) && cached != null)
            return cached;

        Texture2D texture = BuildProceduralIcon(definition.IconKind, definition.IconColor);
        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            texture.width);
        sprite.name = $"Generated_{definition.Id}";
        GeneratedIcons[definition] = sprite;
        return sprite;
    }

    // Purpose: Builds the procedural icon from the current authored data.
    private static Texture2D BuildProceduralIcon(StatusEffectIconKind kind, Color color)
    {
        const int size = 32;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = $"Status_{kind}";
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        Color32 clear = new Color32(0, 0, 0, 0);
        Color32 foreground = color;
        Color32 dark = Color.Lerp(color, Color.black, 0.55f);
        Color32[] pixels = new Color32[size * size];

        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = clear;

        DrawDisc(pixels, size, 16, 16, 14, dark);
        DrawDisc(pixels, size, 16, 16, 11, foreground);

        if (kind == StatusEffectIconKind.Slow)
        {
            DrawDisc(pixels, size, 12, 17, 6, dark);
            DrawDisc(pixels, size, 12, 17, 3, foreground);
            DrawRect(pixels, size, 10, 9, 13, 5, dark);
            DrawRect(pixels, size, 20, 14, 3, 9, dark);
            DrawRect(pixels, size, 22, 21, 1, 5, dark);
            DrawRect(pixels, size, 26, 21, 1, 5, dark);
        }
        else if (kind == StatusEffectIconKind.InfectionRecoveryDown)
        {
            DrawRect(pixels, size, 8, 18, 16, 4, dark);
            DrawRect(pixels, size, 14, 12, 4, 16, dark);
            DrawRect(pixels, size, 22, 7, 3, 9, dark);
            DrawRect(pixels, size, 19, 7, 9, 3, dark);
            DrawRect(pixels, size, 20, 4, 7, 3, dark);
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        return texture;
    }

    // Purpose: Draws the disc authoring UI.
    private static void DrawDisc(Color32[] pixels, int size, int centerX, int centerY, int radius, Color32 color)
    {
        int radiusSquared = radius * radius;

        for (int y = -radius; y <= radius; y++)
        for (int x = -radius; x <= radius; x++)
        {
            if (x * x + y * y > radiusSquared)
                continue;

            SetPixel(pixels, size, centerX + x, centerY + y, color);
        }
    }

    // Purpose: Draws the rect authoring UI.
    private static void DrawRect(Color32[] pixels, int size, int x, int y, int width, int height, Color32 color)
    {
        for (int py = y; py < y + height; py++)
        for (int px = x; px < x + width; px++)
            SetPixel(pixels, size, px, py, color);
    }

    // Purpose: Updates the pixel state.
    private static void SetPixel(Color32[] pixels, int size, int x, int y, Color32 color)
    {
        if (x < 0 || x >= size || y < 0 || y >= size)
            return;

        pixels[y * size + x] = color;
    }
}
