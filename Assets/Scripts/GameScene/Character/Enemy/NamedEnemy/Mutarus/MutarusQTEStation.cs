using UnityEngine;

public class MutarusQTEStation : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField] private GameObject _activeVisual;
    [SerializeField] private GameObject _completedVisual;

    public bool IsActive { get; private set; }
    public bool IsCompleted { get; private set; }
    public bool HasPlayerInside { get; private set; }

    private void Awake()
    {
        SetPatternActive(false);
    }

    public void SetPatternActive(bool active)
    {
        IsActive = active;
        IsCompleted = false;
        HasPlayerInside = false;

        if (_activeVisual != null)
            _activeVisual.SetActive(active);

        if (_completedVisual != null)
            _completedVisual.SetActive(false);
    }

    public void MarkCompleted()
    {
        IsCompleted = true;

        if (_activeVisual != null)
            _activeVisual.SetActive(false);

        if (_completedVisual != null)
            _completedVisual.SetActive(true);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsActive || IsCompleted)
            return;

        if (other.GetComponentInParent<VSplatter_Character>() == null)
            return;

        HasPlayerInside = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponentInParent<VSplatter_Character>() == null)
            return;

        HasPlayerInside = false;
    }
}
