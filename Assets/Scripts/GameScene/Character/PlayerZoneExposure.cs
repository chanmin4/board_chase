using UnityEngine;

[DisallowMultipleComponent]
public class PlayerZoneExposure : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private PlayerInfection _playerInfection;
    
    [SerializeField] private Transform _samplePoint;
    [Header("Don't Touch Refs")]
    [SerializeField] private MaskRenderManager _maskRenderManager;

    [Header("Options")]
    [SerializeField] private bool _requireOpenedSector = true;

    private void Reset()
    {
        if (_playerInfection == null)
            _playerInfection = GetComponent<PlayerInfection>();

        if (_samplePoint == null)
            _samplePoint = transform;
    }

    private void Awake()
    {
        if (_playerInfection == null)
            _playerInfection = GetComponent<PlayerInfection>();

        if (_maskRenderManager == null)
            _maskRenderManager = FindAnyObjectByType<MaskRenderManager>();

        if (_samplePoint == null)
            _samplePoint = transform;
    }

    private void Update()
    {
        if (_playerInfection == null)
            return;

        if (_maskRenderManager == null)
            _maskRenderManager = FindAnyObjectByType<MaskRenderManager>();

        if (_maskRenderManager == null || _samplePoint == null)
            return;

        if (!_maskRenderManager.TryGetStateAtWorld(
                _samplePoint.position,
                out MaskRenderManager.PaintState state,
                _requireOpenedSector))
        {
            return;
        }

        switch (state)
        {
            case MaskRenderManager.PaintState.Virus:
                _playerInfection.AddVirusZoneExposure(Time.deltaTime);
                break;

            case MaskRenderManager.PaintState.Vaccine:
                _playerInfection.AddVaccineZoneRecovery(Time.deltaTime);
                break;
        }
    }
}
