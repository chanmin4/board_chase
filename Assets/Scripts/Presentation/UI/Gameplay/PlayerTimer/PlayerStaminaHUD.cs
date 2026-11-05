using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>Runtime HUD; no input raycasts and no edits to authored Canvas prefabs.</summary>
public sealed class PlayerStaminaHUD : MonoBehaviour
{
    private ShooterStaminaRuntime _stamina;
    private GameObject _canvas;
    private RectTransform _fill;
    private Image _fillImage;
    private TextMeshProUGUI _label;
    private void Start()
    {
        _stamina = EntityComponentResolver.Resolve<ShooterStaminaRuntime>(this);
        _canvas = new GameObject("Stamina HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        DontDestroyOnLoad(_canvas); // Its owner may survive Home/raid scene transitions; OnDestroy still owns cleanup.
        _canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.GetComponent<Canvas>().sortingOrder = 5;
        var scaler = _canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        var panel = new GameObject("Stamina", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(_canvas.transform, false);
        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0, 180);
        rect.sizeDelta = new Vector2(240, 22);
        var bg = panel.GetComponent<Image>(); bg.color = new Color(0, 0, 0, 0.7f); bg.raycastTarget = false;
        var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(panel.transform, false);
        _fill = fill.GetComponent<RectTransform>();
        _fill.anchorMin = Vector2.zero; _fill.anchorMax = Vector2.one;
        _fill.offsetMin = _fill.offsetMax = Vector2.zero;
        _fillImage = fill.GetComponent<Image>(); _fillImage.raycastTarget = false;
        var label = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        label.transform.SetParent(panel.transform, false);
        var labelRect = label.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero; labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
        _label = label.GetComponent<TextMeshProUGUI>();
        _label.fontSize = 16; _label.alignment = TextAlignmentOptions.Center; _label.raycastTarget = false;
    }
    private void Update()
    {
        if (_canvas == null || _stamina == null) return;
        _canvas.SetActive(_stamina.UsesStamina);
        _fill.anchorMax = new Vector2(_stamina.Normalized, 1);
        _fillImage.color = _stamina.IsExhausted ? new Color(0.8f, 0.2f, 0.15f) : new Color(0.05f, 0.65f, 0.65f);
        _label.text = $"STA {_stamina.Current:0} / {_stamina.Maximum:0}";
    }
    private void OnDisable() { if (_canvas != null) _canvas.SetActive(false); }
    private void OnDestroy() { if (_canvas != null) Destroy(_canvas); }
}
