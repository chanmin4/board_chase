using UnityEngine;

public class NamedEnemyBlackboard : MonoBehaviour
{
    [Header("Runtime Lifecycle")]
    public bool spawnInitialized;
    public bool introFinished;

    [Header("Runtime Combat")]
    public bool canEnterPattern = true;
    public bool shouldStopChase;
    public bool attackFinished = true;

    [Header("Runtime Selected Attack")]
    [SerializeField, ReadOnly] private NamedAttackIdSO _selectedAttack;

    public NamedAttackIdSO SelectedAttack => _selectedAttack;
    public bool HasSelectedAttack => _selectedAttack != null;

    public void SelectAttack(NamedAttackIdSO attackId)
    {
        _selectedAttack = attackId;
        attackFinished = false;
    }

    public void ClearSelectedAttack()
    {
        _selectedAttack = null;
    }

    public bool SelectedAttackIs(NamedAttackIdSO attackId)
    {
        return _selectedAttack != null && _selectedAttack == attackId;
    }
}
