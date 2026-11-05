using UnityEngine;

/// <summary>One shared visual definition, independent of authored structure prefabs.</summary>
[CreateAssetMenu(fileName = "SectorDestructionEffects", menuName = "Game/Sector/공용 엄폐 파괴 연기")]
public sealed class SectorDestructionEffectsSO : ScriptableObject
{
    [Header("공용 연기 · 모든 파괴 엄폐에 적용")]
    [SerializeField] private GameObject _smokePrefab;
    [Tooltip("프리팹 배율 1의 기준 연기 지름(월드 단위). 구조물의 긴 바닥 변 / 이 값 × 추가 배율로 크기를 결정합니다.")]
    [SerializeField, Min(0.01f)] private float _referenceFootprint = 10f;
    [SerializeField, Min(0.01f)] private float _sizeMultiplier = 1f;
    [Tooltip("아주 작거나 큰 구조물의 이펙트 배율 안전 범위. 기준 비례 계산 후 제한합니다.")]
    [SerializeField] private Vector2 _scaleLimits = new Vector2(0.15f, 12f);
    [Tooltip("반복 재생은 런타임 인스턴스에서 끕니다. 프리팹 설정 오류 시에도 이 시간 뒤 정리합니다.")]
    [SerializeField, Min(0.1f)] private float _maximumLifetime = 10f;

    public GameObject SmokePrefab => _smokePrefab;
    public float MaximumLifetime => Mathf.Max(0.1f, _maximumLifetime);
    public float ResolveScale(Bounds bounds)
    {
        float footprint = Mathf.Max(bounds.size.x, bounds.size.z);
        float scale = Mathf.Max(0.01f, footprint) / Mathf.Max(0.01f, _referenceFootprint) * Mathf.Max(0.01f, _sizeMultiplier);
        float minimum = Mathf.Max(0.01f, Mathf.Min(_scaleLimits.x, _scaleLimits.y));
        float maximum = Mathf.Max(minimum, Mathf.Max(_scaleLimits.x, _scaleLimits.y));
        return Mathf.Clamp(scale, minimum, maximum);
    }
}
