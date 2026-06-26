using UnityEngine;

[System.Serializable]
public sealed class EnemyScreenSpaceWidgetLayout
{
    [SerializeField] private Vector3 _worldOffset = Vector3.zero;
    [SerializeField] private Vector2 _screenPixelOffset = new Vector2(0f, 50f);
    [SerializeField] private bool _hideWhenBehindCamera = true;
    [SerializeField] private bool _hideWhenOffScreen = true;
    [SerializeField] private bool _clampToScreen = true;
    [SerializeField, Min(0f)] private float _screenEdgePadding = 24f;

    public EnemyScreenSpaceWidgetLayout()
    {
    }

    public EnemyScreenSpaceWidgetLayout(Vector2 screenPixelOffset)
    {
        _screenPixelOffset = screenPixelOffset;
    }

    public Vector3 WorldOffset => _worldOffset;
    public Vector2 ScreenPixelOffset => _screenPixelOffset;
    public bool HideWhenBehindCamera => _hideWhenBehindCamera;
    public bool HideWhenOffScreen => _hideWhenOffScreen;
    public bool ClampToScreen => _clampToScreen;
    public float ScreenEdgePadding => Mathf.Max(0f, _screenEdgePadding);
}
