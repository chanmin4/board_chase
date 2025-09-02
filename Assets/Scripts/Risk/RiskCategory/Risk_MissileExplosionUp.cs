using UnityEngine;

[DisallowMultipleComponent]
public class Risk_MissileExplosionUp : MonoBehaviour
{
    [Header("Installer 호환용(사용되진 않음)")]
    public MonoBehaviour homingMissile;
    public bool applyOnStart = true;

    [Header("Targets")]
    [Tooltip("비워두면 씬에서 전부 자동 검색")]
    public BarrageMissileSpawner[] spawners;

    [Header("Param")]
    [Min(0.01f)] public float radiusMul = 1.25f;

    float[] _origHitR, _origTimeoutR;
    bool _captured;

    void Awake()
    {
        if (spawners == null || spawners.Length == 0)
            spawners = Object.FindObjectsByType<BarrageMissileSpawner>(
    FindObjectsInactive.Include,   // ← 예전의 true (비활성 포함)
    FindObjectsSortMode.None       // 정렬 불필요하면 None이 가장 빠름
);

        if (spawners != null && spawners.Length > 0)
        {
            _origHitR     = new float[spawners.Length];
            _origTimeoutR = new float[spawners.Length];
            for (int i = 0; i < spawners.Length; i++)
            {
                _origHitR[i]     = spawners[i].hitRadiusWorld;
                _origTimeoutR[i] = spawners[i].timeoutRadiusWorld;
            }
            _captured = true;
        }
    }

    void Start()    { if (applyOnStart) Apply(); }
    void OnEnable() { if (Application.isPlaying && applyOnStart) Apply(); }
    void OnDisable(){ if (Application.isPlaying) Revert(); }

    public void Apply()
    {
        if (!_captured) return;
        float m = Mathf.Max(0.01f, radiusMul);
        for (int i = 0; i < spawners.Length; i++)
        {
            if (!spawners[i]) continue;
            spawners[i].hitRadiusWorld     = _origHitR[i] * m;
            spawners[i].timeoutRadiusWorld = _origTimeoutR[i] * m;
        }
    }

    public void Revert()
    {
        if (!_captured) return;
        for (int i = 0; i < spawners.Length; i++)
        {
            if (!spawners[i]) continue;
            spawners[i].hitRadiusWorld     = _origHitR[i];
            spawners[i].timeoutRadiusWorld = _origTimeoutR[i];
        }
    }
}
