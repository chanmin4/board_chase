using UnityEngine;

[CreateAssetMenu(
    fileName = "EnemyMovementStats",
    menuName = "Game/Enemy/Enemy Movement Stats")]
public class EnemyMovementStatsSO : ScriptableObject
{
    [SerializeField] private float _chaseSpeed = 3.5f;

    public float ChaseSpeed => _chaseSpeed;
}
