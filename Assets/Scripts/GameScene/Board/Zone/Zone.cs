using UnityEngine;

[DisallowMultipleComponent]
public class Zone : MonoBehaviour
{
    [Header("Runtime")]
    public int id;
    public int profileIndex;
    public Vector3 centerWorld;
    public float radiusWorld;

    ZoneSpawnManager owner;

    public void Init(ZoneSpawnManager mgr, int _id, int _profile, Vector3 cW, float rW)
    {
        owner = mgr;
        id = _id; profileIndex = _profile; centerWorld = cW; radiusWorld = rW;
        transform.position = new Vector3(cW.x, transform.position.y, cW.z);
    }

    // 필요시 트리거 기반 상호작용으로 확장할 수 있는 훅
    void OnTriggerEnter(Collider other)
    {
        // ex) 특정 태그/레이어만 반응 등
        // owner?.OnZoneTriggerEnter(this, other);
    }
}
