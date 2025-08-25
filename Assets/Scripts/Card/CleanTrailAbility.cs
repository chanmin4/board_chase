using UnityEngine;
using System.Collections;

public class CleanTrailAbility : CardAbility
{
    SurvivalDirector director;
    Transform player;
    float extraRadiusTiles;
    Collider diskCol;
    float diskRadiusWorld;
    Coroutine co;

    public override void Activate(Transform playerTf, SurvivalDirector dir, CardData data)
    {
        StopNow();

        player = playerTf;
        director = dir;
        extraRadiusTiles = Mathf.Max(0f, data.radiusTiles);

        if (!player || !director)
        { Debug.LogWarning("[CleanTrail] refs missing"); return; }

        diskCol = player.GetComponent<Collider>();
        if (diskCol)
        {
            var b = diskCol.bounds;
            diskRadiusWorld = Mathf.Max(b.extents.x, b.extents.z);
        }
        else
        {
            diskRadiusWorld = director.board ? director.board.tileSize * 0.5f : 0.5f;
        }

        IsRunning = true;
        co = StartCoroutine(CleanLoop(data.duration));   // ✅ player.StartCoroutine → this.StartCoroutine
    }

    IEnumerator CleanLoop(float duration)
    {
        float t = 0f;
        var wait = new WaitForSeconds(0.02f);
        while (t < duration && IsRunning)
        {
            if (director && player)
            {
                float addWorld = director.board ? director.board.tileSize * extraRadiusTiles : extraRadiusTiles;
                float r = diskRadiusWorld + addWorld;

                director.ClearCircleWorld(player.position, r);
            }
            t += 0.02f;
            yield return wait;
        }
        StopNow();
    }

    public override void StopNow()
    {
        if (!IsRunning) return;
        IsRunning = false;
        if (co != null) StopCoroutine(co);  // ✅ 역시 this.StopCoroutine
        co = null;
    }

    void OnDisable() { StopNow(); }
}
