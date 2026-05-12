using System;
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

    [Header("Motion")]
    [SerializeField, Min(0.01f)] private float _speed = 0.5f;
    [SerializeField] private bool _randomStartDirection = true;

    [Tooltip("Keeps the marker fully inside the bar instead of allowing half of it to overflow.")]
    [SerializeField] private bool _keepMarkerInsideBar = true;

    private readonly Vector3[] _worldCorners = new Vector3[4];

    private float _progress01;
    private int _direction;

    public override void Begin(Action<QTEResult> onComplete)
    {
        BeginInternal(onComplete);

        _progress01 = 0f;
        _direction = _randomStartDirection && UnityEngine.Random.value > 0.5f ? -1 : 1;

        if (_direction < 0)
            _progress01 = 1f;

        RefreshMarkerPosition();
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
            float markerHalfWidth = GetMarkerHalfWidth(markerParent);
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

        if (IsMarkerInsideSuccessZone())
        {
            Finish(QTEResult.Success);
            return;
        }

        Finish(QTEResult.Fail);
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

    private void GetLocalXRange(RectTransform source, RectTransform targetParent, out float left, out float right)
    {
        source.GetWorldCorners(_worldCorners);

        float x0 = targetParent.InverseTransformPoint(_worldCorners[0]).x;
        float x2 = targetParent.InverseTransformPoint(_worldCorners[2]).x;

        left = Mathf.Min(x0, x2);
        right = Mathf.Max(x0, x2);
    }

    private float GetMarkerHalfWidth(RectTransform markerParent)
    {
        _marker.GetWorldCorners(_worldCorners);

        float x0 = markerParent.InverseTransformPoint(_worldCorners[0]).x;
        float x2 = markerParent.InverseTransformPoint(_worldCorners[2]).x;

        return Mathf.Abs(x2 - x0) * 0.5f;
    }
}
