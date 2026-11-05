/// <summary>
/// 한 생성 개체의 외형 선택을 소유한다. AI 상태 변경/비활성화는 새 스폰이 아니다.
/// 현재 스포너는 Instantiate를 사용하므로 개체마다 새 세션이 생긴다.
/// 향후 풀링을 도입하면 '새 스폰' 경계에서 새 세션을 만들어야 한다.
/// </summary>
public sealed class ShooterSpawnLookSession
{
    private EnemyShooterConfigSO _config;
    private bool _applied;
    public ShooterCustomizeSO Selected { get; private set; }

    public bool ApplyOnce(EnemyShooterConfigSO config, ShooterVisualRig rig)
    {
        if (config == null || rig == null || (_applied && _config == config)) return false;
        bool hadOverride = Selected != null;
        Selected = config.PickSpawnLook();
        _config = config;
        _applied = true;
        if (Selected != null) rig.ApplyCustomize(Selected);
        else if (hadOverride)
        {
            if (rig.CharacterCustomize != null) rig.ApplyCustomize(rig.CharacterCustomize);
            else if (rig.DefaultCustomize != null) rig.ApplyCustomize(rig.DefaultCustomize);
            else rig.ApplyAppearance(rig.DefaultAppearanceProfile);
        }
        return true;
    }
}
