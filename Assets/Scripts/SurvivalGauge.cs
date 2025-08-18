using UnityEngine;
using UnityEngine.UI;

public class SurvivalGauge : MonoBehaviour
{
    public float max = 100f;
    public float current = 100f;

    [Header("Rates (per sec)")]
    public float baseDrain = 6f;          // 항상 빠짐
    public float contaminatedExtra = 12f; // 오염 위에서 추가로 빠짐

    [Header("Colors")]
    public Image bar;
    public Color green = Color.green;
    public Color yellow = Color.yellow;
    public Color red = Color.red;
    public Color purple = new Color(0.6f, 0.2f, 0.8f); // 오염 우선

    bool onContaminated;

    void Update()
    {
        float drain = baseDrain + (onContaminated ? contaminatedExtra : 0f);
        Add(-drain * Time.deltaTime);
        UpdateColor();
    }

    public void Add(float delta)
    {
        current = Mathf.Clamp(current + delta, 0f, max);
    }

    public void SetContaminated(bool v) => onContaminated = v;

    void UpdateColor()
    {
        if (onContaminated) { bar.color = purple; return; }

        float r = current / max;
        if (r > 0.5f) bar.color = green;
        else if (r > 0.2f) bar.color = yellow;
        else bar.color = red;
    }
}
