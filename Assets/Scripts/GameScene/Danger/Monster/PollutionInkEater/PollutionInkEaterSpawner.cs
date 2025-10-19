using UnityEngine;

public class PollutionInkEaterSpawner : MonoBehaviour
{
    public PollutionInkEater inkEaterPrefab;
    public Transform targetPlayer;
    public BoardMaskRenderer maskRenderer;
    public BoardGrid board;

    [Header("Spawn Area")]
    public float ringInner = 6f;
    public float ringOuter = 12f;

    void Awake()
    {
        if (!maskRenderer) maskRenderer = FindAnyObjectByType<BoardMaskRenderer>();
        if (!board) board = FindAnyObjectByType<BoardGrid>();
        if (!targetPlayer) targetPlayer = FindAnyObjectByType<DiskLauncher>()?.transform;
    }

    public bool SpawnOne() // 중앙 매니저가 호출
    {
        if (!inkEaterPrefab) return false;
        Vector3 p = PickSpawnPos();
        var inst = Instantiate(inkEaterPrefab, p, Quaternion.identity);
        inst.maskRenderer = maskRenderer;
        inst.board = board;
        inst.targetPlayer = targetPlayer;
        return inst != null;
    }

    Vector3 PickSpawnPos()
    {
        Vector3 c = (board ? (board.origin + new Vector3(board.width * board.tileSize * 0.5f, 0, board.height * board.tileSize * 0.5f)) : Vector3.zero);

        float r = Random.Range(ringInner, ringOuter);
        float a = Random.Range(0f, Mathf.PI * 2f);

        Vector3 p = c + new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r);
        p.y = 0;
        return p;//현재 조건없음 
    }

}
