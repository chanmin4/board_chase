using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Renderer))]
public class TileCell : MonoBehaviour
{
    public TileCenter center;
    public Renderer rend;

    void Reset()
    {
        if (!center) center = GetComponentInChildren<TileCenter>();
        if (!rend)   rend   = GetComponent<Renderer>();
    }

    public void SetColor(Color c, float a = 1f)
    {
        if (!rend) return;
        var m = rend.material;
        c.a = a;
        m.color = c; // URP/Lit에서 SurfaceType=Transparent 권장
    }

    public IEnumerator FadeAlpha(float to, float dur = .2f)
    {
        if (!rend) yield break;
        float t = 0f; var m = rend.material; float a0 = m.color.a;
        while (t < dur)
        {
            t += Time.deltaTime;
            var col = m.color; col.a = Mathf.Lerp(a0, to, t / dur);
            m.color = col;
            yield return null;
        }
    }
}
