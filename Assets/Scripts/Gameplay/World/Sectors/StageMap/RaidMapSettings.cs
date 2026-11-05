using System;
using System.Collections.Generic;
using UnityEngine;

// Existing branch/full-grid assets keep their serialized meaning until opted in.
public enum StageMapPolicy { Legacy = 0, OddSquareRaid = 1, RadialBraidedRaid = 2 }

// Content is independent of traversal/completion type (Normal/Boss/Start).
public enum RaidRoomRole
{
    None = 0, Normal = 1, Armory = 2, Medical = 3, Vault = 4,
    Boss = 5, Generator = 6, Hatch = 7, Extraction = 8, UnknownCandidate = 9, Merchant = 10
}

[Serializable]
public sealed class RaidMapSettings
{
    [Header("Sparse raid — test tuning (generation version 3)")]
    [Range(.15f, .65f)] public float roomDensity = .32f;
    [Range(0, 100)] public int braidPercent = 25;
    [HideInInspector, Tooltip("구버전 저장 호환용. V3 신규 생성은 사용하지 않습니다.")]
    [Range(1, 8)] public int candidateCount = 4;
    [Tooltip("Number of ordinary rooms with an extraction portal. No dedicated exit room/preset. Generator power only gates the central hatch.")]
    [Range(1, 6)] public int extractionCount = 2;
    [HideInInspector, Range(0, 3)] public int merchantRoomCount = 1;
    [Tooltip("스테이지 전체에서 한 번 추첨합니다. 성공하면 공용 전투방 한 곳에 상인을 추가합니다. 0은 미등장, 100은 공용방이 있을 때 확정입니다.")]
    [Range(0, 100)] public int merchantSpawnPercent = 10;
    [Tooltip("별도 금고 추가 추첨. 발전실·탈출구와의 거리 제한은 없습니다.")]
    [Range(0f, 1f)] public float vaultNearCandidateChance = 0.25f;
    [Tooltip("Prior 65% retained for testing. The redesign's final Boss probability is not decided.")]
    [Range(0, 100)] public int bossSpawnPercent = 65;
    [Tooltip("V3 신규 맵: 보스방을 통과하지 않아도 시작점에서 모든 외곽 탈출구에 도달할 수 있게 배치합니다. 보스 처치 없이 이동 허용하는 옵션은 아닙니다.")]
    public bool preventBossBlockingExtraction = true;
    [HideInInspector, Min(0)] public int minimumExtractionPathDistance = 0;
    [Header("V3 고급 생성 규칙 · OFF는 기존 규칙 유지")]
    public bool customizeTopology;
    public RaidTopologyRules topology = new();
    [Header("Legacy full-grid content weights (V1/V2 only)")]
    [Tooltip("V3 does not use these content weights. Configure Vault Near Candidate Chance above instead.")]
    [Min(0)] public int normalWeight = 70;
    [Min(0)] public int armoryWeight = 10;
    [Min(0)] public int medicalWeight = 15;
    [Min(0)] public int vaultWeight = 5;

    public bool TryValidate(out string error)
    {
        var errors = new List<string>();
        CollectCommonErrors(errors);
        return FinishValidation(errors, out error);
    }

    // Same acceptance rules as the sparse generator, but report every offending value.
    public bool TryValidateSparse(int size, out string error)
    {
        var errors = new List<string>();
        if (bossSpawnPercent < 0 || bossSpawnPercent > 100)
            errors.Add($"보스 출현 확률: 현재 {bossSpawnPercent}% → 허용 범위 0~100%.");
        bool compact = size >= 1 && size < 8;
        if (size < 1 || size > 64) errors.Add($"맵 가로·세로: 현재 {size}칸 → 허용 범위 1~64칸 (1~7은 소형 테스트).");
        if (float.IsNaN(roomDensity) || roomDensity < .15f || roomDensity > .65f)
            errors.Add($"방 밀도: 현재 {Number(roomDensity)} → 허용 범위 0.15~0.65 (15~65%).");
        if (braidPercent < 0 || braidPercent > 100)
            errors.Add($"인접 방 추가 연결: 현재 {braidPercent}% → 허용 범위 0~100%.");
        if (extractionCount < 1 || extractionCount > 6)
            errors.Add($"탈출구 수: 현재 {extractionCount}개 → 허용 범위 1~6개 (소형 맵은 공간에 맞춰 축소).");
        if (merchantSpawnPercent < 0 || merchantSpawnPercent > 100)
            errors.Add($"상인 등장 확률: 현재 {merchantSpawnPercent}% → 허용 범위 0~100%.");
        if (float.IsNaN(vaultNearCandidateChance) || vaultNearCandidateChance < 0 || vaultNearCandidateChance > 1)
            errors.Add($"별도 금고 추가 확률: 현재 {Number(vaultNearCandidateChance)} → 허용 범위 0~1 (0~100%).");
        if (size >= 1 && size <= 64)
        {
            var rules = customizeTopology && topology != null ? topology : new RaidTopologyRules();
            if (!rules.TryValidate(size, out string topologyError, compact)) errors.Add(topologyError);
        }
        return FinishValidation(errors, out error);
    }

    private void CollectCommonErrors(List<string> errors)
    {
        if (bossSpawnPercent < 0 || bossSpawnPercent > 100)
            errors.Add($"보스 출현 확률: 현재 {bossSpawnPercent}% → 허용 범위 0~100%.");
        if (minimumExtractionPathDistance < 0)
            errors.Add($"후보 최소 경로 거리: 현재 {minimumExtractionPathDistance}칸 → 0 이상이어야 합니다.");
        var weights = new[] { ("일반", normalWeight), ("보안실", armoryWeight), ("의료실", medicalWeight), ("금고", vaultWeight) };
        foreach (var weight in weights)
            if (weight.Item2 < 0) errors.Add($"구버전 {weight.Item1} 가중치: 현재 {weight.Item2} → 0 이상이어야 합니다.");
        long sum = (long)normalWeight + armoryWeight + medicalWeight + vaultWeight;
        if (sum <= 0 || sum > int.MaxValue)
            errors.Add($"구버전 맵 가중치 합계: 현재 {sum} → 허용 범위 1~{int.MaxValue}.");
    }

    internal static string Number(float value) => value.ToString("G7", System.Globalization.CultureInfo.InvariantCulture);
    internal static bool FinishValidation(List<string> errors, out string error, string heading = "맵 생성 설정 오류")
    {
        error = errors.Count == 0 ? string.Empty : heading + ":\n• " + string.Join("\n• ", errors);
        return errors.Count == 0;
    }
}

[Serializable]
public sealed class RaidTopologyRules
{
    [Tooltip("0이면 격자²×밀도. 기본 연결 구조보다 적게 만들 수는 없습니다.")]
    [Min(0)] public int targetRoomCount;
    [Min(0)] public int minimumRoomCount;
    [Tooltip("0이면 제한 없음. 중심 링/통로보다 작으면 생성 오류를 표시합니다.")]
    [Min(0)] public int maximumRoomCount;
    [Tooltip("0이면 격자 크기×아래 비율로 계산합니다.")]
    [Min(0)] public int ringRadius;
    [Range(.05f, .45f)] public float ringRadiusRatio = .2f;
    [HideInInspector, Min(0)] public int candidateSeparationWeight = 3;
    [HideInInspector, Min(0)] public int candidateDistanceWeight = 1;
    [HideInInspector, Min(1)] public int vaultCandidateDistance = 2;
    [HideInInspector, Min(0)] public int merchantMinimumDistance = 2;

    public bool TryValidate(int size, out string error, bool compact = false)
    {
        var errors = new List<string>();
        long cells = (long)size * size;
        foreach (var count in new[] { ("목표 방 수", targetRoomCount), ("최소 방 수", minimumRoomCount), ("최대 방 수", maximumRoomCount) })
            if (count.Item2 < 0 || (compact ? count.Item2 > cells : count.Item2 >= cells))
                errors.Add($"{count.Item1}: 현재 {count.Item2}개 → {size}×{size} 격자의 허용 범위 0~{(compact ? cells : cells - 1)}개 (0은 기본/제한 없음).");
        if (maximumRoomCount > 0 && minimumRoomCount > maximumRoomCount)
            errors.Add($"방 수 범위: 최소 {minimumRoomCount}개 > 최대 {maximumRoomCount}개 → 최소가 최대보다 클 수 없습니다.");
        bool validRatio = !float.IsNaN(ringRadiusRatio) && ringRadiusRatio >= .05f && ringRadiusRatio <= .45f;
        if (!validRatio)
            errors.Add($"격자 대비 링 반경 비율: 현재 {RaidMapSettings.Number(ringRadiusRatio)} → 허용 범위 0.05~0.45.");
        int maximumRadius = (size - 1) / 2;
        if (ringRadius < 0)
            errors.Add($"중심 링 반경: 현재 {ringRadius}칸 → 허용 범위 0~{maximumRadius}칸 (0은 비율로 계산).");
        else if (!compact && (ringRadius > 0 || validRatio))
        {
            int radius = ringRadius > 0 ? ringRadius : Mathf.Max(1, Mathf.FloorToInt(size * ringRadiusRatio));
            if (radius < 1 || radius > maximumRadius)
                errors.Add($"중심 링 반경: 현재 {radius}칸" + (ringRadius == 0 ? " (자동 계산)" : "") + $" → {size}×{size} 격자에서 허용 범위 1~{maximumRadius}칸.");
        }
        return RaidMapSettings.FinishValidation(errors, out error, "고급 생성 규칙 오류");
    }
}
