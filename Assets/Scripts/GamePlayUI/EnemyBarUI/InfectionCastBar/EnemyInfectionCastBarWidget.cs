using UnityEngine;
using UnityEngine.UI;

public class EnemyInfectionCastBarWidget : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RectTransform _root;
    [SerializeField] private RectTransform _fillRect;
    [SerializeField] private Image _fillImage;
    [SerializeField] private UICanvasGroupOpacity _uicanvasgroupopacity;

    [Header("Layout")]
    [SerializeField] private Vector2 _screenOffset = new Vector2(0f, -10f);

    private Enemy _enemy;
    private float _fillFullWidth;

    public RectTransform Root => _root != null ? _root : (RectTransform)transform;

    private void Awake()
    {
        if (_root == null)
            _root = (RectTransform)transform;

        if (_fillRect == null && _fillImage != null)
            _fillRect = _fillImage.rectTransform;

        if (_fillRect != null)
        {
            _fillFullWidth = _fillRect.sizeDelta.x;
            _fillRect.pivot = new Vector2(0f, 0.5f);
            _fillRect.anchorMin = new Vector2(0f, 0.5f);
            _fillRect.anchorMax = new Vector2(0f, 0.5f);
        }

        EnsureFillImageMode();
        RefreshImmediate();
    }

    public void Bind(EnemyScreenSpaceUIAnchor anchor)
    {
        _enemy = anchor != null ? anchor.Enemy : null;
        RefreshImmediate();
    }

    public void SetScreenPosition(Vector2 anchoredPosition)
    {
        Root.anchoredPosition = anchoredPosition + _screenOffset;
    }

    public void TickVisualState()
    {
        if (_enemy == null || _uicanvasgroupopacity == null)
            return;

        if (!_enemy.IsInfectionCasting)
        {
            _uicanvasgroupopacity.Hide();
            RefreshImmediate();
            return;
        }

        _uicanvasgroupopacity.Show();
        RefreshImmediate();
    }

    private void RefreshImmediate()
    {
        float normalized = _enemy != null ? _enemy.InfectionCastProgress01 : 0f;

        if (_fillImage != null)
        {
            EnsureFillImageMode();
            _fillImage.fillAmount = normalized;
        }

        if (_fillRect != null)
        {
            Vector2 size = _fillRect.sizeDelta;
            size.x = _fillFullWidth * normalized;
            _fillRect.sizeDelta = size;
        }
    }


    private void EnsureFillImageMode()
    {
        if (_fillImage == null)
            return;

        if (_fillImage.type != Image.Type.Filled)
            _fillImage.type = Image.Type.Filled;

        if (_fillImage.fillMethod != Image.FillMethod.Horizontal)
            _fillImage.fillMethod = Image.FillMethod.Horizontal;

        if (_fillImage.fillOrigin != (int)Image.OriginHorizontal.Left)
            _fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
    }
}
