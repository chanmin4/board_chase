using UnityEngine;

public class DiskSkinApplier : MonoBehaviour
{
    [Header("Renderer-based (기존)")]
    public Renderer meshRenderer; // Cylinder MeshRenderer (fallback용)

    [Header("Prefab-based (신규)")]
    public Transform visualAnchor; // 빈 자식(VisualAnchor) 할당
    public bool preferPrefab = true;
    public bool autoFit = true;
    public float targetRadius = 0.5f;
    public float thickness   = 0.1f;
    public bool rotateToXZ   = true;

    Material _orig;
    GameObject _currentSkin;

    void Awake()
    {
        if (!meshRenderer) meshRenderer = GetComponentInChildren<Renderer>(true);
        if (meshRenderer) _orig = meshRenderer.sharedMaterial;

        if (!visualAnchor) {
            var go = new GameObject("VisualAnchor");
            go.transform.SetParent(transform, false);
            visualAnchor = go.transform;
        }
    }

    void OnEnable() { StartCoroutine(ApplyNextFrame()); }
    System.Collections.IEnumerator ApplyNextFrame() { yield return null; RewardDB.EnsureLoaded(); Apply(); }

    public void Apply()
    {
        var pm = ProgressManager.Instance;
        if (pm == null) return;

        var id = pm.Data.equippedSkinId;
        var so = RewardDB.Get(id) as SkinRewardSO;
        Debug.Log($"[Skin] TryApply id={id}, so={(so!=null)}, mr={(meshRenderer!=null)}");

        if (so == null) { ClearPrefabSkin(); if (_orig && meshRenderer) meshRenderer.sharedMaterial = _orig; return; }

        // ✅ 프리팹이 있으면 우선 프리팹 장착
        if (preferPrefab && so.skinPrefab && visualAnchor) {
            ApplyPrefabSkin(so.skinPrefab);
            return;
        }

        // ✅ 없으면 기존 머티리얼 방식 유지
        ClearPrefabSkin();
        if (so.diskMaterialPreset && meshRenderer) meshRenderer.material = so.diskMaterialPreset;
        else if (meshRenderer && _orig) meshRenderer.sharedMaterial = _orig;
    }

    void ApplyPrefabSkin(GameObject prefab)
    {
        ClearPrefabSkin();

        // 원본 프리팹의 localPosition/Rotation/Scale 그대로 유지
        _currentSkin = Instantiate(prefab, visualAnchor, false); // false = prefab의 local TRS 유지

        // 필요시: 프리팹 콜라이더 끄기/그림자 조정만
        MakePureVisual(_currentSkin.transform);

        // 자동 맞춤이 필요할 때만 켜기
        if (autoFit) AutoFit(_currentSkin.transform, targetRadius, thickness, rotateToXZ);

        //if (meshRenderer) meshRenderer.enabled = false; // z-fighting 방지
    }

    void ClearPrefabSkin()
    {
        if (_currentSkin) {
            if (Application.isPlaying) Destroy(_currentSkin);
            else DestroyImmediate(_currentSkin);
            _currentSkin = null;
        }
        if (meshRenderer) meshRenderer.enabled = true;
    }
    
    void MakePureVisual(Transform root)
    {
        foreach (var c in root.GetComponentsInChildren<Collider>(true)) c.enabled = false;
        foreach (var rb in root.GetComponentsInChildren<Rigidbody>(true)) { rb.detectCollisions=false; rb.isKinematic=true; rb.useGravity=false; }
        foreach (var r in root.GetComponentsInChildren<Renderer>(true)) { r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false; }
    }

    void AutoFit(Transform t, float targetR, float yHeight, bool rotateXZ)
    {
        var mf = t.GetComponentInChildren<MeshFilter>();
        if (!mf || !mf.sharedMesh) return;
        var b = mf.sharedMesh.bounds;
        float dia = Mathf.Max(b.size.x, b.size.z);
        if (dia < 1e-4f) return;
        float scaleXZ = (targetR * 2f) / dia;
        float scaleY  = yHeight / Mathf.Max(b.size.y, 1e-4f);
        t.localScale  = new Vector3(scaleXZ, scaleY, scaleXZ);
        if (rotateXZ) t.localRotation = Quaternion.Euler(90f, 0f, 0f);
    }
}
