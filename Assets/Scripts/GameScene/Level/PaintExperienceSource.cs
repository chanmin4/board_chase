using UnityEngine;

[DisallowMultipleComponent]
public class PaintExperienceSource : MonoBehaviour
{
    [Header("Reward")]
    [SerializeField] private PaintExperienceRewardSO _reward;

    [Header("Listening To")]
    [SerializeField] private MaskRenderManagerEventChannelSO _maskRenderManagerReadyChannel;

    [Header("Broadcasting On")]
    [SerializeField] private PlayerExperienceGainEventChannelSO _xpGainChannel;

    private MaskRenderManager _maskRenderManager;

    private void OnEnable()
    {
        if (_maskRenderManagerReadyChannel != null)
        {
            _maskRenderManagerReadyChannel.OnEventRaised += OnMaskRenderManagerChanged;

            if (_maskRenderManagerReadyChannel.Current != null)
                OnMaskRenderManagerChanged(_maskRenderManagerReadyChannel.Current);
        }
    }

    private void OnDisable()
    {
        if (_maskRenderManagerReadyChannel != null)
            _maskRenderManagerReadyChannel.OnEventRaised -= OnMaskRenderManagerChanged;

        UnbindMaskRenderManager();
    }

    private void OnMaskRenderManagerChanged(MaskRenderManager manager)
    {
        if (_maskRenderManager == manager)
            return;

        UnbindMaskRenderManager();

        _maskRenderManager = manager;

        if (_maskRenderManager != null)
            _maskRenderManager.OnCirclePaintImpactAccepted += OnCirclePaintImpactAccepted;
    }

    private void UnbindMaskRenderManager()
    {
        if (_maskRenderManager != null)
            _maskRenderManager.OnCirclePaintImpactAccepted -= OnCirclePaintImpactAccepted;

        _maskRenderManager = null;
    }

    private void OnCirclePaintImpactAccepted(MaskRenderManager.CirclePaintImpact impact)
    {
        if (_reward == null || _xpGainChannel == null)
            return;

        float xp = _reward.CalculateXp(impact);

        if (xp <= 0f)
            return;
        Debug.Log($"Gained {xp} XP from painting at {impact.worldPos}");
        _xpGainChannel.RaiseEvent(new PlayerExperienceGain(
            xp,
            PlayerExperienceSource.Paint,
            impact.worldPos,
            gameObject));
    }
}
