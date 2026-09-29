/*
 * Role hierarchy: Core > Events > ScriptableObject
 * Primary responsibility: Implements the focused NamedOutsidePressureRequest responsibility and collaborates with its role group.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using UnityEngine;

public readonly struct NamedOutsidePressureRequest
{
    public readonly SectorRuntime ExcludedSector;
    public readonly float MinVirusPercent;
    public readonly float MaxVirusPercent;

    public NamedOutsidePressureRequest(
        SectorRuntime excludedSector,
        float minVirusPercent,
        float maxVirusPercent)
    {
        ExcludedSector = excludedSector;
        MinVirusPercent = minVirusPercent;
        MaxVirusPercent = maxVirusPercent;
    }
}

[CreateAssetMenu(
    fileName = "NamedOutsidePressureRequestChannel",
    menuName = "Events/Named Outside Pressure Request Channel")]
public class NamedOutsidePressureRequestEventChannelSO : ScriptableObject
{
    public event Action<NamedOutsidePressureRequest> OnEventRaised;

    // Purpose: Publishes the event notification.
    public void RaiseEvent(NamedOutsidePressureRequest request)
    {
        OnEventRaised?.Invoke(request);
    }
}
