using UnityEngine;

[DisallowMultipleComponent]
public class EnemyMovementStatsProvider : MonoBehaviour
{
    [SerializeField] private EnemyMovementStatsSO _movementStats;

    public EnemyMovementStatsSO MovementStats => _movementStats;

    public float NormalMovementSpeed =>
        _movementStats != null ? _movementStats.NormalMovementSpeed : 2.2f;

    public float PlayerChaseMovementSpeed =>
        _movementStats != null ? _movementStats.PlayerChaseMovementSpeed : 3.5f;
}
