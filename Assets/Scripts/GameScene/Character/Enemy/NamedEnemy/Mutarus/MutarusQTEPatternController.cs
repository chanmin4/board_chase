using System;
using UnityEngine;

public class MutarusQTEPatternController : MonoBehaviour
{

    [Tooltip("QTE UI panels under Canvas_Pattern. They should start inactive.")]
    [SerializeField] private QTEBase[] _qtePanels;
    [Header("Input Lock")]
    [SerializeField] private InputReader _inputReader;

private bool _qteInputLocked;
    [Header("Station Listening")]
    [Tooltip("Runtime channel used to receive stations from the current battle sector.")]
    [SerializeField] private MutarusQTEStationGroupEventChannelSO _stationGroupReadyChannel;

    [Header("Broadcasting On")]
    [Tooltip("Runtime channel used by MutarusQTEPatternActionSO to find this controller.")]
    [SerializeField] private MutarusQTEPatternControllerEventChannelSO _readyChannel;
    [Header("Listening To")]
    [Tooltip("Raised by the named battle sector resetter when the battle sector is reset from another scene.")]
    [SerializeField] private MutarusQTEPatternResetRequestEventChannelSO _resetRequestChannel;
    [Header("Canvas")]
    [Tooltip("Optional root object for the whole pattern canvas/root. It is enabled while the pattern is running.")]
    [SerializeField] private GameObject _patternRoot;
    [Tooltip("CanvasGroup on QTEPatternRoot. Safer than SetActive because the controller stays enabled.")]
    [SerializeField] private CanvasGroup _patternCanvasGroup;
    [Header("Runtime Player")]
    [SerializeField] private TransformAnchor _playerAnchor;
    [Header("Runtime Debug")]
    [SerializeField, ReadOnly] private bool _isRunning;
    [SerializeField, ReadOnly] private float _timeRemaining;

    private MutarusQTEStationGroup _stationGroup;
    private MutarusQTEStation[] _stations;

    private Action<NamedPatternResult> _onComplete;
    private QTEBase _activeQTE;
    private MutarusQTEStation _activeStation;
    private bool _finishing;

    public bool IsRunning => _isRunning;
    public float TimeRemaining => _timeRemaining;

    private void Awake()
    {
        EnsureQTEPanelsReady();
        HideAllQTEPanels();
        SetPatternRootVisible(false);
    }


    private void OnEnable()
    {

        if (_stationGroupReadyChannel != null)
        {
            _stationGroupReadyChannel.OnEventRaised += HandleStationGroupReady;
            HandleStationGroupReady(_stationGroupReadyChannel.Current);
        }

        if (_readyChannel != null)
            _readyChannel.RaiseEvent(this);
        if (_resetRequestChannel != null)
            _resetRequestChannel.OnEventRaised += HandleResetRequest;
    }

    private void OnDisable()
    {

        if (_stationGroupReadyChannel != null)
            _stationGroupReadyChannel.OnEventRaised -= HandleStationGroupReady;

        if (_readyChannel != null)
            _readyChannel.Clear(this);
        if (_resetRequestChannel != null)
            _resetRequestChannel.OnEventRaised -= HandleResetRequest;
    }

    private void Update()
    {
        if (!_isRunning)
            return;

        _timeRemaining -= Time.deltaTime;

        if (_timeRemaining <= 0f)
            Finish(NamedPatternResult.PlayerFailed);
    }

    private void HandleResetRequest()
    {
        CancelPatternWithoutResult();
    }

    private void EnsureQTEPanelsReady()
    {
        if (_qtePanels == null)
            return;

        for (int i = 0; i < _qtePanels.Length; i++)
        {
            QTEBase qte = _qtePanels[i];

            if (qte == null)
                continue;

            // UI는 SetActive로 껐다 켜지 말고 CanvasGroup으로만 숨긴다.
            // 단, 에디터에서 꺼둔 상태면 CanvasGroup alpha를 바꿔도 화면에 안 나오므로 런타임에 켜둔다.
            if (!qte.gameObject.activeSelf)
                qte.gameObject.SetActive(true);

            CanvasGroup group = qte.GetComponent<CanvasGroup>();
            if (group == null)
                group = qte.gameObject.AddComponent<CanvasGroup>();

            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
        }
    }
    public void BeginPattern(float duration, Action<NamedPatternResult> onComplete)
    {
        CancelPatternWithoutResult();

        _isRunning = true;
        _finishing = false;
        _timeRemaining = Mathf.Max(0.01f, duration);
        _onComplete = onComplete;

        SetPatternRootVisible(true);
        HideAllQTEPanels();

        if (_stationGroup != null)
            _stationGroup.SetPatternActive(true);
    }

    public void CancelPatternWithoutResult()
    {
        UnlockGameplayInputForQTE();

        if (_activeQTE != null && _activeQTE.Running)
            _activeQTE.Cancel();

        _activeQTE = null;
        _activeStation = null;
        _isRunning = false;
        _finishing = false;
        _onComplete = null;

        HideAllQTEPanels();

        if (_stationGroup != null)
            _stationGroup.SetPatternActive(false);

        SetPatternRootVisible(false);
    }

    private void HandleStationGroupReady(MutarusQTEStationGroup group)
    {
        _stationGroup = group;
        _stations = group != null ? group.Stations : null;

        if (_isRunning && _stationGroup != null)
            _stationGroup.SetPatternActive(true);
    }

    private void HandleInteract()
    {
        Debug.Log(
            $"[MutarusQTEPatternController] Interact. " +
            $"running={_isRunning}, activeQTE={_activeQTE}, " +
            $"stationGroup={_stationGroup}, stations={(_stations != null ? _stations.Length : 0)}",
            this);

        if (!_isRunning || _activeQTE != null)
            return;

        MutarusQTEStation station = FindInteractableStation();
        if (station == null)
        {
            Debug.Log("[MutarusQTEPatternController] No interactable station.", this);
            return;
        }

        QTEBase qte = PickRandomQTE();
        if (qte == null)
        {
            Debug.LogWarning("[MutarusQTEPatternController] No QTE panel found.", this);
            return;
        }

        _activeStation = station;
        _activeQTE = qte;

        SetQTEPanelVisible(_activeQTE, true);
        _activeQTE.Begin(HandleQTEResult);
    }
    private void HandleQTEResult(QTEResult result)
    {
        if (_finishing)
            return;
        UnlockGameplayInputForQTE();
        QTEBase completedQTE = _activeQTE;
        MutarusQTEStation completedStation = _activeStation;

        _activeQTE = null;
        _activeStation = null;

        SetQTEPanelVisible(completedQTE, false);

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
    private void LockGameplayInputForQTE()
    {
        if (_qteInputLocked)
            return;

        if (_inputReader != null)
            _inputReader.DisableAllInput();

        _qteInputLocked = true;
    }

    private void UnlockGameplayInputForQTE()
    {
        if (!_qteInputLocked)
            return;

        if (_inputReader != null)
            _inputReader.EnableGameplayInput();

        _qteInputLocked = false;
    }
    private void Finish(NamedPatternResult result)
    {
        if (!_isRunning)
            return;
        UnlockGameplayInputForQTE();
        _finishing = true;
        _isRunning = false;

        if (_activeQTE != null && _activeQTE.Running){
            _activeQTE.Cancel();     
        }
        SetQTEPanelVisible(_activeQTE, false);
        _activeQTE = null;
        _activeStation = null;

        HideAllQTEPanels();

        if (_stationGroup != null)
            _stationGroup.SetPatternActive(false);

        SetPatternRootVisible(false);

        Action<NamedPatternResult> callback = _onComplete;
        _onComplete = null;

        callback?.Invoke(result);

        _finishing = false;
    }

    private MutarusQTEStation FindInteractableStation()
    {
        if (_stations == null)
            return null;

        Transform player = _playerAnchor != null && _playerAnchor.isSet
            ? _playerAnchor.Value
            : null;

        for (int i = 0; i < _stations.Length; i++)
        {
            MutarusQTEStation station = _stations[i];

            if (station == null)
                continue;



            if (!station.IsActive || station.IsCompleted)
                continue;

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
            if (_qtePanels[i] == null)
                continue;

            SetQTEPanelVisible(_qtePanels[i], false);
        }
    }
    private void SetQTEPanelVisible(QTEBase qte, bool visible)
    {
        if (qte == null)
            return;

        if (!qte.gameObject.activeSelf)
            qte.gameObject.SetActive(true);

        // 절대 GetComponentInParent 쓰지 말 것.
        // 패널별 CanvasGroup만 조작해야 QTEPatternRoot가 같이 꺼지지 않는다.
        CanvasGroup group = qte.GetComponent<CanvasGroup>();
        if (group == null)
            group = qte.gameObject.AddComponent<CanvasGroup>();

        group.alpha = visible ? 1f : 0f;
        group.interactable = visible;
        group.blocksRaycasts = visible;

        Debug.Log(
            $"[MutarusQTEPatternController] SetQTEPanelVisible qte={qte.name}, visible={visible}, " +
            $"activeSelf={qte.gameObject.activeSelf}, activeInHierarchy={qte.gameObject.activeInHierarchy}, alpha={group.alpha}",
            this);
    }

    private void SetPatternRootVisible(bool visible)
    {
        if (_patternCanvasGroup == null)
            return;

        _patternCanvasGroup.alpha = visible ? 1f : 0f;
        _patternCanvasGroup.interactable = visible;
        _patternCanvasGroup.blocksRaycasts = visible;
    }

    public bool TryStartQTE(MutarusQTEStation station)
    {
        Debug.Log(
            $"[MutarusQTEPatternController] TryStartQTE. " +
            $"running={_isRunning}, activeQTE={_activeQTE}, station={station}",
            this);

        if (!_isRunning || _activeQTE != null)
            return false;

        if (station == null || !station.IsActive || station.IsCompleted)
            return false;

        QTEBase qte = PickRandomQTE();
        if (qte == null)
        {
            Debug.LogWarning("[MutarusQTEPatternController] TryStartQTE failed. No QTE panel assigned.", this);
            return false;
        }

        _activeStation = station;
        _activeQTE = qte;
        LockGameplayInputForQTE();
        SetPatternRootVisible(true);
        HideAllQTEPanels();
        SetQTEPanelVisible(_activeQTE, true);

        Debug.Log($"[MutarusQTEPatternController] QTE started. qte={_activeQTE.name}, station={station.name}", this);

        _activeQTE.Begin(HandleQTEResult);
        return true;
    }
}
