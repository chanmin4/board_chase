using UnityEngine;

[CreateAssetMenu(
    fileName = "NamedAttackId",
    menuName = "Named Enemy/Attack Id")]
public class NamedAttackIdSO : ScriptableObject
{
    [SerializeField] private string _debugName;

    public string DebugName
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(_debugName))
                return _debugName;

            return name;
        }
    }
}
