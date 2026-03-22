/*
using System.Collections;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private CinemachineVirtualCamera virtualCamera;
    [SerializeField] private Transform cameraTarget;

    [Header("Settings")]
    [SerializeField] private float slideDuration = 0.45f;
    [SerializeField] private AnimationCurve slideCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Coroutine _moveCoroutine;
    private bool _isMoving;

    public void MoveToSectorCamera(SectorRuntime sector)
    {
        if (sector == null || sector.cameraPoint == null)
            return;

        if (_moveCoroutine != null)
            StopCoroutine(_moveCoroutine);

        _moveCoroutine = StartCoroutine(SlideTo(sector.cameraPoint));
    }

    private IEnumerator SlideTo(SectorCameraPoint point)
    {
        _isMoving = true;

        Vector3 startPos = cameraTarget.position;
        Quaternion startRot = cameraTarget.rotation;

        Vector3 endPos = point.transform.position + point.positionOffset;
        Quaternion endRot = Quaternion.Euler(point.eulerAngles);

        float startSize = virtualCamera.m_Lens.OrthographicSize;
        float endSize = point.orthoSize;

        float t = 0f;

        while (t < slideDuration)
        {
            t += Time.deltaTime;
            float k = slideCurve.Evaluate(t / slideDuration);

            cameraTarget.position = Vector3.Lerp(startPos, endPos, k);
            cameraTarget.rotation = Quaternion.Slerp(startRot, endRot, k);
            virtualCamera.m_Lens.OrthographicSize = Mathf.Lerp(startSize, endSize, k);

            yield return null;
        }

        cameraTarget.position = endPos;
        cameraTarget.rotation = endRot;
        virtualCamera.m_Lens.OrthographicSize = endSize;

        _isMoving = false;
        _moveCoroutine = null;
    }
}
*/
/*
using UnityEngine;

public class SectorRuntime : MonoBehaviour
{
    public Vector2Int sectorCoord;
    public SectorCameraPoint cameraPoint;

    [Header("Neighbors")]
    public SectorRuntime up;
    public SectorRuntime down;
    public SectorRuntime left;
    public SectorRuntime right;
}

using UnityEngine;

public class SectorRuntime : MonoBehaviour
{
    public Vector2Int sectorCoord;
    public SectorCameraPoint cameraPoint;

    [Header("Neighbors")]
    public SectorRuntime up;
    public SectorRuntime down;
    public SectorRuntime left;
    public SectorRuntime right;
}

using UnityEngine;

public class SectorCameraPoint : MonoBehaviour
{
    [Header("Camera View")]
    public Vector3 positionOffset;
    public Vector3 eulerAngles = new Vector3(55f, 0f, 0f);
    public float orthoSize = 12f;
}
*/