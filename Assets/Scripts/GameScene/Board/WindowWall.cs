using UnityEngine;
using System.Collections;

public class WindowWall : MonoBehaviour
{
    [Header("Walls (자식 4개)")]
    public Transform wallL, wallR, wallF, wallB;

    [Header("Shape")]
    public float height = 1.0f;      // 벽 높이
    public float thickness = 0.2f;   // 벽 두께

    [Header("Visual")]
    public float fadeOut = 0.12f;
    public float fadeIn  = 0.18f;
    public float targetAlpha = 0.35f;

    Renderer[] rends;

    void Awake() { rends = GetComponentsInChildren<Renderer>(true); }

    // [fromX, toX] x범위, z는 [-halfZ, +halfZ]
    public void SetWindowSmooth(float fromX, float toX, float halfZ = 1.5f)
    {
        StopAllCoroutines();
        StartCoroutine(CoSet(fromX, toX, halfZ));
    }

    IEnumerator CoSet(float fromX, float toX, float halfZ)
    {
        yield return StartCoroutine(FadeAll(0f, fadeOut));
        PlaceWalls(fromX, toX, halfZ);
        yield return StartCoroutine(FadeAll(targetAlpha, fadeIn));
    }

    void PlaceWalls(float fromX, float toX, float halfZ)
    {
        float midX = (fromX + toX) * 0.5f;
        float sizeX = Mathf.Abs(toX - fromX);
        float sizeZ = halfZ * 2f;

        wallL.position   = new Vector3(fromX, height * .5f, 0f);
        wallL.localScale = new Vector3(thickness, height, sizeZ);

        wallR.position   = new Vector3(toX, height * .5f, 0f);
        wallR.localScale = new Vector3(thickness, height, sizeZ);

        wallF.position   = new Vector3(midX, height * .5f, +halfZ);
        wallF.localScale = new Vector3(sizeX, height, thickness);

        wallB.position   = new Vector3(midX, height * .5f, -halfZ);
        wallB.localScale = new Vector3(sizeX, height, thickness);
    }

    IEnumerator FadeAll(float to, float dur)
    {
        if (rends == null || rends.Length == 0 || dur <= 0f)
        {
            // 즉시 적용
            foreach (var r in rends)
            {
                var c = r.material.color; c.a = to; r.material.color = c;
            }
            yield break;
        }

        float t = 0f;
        float[] a0 = new float[rends.Length];
        for (int i = 0; i < rends.Length; i++) a0[i] = rends[i].material.color.a;

        while (t < dur)
        {
            t += Time.deltaTime;
            float k = t / dur;
            for (int i = 0; i < rends.Length; i++)
            {
                var m = rends[i].material;
                var c = m.color; c.a = Mathf.Lerp(a0[i], to, k); m.color = c;
            }
            yield return null;
        }
    }
}
