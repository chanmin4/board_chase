using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class QTE_KeyPrompt : QTEBase
{
    [Header("Rules")]
    [SerializeField] private KeyCode[] _keyPool =
    {
        KeyCode.Q,
        KeyCode.W,
        KeyCode.E,
        KeyCode.R
    };

    [SerializeField, Min(1)] private int _sequenceLength = 5;

    [Tooltip("If true, pressing the wrong QTE key immediately fails this QTE.")]
    [SerializeField] private bool _wrongInputFails = true;

    [Header("UI")]
    [SerializeField] private GameObject _keyTextItemPrefab;
    [SerializeField] private Transform _keyContainer;

    private readonly List<KeyCode> _sequence = new();
    private readonly List<GameObject> _items = new();

    private int _currentIndex;

    public override void Begin(Action<QTEResult> onComplete)
    {
        BeginInternal(onComplete);

        _currentIndex = 0;
        ClearItems();
        BuildSequence();
        BuildUI();
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

        for (int i = 0; i < _keyPool.Length; i++)
        {
            KeyCode pressedKey = _keyPool[i];

            if (!Input.GetKeyDown(pressedKey))
                continue;

            HandleKeyPressed(pressedKey);
            return;
        }
    }

    private void HandleKeyPressed(KeyCode pressedKey)
    {
        if (_currentIndex < 0 || _currentIndex >= _sequence.Count)
            return;

        KeyCode expectedKey = _sequence[_currentIndex];

        if (pressedKey == expectedKey)
        {
            if (_currentIndex < _items.Count && _items[_currentIndex] != null)
                _items[_currentIndex].SetActive(false);

            _currentIndex++;

            if (_currentIndex >= _sequence.Count)
                Finish(QTEResult.Success);

            return;
        }

        if (_wrongInputFails)
            Finish(QTEResult.Fail);
    }

    private void BuildSequence()
    {
        _sequence.Clear();

        for (int i = 0; i < _sequenceLength; i++)
        {
            int index = UnityEngine.Random.Range(0, _keyPool.Length);
            _sequence.Add(_keyPool[index]);
        }
    }

    private void BuildUI()
    {
        if (_keyTextItemPrefab == null || _keyContainer == null)
            return;

        for (int i = 0; i < _sequence.Count; i++)
        {
            GameObject item = Instantiate(_keyTextItemPrefab, _keyContainer);

            if (item.TryGetComponent(out TextMeshProUGUI text))
                text.text = _sequence[i].ToString();

            _items.Add(item);
        }
    }

    private void ClearItems()
    {
        for (int i = 0; i < _items.Count; i++)
        {
            if (_items[i] != null)
                Destroy(_items[i]);
        }

        _items.Clear();
    }

    private void OnDisable()
    {
        ClearItems();
    }
}
