using UnityEngine;

public class MutarusQTEStationGroup : MonoBehaviour
{
    [Header("Broadcasting On")]
    [SerializeField] private MutarusQTEStationGroupEventChannelSO _readyChannel;

    [Header("Stations")]
    [SerializeField] private MutarusQTEStation[] _stations;
    
    public MutarusQTEStation[] Stations => _stations;

    private void Reset()
    {
        _stations = GetComponentsInChildren<MutarusQTEStation>(true);
    }

    private void OnEnable()
    {
        if (_stations == null || _stations.Length == 0)
            _stations = GetComponentsInChildren<MutarusQTEStation>(true);

        if (_readyChannel != null)
            _readyChannel.RaiseEvent(this);
    }

    private void OnDisable()
    {
        if (_readyChannel != null)
            _readyChannel.Clear(this);
    }

    public void SetPatternActive(bool active)
    {
        if (_stations == null)
            return;

        for (int i = 0; i < _stations.Length; i++)
        {
            if (_stations[i] == null)
                continue;

            if (active)
                _stations[i].SetPatternActive(true);
            else
                _stations[i].Clear();
        }
    }
}
