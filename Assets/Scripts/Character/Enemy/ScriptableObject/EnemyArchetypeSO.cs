using UnityEngine;

[CreateAssetMenu(
    fileName = "EnemyArchetype",
    menuName = "Game/Enemy/Enemy Archetype")]
public class EnemyArchetypeSO : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string _id = "enemy_common";
    [SerializeField] private string _displayName = "Enemy";

    [Header("Prefab")]
    [SerializeField] private Enemy _enemyPrefab;

    public string Id => _id;
    public string DisplayName => _displayName;
    public Enemy EnemyPrefab => _enemyPrefab;
    public bool IsValid => _enemyPrefab != null;
}
