using UnityEngine;

/// <summary>World-owned one-shot effect: disabling a destroyed cover cannot hide its smoke.</summary>
public sealed class SectorDestructionEffectPlayer : MonoBehaviour
{
    private static SectorDestructionEffectsSO _settings;
    private static bool _loaded;
    private ParticleSystem[] _particles;
    private float _expiresAt;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetCache() { _settings = null; _loaded = false; }

    public static void PlayFor(Transform structure)
    {
        if (structure == null) return;
        if (!_loaded)
        {
            _settings = Resources.Load<SectorDestructionEffectsSO>("SectorDestructionEffects");
            _loaded = true;
        }
        if (_settings == null || _settings.SmokePrefab == null) return;
        Bounds bounds = ResolveBounds(structure);
        GameObject instance = Instantiate(_settings.SmokePrefab, bounds.center, _settings.SmokePrefab.transform.rotation);
        instance.name = "Cover destruction smoke";
        instance.transform.localScale *= _settings.ResolveScale(bounds);
        var particles = instance.GetComponentsInChildren<ParticleSystem>(true);
        if (particles.Length == 0)
        {
            Debug.LogWarning("[SectorDestruction] 공용 연기 프리팹에 ParticleSystem이 없습니다.", _settings);
            Destroy(instance); return;
        }
        foreach (var particle in particles)
        {
            particle.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particle.main;
            main.loop = false;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.stopAction = ParticleSystemStopAction.None;
        }
        var lifetime = instance.AddComponent<SectorDestructionEffectPlayer>();
        lifetime._particles = particles;
        lifetime._expiresAt = Time.time + _settings.MaximumLifetime;
        instance.SetActive(true);
        foreach (var particle in particles)
            if (particle.gameObject.activeInHierarchy) particle.Play(false);
    }

    // Renderer bounds, not prefab pivot. Ignore particle/trail/line renderers so an existing effect cannot inflate the object.
    private static Bounds ResolveBounds(Transform structure)
    {
        Bounds bounds = new Bounds(structure.position, Vector3.one);
        bool found = false;
        foreach (var renderer in structure.GetComponentsInChildren<Renderer>(false))
        {
            if (!renderer.enabled || renderer is ParticleSystemRenderer || renderer is TrailRenderer || renderer is LineRenderer) continue;
            if (!found) { bounds = renderer.bounds; found = true; }
            else bounds.Encapsulate(renderer.bounds);
        }
        if (found) return bounds;
        foreach (var collider in structure.GetComponentsInChildren<Collider>(false))
        {
            if (!collider.enabled || collider.isTrigger) continue;
            if (!found) { bounds = collider.bounds; found = true; }
            else bounds.Encapsulate(collider.bounds);
        }
        return bounds;
    }

    private void Update()
    {
        if (_particles == null || Time.time >= _expiresAt) { Destroy(gameObject); return; }
        foreach (var particle in _particles)
            if (particle != null && particle.gameObject.activeInHierarchy && particle.IsAlive(false)) return;
        Destroy(gameObject);
    }
}
