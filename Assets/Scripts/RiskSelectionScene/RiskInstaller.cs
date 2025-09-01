using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;

/// <summary>
/// 선택 씬에서 생성되며 DontDestroyOnLoad로 유지.
/// 다음 씬(보통 게임 씬)이 로드되면 RiskSession의 선택 목록을 읽어
/// 대상 오브젝트를 찾아 패치 컴포넌트를 부착/Apply하고, 스스로 파괴.
/// </summary>
public class RiskInstaller : MonoBehaviour
{
    [Tooltip("비워두면 첫 번째 로드된 씬에서 적용, 채우면 해당 이름과 일치할 때만 적용")]
    public string applyOnSceneName = ""; // 예: "Game"
    public bool autoDestroyAfterApply = true;

    bool _applied;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public static RiskInstaller Spawn(string applyOnSceneName = "")
    {
        var go = new GameObject("__RiskInstaller");
        var inst = go.AddComponent<RiskInstaller>();
        inst.applyOnSceneName = applyOnSceneName;
        return inst;
    }

    void OnSceneLoaded(Scene s, LoadSceneMode mode)
    {
        if (_applied) return;
        if (!string.IsNullOrEmpty(applyOnSceneName) && s.name != applyOnSceneName) return;

        ApplyAll();
        _applied = true;

        if (autoDestroyAfterApply) Destroy(gameObject);
    }

    void ApplyAll()
    {
        if (RiskSession.Set == null || RiskSession.Selected.Count == 0)
        {
            Debug.Log("[RiskInstaller] 적용할 선택이 없거나 세트가 비어있음");
            return;
        }

        // === 예) DragCooldownAdd 합산 후 적용 ===
        var launcher = FindAnyObjectByType<DiskLauncher>(); // 미리 붙여둘 필요 없음. 씬에 존재하면 찾음.
        if (launcher)
        {
            float extraCool = RiskSession.Selected
                .Where(d => d && d.type == RiskType.DragCooldownAdd)
                .Sum(d => d.float_parameter1);

            if (extraCool > 0f)
            {
                var patch = launcher.GetComponent<Risk_DragCooldown>();
                if (!patch) patch = launcher.gameObject.AddComponent<Risk_DragCooldown>();
                patch.disklauncher = launcher;
                patch.addSeconds = extraCool;
                patch.applyOnStart = false;
                patch.Apply();
                Debug.Log($"[RiskInstaller] DragCooldownAdd +{extraCool:0.##}s 적용");
            }
        }
        else
        {
            Debug.LogWarning("[RiskInstaller] DiskLauncher를 씬에서 찾지 못함");
        }

        // === TODO: 다른 RiskType도 같은 패턴으로 확장 ===
        // MissileSpeedUp, MissileExplosionUp, MissileSpawnEveryCycle, MissileCountUp ...
        // ZoneGaugeGainMinus, ZoneReqHitsAdd, ZoneCompositionChange ...
        // CardChargeRequiredAdd, CardDisabled ...
        // PollutionFrictionEnable ...
    }
}
