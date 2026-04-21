using UnityEngine;

[CreateAssetMenu(
    fileName = "PaintExperienceReward",
    menuName = "Game/Experience/Paint Experience Reward")]
public class PaintExperienceRewardSO : ScriptableObject
{
    [Header("Area XP")]
    [Min(0f)]
    [SerializeField] private float xpPerNeutralArea = 0.6f;

    [Min(0f)]
    [SerializeField] private float xpPerVirusOverwriteArea = 0.6f;

    [Header("Minimum Valid Paint")]
    [Min(0f)]
    [SerializeField] private float minimumValidArea = 0.01f;

    [Header("Optional")]
    [Min(0f)]
    [SerializeField] private float validStampBonus = 0f;

    public float CalculateXp(MaskRenderManager.CirclePaintImpact impact)
    {
        if (impact.channel != MaskRenderManager.PaintChannel.Vaccine)
            return 0f;

        if (impact.ValidArea < minimumValidArea)
            return 0f;

        float xp = 0f;

        xp += impact.neutralArea * xpPerNeutralArea;
        xp += impact.overwrittenVirusArea * xpPerVirusOverwriteArea;

        if (xp > 0f)
            xp += validStampBonus;

        return Mathf.Max(0f, xp);
    }
}
