using UnityEngine;

[CreateAssetMenu(
    fileName = "EnemyExperienceReward",
    menuName = "Game/Experience/Enemy Experience Reward")]
public class EnemyExperienceRewardSO : ScriptableObject
{
    [Header("Reward")]
    [Min(0f)]
    [SerializeField] private float xpOnDeath = 25f;

    public float XpOnDeath => xpOnDeath;
}
