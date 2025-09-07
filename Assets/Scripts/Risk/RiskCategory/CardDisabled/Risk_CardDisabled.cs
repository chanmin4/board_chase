using UnityEngine;

[DisallowMultipleComponent]
public class Risk_CardDisabled : MonoBehaviour
{
    public bool applyOnStart = true;
    public CardManager[] targets; // 비면 자동 수집
    public bool disable = true;

    bool[] orig;
    bool captured;

    void Awake()
    {
        if (targets == null || targets.Length == 0) {
#if UNITY_2023_1_OR_NEWER
            targets = Object.FindObjectsByType<CardManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
            targets = FindObjectsOfType<CardManager>(true);
#endif
        }
        if (targets != null && targets.Length > 0) {
            orig = new bool[targets.Length];
            for (int i = 0; i < targets.Length; i++)
                if (targets[i]) orig[i] = targets[i].riskDisableUse;
            captured = true;
        }
    }

    void Start(){ if (applyOnStart) Apply(); }
    void OnEnable(){ if (Application.isPlaying && applyOnStart) Apply(); }
    void OnDisable(){ if (Application.isPlaying) Revert(); }

    public void Apply()
    {
        if (!captured) return;
        for (int i = 0; i < targets.Length; i++)
            if (targets[i]) targets[i].riskDisableUse = disable;
    }

    public void Revert()
    {
        if (!captured) return;
        for (int i = 0; i < targets.Length; i++)
            if (targets[i]) targets[i].riskDisableUse = orig[i];
    }
}
