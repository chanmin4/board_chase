using System;
using UnityEngine;

/// <summary>
/// Inert compatibility component for existing prefab GUIDs.
/// Original stack/decay implementation: WIP_ProjectDump/PaintMark/EntityPaintMarkController.cs.txt.
/// </summary>
[DisallowMultipleComponent, AddComponentMenu("")]
public class EntityPaintMarkController : MonoBehaviour
{
    public event Action<EntityPaintMarkController> OnChanged { add { } remove { } }
    public PaintMarkFaction ActiveFaction => PaintMarkFaction.None;
    public PaintMarkFaction DisplayFaction => PaintMarkFaction.None;
    public float VaccineMarkAmount => 0f;
    public float VirusMarkAmount => 0f;
    public int VaccineStacks => 0;
    public int VirusStacks => 0;
    public int ActiveStacks => 0;
    public int DisplayStacks => 0;
    public int ArmorClassDelta => 0;
    public float CurrentMarkAmount => 0f;
    public float CurrentMaxMarkAmount => 0f;
    public float Normalized => 0f;
    public float DisplayNormalized => 0f;
    public float DisplayMarkAmount => 0f;
    public float DisplayMaxMarkAmount => 0f;
    public bool IsMarkEnabled => false;
    public bool IsActive => false;
    private void Awake() => enabled = false;
    public void SetStatConfig(EntityStatConfigSO statConfig) { }
    public void AddMark(PaintMarkFaction faction, float amount) { }
    public float ReduceMark(float amount) => 0f;
}
