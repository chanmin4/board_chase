// CleanTrailAbility.cs  (통합본 교체)
using UnityEngine;
using System.Collections;

public class CleanTrailAbility : CardAbility
{
    SurvivalDirector director;
    Transform player;
    float extraRadiusTiles;        // CardData.radiusTiles (추가 범위용)

    Collider diskCol;              // 디스크 콜라이더로 실반경 계산
    float diskRadiusWorld;         // 콜라이더에서 구한 월드반경
    float sampleInterval = 0.02f;  // 너무 자주 호출 방지(50Hz)

    Coroutine runCo, cleanCo;

    public override void Activate(Transform playerTf, SurvivalDirector dir, CardData data)
    {
        StopNow(); // 중복 방지

        player = playerTf;
        director = dir;
        extraRadiusTiles = Mathf.Max(0f, data.radiusTiles);

        if (!player || !director) { Debug.LogWarning("[CleanTrail] refs missing"); return; }

        diskCol = player.GetComponent<Collider>();
        if (diskCol != null)
        {
            // 콜라이더의 bounds로 월드반경 추정 (원형/사각형 모두 커버)
            var b = diskCol.bounds;
            // 가로,세로 중 큰 값을 반경으로 (반지름 ~ half of max dimension)
            diskRadiusWorld = Mathf.Max(b.extents.x, b.extents.z);
        }
        else
        {
            // 콜라이더가 없다면 보드 타일 크기 기준으로 0.5타일 정도를 기본 반경으로 사용
            diskRadiusWorld = director.board ? director.board.tileSize * 0.5f : 0.5f;
        }

        IsRunning = true;

        runCo   = StartCoroutine(RunDuration(data.duration));
        cleanCo = StartCoroutine(CleanLoop());
    }

    IEnumerator RunDuration(float sec)
    {
        yield return new WaitForSeconds(sec);
        StopNow();
    }

    IEnumerator CleanLoop()
    {
        var wait = new WaitForSeconds(sampleInterval);
        while (IsRunning)
        {
            if (director && player)
            {
                float addWorld = director.board ? director.board.tileSize * extraRadiusTiles : extraRadiusTiles;
                float r = diskRadiusWorld + addWorld;
                director.ClearCircleWorld(player.position, r);
            }
            yield return wait;
        }
    }

    public override void StopNow()
    {
        if (!IsRunning) return;
        IsRunning = false;

        if (runCo != null)   StopCoroutine(runCo);
        if (cleanCo != null) StopCoroutine(cleanCo);

    }

    void OnDisable()
    {
        if (IsRunning) StopNow();
    }
}
