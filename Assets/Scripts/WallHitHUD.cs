using UnityEngine;
using TMPro;

public class WallHitHUD : MonoBehaviour
{
    public SurvivalDirector director;
    public TMP_Text label;  // Canvas 안 TextMeshProUGUI

    void Awake()
    {
        if (!director) director = FindAnyObjectByType<SurvivalDirector>();
        if (director)
        {
            director.OnWallHitsChanged += UpdateView;
            UpdateView(director.CurrentWallHits); // 초기 표시
        }
    }
    void OnDestroy()
    {
        if (director) director.OnWallHitsChanged -= UpdateView;
    }

    void UpdateView(int count)
    {
        if (!label) return;
        var (s, m, l) = director.GetWallRequirements();
        label.text = $"WALL HITS: {count}   (Req  S:{s}  M:{m}  L:{l})";
    }
}
