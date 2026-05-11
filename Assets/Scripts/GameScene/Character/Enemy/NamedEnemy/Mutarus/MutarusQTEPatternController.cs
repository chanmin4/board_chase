using System;
using UnityEngine;

public class MutarusQTEPatternController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private InputReader _inputReader;
    [SerializeField] private QTEBase[] _qtePanels;
    [SerializeField] private MutarusQTEStation[] _stations;
    [Header("Broadcasting On")]
    [SerializeField] private MutarusQTEPatternControllerEventChannelSO _readyChannel;

    [Header("Runtime")]
    [SerializeField, ReadOnly] private bool _isRunning;
    [SerializeField, ReadOnly] private float _timeRemaining;

    private Action<NamedPatternResult> _onComplete;
    private QTEBase _activeQTE;
    private MutarusQTEStation _activeStation;
    private bool _finishing;

    public bool IsRunning => _isRunning;

    private void Awake()
    {
        HideAllQTEPanels();
        SetStationsActive(false);
    }

    private void OnEnable()
    {
        if (_inputReader != null)
            _inputReader.InteractEvent += HandleInteract;

        if (_readyChannel != null)
            _readyChannel.RaiseEvent(this);
    }

    private void OnDisable()
    {
        if (_inputReader != null)
            _inputReader.InteractEvent -= HandleInteract;

        if (_readyChannel != null)
            _readyChannel.Clear(this);
    }

    private void Update()
    {
        if (!_isRunning)
            return;

        _timeRemaining -= Time.deltaTime;

        if (_timeRemaining <= 0f)
            Finish(NamedPatternResult.PlayerFailed);
    }

    public void BeginPattern(float duration, Action<NamedPatternResult> onComplete)
    {
        CancelPatternWithoutResult();

        _isRunning = true;
        _finishing = false;
        _timeRemaining = Mathf.Max(0.01f, duration);
        _onComplete = onComplete;

        HideAllQTEPanels();
        SetStationsActive(true);
    }

    public void CancelPatternWithoutResult()
    {
        if (_activeQTE != null && _activeQTE.Running)
            _activeQTE.Cancel();

        _activeQTE = null;
        _activeStation = null;
        _isRunning = false;
        _finishing = false;
        _onComplete = null;

        HideAllQTEPanels();
        SetStationsActive(false);
    }

    private void HandleInteract()
    {
        if (!_isRunning || _activeQTE != null)
            return;

        MutarusQTEStation station = FindInteractableStation();
        if (station == null)
            return;

        QTEBase qte = PickRandomQTE();
        if (qte == null)
            return;

        _activeStation = station;
        _activeQTE = qte;

        _activeQTE.Begin(HandleQTEResult);
    }

    private void HandleQTEResult(QTEResult result)
    {
        if (_finishing)
            return;

        QTEBase completedQTE = _activeQTE;
        MutarusQTEStation completedStation = _activeStation;

        _activeQTE = null;
        _activeStation = null;

        if (completedQTE != null)
            completedQTE.gameObject.SetActive(false);

        if (result == QTEResult.Cancelled)
            return;

        if (result == QTEResult.Fail)
        {
            Finish(NamedPatternResult.PlayerFailed);
            return;
        }

        if (completedStation != null)
            completedStation.MarkCompleted();

        if (AreAllStationsCompleted())
            Finish(NamedPatternResult.PlayerSucceeded);
    }

    private void Finish(NamedPatternResult result)
    {
        if (!_isRunning)
            return;

        _finishing = true;
        _isRunning = false;

        if (_activeQTE != null && _activeQTE.Running)
            _activeQTE.Cancel();

        _activeQTE = null;
        _activeStation = null;

        HideAllQTEPanels();
        SetStationsActive(false);

        Action<NamedPatternResult> callback = _onComplete;
        _onComplete = null;

        callback?.Invoke(result);

        _finishing = false;
    }

    private MutarusQTEStation FindInteractableStation()
    {
        if (_stations == null)
            return null;

        for (int i = 0; i < _stations.Length; i++)
        {
            MutarusQTEStation station = _stations[i];

            if (station == null)
                continue;

            if (!station.IsActive || station.IsCompleted)
                continue;

            if (station.HasPlayerInside)
                return station;
        }

        return null;
    }

    private QTEBase PickRandomQTE()
    {
        if (_qtePanels == null || _qtePanels.Length == 0)
            return null;

        int guard = 0;

        while (guard < 20)
        {
            QTEBase qte = _qtePanels[UnityEngine.Random.Range(0, _qtePanels.Length)];

            if (qte != null)
                return qte;

            guard++;
        }

        return null;
    }

    private bool AreAllStationsCompleted()
    {
        if (_stations == null || _stations.Length == 0)
            return false;

        for (int i = 0; i < _stations.Length; i++)
        {
            if (_stations[i] == null || !_stations[i].IsCompleted)
                return false;
        }

        return true;
    }

    private void HideAllQTEPanels()
    {
        if (_qtePanels == null)
            return;

        for (int i = 0; i < _qtePanels.Length; i++)
        {
            if (_qtePanels[i] != null)
                _qtePanels[i].gameObject.SetActive(false);
        }
    }

    private void SetStationsActive(bool active)
    {
        if (_stations == null)
            return;

        for (int i = 0; i < _stations.Length; i++)
        {
            if (_stations[i] != null)
                _stations[i].SetPatternActive(active);
        }
    }
}
