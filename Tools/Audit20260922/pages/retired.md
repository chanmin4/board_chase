# WIP·은퇴·잔여 연결 경계 | 옛 기획 대체

검사 기준: 2026-09-22 / E:/github/board_chase/board_chase. 현재 파일의 정적 연결·핵심 실행 경로 기준이며, 별도 표시 없는 항목은 이번 감사에서 플레이 테스트한 결과가 아닙니다.

## 이 문서의 목적

옛 기획의 원본 백업 본문은 사용자의 현재 프로젝트 기준 요청에 따라 제거하고 이 경계표로 대체했다. 미래 개발의 기준으로 과거 시스템을 다시 끌어오지 않도록 무엇이 실제 남아 있는지 기록한다. Unity 자산/코드는 이번 문서 작업에서 삭제하지 않았다.

## 은퇴 처리된 경로

- InfectionControlManager: 전역 감염통제력 메인 Update/구독 경로가 retiredWip 가드로 막혀 있다. 클래스와 씬 컴포넌트는 남아 있다.
- MetaProgressController: OnEnable에서 WIP 기능을 꺼 두며 구매·레벨·재화 경로가 비활성/0 반환이다. 영구 강화 상점으로 설명하지 않는다.
- PlayerCurrencyRuntime: 옛 숫자형 런 지갑은 0/비활성 호환 경로. 현재 거래는 물리 CurrencyItemSO 스택을 사용한다.
- RoguelikeCurrencyRewardApplier: 옛 런 보상 재화 경로 WIP.
- Named 선택 보상/옛 회복 훅: 현재 루팅·퀘스트 경로와 혼동하지 않는다.
- 별도 플레이어 근접무기 콘텐츠: WIP로 보존. 현재 플레이어는 무장 없음/총 탄 없음 비상 근접이 기준.

## 남아 있어서 무시하면 안 되는 것

MaskRenderManager, SectorPaint, SectorOccupancy와 페인트 샘플/투사체/감염/시야 연결은 현재 파일에 존재한다. ‘옛 점유 기획을 삭제’와 ‘페인트 코드를 삭제’는 다르다. 제거하려면 각 소비자 대체와 실제 플레이 검증이 필요하다.

설정 enum의 Paint·UpgradePanel 명칭, 구형 HUD/Creature/StateMachine, save의 legacy meta 필드도 존재한다. 이름만 보고 현재 사용자 기능이라고 판정하거나 참조 0만으로 삭제하면 안 된다.

## 기반만 있고 연결 확인이 안 된 것

- ShooterVisualRig: 자산 직렬화 참조 0, 프로젝트 런타임의 AddComponent 생성도 발견하지 못했다. 장착 컨트롤러는 기존 리그를 찾는 경로다.
- ShieldArmorRuntime: 직렬화 장착 자산 0. 실더 보스 완료 아님.
- RaidProgressionDraft_WIP: draftOnly=true, 실제 퀘스트 참조 null. 새 퀘스트 라인 완료 아님.
- 특별 탈출: 최소 스테이지와 틀만 있으며 현재 비활성.
- 역할별 전용 방 프리셋/Encounter 일부: null fallback 상태.

## 다음 삭제 작업의 원칙

정적 참조·동적 생성·Resources/Addressables·저장 GUID를 모두 확인하고 삭제 대상을 좁힌다. 기존 저장 마이그레이션, 프리팹 Missing Script, 이벤트 구독, UI 노출을 검증한 뒤 실제 제거한다. 이번 감사는 이 작업을 실행하지 않았다.

## 코드·데이터 근거

- `E:/github/board_chase/board_chase/Assets/Scripts/Gameplay/Combat/Infection/InfectionControlManager.cs`
- `E:/github/board_chase/board_chase/Assets/Scripts/Gameplay/Progression/Meta/MetaProgressController.cs`
- `E:/github/board_chase/board_chase/Assets/Scripts/Presentation/UI/Gameplay/Currency/PlayerCurrencyRuntime.cs`
- `E:/github/board_chase/board_chase/Assets/Scripts/Gameplay/Progression/Meta/RoguelikeCurrencyRewardApplier.cs`
- `E:/github/board_chase/board_chase/Assets/Scripts/Gameplay/Entities/Shooter/Common/Visuals/ShooterVisualRig.cs`
- `E:/github/board_chase/board_chase/Assets/Scripts/Gameplay/Entities/Common/Equipment/ShieldArmorRuntime.cs`