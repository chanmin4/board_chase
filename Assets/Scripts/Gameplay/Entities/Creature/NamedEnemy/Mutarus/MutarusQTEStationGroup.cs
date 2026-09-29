/*
 * Role hierarchy: Gameplay > Entities > Creature > NamedEnemy > Mutarus
 * Primary responsibility: Implements the focused MutarusQTEStationGroup responsibility and collaborates with its role group.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

public class MutarusQTEStationGroup : MonoBehaviour
{
    [Header("Broadcasting On")]
    [SerializeField] private MutarusQTEStationGroupEventChannelSO _readyChannel;

    [Header("Stations")]
    [SerializeField] private MutarusQTEStation[] _stations;
    
    public MutarusQTEStation[] Stations => _stations;

    // Purpose: Initializes Inspector defaults for this component.
    private void Reset()
    {
        _stations = GetComponentsInChildren<MutarusQTEStation>(true);
    }

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    private void OnEnable()
    {
        if (_stations == null || _stations.Length == 0)
            _stations = GetComponentsInChildren<MutarusQTEStation>(true);

        if (_readyChannel != null)
            _readyChannel.RaiseEvent(this);
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        if (_readyChannel != null)
            _readyChannel.Clear(this);
    }

    // Purpose: Updates the pattern active state.
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
