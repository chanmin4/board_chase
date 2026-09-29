/*
 * Role hierarchy: Gameplay > Progression > Hideout
 * Primary responsibility: Owns manually authored Hideout services and resident reset hooks.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class HomeSector : MonoBehaviour
{
    [Header("Runtime Root")]
    [SerializeField] private SectorRuntime _sectorRuntime;

    [Header("Home Residents")]
    [Tooltip("Explicit Home quest NPC Damageable components reset whenever the player enters the HomeSector. Do not register the player or normal combat entities here.")]
    [SerializeField] private Damageable[] _homeResidents = Array.Empty<Damageable>();

    public SectorRuntime SectorRuntime => _sectorRuntime;

    // Purpose: Initializes initialize for use by this component.
    public void Initialize(SectorRuntime sectorRuntime)
    {
        _sectorRuntime = sectorRuntime != null ? sectorRuntime : _sectorRuntime;

        if (_sectorRuntime == null)
            TryGetComponent(out _sectorRuntime);
    }

    // Purpose: Performs the reset home residents workflow for this component.
    public void ResetHomeResidents()
    {
        for (int i = 0; i < _homeResidents.Length; i++)
        {
            Damageable resident = _homeResidents[i];
            if (resident == null)
                continue;

            resident.gameObject.SetActive(true);
            resident.ResetHealthFromConfig();
        }

        HomeQuestNpc[] questNpcs = GetComponentsInChildren<HomeQuestNpc>(true);
        for (int i = 0; i < questNpcs.Length; i++)
        {
            if (questNpcs[i] != null)
                questNpcs[i].ResetForHomeVisit();
        }
    }

#if UNITY_EDITOR
    // Purpose: Normalizes Inspector-authored data after it changes in the Editor.
    private void OnValidate()
    {
        if (_sectorRuntime == null)
            TryGetComponent(out _sectorRuntime);
    }
#endif
}
