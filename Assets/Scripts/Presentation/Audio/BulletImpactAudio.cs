/*
 * Role hierarchy: Presentation > Audio
 * Primary responsibility: Routes ballistic bullet hits to surface cues through the existing AudioManager.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

public static class BulletImpactAudio
{
    public const string CatalogResourcePath = "BulletImpactAudioCatalog";

    private static BulletImpactAudioCatalogSO _catalog;
    private static bool _catalogLoadAttempted;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntimeState()
    {
        _catalog = null;
        _catalogLoadAttempted = false;
    }

    public static bool SupportsBullet(BulletSO bullet)
    {
        // Rockets currently also use BasicBullet; the component type alone is not enough.
        // Special ammo (paint/zones/injections), melee and throwables keep their own audio paths.
        return bullet != null && bullet.IsBasicAmmo && bullet.Family != BulletFamily.Rocket;
    }

    public static bool TryPlay(BulletSO bullet, Collider hitCollider, Vector3 hitPoint)
    {
        if (!SupportsBullet(bullet) || hitCollider == null)
            return false;

        ObstacleSurface surface = hitCollider.GetComponentInParent<ObstacleSurface>();
        if (surface == null || !surface.isActiveAndEnabled)
            return false;

        if (!_catalogLoadAttempted)
        {
            _catalogLoadAttempted = true;
            _catalog = Resources.Load<BulletImpactAudioCatalogSO>(CatalogResourcePath);
            if (_catalog == null)
                Debug.LogWarning("[BulletImpactAudio] Resources/BulletImpactAudioCatalog is missing. Obstacle impact audio is disabled.");
        }

        return TryPlay(_catalog, surface, hitPoint);
    }

    public static bool TryPlay(
        BulletImpactAudioCatalogSO catalog,
        ObstacleSurface surface,
        Vector3 hitPoint)
    {
        if (catalog == null || surface == null || !surface.isActiveAndEnabled ||
            (catalog.GameState != null && catalog.GameState.CurrentGameState == GameState.Cutscene))
        {
            return false;
        }

        AudioCueEventChannelSO channel = catalog.SfxEventChannel;
        if (channel == null || channel.OnAudioCuePlayRequested == null)
            return false;

        AudioCueSO cue = catalog.ResolveCue(surface.Surface);
        if (cue == null)
            return false;

        AudioConfigurationSO configuration = catalog.AudioConfiguration != null
            ? catalog.AudioConfiguration
            : cue.DefaultPlaybackConfiguration;
        if (configuration == null)
            return false;

        float now = Time.unscaledTime;
        if (!surface.CanPlayImpact(now, catalog.MinimumIntervalPerObstacle))
            return false;

        AudioCueKey key = channel.RaisePlayEvent(cue, configuration, hitPoint);
        if (!key.IsValid)
            return false;

        surface.RecordImpact(now);
        return true;
    }
}
