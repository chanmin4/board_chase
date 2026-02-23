/*
using UnityEngine;
using System.Collections;

public class ZoneCriticalArc : CardAbility
{
    [Header("Ref")]
    public SurvivalDirector director;
    Transform player;

    [Header("Zone Arc change")]
    public float changearc = 360f;

    Coroutine co;

    // 내부 상태(복구용)
    float prevArc;
    bool  applied;

    public override void Activate(Transform playerTf, SurvivalDirector dir, CardData data)
    {
        // 이미 돌고 있으면 먼저 정리(원래 값 복구)
        StopNow();

        player   = playerTf;
        director = dir;

        if (!player || !director)
        {
            Debug.LogWarning("[ZoneCriticalArc] refs missing");
            return;
        }

        // 적용 + 코루틴으로 지속시간 관리
        prevArc         = director.bonusArcDeg;
        director.SetBonusArcForAll(changearc);
        applied         = true;

        IsRunning = true;
        float dur = (data != null) ? Mathf.Max(0f, data.duration) : 0f;
        co = StartCoroutine(RunDuration(dur));
    }

    IEnumerator RunDuration(float duration)
    {
        if (duration > 0f)
            yield return new WaitForSeconds(duration);
        // 시간이 끝나면 종료(복구 포함)
        StopNow();
    }

    public override void StopNow()
    {
        if (co != null)
        {
            StopCoroutine(co);
            co = null;
        }

        // 적용되어 있으면 복구
        if (applied && director)
        {
            director.SetBonusArcForAll(prevArc);
        }
        applied   = false;
        IsRunning = false;
    }

    void OnDisable()
    {
        // 비활성화/파괴 시에도 안전 복구
        StopNow();
    }
}
*/