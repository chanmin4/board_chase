using UnityEngine;

[DisallowMultipleComponent]
public class InkBulletSpawner : MonoBehaviour
{
    [Header("Spawn Point")]
    public Transform muzzle;                 // 비우면 this.transform 사용
    public float spawnForward = 0.6f;
    public float spawnUp = 0.25f;

    [Header("Parent (recommended)")]
    public Transform projectilesRoot;        // 비우면 자동 생성(ProjectilesRoot)
    public bool parentUnderRoot = true;      // true 권장(무기 자식으로 두면 같이 끌려감)

    [Header("Prefab (optional)")]
    public GameObject bulletPrefab;          // 비우면 Default Cylinder 생성

    [Header("Default Cylinder Visual")]
    public float defaultBulletLength = 0.45f;
    public float defaultBulletRadius = 0.08f;
    public Color defaultColor = new Color(0.1f, 0.9f, 1f, 1f);

    [Header("Bullet Tuning Defaults")]
    public float defaultCastRadius = 0.12f;
    public float defaultMaxLife = 2.0f;

    [Header("Debug")]
    public bool debugDraw = false;

    static Transform _globalRoot;

    Transform GetRoot()
    {
        if (!parentUnderRoot) return null;

        if (projectilesRoot) return projectilesRoot;
        if (_globalRoot) return _globalRoot;

        var go = new GameObject("ProjectilesRoot");
        _globalRoot = go.transform;
        return _globalRoot;
    }

    Transform GetMuzzle() => muzzle ? muzzle : transform;

    /// <summary>
    /// 공용 발사 함수 (무기마다 여기만 호출)
    /// </summary>
    public InkBullet SpawnInkBullet(
        PlayerDisk owner,
        Vector3 aimPoint,
        float speed,
        float directDamage,
        float paintRadiusWorld,
        bool clearMask,
        LayerMask damageMask,
        LayerMask blockMask,
        float? castRadius = null,
        float? maxLife = null
    )
    {
        Transform m = GetMuzzle();

        // aim 방향 (XZ 기준)
        Vector3 dir = aimPoint - m.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 1e-6f) dir = m.forward;
        dir.Normalize();

        // 스폰 위치: muzzle 기준
        Vector3 spawnPos = m.position + dir * spawnForward + Vector3.up * spawnUp;

        if (debugDraw)
        {
            Debug.DrawRay(spawnPos, dir * 2f, Color.cyan, 0.2f);
            Debug.Log($"[InkBulletSpawner] spawnPos={spawnPos}, muzzle={m.name}, owner={(owner? owner.name:"null")}");
        }

        GameObject go = CreateBulletObject();
        go.transform.position = spawnPos;

        // Cylinder/Prefab 상관없이 “길이축을 dir로” 정렬
        go.transform.rotation = Quaternion.FromToRotation(Vector3.up, dir);

        // 부모 정리(권장: Root 아래)
        Transform root = GetRoot();
        if (root) go.transform.SetParent(root, true);
        else go.transform.SetParent(null, true);    // root가 없으면 월드 루트
        // 레이어는 owner(플레이어)와 동일하게 맞추면 카메라 컬링 문제 줄어듦
        if (owner) SetLayerRecursively(go, owner.gameObject.layer);

        // InkBullet 컴포넌트 확보
        InkBullet bullet = go.GetComponent<InkBullet>();
        if (!bullet) bullet = go.AddComponent<InkBullet>();

        // Init + 추가 튜닝값 적용
        bullet.Init(owner, aimPoint, speed, directDamage, paintRadiusWorld, clearMask, damageMask, blockMask);
        bullet.castRadius = castRadius ?? defaultCastRadius;
        bullet.maxLife = maxLife ?? defaultMaxLife;

        return bullet;
    }

    GameObject CreateBulletObject()
    {
        if (bulletPrefab != null)
        {
            // Prefab 기반
            return Instantiate(bulletPrefab);
        }

        // Default: Cylinder
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = "InkBullet_Cylinder(Default)";
        var col = go.GetComponent<Collider>();
        if (col) Destroy(col); // 판정은 SphereCast로 하니까 기본 콜라이더 제거(원하면 남겨도 됨)

        // 비주얼 스케일(원통은 Y가 길이)
        go.transform.localScale = new Vector3(defaultBulletRadius * 2f, defaultBulletLength * 0.5f, defaultBulletRadius * 2f);

        // 머티리얼
        var r = go.GetComponent<Renderer>();
        if (r != null)
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Unlit");
            if (sh == null) sh = Shader.Find("Unlit/Color");
            if (sh == null) sh = Shader.Find("Standard");
            var mat = new Material(sh);
            mat.color = defaultColor;
            r.sharedMaterial = mat;
        }

        return go;
    }

    static void SetLayerRecursively(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform t in go.transform)
            SetLayerRecursively(t.gameObject, layer);
    }
}
