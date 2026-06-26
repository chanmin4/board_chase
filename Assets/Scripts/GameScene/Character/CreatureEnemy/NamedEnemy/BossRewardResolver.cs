using UnityEngine;

public class BossRewardResolver : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private NamedSectorController _namedSectorController;

    [Header("Listening")]
    [SerializeField] private BossRewardSelectedEventChannelSO _rewardSelectedChannel;

    private void Reset()
    {
        if (_namedSectorController == null)
            _namedSectorController = FindAnyObjectByType<NamedSectorController>();
    }

    private void Awake()
    {
        if (_namedSectorController == null)
            _namedSectorController = FindAnyObjectByType<NamedSectorController>();
    }

    private void OnEnable()
    {
        if (_rewardSelectedChannel != null)
            _rewardSelectedChannel.OnEventRaised += HandleRewardSelected;
    }

    private void OnDisable()
    {
        if (_rewardSelectedChannel != null)
            _rewardSelectedChannel.OnEventRaised -= HandleRewardSelected;
    }

    private void HandleRewardSelected(BossRewardSelection selection)
    {
        ApplyRewardPlaceholder(selection);

        if (_namedSectorController != null)
            _namedSectorController.ConfirmNamedRewardAndEndBattle();
    }

    private void ApplyRewardPlaceholder(BossRewardSelection selection)
    {
        Debug.Log(
            $"[BossRewardResolver] Reward selected. " +
            $"reward={selection.reward?.RewardId}, sourceSector={selection.sourceSector?.name}",
            this);

        // TODO:
        // - reward id에 따라 제거 공격력 / 코팅 반경 / 이동속도 등 보상 적용
        // - 네임드 처치 효과 적용
        // - sourceSector 기준 십자 5칸 플레이어 점유 100%
        // - 감염통제량 일부 회복
        // - 플레이어 감염률 회복
    }
}
