/*
 * Role hierarchy: Presentation > Audio
 * Primary responsibility: Identifies an obstacle's physical surface for shared bullet impact audio.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

// Serialized in obstacle prefabs. Keep these values stable.
public enum ObstacleSurfaceType
{
    Default = 0,
    Metal = 1,
    Wood = 2,
    Concrete = 3,
    Sand = 4,
    Rubber = 5
}

[DisallowMultipleComponent]
[AddComponentMenu("Audio/Obstacle Surface")]
public sealed class ObstacleSurface : MonoBehaviour
{
    [Tooltip("표면 종류만 선택하세요. 재질별 총탄 피격음은 Resources/BulletImpactAudioCatalog에서 공통 설정합니다. 자식 Collider에도 적용됩니다.")]
    [SerializeField] private ObstacleSurfaceType _surface = ObstacleSurfaceType.Default;

    private float _lastImpactTime = float.NegativeInfinity;

    public ObstacleSurfaceType Surface => _surface;

    private void OnEnable()
    {
        _lastImpactTime = float.NegativeInfinity;
    }

    internal bool CanPlayImpact(float now, float minimumInterval)
    {
        // A clock reset must not leave an obstacle silent after entering play mode.
        return now < _lastImpactTime || now - _lastImpactTime >= minimumInterval;
    }

    internal void RecordImpact(float now)
    {
        _lastImpactTime = now;
    }
}
