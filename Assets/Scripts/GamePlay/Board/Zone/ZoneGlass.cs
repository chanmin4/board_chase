using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class ZoneGlass : MonoBehaviour
{
    [Header("Target")]
    public Renderer targetRenderer;

    [Header("Shader Property Name")]
    public string texturePropertyName = "_BaseMap"; // URP/Lit 기본

    [Header("Textures")]
    public Texture2D baseTexture;

    [System.Serializable]
    public struct Band
    {
        [Range(0f, 1f)] public float minPercent; // 포함
        [Range(0f, 1f)] public float maxPercent; // 미포함
        public Texture2D texture;
    }

    [Header("Progress Bands (0은 baseTexture)")]
    public List<Band> bands = new List<Band>();

    [Header("Remove Visual On Consume/Expire")]
    public bool disableWholeObjectOnRemove = false;

    // runtime
    private MaterialPropertyBlock _mpb;
    private int _propId = -1;

    void Reset()
    {
        targetRenderer = GetComponentInChildren<Renderer>();
    }

    void Awake()
    {
        if (!targetRenderer) targetRenderer = GetComponentInChildren<Renderer>();
        _mpb = new MaterialPropertyBlock();
        _propId = Shader.PropertyToID(string.IsNullOrEmpty(texturePropertyName) ? "_BaseMap" : texturePropertyName);
        ApplyTexture(baseTexture); // 초기 상태
    }

    // -------------------- 외부에서 이 3개만 호출 --------------------

    // 히트 진행 업데이트 (curHits/reqHits는 외부에서 계산)
    public void OnZoneHitProgress(int curHits, int reqHits)
    {
        if (reqHits <= 0) return;          // 가드
        if (curHits <= 0) { ApplyTexture(baseTexture); return; }

        float pct = Mathf.Clamp01((float)curHits / reqHits);
        if (pct >= 1f) pct = 0.999f;       // 100% 직전까지만 시각화(소비는 별도)

        Texture2D chosen = null;
        for (int i = 0; i < bands.Count; i++)
        {
            var b = bands[i];
            float min = Mathf.Clamp01(b.minPercent);
            float max = Mathf.Clamp01(b.maxPercent);
            if (pct >= min && pct < max)
            {
                chosen = b.texture;
                break;
            }
        }

        if (!chosen && bands.Count > 0)
        {
            float bestMin = -1f; int bestIdx = -1;
            for (int i = 0; i < bands.Count; i++)
                if (bands[i].minPercent <= pct && bands[i].minPercent > bestMin)
                { bestMin = bands[i].minPercent; bestIdx = i; }

            if (bestIdx >= 0) chosen = bands[bestIdx].texture;
        }

        ApplyTexture(chosen ? chosen : baseTexture);
    }

    // 요구치 달성(소비) 시
    public void OnZoneConsumed()
    {
        RemoveVisual();
    }

    // 만료 시
    public void OnZoneExpired()
    {
        RemoveVisual();
    }

    // -------------------- 내부 유틸 --------------------
    private void ApplyTexture(Texture2D tex)
    {
        if (!targetRenderer) return;

        targetRenderer.GetPropertyBlock(_mpb);

        if (_propId == -1) _propId = Shader.PropertyToID("_BaseMap");
        _mpb.SetTexture(_propId, tex);
        targetRenderer.SetPropertyBlock(_mpb);

        // 셰이더가 _BaseMap이 아니라 _MainTex만 쓰는 경우 대비
        if (!targetRenderer.sharedMaterial || !targetRenderer.sharedMaterial.HasProperty(_propId))
        {
            int mainTexId = Shader.PropertyToID("_MainTex");
            targetRenderer.GetPropertyBlock(_mpb);
            _mpb.SetTexture(mainTexId, tex);
            targetRenderer.SetPropertyBlock(_mpb);
        }
    }

    private void RemoveVisual()
    {
        if (disableWholeObjectOnRemove) gameObject.SetActive(false);
        else if (targetRenderer)        targetRenderer.enabled = false;
    }
}
