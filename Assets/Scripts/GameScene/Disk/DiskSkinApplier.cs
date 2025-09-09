// DiskSkinApplier.cs  (Cylinder에 붙이기)
using UnityEngine;
public class DiskSkinApplier : MonoBehaviour
{
    public Renderer meshRenderer;     // Cylinder의 MeshRenderer 연결

    Material _orig;
    void Awake()
    {
        if (!meshRenderer) meshRenderer = GetComponentInChildren<Renderer>(true);
        if (meshRenderer) _orig = meshRenderer.sharedMaterial;
    }
    void OnEnable() { StartCoroutine(ApplyNextFrame()); }

    System.Collections.IEnumerator ApplyNextFrame()
    {
        yield return null; // 한 프레임 대기
        RewardDB.EnsureLoaded();
        Apply();
    }

    public void Apply()
    {
        var pm = ProgressManager.Instance;
        if (pm == null || meshRenderer == null) return;

        var id = pm.Data.equippedSkinId;
        var so = RewardDB.Get(id) as SkinRewardSO;
        Debug.Log($"[Skin] TryApply id={id}, so={(so!=null)}, mr={(meshRenderer!=null)}");
        if (so == null)
        {
            // SO 못 찾으면 기본값 복구
            if (_orig) meshRenderer.sharedMaterial = _orig;
            Debug.LogWarning($"[Skin] SO not found for id={id}");
            return;
        }

        if (so.diskMaterialPreset)
        {
            // 인스턴스 머티리얼 사용: 런타임에만 적용
            meshRenderer.material = so.diskMaterialPreset;
            // 공유 머티리얼로 바꾸고 싶으면: meshRenderer.sharedMaterial = so.diskMaterialPreset;
        }
    }
}
