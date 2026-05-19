using UnityEngine;

[CreateAssetMenu(
    fileName = "NormalEnemyMoveToInfectionTargetConfig",
    menuName = "ScriptableObject/GameScene/Enemy/Definition_Config/Normal Enemy Move To Infection Target Config")]
public class NormalEnemyMoveToInfectionTargetConfigSO : ScriptableObject
{
    [SerializeField] private float _moveSpeed = 3.5f;
    [SerializeField] private float _stoppingDistance = 0.8f;

    public float MoveSpeed => DifficultyRuntime.ApplyEnemyNormalMoveSpeed(_moveSpeed);
    public float StoppingDistance => Mathf.Max(0f, _stoppingDistance);
}
