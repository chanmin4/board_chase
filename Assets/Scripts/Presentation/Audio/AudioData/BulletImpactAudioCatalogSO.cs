/*
 * Role hierarchy: Presentation > Audio > AudioData
 * Primary responsibility: Maps obstacle surfaces to shared bullet impact cues.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using UnityEngine;

[CreateAssetMenu(fileName = "BulletImpactAudioCatalog", menuName = "Audio/Bullet Impact Audio Catalog")]
public sealed class BulletImpactAudioCatalogSO : DescriptionBaseSO
{
    [Serializable]
    public sealed class SurfaceCue
    {
        public ObstacleSurfaceType surface;
        public AudioCueSO cue;
    }

    [Header("Shared Output")]
    [SerializeField] private AudioCueEventChannelSO _sfxEventChannel;
    [Tooltip("모든 장애물 총탄 피격음에 적용하는 공통 3D 재생 설정입니다.")]
    [SerializeField] private AudioConfigurationSO _audioConfiguration;
    [SerializeField] private GameStateSO _gameState;

    [Header("Surface Sounds")]
    [Tooltip("Default 표면 또는 비어 있는 표면 항목에 사용할 기본 피격음입니다.")]
    [SerializeField] private AudioCueSO _defaultCue;
    [SerializeField] private SurfaceCue[] _surfaces = Array.Empty<SurfaceCue>();

    [Header("Impact Density")]
    [Tooltip("같은 장애물의 최소 재생 간격(초). 샷건 산탄의 중첩음을 줄이며 피해/AI 청각 판정에는 영향이 없습니다. 0이면 모든 탄환마다 재생합니다.")]
    [SerializeField, Min(0f)] private float _minimumIntervalPerObstacle = 0.04f;

    public AudioCueEventChannelSO SfxEventChannel => _sfxEventChannel;
    public AudioConfigurationSO AudioConfiguration => _audioConfiguration;
    public GameStateSO GameState => _gameState;
    public float MinimumIntervalPerObstacle => Mathf.Max(0f, _minimumIntervalPerObstacle);

    public AudioCueSO ResolveCue(ObstacleSurfaceType surface)
    {
        if (_surfaces != null)
        {
            foreach (SurfaceCue entry in _surfaces)
            {
                if (entry != null && entry.surface == surface &&
                    entry.cue != null && entry.cue.HasPlayableClip && !entry.cue.looping)
                {
                    return entry.cue;
                }
            }
        }

        return _defaultCue != null && _defaultCue.HasPlayableClip && !_defaultCue.looping
            ? _defaultCue
            : null;
    }
}
