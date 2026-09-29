using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class ObstacleBulletImpactAudioTests
{
    private const string ObstacleFolder = "Assets/Prefabs/GameScene/Sector/SectorObject/ObstacleObject";
    private readonly List<Object> _temporaryObjects = new();
    private object _previousCatalog;
    private object _previousLoadAttempted;
    private BulletImpactAudioCatalogSO _catalog;
    private AudioCueEventChannelSO _channel;
    private AudioCueSO _fallback;
    private AudioCueSO _metal;
    private AudioConfigurationSO _configuration;
    private int _playCount;
    private Vector3 _lastPoint;
    private AudioCueSO _lastCue;

    [SetUp]
    public void SetUp()
    {
        _previousCatalog = RouterField("_catalog").GetValue(null);
        _previousLoadAttempted = RouterField("_catalogLoadAttempted").GetValue(null);
        _catalog = MakeAsset<BulletImpactAudioCatalogSO>();
        _channel = MakeAsset<AudioCueEventChannelSO>();
        _configuration = MakeAsset<AudioConfigurationSO>();
        _configuration.spatialBlend = 1f;
        _fallback = MakeCue();
        _metal = MakeCue();
        SetField(_catalog, "_sfxEventChannel", _channel);
        SetField(_catalog, "_audioConfiguration", _configuration);
        SetField(_catalog, "_defaultCue", _fallback);
        SetField(_catalog, "_surfaces", new[]
        {
            new BulletImpactAudioCatalogSO.SurfaceCue { surface = ObstacleSurfaceType.Metal, cue = _metal }
        });
        RouterField("_catalog").SetValue(null, _catalog);
        RouterField("_catalogLoadAttempted").SetValue(null, true);
        _playCount = 0;
        _channel.OnAudioCuePlayRequested += (cue, config, point) =>
        {
            Assert.That(config, Is.SameAs(_configuration));
            _playCount++;
            _lastPoint = point;
            _lastCue = cue;
            return AudioCueKey.Create();
        };
    }

    [TearDown]
    public void TearDown()
    {
        RouterField("_catalog").SetValue(null, _previousCatalog);
        RouterField("_catalogLoadAttempted").SetValue(null, _previousLoadAttempted);
        for (int i = _temporaryObjects.Count - 1; i >= 0; i--)
        {
            if (_temporaryObjects[i] != null)
                Object.DestroyImmediate(_temporaryObjects[i]);
        }
        _temporaryObjects.Clear();
    }

    [TestCase(typeof(BasicBulletSO), true)]
    [TestCase(typeof(LightBulletSO), true)]
    [TestCase(typeof(MediumBulletSO), true)]
    [TestCase(typeof(HeavyBulletSO), true)]
    [TestCase(typeof(SniperBulletSO), true)]
    [TestCase(typeof(ShotgunShellSO), true)]
    [TestCase(typeof(RocketBulletSO), false)]
    [TestCase(typeof(InfectionInjectionBulletSO), false)]
    public void BallisticAmmoRouting_ExcludesRocketsAndSpecialProjectiles(Type ammoType, bool expected)
    {
        BulletSO ammo = (BulletSO)ScriptableObject.CreateInstance(ammoType);
        _temporaryObjects.Add(ammo);
        ObstacleSurface surface = MakeSurface();
        Collider collider = surface.gameObject.AddComponent<BoxCollider>();

        Assert.That(BulletImpactAudio.SupportsBullet(ammo), Is.EqualTo(expected));
        Assert.That(BulletImpactAudio.TryPlay(ammo, collider, Vector3.one), Is.EqualTo(expected));
        Assert.That(_playCount, Is.EqualTo(expected ? 1 : 0));
    }

    [Test]
    public void ChildCollider_UsesParentSurfaceAndExactContactPoint()
    {
        ObstacleSurface surface = MakeSurface();
        SetField(surface, "_surface", ObstacleSurfaceType.Metal);
        GameObject child = new GameObject("Child Collider");
        child.transform.SetParent(surface.transform);
        Collider collider = child.AddComponent<BoxCollider>();
        Vector3 point = new Vector3(7f, 2f, -4f);

        Assert.That(BulletImpactAudio.TryPlay(MakeAsset<LightBulletSO>(), collider, point), Is.True);
        Assert.That(_lastCue, Is.SameAs(_metal));
        Assert.That(_lastPoint, Is.EqualTo(point));
    }

    [Test]
    public void ShotgunOverlap_IsLimitedPerObstacleAndCanBeDisabled()
    {
        ObstacleSurface first = MakeSurface();
        ObstacleSurface second = MakeSurface();
        SetField(_catalog, "_minimumIntervalPerObstacle", 60f);

        Assert.That(BulletImpactAudio.TryPlay(_catalog, first, Vector3.zero), Is.True);
        Assert.That(BulletImpactAudio.TryPlay(_catalog, first, Vector3.zero), Is.False);
        Assert.That(BulletImpactAudio.TryPlay(_catalog, second, Vector3.zero), Is.True);
        SetField(_catalog, "_minimumIntervalPerObstacle", 0f);
        Assert.That(BulletImpactAudio.TryPlay(_catalog, first, Vector3.zero), Is.True);
        Assert.That(_playCount, Is.EqualTo(3));
    }

    [Test]
    public void MissingEmptyOrLoopingSurfaceCue_UsesPlayableDefault()
    {
        Assert.That(_catalog.ResolveCue(ObstacleSurfaceType.Wood), Is.SameAs(_fallback));
        SetField(_metal, "_audioClipGroups", Array.Empty<AudioClipsGroup>());
        Assert.That(_catalog.ResolveCue(ObstacleSurfaceType.Metal), Is.SameAs(_fallback));
        _metal = MakeCue();
        _metal.looping = true;
        SetField(_catalog, "_surfaces", new[]
        {
            new BulletImpactAudioCatalogSO.SurfaceCue { surface = ObstacleSurfaceType.Metal, cue = _metal }
        });
        Assert.That(_catalog.ResolveCue(ObstacleSurfaceType.Metal), Is.SameAs(_fallback));
        _fallback.looping = true;
        Assert.That(_catalog.ResolveCue(ObstacleSurfaceType.Metal), Is.Null);
    }

    [Test]
    public void UntaggedOrDisabledTargetsAndMissingAudioListener_DoNotEmit()
    {
        ObstacleSurface surface = MakeSurface();
        Collider collider = surface.gameObject.AddComponent<BoxCollider>();
        BulletSO bullet = MakeAsset<MediumBulletSO>();
        Assert.That(BulletImpactAudio.TryPlay(null, collider, Vector3.zero), Is.False);
        Assert.That(BulletImpactAudio.TryPlay(bullet, null, Vector3.zero), Is.False);
        surface.enabled = false;
        Assert.That(BulletImpactAudio.TryPlay(bullet, collider, Vector3.zero), Is.False);
        surface.enabled = true;
        _channel.OnAudioCuePlayRequested = null;
        Assert.That(BulletImpactAudio.TryPlay(bullet, collider, Vector3.zero), Is.False);
        Object.DestroyImmediate(surface);
        Assert.That(BulletImpactAudio.TryPlay(bullet, collider, Vector3.zero), Is.False);
        Assert.That(_playCount, Is.Zero);
    }

    [Test]
    public void FailedPlayback_DoesNotConsumeObstacleCooldown()
    {
        ObstacleSurface surface = MakeSurface();
        SetField(_catalog, "_minimumIntervalPerObstacle", 60f);
        _channel.OnAudioCuePlayRequested = (cue, config, point) => AudioCueKey.Invalid;
        Assert.That(BulletImpactAudio.TryPlay(_catalog, surface, Vector3.zero), Is.False);
        _channel.OnAudioCuePlayRequested = (cue, config, point) => AudioCueKey.Create();
        Assert.That(BulletImpactAudio.TryPlay(_catalog, surface, Vector3.zero), Is.True);
    }

    [Test]
    public void AuthoredCatalog_HasPlayableOneShotForEverySurface()
    {
        BulletImpactAudioCatalogSO authored = Resources.Load<BulletImpactAudioCatalogSO>(BulletImpactAudio.CatalogResourcePath);
        Assert.That(authored, Is.Not.Null);
        Assert.That(authored.SfxEventChannel, Is.Not.Null);
        Assert.That(authored.AudioConfiguration, Is.Not.Null);
        Assert.That(authored.AudioConfiguration.spatialBlend, Is.EqualTo(1f));
        foreach (ObstacleSurfaceType surface in Enum.GetValues(typeof(ObstacleSurfaceType)))
        {
            AudioCueSO cue = authored.ResolveCue(surface);
            Assert.That(cue, Is.Not.Null, surface.ToString());
            Assert.That(cue.HasPlayableClip, Is.True, surface.ToString());
            Assert.That(cue.looping, Is.False, surface.ToString());
        }
    }

    [Test]
    public void ObstacleFolderPrefabs_HaveSurfaceAndBallisticCollisionLayer()
    {
        string[] prefabs = AssetDatabase.FindAssets("t:Prefab", new[] { ObstacleFolder });
        Assert.That(prefabs, Is.Not.Empty);
        int structureLayer = LayerMask.NameToLayer("SectorStructure");
        Assert.That(structureLayer, Is.GreaterThanOrEqualTo(0));
        foreach (string guid in prefabs)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.That(prefab.GetComponent<ObstacleSurface>(), Is.Not.Null, path);
            Assert.That(prefab.GetComponentsInChildren<ObstacleAudio>(true), Is.Empty, path);
            Collider collider = prefab.GetComponent<Collider>();
            Assert.That(collider, Is.Not.Null, path);
            Assert.That(collider.gameObject.layer, Is.EqualTo(structureLayer), path);
        }
    }

    private T MakeAsset<T>() where T : ScriptableObject
    {
        T asset = ScriptableObject.CreateInstance<T>();
        _temporaryObjects.Add(asset);
        return asset;
    }

    private AudioCueSO MakeCue()
    {
        AudioCueSO cue = MakeAsset<AudioCueSO>();
        AudioClip clip = AudioClip.Create("Test Impact", 32, 1, 44100, false);
        _temporaryObjects.Add(clip);
        SetField(cue, "_audioClipGroups", new[] { new AudioClipsGroup { audioClips = new[] { clip } } });
        return cue;
    }

    private ObstacleSurface MakeSurface()
    {
        GameObject obstacle = new GameObject("Test Obstacle");
        _temporaryObjects.Add(obstacle);
        return obstacle.AddComponent<ObstacleSurface>();
    }

    private static void SetField(object target, string name, object value)
    {
        target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    }

    private static FieldInfo RouterField(string name)
    {
        return typeof(BulletImpactAudio).GetField(name, BindingFlags.Static | BindingFlags.NonPublic);
    }
}
