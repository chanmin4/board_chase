using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "SectorRuntimeEventChannel",
    menuName = "Events/SectorRuntimeEventChannel")]
public class SectorRuntimeEventChannelSO : ScriptableObject
{
    public event Action<SectorRuntime> OnEventRaised;
    /// <summary>
    /// SectorRuntime 하나를 이벤트로 전달한다.
    /// </summary>
    public void RaiseEvent(SectorRuntime sector)
    {
        OnEventRaised?.Invoke(sector);
    }
}