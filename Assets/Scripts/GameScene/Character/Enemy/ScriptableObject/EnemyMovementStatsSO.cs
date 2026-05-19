using UnityEngine;

[CreateAssetMenu(
    fileName = "EnemyMovementStats",
    menuName = "Game/Enemy/Enemy Movement Stats")]
public class EnemyMovementStatsSO : ScriptableObject
{
    [Header("Movement")]
    [SerializeField] private float _normalMovementSpeed = 2.2f;
    [SerializeField] private float _playerChaseMovementSpeed = 3.5f;

    public float NormalMovementSpeed => DifficultyRuntime.ApplyEnemyNormalMoveSpeed(_normalMovementSpeed);
    public float PlayerChaseMovementSpeed => DifficultyRuntime.ApplyEnemyChaseMoveSpeed(_playerChaseMovementSpeed);
}
