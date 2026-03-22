using UnityEngine;

public enum StageLevel
{
    stage1_1x1,
    stage2_1x2,
    stage3_2x2,
    stage4_2x3,
    stage5_3x3,
    stage6_4x4,
    stage7_5x5


}
[CreateAssetMenu(menuName = "Game/GameStage", fileName = "Stage")]
public class GameStage : ScriptableObject
{
    [Header("StageLevel")]
    public StageLevel stage;
    
}
