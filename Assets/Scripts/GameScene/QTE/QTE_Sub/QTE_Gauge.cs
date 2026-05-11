using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QTE_Gauge : QTEBase
{
    [Header("Keys")]
    [SerializeField] private KeyCode[] _candidates =
    {
        KeyCode.Q,
        KeyCode.W,
        KeyCode.E,
        KeyCode.R
    };

    [Header("Gauge")]
    [SerializeField, Range(0f, 100f)] private float _startGauge = 50f;
    [SerializeField, Range(0f, 100f)] private float _successGauge = 100f;
    [SerializeField, Range(0f, 100f)] private float _failGauge = 0f;

    [SerializeField, Min(0f)] private float _incrementPerCorrectPress = 10f;
    [SerializeField, Min(0f)] private float _penaltyOnWrongPress = 12f;
    [SerializeField, Min(0f)] private float _decayPerSecond = 4f;

    [Header("Key Change")]
    [SerializeField, Min(1)] private int _minCorrectPressesToChangeKey = 4;
    [SerializeField, Min(1)] private int _maxCorrectPressesToChangeKey = 7;

    [Header("UI")]
    [SerializeField] private Slider _gaugeSlider;
    [SerializeField] private TextMeshProUGUI _currentKeyText;

    private KeyCode _currentKey;
    private float _gauge;
    private int _correctPressCount;
    private int _nextKeyChangeThreshold;

    public override void Begin(Action<QTEResult> onComplete)
    {
        BeginInternal(onComplete);

        _gauge = _startGauge;
        _correctPressCount = 0;

        PickNewKey();
        SetNextKeyChangeThreshold();
        RefreshUI();
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

        TickDecay();
        HandleInput();
        CheckResult();
        RefreshUI();
    }

    private void TickDecay()
    {
        if (_decayPerSecond <= 0f)
            return;

        _gauge = Mathf.Max(_failGauge, _gauge - _decayPerSecond * Time.deltaTime);
    }

    private void HandleInput()
    {
        for (int i = 0; i < _candidates.Length; i++)
        {
            KeyCode key = _candidates[i];

            if (!Input.GetKeyDown(key))
                continue;

            if (key == _currentKey)
                HandleCorrectPress();
            else
                HandleWrongPress();

            return;
        }
    }

    private void HandleCorrectPress()
    {
        _correctPressCount++;
        _gauge = Mathf.Min(_successGauge, _gauge + _incrementPerCorrectPress);

        if (_correctPressCount < _nextKeyChangeThreshold)
            return;

        _correctPressCount = 0;
        PickNewKey();
        SetNextKeyChangeThreshold();
    }

    private void HandleWrongPress()
    {
        _gauge = Mathf.Max(_failGauge, _gauge - _penaltyOnWrongPress);
    }

    private void CheckResult()
    {
        if (_gauge >= _successGauge)
        {
            Finish(QTEResult.Success);
            return;
        }

        if (_gauge <= _failGauge)
            Finish(QTEResult.Fail);
    }

    private void PickNewKey()
    {
        if (_candidates == null || _candidates.Length == 0)
        {
            _currentKey = KeyCode.None;
            return;
        }

        if (_candidates.Length == 1)
        {
            _currentKey = _candidates[0];
            return;
        }

        KeyCode nextKey = _currentKey;
        int guard = 0;

        while (nextKey == _currentKey && guard < 20)
        {
            nextKey = _candidates[UnityEngine.Random.Range(0, _candidates.Length)];
            guard++;
        }

        _currentKey = nextKey;
    }

    private void SetNextKeyChangeThreshold()
    {
        int min = Mathf.Max(1, _minCorrectPressesToChangeKey);
        int max = Mathf.Max(min, _maxCorrectPressesToChangeKey);

        _nextKeyChangeThreshold = UnityEngine.Random.Range(min, max + 1);
    }

    private void RefreshUI()
    {
        if (_gaugeSlider != null)
            _gaugeSlider.value = Mathf.InverseLerp(_failGauge, _successGauge, _gauge);

        if (_currentKeyText != null)
            _currentKeyText.text = _currentKey.ToString();
    }
}
