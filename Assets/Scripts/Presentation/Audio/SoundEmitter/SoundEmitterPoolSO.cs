/*
 * Role hierarchy: Presentation > Audio > SoundEmitter
 * Primary responsibility: Defines the SoundEmitterPoolSO data asset used to configure runtime rules in the Inspector.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SoundEmitterPool", menuName = "Audio/Sound Emitter Pool")]
public class SoundEmitterPoolSO : ScriptableObject
{
    [SerializeField] private SoundEmitter _prefab;

    private readonly Queue<SoundEmitter> _available = new();
    private readonly List<SoundEmitter> _allRuntimeInstances = new();
    private Transform _parent;
    private bool _isPrewarmed;

    // Purpose: Updates the parent state.
    public void SetParent(Transform parent)
    {
        _parent = parent;

        for (int i = 0; i < _allRuntimeInstances.Count; i++)
        {
            if (_allRuntimeInstances[i] != null)
                _allRuntimeInstances[i].transform.SetParent(_parent);
        }
    }

    // Purpose: Performs the prewarm workflow for this component.
    public void Prewarm(int size)
    {
        if (_prefab == null)
        {
            Debug.LogError("SoundEmitterPoolSO: prefab is missing.");
            return;
        }

        if (_isPrewarmed)
            return;

        _isPrewarmed = true;

        for (int i = 0; i < size; i++)
            CreateAndStoreEmitter();
    }

    // Purpose: Requests request from the owning service.
    public SoundEmitter Request()
    {
        if (_prefab == null)
        {
            Debug.LogError("SoundEmitterPoolSO: prefab is missing.");
            return null;
        }

        while (_available.Count > 0)
        {
            SoundEmitter pooledEmitter = _available.Dequeue();
            if (pooledEmitter == null)
                continue;

            pooledEmitter.transform.SetParent(_parent);
            pooledEmitter.gameObject.SetActive(true);
            return pooledEmitter;
        }

        SoundEmitter emitter = CreateAndStoreEmitter();
        emitter.gameObject.SetActive(true);
        return emitter;
    }

    // Purpose: Performs the return workflow for this component.
    public void Return(SoundEmitter emitter)
    {
        if (emitter == null)
            return;

        emitter.transform.SetParent(_parent);
        emitter.gameObject.SetActive(false);
        _available.Enqueue(emitter);
    }

    // Purpose: Creates the and store emitter owned by this workflow.
    private SoundEmitter CreateAndStoreEmitter()
    {
        SoundEmitter emitter = Instantiate(_prefab, _parent);
        emitter.gameObject.SetActive(false);
        _allRuntimeInstances.Add(emitter);
        _available.Enqueue(emitter);
        return emitter;
    }
}
