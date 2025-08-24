using UnityEngine;
using TMPro;
using System.Text;
using System.Linq;

/// <summary>
/// 벽 튕김 카운트 + 각 LifeZone 프로필(Inspector 리스트)의 요구 튕김 수를
/// 자동으로 요약해 보여주는 HUD.
/// - SurvivalDirector.OnWallHitsChanged 구독
/// - 씬 시작/세트 리셋 시에도 최신 상태로 표시
/// </summary>
public class WallHitHUD : MonoBehaviour
{
    [Header("Refs")]
    public SurvivalDirector director;
    public TMP_Text label;  // Canvas 안 TextMeshProUGUI

    [Header("Options")]
    [Tooltip("프로필 이름을 함께 표시 (name 필드가 비어있으면 P1, P2... 로 대체)")]
    public bool showProfileNames = true;

    [Tooltip("요구치 요약을 한 줄로 표시 (예: Req: P1=1, P2=2, P3=3...)")]
    public bool compactSummary = true;

    void OnEnable()
    {
        if (!director) director = FindAnyObjectByType<SurvivalDirector>();

        if (director)
        {
            director.OnWallHitsChanged += UpdateView;
            director.OnZonesReset      += HandleResetOrSpawnChange;
            director.OnZoneSpawned     += _ => HandleResetOrSpawnChange();
        }

        // 초기 표시
        UpdateView(director ? director.CurrentWallHits : 0);
    }

    void OnDisable()
    {
        if (!director) return;
        director.OnWallHitsChanged -= UpdateView;
        director.OnZonesReset      -= HandleResetOrSpawnChange;
        director.OnZoneSpawned     -= _ => HandleResetOrSpawnChange();
    }

    void HandleResetOrSpawnChange()
    {
        UpdateView(director ? director.CurrentWallHits : 0);
    }

    public void UpdateView(int wallHits)
    {
        if (!label)
            return;

        if (!director || director.zoneProfiles == null || director.zoneProfiles.Count == 0)
        {
            label.text = $"WALL HITS: {wallHits}";
            return;
        }

        // 다음 목표(현재보다 큰 가장 작은 요구치)
        int nextTarget = NextTargetRequirement(wallHits);

        var sb = new StringBuilder(128);
        sb.Append("WALL HITS: ").Append(wallHits);

        if (nextTarget > wallHits)
        {
            sb.Append("   (Next: ").Append(nextTarget).Append(")");
        }

        // 요구치 요약
        sb.AppendLine();

        if (compactSummary)
        {
            sb.Append("Req: ");
            for (int i = 0; i < director.zoneProfiles.Count; i++)
            {
                var p = director.zoneProfiles[i];
                string pname = showProfileNames && !string.IsNullOrWhiteSpace(p.name) ? p.name : $"P{i + 1}";
                sb.Append(pname).Append('=').Append(Mathf.Clamp(p.requiredWallHits, 0, 5));
                if (i < director.zoneProfiles.Count - 1) sb.Append(", ");
            }
        }
        else
        {
            // 멀티라인 상세
            for (int i = 0; i < director.zoneProfiles.Count; i++)
            {
                var p = director.zoneProfiles[i];
                string pname = showProfileNames && !string.IsNullOrWhiteSpace(p.name) ? p.name : $"Profile {i + 1}";
                sb.Append("- ").Append(pname)
                  .Append(" : need ").Append(Mathf.Clamp(p.requiredWallHits, 0, 5))
                  .AppendLine();
            }
        }

        label.text = sb.ToString();
    }

    int NextTargetRequirement(int wallHits)
    {
        if (!director || director.zoneProfiles == null || director.zoneProfiles.Count == 0)
            return 0;

        // 현재 벽 히트보다 큰 요구치 중 최솟값
        var higher = director.zoneProfiles
                             .Select(p => Mathf.Clamp(p.requiredWallHits, 0, 5))
                             .Where(req => req > wallHits)
                             .OrderBy(req => req)
                             .FirstOrDefault();

        return higher; // 없으면 0
    }
}
