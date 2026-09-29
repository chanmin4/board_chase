/*
 * Role hierarchy: Gameplay > Entities > Creature > NamedEnemy > Mutarus
 * Primary responsibility: Implements the focused MutarusNamedPatternRuntimeResetter responsibility and collaborates with its role group.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

[DisallowMultipleComponent]
public class MutarusNamedPatternRuntimeResetter : MonoBehaviour, INamedPatternRuntimeResetter
{
    [SerializeField] private MutarusQTEPatternResetRequestEventChannelSO _qtePatternResetRequestChannel;

    // Purpose: Performs the force reset named pattern runtime workflow for this component.
    public void ForceResetNamedPatternRuntime()
    {
        if (_qtePatternResetRequestChannel != null)
            _qtePatternResetRequestChannel.RaiseEvent();
    }
}
