using UnityEngine;

[CreateAssetMenu(
    fileName = "EnemyUIFollowSettings",
    menuName = "Game/UI/Enemy UI Follow Settings")]
public class EnemyUIFollowSettingsSO : ScriptableObject
{
    [Header("World Offset")]
    [SerializeField] private Vector3 _worldOffset = new Vector3(0f, 0.25f, 0f);

    [Header("Screen Offset")]
    [SerializeField] private Vector2 _screenPixelOffset = new Vector2(0f, 18f);

    [Header("Visibility")]
    [SerializeField] private bool _hideWhenBehindCamera = true;
    [SerializeField] private bool _hideWhenOffScreen = false;
    [SerializeField] private bool _clampToScreen = true;
    [SerializeField] private float _screenEdgePadding = 24f;

    public Vector3 WorldOffset => _worldOffset;
    public Vector2 ScreenPixelOffset => _screenPixelOffset;
    public bool HideWhenBehindCamera => _hideWhenBehindCamera;
    public bool HideWhenOffScreen => _hideWhenOffScreen;
    public bool ClampToScreen => _clampToScreen;
    public float ScreenEdgePadding => _screenEdgePadding;
}
