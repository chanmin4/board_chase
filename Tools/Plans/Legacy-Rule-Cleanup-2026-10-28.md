# Meta WIP 격리 · 패시브 공용 규칙 중복 정리

## Meta

`Assets/Scripts/WIP/Legacy/MetaProgression/`으로 스크립트 8개, 카탈로그·이벤트 에셋 3개, 전용 통계 프리팹 1개를 `.meta`와 함께 이동했다. 12개 GUID가 그대로이고 중복 선언이 없는 것을 확인했다. MainMenu Canvas 전체는 이동하지 않았다.

Game System Tool에서 MetaUpgradeCatalogSO를 제외하고 생성 메뉴·메인 메뉴 진입도 폐기했다. 기존 프리팹의 구형 컴포넌트는 타입/GUID 호환용으로만 남긴다. 구매/수치 적용/구형 통계 UI 계산은 비활성이고, 원본 계산 코드는 WIP의 `#if false` 영역에 보존했다. 옛 저장 필드와 enum은 삭제·재번호 부여하지 않았다.

현재 성장과 저장 경로인 PlayerStatsRuntime, 퀘스트 영구 보상, Profile.experience, 실제 프로필 파일을 초기화하거나 다른 저장 구조로 이전하지 않았다.

## 패시브 설정의 단일 원본

- **PassiveModuleRules:** 등급별 재료, 리롤 소모 수량, 패시브 분해 시간, 적 기본 방호·후보 풀.
- **GameSystemRules.recycling:** 분해기 칸 수와 무기·방탄 레시피 10개. 패시브 중복 레시피 5개는 제거했다.
- **PassiveItemSO:** 아이템별 분해 수량. 기본 1, 불활성 2를 그대로 사용한다.

시간제 패시브 분해는 PassiveModuleRules에서 리롤과 같은 재료를 읽어 레시피를 만든다. 사용하지 않는 Dismantle Amount 필드와 즉시 분해의 비활성 본문을 정리했다. 즉시 분해 API 자체는 실패 안내만 반환하는 호환 경로로 남긴다.

기존 큐의 outputGuid/outputAmount/remaining/duration은 변경하지 않는다. 공용 원본을 수정하면 이후 등록하는 작업에 적용된다. 툴의 한글 필드명·설명과 원본 열기 버튼도 정리했다.

## 확인

- 격리 Unity 참조 컴파일: 런타임 오류 0/경고 38, 에디터 오류 0/경고 0.
- WIP 호환/단일 원본 검사 35개, 게임 시스템 2,647개, 모듈 4,257개, 에셋 계약 220개 통과.
- 스태미나 73개와 상태이상 상호작용 17개 회귀 검사도 통과.
- Native EditMode 테스트 소스 추가·컴파일. Unity 내부 실행/재임포트/Play Mode는 미실시.
- dotnet --no-restore는 기존 project.assets.json 부재로 NETSDK1004. 대신 격리 Unity 컴파일러 검증을 수행했다.

주요 수정: PassiveModuleRulesSO, GameSystemRulesSO, PlayerInventoryRuntime.Recycling/PassiveModules, 공용 SO 2개, Game System Tool/Inspector, UIMainMenu, Save의 구형 필드 안내, 격리 컴파일 도구와 회귀 테스트. 이동한 파일 전체는 WIP 폴더에서 확인할 수 있다.
