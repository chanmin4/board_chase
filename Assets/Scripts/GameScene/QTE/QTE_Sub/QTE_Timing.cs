using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QTE_Timing : QTEBase
{
    [Header("Input")]
    [SerializeField] private KeyCode _confirmKey = KeyCode.Space;

    [Header("UI")]
    [SerializeField] private RectTransform _barContainer;
    [SerializeField] private RectTransform _marker;
    [SerializeField] private Image _successZone;
    [SerializeField] private TMP_Text _progressText;

    [Header("Motion")]
    [SerializeField, Min(0.01f)] private float _speed = 1f;
    [SerializeField] private bool _randomStartDirection = true;

    [Tooltip("Keeps the marker fully inside the bar instead of allowing half of it to overflow.")]
    [SerializeField] private bool _keepMarkerInsideBar = true;

    [Header("Success Zone Random")]
    [SerializeField] private bool _randomizeSuccessZoneOnBegin = true;
    [SerializeField] private bool _randomizeSuccessZoneAfterHit = true;
    [SerializeField, Min(0f)] private float _successZoneEdgePadding = 0f;
    [Tooltip("Minimum local X distance the success zone should move from its previous position. 0 disables this rule.")]
    [SerializeField, Min(0f)] private float _minSuccessZoneMoveDistance = 0f;

    [SerializeField, Min(1)] private int _randomizeRetryCount = 12;
    private readonly Vector3[] _worldCorners = new Vector3[4];

    private MutarusQTEStation _station;
    private float _progress01;
    private int _direction;

    public override void Begin(Action<QTEResult> onComplete)
    {
        BeginForStation(null, onComplete);
    }

    public void BeginForStation(MutarusQTEStation station, Action<QTEResult> onComplete)
    {
        _station = station;

        BeginInternal(onComplete);

        _progress01 = 0f;
        _direction = _randomStartDirection && UnityEngine.Random.value > 0.5f ? -1 : 1;

        if (_direction < 0)
            _progress01 = 1f;

        if (_randomizeSuccessZoneOnBegin)
            RandomizeSuccessZonePosition();

        RefreshMarkerPosition();
        RefreshProgressText();
    }

    public override void Cancel()
    {
        _station = null;
        base.Cancel();
    }

    private void Update()
    {
        if (!IsRunning)
            return;

        TickMarker();

        if (ShouldIgnoreInputThisFrame())
            return;

        if (IsKeyboardCancelPressed())
        {
            Cancel();
            return;
        }

        if (Input.GetKeyDown(_confirmKey))
            EvaluateAndFinish();
    }

    private void TickMarker()
    {
        _progress01 += _direction * _speed * Time.deltaTime;

        if (_progress01 >= 1f)
        {
            _progress01 = 1f;
            _direction = -1;
        }
        else if (_progress01 <= 0f)
        {
            _progress01 = 0f;
            _direction = 1;
        }

        RefreshMarkerPosition();
    }

    private void RefreshMarkerPosition()
    {
        if (_barContainer == null || _marker == null)
            return;

        RectTransform markerParent = _marker.parent as RectTransform;
        if (markerParent == null)
            return;

        GetLocalXRange(_barContainer, markerParent, out float left, out float right);

        if (_keepMarkerInsideBar)
        {
            float markerHalfWidth = GetRectHalfWidth(_marker, markerParent);
            left += markerHalfWidth;
            right -= markerHalfWidth;

            if (left > right)
            {
                float center = (left + right) * 0.5f;
                left = center;
                right = center;
            }
        }

        float x = Mathf.Lerp(left, right, _progress01);

        Vector3 localPosition = _marker.localPosition;
        localPosition.x = x;
        _marker.localPosition = localPosition;
    }

    private void EvaluateAndFinish()
    {
        if (_barContainer == null || _marker == null || _successZone == null)
        {
            Finish(QTEResult.Fail);
            return;
        }

        if (!IsMarkerInsideSuccessZone())
        {
            Finish(QTEResult.Fail);
            return;
        }

        bool stationCompleted = RegisterSuccess();

        if (stationCompleted)
        {
            _station = null;
            Finish(QTEResult.Success);
            return;
        }

        if (_randomizeSuccessZoneAfterHit)
            RandomizeSuccessZonePosition();

        RefreshProgressText();
    }

    private bool RegisterSuccess()
    {
        if (_station == null)
            return true;

        return _station.RegisterTimingSuccess();
    }

    private bool IsMarkerInsideSuccessZone()
    {
        RectTransform markerParent = _marker.parent as RectTransform;
        if (markerParent == null)
            return false;

        RectTransform zoneRect = _successZone.rectTransform;

        GetLocalXRange(zoneRect, markerParent, out float zoneLeft, out float zoneRight);

        float markerX = _marker.localPosition.x;
        return markerX >= zoneLeft && markerX <= zoneRight;
    }

    private void RandomizeSuccessZonePosition()
    {
        if (_barContainer == null || _successZone == null)
            return;

        RectTransform zoneRect = _successZone.rectTransform;
        RectTransform zoneParent = zoneRect.parent as RectTransform;

        if (zoneParent == null)
            return;

        GetLocalXRange(_barContainer, zoneParent, out float left, out float right);

        float halfWidth = GetRectHalfWidth(zoneRect, zoneParent);
        left += halfWidth + _successZoneEdgePadding;
        right -= halfWidth + _successZoneEdgePadding;

        if (left > right)
        {
            float center = (left + right) * 0.5f;
            left = center;
            right = center;
        }

        float previousX = zoneRect.localPosition.x;
        float nextX = previousX;

        int retryCount = Mathf.Max(1, _randomizeRetryCount);
        float minMoveDistance = Mathf.Max(0f, _minSuccessZoneMoveDistance);

        for (int i = 0; i < retryCount; i++)
        {
            float candidate = UnityEngine.Random.Range(left, right);

            if (minMoveDistance <= 0f || Mathf.Abs(candidate - previousX) >= minMoveDistance)
            {
                nextX = candidate;
                break;
            }

            nextX = candidate;
        }

        if (minMoveDistance > 0f && Mathf.Abs(nextX - previousX) < minMoveDistance)
        {
            float leftCandidate = previousX - minMoveDistance;
            float rightCandidate = previousX + minMoveDistance;

            bool canMoveLeft = leftCandidate >= left;
            bool canMoveRight = rightCandidate <= right;

            if (canMoveLeft && canMoveRight)
                nextX = UnityEngine.Random.value < 0.5f ? leftCandidate : rightCandidate;
            else if (canMoveLeft)
                nextX = leftCandidate;
            else if (canMoveRight)
                nextX = rightCandidate;
            else
                nextX = Mathf.Clamp(nextX, left, right);
        }

        Vector3 localPosition = zoneRect.localPosition;
        localPosition.x = nextX;
        zoneRect.localPosition = localPosition;
    }

    private void RefreshProgressText()
    {
        if (_progressText == null)
            return;

        if (_station == null)
        {
            _progressText.gameObject.SetActive(false);
            return;
        }

        _progressText.gameObject.SetActive(true);
        _progressText.text = $"{_station.TimingSuccessCount}/{_station.TimingSuccessRequired}";
    }

    private void GetLocalXRange(RectTransform source, RectTransform targetParent, out float left, out float right)
    {
        source.GetWorldCorners(_worldCorners);

        float x0 = targetParent.InverseTransformPoint(_worldCorners[0]).x;
        float x2 = targetParent.InverseTransformPoint(_worldCorners[2]).x;

        left = Mathf.Min(x0, x2);
        right = Mathf.Max(x0, x2);
    }

    private float GetRectHalfWidth(RectTransform source, RectTransform targetParent)
    {
        source.GetWorldCorners(_worldCorners);

        float x0 = targetParent.InverseTransformPoint(_worldCorners[0]).x;
        float x2 = targetParent.InverseTransformPoint(_worldCorners[2]).x;

        return Mathf.Abs(x2 - x0) * 0.5f;
    }
}
