# 10. 시야·카메라·렌더링·물리·성능 경계

검사 기준: 2026-09-22 / E:/github/board_chase/board_chase. 현재 파일의 정적 연결·핵심 실행 경로 기준이며, 별도 표시 없는 항목은 이번 감사에서 플레이 테스트한 결과가 아닙니다.

## AI 감지와 화면 표시

EntityVisionController는 거리·방향·LOS를 판단한다. VisionVisibilitySystem은 플레이어 시야를 기준으로 대상 Renderer 표시를 관리한다. 소리 인지는 SoundStimulus 계열이다. 하나를 끄면 나머지도 자동으로 꺼지는 단일 시스템이 아니다.

VisionVisibilitySystem의 기본 적 등록 스캔은 0.25초, 시야 갱신 기본 간격은 0으로 LateUpdate 매 프레임 판정을 허용한다. 스폰 준비 중인 적을 별도 취급한다. 백신 페인트 강조 옵션이 남아 있지만 기본적으로 시야 밖까지 페인트로 강제 공개하지 않는 옵션값이다. 실제 씬 override는 자산에서 확인한다.

## 카메라 가림 표현

CameraEntityOcclusionFadeController라는 이름과 달리 현재 코드의 핵심은 구조물 뒤 Character에 깊이 기반 실루엣 overlay를 붙이는 것이다. 모든 장애물 머티리얼을 반투명으로 바꾸는 기능으로 설명하지 않는다.

Resources/CameraOccluderDither 셰이더와 캐릭터 Renderer 추적, 깊이 차이, SectorStructure 레이어를 사용한다. 런타임 EnsureFor로 생성될 수 있다. 임시 머티리얼/렌더러는 비활성·파괴 때 정리한다. 손 무기와 시체의 Renderer도 뒤늦게 갱신될 수 있다.

## 월드 생성과 내비게이션

StageSectorInstantiator가 생성한 지형·장애물·입구와 NavMesh 준비 상태는 적 AI, 포탈 이동, 로딩 준비 게이트의 전제다. 프리팹이 예뻐 보여도 collider/레이어/내비게이션 통로가 잘못되면 실제 방 기능은 완료가 아니다.

투사체의 충돌 레이어와 표면 소리 분류, 시야 가림 레이어, 카메라 실루엣 대상 레이어는 별개 정책이다. Material 또는 Layer 하나로 모든 의미를 겸하지 않는다.

## URP 현재 주의점

Unity 6000.0.47f1 / URP 17.0.4 환경이다. DefaultVolumeProfile에는 CopyPasteTestComponent·VolumeComponentSupported 계열 및 fileID 0인 테스트 컴포넌트가 남아 있고 일부 미해결 스크립트 GUID도 있다. 현재 파일에서 재확인한 항목이며 이전 감사의 추측만 가져온 것이 아니다. 정리와 임포트/실렌더 검증이 필요하다.

머티리얼 파일의 미해결 GUID 중에는 이전 업그레이더 메타데이터도 있으므로 모두 분홍 셰이더라고 단정하지 않는다. Renderer가 참조하는 Material → 실제 shader/texture → URP 호환 → 화면 출력 순서로 검사한다.

## 성능 검증 범위

이번 감사에서는 Unity Profiler 측정이나 목표 하드웨어 FPS 수치를 만들지 않았다. 큰 13×13 맵, 활성 적 수, LOS raycast, 마스크 샘플링, 시체/아이템 수, UI rebuild, 오디오 동시 발음, 저장 디스크 쓰기를 실제 부하 조건에서 측정해야 한다. 풀링 코드가 있다는 것만으로 성능 완료라고 표시하지 않는다.

## 코드·데이터 근거

- `E:/github/board_chase/board_chase/Assets/Scripts/Gameplay/Entities/Shooter/Common/Vision/EntityVisionController.cs`
- `E:/github/board_chase/board_chase/Assets/Scripts/Gameplay/Entities/Shooter/Common/Vision/VisionVisibilitySystem.cs`
- `E:/github/board_chase/board_chase/Assets/Scripts/Presentation/Camera/CameraEntityOcclusionFadeController.cs`
- `E:/github/board_chase/board_chase/Assets/Scripts/Presentation/Camera/CameraManager.cs`
- `E:/github/board_chase/board_chase/Assets/Scripts/Gameplay/World/Sectors/StageMap/StageSectorInstantiator.cs`
- `E:/github/board_chase/board_chase/Assets/Scripts/Gameplay/Combat/Projectiles/ProjectileRootRegistry.cs`
- `E:/github/board_chase/board_chase/Assets/Settings/DefaultVolumeProfile.asset`