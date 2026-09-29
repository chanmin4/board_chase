/*
 * Role hierarchy: Gameplay > World > Sectors > Portal
 * Primary responsibility: Defines the serialized boundary collider and portal references for one sector edge.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class SectorEdge
{
    public Collider  bound;
    public GameObject portal;
    //public SectorPortalTrigger portalTrigger;
}
