using UnityEngine;
using UnityEngine.AddressableAssets;

/// <summary>
/// 공통 씬 데이터 베이스 (Locations, Menus, Managers 등)
/// </summary>
public class GameSceneSO : DescriptionBaseSO
{
    public GameSceneType sceneType;
    public AssetReference sceneReference; // Addressables로 씬 로드용
    public AudioCueSO musicTrack;         // (선택) 씬 BGM

    public enum GameSceneType
    {
        Location,
        Menu,
        Risk,
        Initialisation,
        PersistentManager,
        Gameplay,

        Art,
    }
}