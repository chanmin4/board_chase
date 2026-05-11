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
    [SerializeField] private Image _greatZone;

    [Header("Motion")]
    [SerializeField] private Vector2 _speedRange = new Vector2(0.8f, 1.2f);
    [SerializeField] private bool _randomStartDirection = true;


    private float _progress01;
    private float _speed;
    private int _direction;

    public override void Begin(Action<QTEResult> onComplete)
    {
        BeginInternal(onComplete);

        _progress01 = 0f;
        _speed = UnityEngine.Random.Range(_speedRange.x, _speedRange.y);
        _direction = _randomStartDirection && UnityEngine.Random.value > 0.5f ? -1 : 1;

        if (_direction < 0)
            _progress01 = 1f;

        RefreshMarkerPosition();
    }

    private void Update()
    {
        if (!IsRunning)
            return;

        if (IsKeyboardCancelPressed())
        {
            Cancel();
            return;
        }

        TickMarker();

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

        Rect rect = _barContainer.rect;

        float left = -rect.width * _barContainer.pivot.x;
        float right = rect.width * (1f - _barContainer.pivot.x);
        float x = Mathf.Lerp(left, right, _progress01);

        Vector2 position = _marker.anchoredPosition;
        position.x = x;
        _marker.anchoredPosition = position;
    }

    private void EvaluateAndFinish()
    {
        if (_marker == null)
        {
            Finish(QTEResult.Fail);
            return;
        }

        float markerX = _marker.anchoredPosition.x;


        if (IsInsideZone(markerX, _successZone))
        {
            Finish(QTEResult.Success);
            return;
        }

        Finish(QTEResult.Fail);
    }

    private bool IsInsideZone(float markerX, Image zone)
    {
        if (zone == null)
            return false;

        RectTransform zoneRect = zone.rectTransform;

        float left = zoneRect.anchoredPosition.x - zoneRect.rect.width * zoneRect.pivot.x;
        float right = left + zoneRect.rect.width;

        return markerX >= left && markerX <= right;
    }
}
