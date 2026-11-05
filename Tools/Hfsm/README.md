# HFSM: 사용법과 코드 수정 위치

기존 프리팹을 새 것으로 바꿀 필요는 없습니다. 같은 `StateMachine` 컴포넌트가 공통 실행기를 사용하고, 기존 상태·행동·조건 SO와 Coordinator 연결을 그대로 읽습니다.

## 그래프를 처음 열 때

1. `Tools > Authoring > HFSM Transition Graph`를 엽니다.
2. `Enemy Base`를 누르거나 `캐릭터` 칸에 캐릭터 프리팹/씬 인스턴스를 넣습니다. Project/Hierarchy에서 선택한 뒤 `Use Selected`도 가능합니다.
3. 왼쪽은 실제 계층 목록, 가운데는 상태/전환, 오른쪽은 선택한 상태의 행동 또는 전환 조건입니다. 패널 경계는 드래그로 폭을 조절합니다.
4. 가운데는 **현재 계층만** 표시합니다. 내부 상태가 있는 카드 더블클릭/`다음 계층 보기`로 내부에 들어가며 상단 `최상위 · 1계층`/경로 버튼으로 나옵니다. 왼쪽 삼각형은 목록만 접고 펼치며 가운데에 안쪽 상태를 한꺼번에 펼치지 않습니다.
5. 빈 공간을 클릭하면 현재 테이블 설정을 봅니다. `Validate`로 연결/조건을 검사하고 `Save`로 표시된 HFSM 테이블·상태 에셋을 저장합니다. 작업 씬이나 캐릭터 프리팹을 저장하는 버튼은 아닙니다.

### 전환 연결/편집

- 상태 우클릭 -> `Make Transition` -> 목적 상태 클릭.
- 기본은 **현재 계층의 모든 화살표를 표시**합니다. 상태 선택은 관련 선을 강조할 뿐 다른 선을 숨기지 않습니다. 복잡할 때만 `전체 전환 보기`를 꺼 관련 선만 봅니다.
- 상태 이름 뒤 `_숫자`는 계층 깊이입니다. 예: `Alive_Shooter_1`, `Patrol_EnemyShooter_2`. 최상위는 1, 안으로 들어갈 때마다 1씩 증가합니다. 왼쪽 목록·노드·전환 대상·오른쪽 설정에 같은 표기를 사용하며 에셋 이름/GUID는 바꾸지 않습니다. 실행기 선택도 Parent/Child 타입 대신 `N계층 · 테이블 / 캐릭터`로 표시합니다.
- 캐릭터를 선택한 채 안쪽 실행기를 직접 골라도 실제 연결을 따라 2/3계층 번호를 유지합니다. SO 테이블만 열면 외부 Coordinator 연결을 알 수 없으므로 그 테이블의 최상위를 1로 표시합니다. 전체 계층은 캐릭터 프리팹을 넣어 확인하세요.
- 선은 카드 내부를 통과하지 않는 직각 통로로 배치합니다. 출발/도착 포트를 나눠 왕복·동일 대상·자기 전환을 떨어뜨리며, 이미 사용한 통로를 피하도록 경로를 계산합니다. 원래 작성 행마다 별도 경로가 있으며 전환을 합치거나 순서를 바꾸지 않습니다. 모든 선을 표시하는 복잡한 그래프에서는 일부 교차가 남을 수 있습니다. 수동으로 카드끼리 겹치면 통로를 확보할 수 없으므로 카드를 옮기거나 `배치 정리`를 사용하세요.
- 선 위에는 긴 조건명 대신 짧은 `#행 번호` 배지만 표시합니다. 전환 10개 초과 화면에서는 선택한 상태의 관련 선·선택/마우스를 올린 선에만 번호가 표시됩니다. 화살표 자체는 계속 모두 표시합니다. 배지는 상태 카드/다른 배지와 겹칠 자리에는 표시하지 않습니다. 해당 선이나 오른쪽 목록으로도 같은 전환을 선택할 수 있습니다.
- 화살표, 번호 배지 또는 오른쪽의 나가는/들어오는 전환 목록 클릭 -> 목적 상태, 조건, 우선순위 편집. 목록은 우선순위 내림차순, 같으면 실제 작성 행 번호 순입니다. 실제 데이터 행 순서는 바꾸지 않습니다.
- 오른쪽 `전환 보기` 탭에서 대상 상태를 확인하고 `조건 N개 펼치기`로 전체 조건명을 봅니다. `행동 보기` 탭은 이 상태의 행동 SO, 설명과 실행 순서를 따로 표시합니다. 긴 이름과 설명은 패널 폭에 맞춰 줄바꿈하고 실제 글 높이를 계산합니다. 표시용 줄바꿈으로 에셋 이름/작성 데이터가 바뀌지는 않습니다.
- 접힌 부모 카드에 연결된 `↗` 선은 실제 자식의 계층 경계 전환일 수 있습니다. 선 선택 시 실제 출발/도착 상태가 나옵니다. 내부 화면의 `상위 / 다른 계층` 연결점도 같은 실제 행을 표시합니다. 자식의 내부 전환을 가짜 부모 자기 전환으로 표시하지 않습니다.
- 화살표 우클릭 -> `Reconnect Target` 또는 `Delete Transition`.
- `Ctrl+Z`/Redo 지원. 기존 에셋 자체를 지우는 기능이 아닙니다.
- 숫자가 높은 Priority를 먼저 평가합니다. 같으면 기존 작성 행 순서입니다. 노드 배치 순서는 실행 순서와 무관합니다.
- 조건 없는 기존 전환은 실행되지 않습니다. 무조건 전환이 필요할 때만 `Always`를 켭니다.
- 다른 테이블 사이의 연결은 실제 Coordinator 연결입니다. 일반 상태 전환선으로 바꾸거나 자동 병합하지 않습니다.

### 화면 조작 / 실행 확인

- 휠: 확대/축소, 가운데 버튼 드래그: 이동, `F`: 선택 상태를 가운데로. 기본은 읽을 수 있는 카드 크기(최소 80%)를 유지하며 좁은 창에서는 이동해 봅니다. `전체 보기`는 현재 계층을 축소한 개요, `읽기 크기`는 읽을 수 있는 배치로 돌아갑니다.
- 기본 카드는 300×120, 카드 사이 가로 통로 270 / 세로 통로 150으로 넓혔습니다. 창 폭을 줄여도 자동으로 한 줄로 압축하지 않습니다. 새 배치의 로컬 키는 `.routed.v1`이며 이전 밀집 배치 설정은 따로 보존합니다. 선 경로는 카드 위치/연결이 바뀔 때만 다시 계산하고, 선택·강조·확대·이동에는 캐시를 사용합니다.
- 노드 드래그는 레이아웃만 바꿉니다. 부모 관계나 실행 데이터를 바꾸지 않습니다.
- `목록 펼치기`/`목록 접기`, 개별 목록 접기와 노드 위치는 로컬 EditorPrefs에 저장됩니다. `배치 정리`는 현재 계층의 화면 위치만 초기 배치로 돌립니다. 기존 인라인 배치의 로컬 설정은 별도로 남겨 둡니다.
- Play Mode에서는 **Hierarchy의 실행 중인 캐릭터**를 선택합니다. 활성 상태는 초록색으로 표시하며 데이터 편집은 잠깁니다. 멈춰 있는 프리팹 에셋에는 실행 강조가 없습니다.

### 공유 전환 규칙 / 일반 Shooter 정리

`공유 규칙`은 BT 실행 노드나 새로운 상태/계층이 아닙니다. 여러 전환 행이 **같은 도착 상태·조건 순서·조건 결과/AND/OR·종료 사유**를 참조하는 작성용 SO입니다. 실제 실행은 각 출발 상태의 기존 행에서 일어납니다. 출발 상태, 행 위치, 우선순위는 공유 규칙이 아니라 **각 사용처**가 소유합니다. 조건을 미리 모두 계산하거나 검사 순서를 새로 정하지 않습니다.

- `규칙 정리 (공유 / 개별)`은 공유 규칙이 있는 현재 계층을 `적용 상태 → 규칙 → 도착 상태`로 표시합니다. 같은 목적지별로 짧은 가지와 하나의 화살표를 써 연결의 교차를 줄입니다. 폭이 좁으면 가로 스크롤, 길면 세로 스크롤로 봅니다. 이 보기에서 카드의 세로 순서는 검사 순서가 아닙니다.
- 공유 카드 클릭 → 오른쪽에서 적용 테이블/상태, 검사 위치와 수정 영향 범위를 확인하고 공통 조건을 편집합니다. `이 공유 규칙 SO만 저장`은 그 규칙 에셋만 저장합니다. 위쪽 `Save`는 열린 HFSM 테이블·상태·연결된 규칙 SO를 저장하며 씬/프리팹은 저장하지 않습니다.
- 왼쪽 상태 선택 → 나가는 전환의 **검사 1, 2, 3...** 목록에서 실제 우선순위를 확인합니다. 높은 Priority 먼저, 같으면 원래 행 순서, 첫 성공 전환만 선택합니다. 개별 완료/취소 조건도 원래 위치에 남습니다.
- `전체 전환선 (원본)`은 모든 원래 전환을 별도 선으로 보여줍니다. 상태 추가·전환 추가·연결은 기존 방식으로 하며 공유 사용처의 목적지를 다시 연결하면 해당 행만 공유에서 분리합니다.
- 개별 전환에서 `이 내용으로 공유 규칙 만들기...`는 완전히 동일한 전환만 명시적으로 묶습니다. `이 사용처에만 공유 규칙 연결`은 선택한 한 행의 내용을 바꾸는 작업이므로 확인 창을 띄웁니다.
- 공유 사용처에서 `공유 해제 → 현재 내용을 개별 전환으로 복사`는 **현재 규칙 값**을 복사합니다. 남겨 둔 이전 인라인 스냅샷으로 돌아가지 않습니다. 공유 SO가 없어지면 오류로 표시/컴파일을 중단하며 옛 값으로 몰래 실행하지 않습니다.

일반 Shooter의 이전 대상은 `Enemy_ShooterBase_Alive.asset` 한 개입니다. 현재 33행 중 동일한 선택 조건 24행을 6개 규칙(근접 선택·투척 선택·사격 선택·추적 선택·조사 선택·순찰 복귀)으로 정리하고, 서로 다른 완료 조건 9행은 개별로 유지합니다. 따라서 작성 지도는 15카드지만 실제 실행 전환은 여전히 33개입니다. `Mode`의 Spawning/Alive/Dead, Alive의 7개 상태, 근접 동작, Coordinator, 프리팹과 전투 컨트롤러는 이 정리에서 바꾸지 않습니다. 아직 Butcher 전용 근접 분리를 한 것은 아닙니다.

새 작성 코드는 `SharedTransitionRuleSO`(설정), `HfsmSharedRuleAuthoring`(생성/연결/해제), `HfsmRuleMap`(표시 그룹), `HFSMTransitionGraphWindow.Rules.cs`(규칙 UI)로 책임을 나눕니다. `HfsmSharedRuleMigration`은 메뉴/자동 실행 없는 명시적 이전 API이며 저장된 테이블만 받습니다. 메모리 비교를 먼저 하고 원본 asset/meta를 Library에 백업한 뒤 해당 테이블과 새 규칙 SO만 저장합니다. 실제 상태를 삭제하거나 행을 재배열하지 않습니다.

검사: `HfsmSharedRuleTests`는 메모리 SO로 32개 입력 시퀀스의 조건 준비/단락 평가·행동 Awake/Enter/Update/Exit/Cancel·재활성화/초기화 추적, 적용 범위, 개별 공격 완료 조건, 캐릭터별 인스턴스 분리, 연결 해제를 비교합니다. 최초 v1 검사는 메모리 사본/UI 검사가 통과한 뒤 위 한 테이블을 이전했습니다. 현재 `HfsmSharedRuleVerificationRunner.Run()`의 기본값은 **읽기 전용**이며 이미 이전된 테이블을 검사합니다. `allowMigration: true`를 명시할 때만 이전 API를 호출하므로 새 이전은 저장·Play Mode/Prefab Mode 종료 후 명시적으로 요청한 경우에만 실행하세요. v1/v2 보고서는 `Library/HfsmRefactor-20260929/shared-rules-verification-v*.txt`, 원본 백업은 같은 폴더의 `SharedRuleBackups/`입니다. 실제 Play Mode의 NavMesh·발사체·애니메이션 회귀 검사를 대체하지 않습니다.

2026-10-02 완료: 공유 규칙 6개 생성 / 24행 연결 / 개별 9행 보존. v1은 이전과 실제 Alive 화면까지 통과했지만 검사 창이 끝나기 전에 닫혀 290 passed / 1 failed로 남습니다. 이를 숨기거나 원본 보고서를 덮어쓰지 않았습니다. 읽기 전용 후속 v2는 **405 passed / 0 failed**이며, 800폭 창의 패널·카드 겹침, 공유/개별 표시, 사용처/검사 순서, 기본 화살표, 원본 33개 연결, 실제 계층, 작업 씬/프리팹/전체 작성 파일 보존까지 확인했습니다. 파일 기준 검사도 33행 전체의 실행 정의와 인라인 스냅샷이 이전 직전 백업과 같음을 확인했습니다. 임시 `HfsmSharedRuleVerificationOnce` 소스/meta는 검사 완료 후 제거했습니다. 규칙 표시 이름은 별도 `_displayName`에 저장하고 원본 파일 이름과 `m_Name`은 맞춰 Unity의 이름 불일치 경고를 피합니다.

#### 현재 공유 규칙의 정확한 적용 범위

아래 상태 이름은 읽기 편하게 접미사 `_EnemyShooter`를 생략했습니다. 여기서 `공유`란 설정 공유이며 캐릭터의 타겟/타이머/진행 상태 공유가 아닙니다. UI의 사용처 개수는 **전환 행 개수**이지 이 규칙으로 스폰될 몬스터 개수가 아닙니다. 이 Alive 테이블을 사용하는 여러 캐릭터에는 함께 적용되지만, 자체 테이블을 가진 Player/보스가 자동으로 같은 규칙을 쓰는 것은 아닙니다.

| 공유 규칙 | 원래 조건 SO (True 필요) | 적용되는 출발 상태 | 목적지 |
| --- | --- | --- | --- |
| 근접 공격 선택 | `ShouldUseMeleeCondition` | Idle / Patrol / Investigate / ChaseTarget / AttackTarget | Melee |
| 투척 공격 선택 | `ShouldUseThrowableCondition` | Idle / Patrol / Investigate / ChaseTarget / AttackTarget | Throwable |
| 사격 선택 | `CanAttackTargetCondition` | Idle / Patrol / Investigate / ChaseTarget | AttackTarget |
| 추적 선택 | `ShouldChaseTargetCondition` | Idle / Patrol / Investigate / AttackTarget | ChaseTarget |
| 조사 선택 | `ShouldInvestigateCondition` | Idle / Patrol / ChaseTarget / AttackTarget | Investigate |
| 순찰 복귀 | `ShouldPatrolCondition` | ChaseTarget / AttackTarget | Patrol |

모든 위 규칙은 `Always=false`, `ExitReason=Transition`입니다. 규칙 SO에 출발 상태 전체를 소유하는 Any State 목록이나 공통 Priority를 새로 넣지 않았습니다. 사용처의 `UseSharedRule`과 `SharedRule` 참조가 적용 범위를 결정합니다.

#### 현재 상태별 실제 검사 순서

현재 33행의 Priority는 모두 0입니다. 따라서 아래 순서는 **그대로 유지된 작성 행 순서**입니다. 각 상태의 첫 성공 행만 전환하고, 성공 뒤의 행은 평가하지 않습니다. 아무 조건도 성공하지 않으면 현재 상태에 남습니다. 같은 틱에 목적지의 전환을 다시 선택해 연속으로 여러 상태를 거치지 않습니다.

| 현재 상태 | 순서대로 검사할 후보 |
| --- | --- |
| Idle | 근접 → 투척 → 사격 → 추적 → 조사 → `ShouldPatrol AND PatrolIdleFinished`이면 Patrol |
| Patrol | 근접 → 투척 → 사격 → 추적 → 조사 → `PatrolDestinationReached`이면 Idle |
| Investigate | 근접 → 투척 → 사격 → 추적 → `InvestigationFinished`이면 Patrol |
| ChaseTarget | 근접 → 투척 → 사격 → 조사 → 순찰 복귀 |
| AttackTarget | 근접 → 투척 → 추적 → 조사 → 순찰 복귀 |
| Throwable | `ThrowableActionFinished`이면 ChaseTarget |
| Melee | `MeleeActionFinished AND ShouldUseMelee`이면 Melee 재진입 → `MeleeActionFinished AND CanAttackTarget`이면 AttackTarget → `MeleeActionFinished AND ShouldChaseTarget`이면 ChaseTarget → `MeleeActionFinished AND ShouldInvestigate`이면 Investigate → `MeleeActionFinished`이면 Idle |

개별 예외 9개는 위 Idle/Patrol/Investigate/Throwable의 마지막 행 4개와 Melee의 5개입니다. 예를 들어 Idle→Patrol은 대기 완료까지 필요하므로 ChaseTarget→Patrol의 공유 `ShouldPatrol` 하나로 합치지 않았습니다. Melee도 완료되기 전에는 공통 사격/추적 후보로 바뀌지 않습니다. Melee의 첫 행은 다음 근접 공격을 위한 원래 자기 전환이며 이를 삭제하거나 단순 유지 상태로 바꾸지 않았습니다.

#### 조건 / 행동 / 실행기의 수정 경계

- `조건`: **언제** 바뀌는지. 예: `CanAttackTarget`은 단순 사거리만이 아니라 유효 총/탄약·근접 범위 제외·사격/엄폐/버스트 유지까지 포함한 기존 판정입니다. 정확한 게임 판정은 `EnemyShooterCombatController.Queries.cs` 및 관련 Domain을 읽습니다.
- `행동`: 해당 상태에서 **무엇을** 하는지. 상태 SO의 Actions 순서대로 수행합니다. 공유 규칙으로 발사·이동·투척·피해 처리 코드를 옮기지 않았습니다.
- `공유 규칙`: 위 조건 SO 참조·도착 상태·종료 사유를 여러 사용처가 한 번 작성해서 재사용하는 설정입니다. 새 타이머/캐시/캐릭터 필드나 새로운 실행 Tick이 없습니다.
- `실행기`: 기존 전환 검사와 진입/업데이트/종료를 수행합니다. 이번 작업에서 HfsmRuntime/StateMachine/Coordinator와 전투 컨트롤러는 수정하지 않았습니다.

기존 조건에는 인지 갱신, 총 슬롯 활성화 시도, 조사 단서 정리 등 준비/판정 부수효과가 있습니다. **후보 조건을 프레임 처음에 전부 미리 평가하지 않습니다.** 필요한 행을 원래 순서에서 검사하고 기존 캐시/AND 단락 평가를 유지합니다. 조건 결과를 어디서나 다시 읽는 실시간 디버그 기능도 추가하지 않았습니다.

이것은 BT의 전체 실행/Running/Abort/Sequence 구조를 도입한 것이 아닙니다. `공유된 선택 규칙 + 첫 성공 후보 선택`이라는 정리 방식을 HFSM 작성에 적용한 것입니다. 기존 Coordinator가 Alive에서 안쪽 머신을 켜고 Spawning/Dead에서 끄는 구조도 그대로입니다. 기존 `enabled=false`는 Suspend이지 공격 Exit/Cancel을 새로 강제하는 동작이 아니며, 재활성화/Reset 정책도 이번 정리에서 바꾸지 않았습니다.

파일 기준 재검사 (읽기 전용):

```powershell
node Tools/Hfsm/verify-shared-rules.cjs Library/HfsmRefactor-20260929/SharedRuleBackups/20261002-013042-987-Enemy_ShooterBase_Alive.asset
```

이 명령은 원본 백업의 전환 33행과 현재 공유 SO를 펼친 33행을 대조하고, 보존된 인라인 스냅샷·시작 상태·계층·테이블 스크립트·meta/GUID까지 비교합니다. 향후 의도적으로 규칙 내용을 바꾸면 원래 동작과 달라졌다는 실패가 **정상**입니다. 새 밸런스 변경을 무조건 기존 백업과 일치시키라는 의미는 아닙니다.

## 구조: 무엇을 어디서 고치나

### 2026-10-02 공격 선택 정책: 총 우선 / 전술 투척 / 버스트 완료

이번 변경은 위 공유 규칙 정리와 별개인 **명시적인 행동 정책 변경**입니다. 전환 33행의 출발/목적지/조건 SO/순서, 공유 SO 6개, 기존 계층/Coordinator/GUID는 수정하지 않았습니다. 기존 조건이 읽는 캐릭터별 판단만 강화했습니다. Player의 공격과 투척은 이 적 전용 선택 정책을 사용하지 않습니다. 공통 `EnemyShooterCombatController`를 사용하는 적/보스에는 새 판단이 적용됩니다.

1. **근접 가능하면 먼저**: 기존 검사 순서에서 근접이 첫 후보입니다. Butcher의 `UseMeleeAttack`은 그대로 켜져 있으며 일반 몹의 근접/투척 사용 여부를 새로 켜지 않았습니다. 총을 실제로 쏘기 시작한 버스트만 아래 완료 경계를 기다립니다.
2. **열린 사격각에서는 총이 기본**: 처음 발견했거나 교전 중인 타겟에게 열린 총구 경로가 있으면 투척하지 않습니다. 시야각 밖으로 나갔다는 이유만으로도 투척하지 않습니다. 선제 수류탄 확률/정책은 추가하지 않았습니다.
3. **투척할 이유**: 직접 목격한 타겟의 총구 경로가 기본 **1.5초 연속** 막혀 있어야 합니다. 각 Config의 `Throwable Blocked Fire Seconds`로 조정합니다(최소 0.1초). 경로가 열리면 누적 시간을 초기화합니다. `Throwable Recently Known Seconds` 안의 직접 목격 정보만 유효하므로 대기 시간은 목격 유효 시간보다 짧게 설정하세요. 총이 없거나 사거리 밖이라는 이유만으로는 투척 기회가 생기지 않습니다.
4. **투척할 수 있는가**: 기존 사용 설정·소지 투척물·쿨다운을 확인하고, 실제 투척 목표점 기준 거리·자기 폭발 위험·아군 안전·안전한 포물선 경로를 모두 확인합니다. 아군 레이어가 비어 있으면 `Enemy` 레이어를 자동 사용하며 레이어도 없으면 투척을 막습니다. 보이지 않는 적의 현재 Transform이 아니라 **직접 마지막으로 목격한 위치**로 던집니다. 소비 직전에도 전술적 이유/안전을 재검사하며 실패하면 소지품을 소비하지 않습니다. 경로 사전 검사는 발사체나 View를 생성하지 않습니다.
5. **현재 버스트 마무리**: 첫 발이 실제 발사되고 남은 탄이 있는 `FiringBurst` 동안에는 근접·투척으로 바꾸지 않습니다. 탄창 전체 소진이나 버스트 휴식 종료를 기다리는 것은 아닙니다. 버스트가 끝나면 다음 전환 검사에서 기존 순서대로 선택합니다. 시작 전 반응 대기, 버스트 휴식, 발사 실패는 잠그지 않습니다. 근접 거리에 들어와도 정상 사격 가능한 남은 버스트를 현재 총으로 마치며, 일반 엄폐/측면 이동 갱신이 계속 return하여 완료되지 않는 잠금도 방지합니다.
6. **중단은 즉시**: 목표 상실, 사용할 수 없는 총/탄약, 빈 탄창/재장전, 유지 사거리 이탈, 사망·행동 차단·긴급 회피는 완료 보호를 해제합니다. 가려진 뒤 남은 버스트를 쏠지는 기존 `FinishStartedBurstAfterSightLost` 설정을 그대로 따르며, 벽을 통과하는 탄환이나 무조건 계속 발사하는 새 규칙은 아닙니다.
7. **이동 사격은 AttackTarget 내부**: `StopWhileAttacking`, `RepositionBetweenBursts`, `MoveWhileFiring`의 의미는 그대로입니다. 공격 간격마다 ChaseTarget으로 왕복하도록 바꾸지 않았습니다. 실제 추적이 필요할 때만 원래 추적 조건을 사용합니다.

`EnemyShooterCombatController.Decisions.cs`는 캐릭터별 판단 연결/마지막 목격 위치, `EnemyShooterCombatDecisionPolicy.cs`는 순수 완료 규칙과 타겟별 차폐 타이머입니다. 관측은 기존 인지 갱신 후 진행하며 조건 getter 자체에 타이머 누적/난수/발사/무기 전환을 넣지 않았습니다. 타겟 변경·해제, 비활성화, 긴급 중단 때 투척 관측을 초기화합니다. SO에는 설정만 저장하고 런타임 상태는 각 캐릭터가 독립적으로 가집니다. 옛 `ThrowablePreferWhenNoLineOfSight`/`ThrowableBlockWhenCanAttack` 필드는 데이터 호환용으로 유지하지만 Inspector에서는 숨겼으며 새 필수 전술 정책을 해제하는 옵션은 아닙니다.

검사:

```powershell
node Tools/Hfsm/compile.cjs
node Tools/Hfsm/run-managed.cjs combat
```

`HfsmCombatDecisionPolicyTests`는 명시적인 시간 입력을 사용하는 순수 회귀 테스트이며 `HfsmCombatDecisionIntegrationTests`는 임시 Preview 씬/메모리 SO에서 실제 컨트롤러의 선택·장비 슬롯·안전 사전 검사·소지품 보존을 확인합니다. `HfsmCombatDecisionVerificationRunner.Run()`은 기존 실행기/공유 규칙 회귀 검사도 같이 실행하고 실제 작성 파일 해시와 작업 씬 dirty 상태를 비교합니다. 결과는 `Library/HfsmRefactor-20260929/combat-decisions-verification-v2.txt`에 기록합니다. 실제 게임의 NavMesh·발사체·애니메이션 Play Mode 검증을 대신하지 않습니다.

### 일반 적의 실제 계층과 HFSM의 의미

`Spawning`, `Alive`, `Dead`는 부모-자식 사슬이 아니라 같은 Mode FSM의 형제 상태입니다. `Alive` 안에 `Idle`(초기), `Patrol`, `ChaseTarget`, `Investigate`, `AttackTarget`, `Melee`, `Throwable` 선택지가 있습니다. 일반 적은 한 시점에 Mode 상태 하나와 Alive 행동 상태 하나를 선택합니다. 예: `Alive + ChaseTarget`. 아래 Actions 목록은 또 다른 자식 상태가 아니라 선택된 상태가 순서대로 실행하는 작업들입니다.

계층(소속)은 트리지만 전환(이동)은 그래프입니다. `Patrol -> ChaseTarget`은 형제 상태 사이 이동이지 `ChaseTarget`이 `Patrol`의 자식이라는 뜻이 아닙니다. 부모마다 여러 자식을 정의할 수 있지만 일반적인 배타적 계층에서는 각 단계의 한 자식만 활성화합니다. 병렬 영역/여러 하위 실행기는 별도 개념입니다.

현재 EnemyBasePrefab은 기존 Mode / Alive 컴포넌트를 `StateMachineCoordinator`가 연결합니다. Alive에서 행동 FSM을 켜고 Spawning/Dead에서 끕니다. 실행 순서도 Parent(-200) -> Coordinator(0) -> Child(200)를 유지합니다. 공통 `HfsmRuntime`은 네이티브 SO 계층도 지원하지만 기존 프리팹의 두 실행기를 단일 네이티브 트리로 자동 변환한 것은 아닙니다. 특히 기존 컴포넌트 비활성화는 Suspend이고 Exit이 아니므로, 단일 네이티브 HFSM의 자식 종료 정책과 혼동하지 않아야 합니다.

| 책임 | 파일/계층 |
| --- | --- |
| 계층 진입·종료·전환·중단 순서 | `Assets/Scripts/StateMachine/Core/HfsmRuntime.cs`, `HfsmNode.cs` |
| Unity 호출, 컴포넌트 캐시, 기존 공개 API | `Core/StateMachine.cs` |
| SO -> 캐릭터별 실행 인스턴스 조립 | `ScriptableObjects/StateGraphCompiler.cs` |
| 상태의 행동 목록 / 조건 / 전환 | `StateSO`, `StateActionSO`, `StateConditionSO`, `TransitionTableSO` |
| 기존 여러 FSM의 활성화/초기화 | `StateMachineCoordinator`, `StateMachineGroupCoordinator` |
| 실제 계층의 읽기 전용 설명 | `Core/StateMachineHierarchy.cs` |
| 실제 계층 모델 / 표시 투영 / 화면 / 데이터 수정 | `HfsmGraphModel`, `HfsmGraphPresentation`, `HFSMTransitionGraphWindow.*`, `HfsmGraphAuthoring` |

### 적 전투 코드

폴더: `Assets/Scripts/Gameplay/Entities/Shooter/Enemy/StateMachine/`

`EnemyShooterCombatController.cs`는 직렬화 필드와 Unity 생명주기를 유지하는 진입점입니다. 각 partial 파일의 앞부분은 기존 호출자를 위한 짧은 위임 메서드이고, 아래의 `...Domain` 클래스가 실제 기능을 담당합니다.

| 파일 접미사 | 수정할 기능 |
| --- | --- |
| `Perception` | 타겟 감지, 시야/인지 갱신, 전투 판단 |
| `PatrolChase` | 순찰·추적 행동 |
| `Investigation` | 소리/마지막 목격 위치 조사 |
| `RangedAttack` | 발사, 버스트, 쿨다운, 총구 막힘 대응 |
| `EquipmentAttacks` | 근접 공격·투척 행동 |
| `Tactics` | 엄폐·피킹·전술 이동 |
| `Evasion` | 회피/대시·위협 회피 |
| `Navigation` | 경로/목적지/이동 속도·도착 판정 |
| `Hearing` | 소리 입력과 조사 관련 처리 |
| `Status` | 상태 변화 대응 |
| `Queries` / `Utilities` | 외부 질의 / 공통 정적 보조 함수 |
| `Diagnostics` | 기존 디버그 스냅샷·테스트 오버라이드, 의사결정 경로가 아님 |

공유 런타임 필드는 아직 기존 컴포넌트에 남아 있습니다. 이번 변경은 동작과 프리팹 직렬화를 보존하기 위한 기능 분리입니다. 각 Domain이 다른 게임에 그대로 붙는 독립 패키지나 완전히 분리된 데이터 컨텍스트라는 뜻은 아닙니다. 새 기능은 관련 Domain과 작은 Action/Condition에 추가하고, 공통 실행기에 적 전용 코드를 넣지 마세요.

## 새 행동/조건 추가

1. Project에서 `Create > State Machines > Action Script` 또는 `Condition Script`를 선택합니다.
2. 행동은 `Awake`에서 캐릭터 기능 참조를 확보하고 `OnStateEnter`, `OnUpdate`, `OnStateExit`에 역할을 나눕니다. 큰 상속 계층 대신 작은 행동 SO들을 상태의 Actions 목록에 조합합니다.
3. 설정은 SO, 타이머/대상/진행 상태는 실행 인스턴스에 둡니다. 같은 SO를 두 캐릭터가 사용해도 실행 인스턴스는 별개입니다. 단, **동일 머신 안에서 같은 행동 SO는 실행 인스턴스를 공유**하므로 동시에 활성화할 부모/자식에 같은 상태ful 행동을 중복 배치하지 마세요.
4. `Statement()`는 조건 결과를 반환합니다. 기존 코드에 필요한 준비 작업은 `PrepareEvaluation()`에 둡니다. 이는 프레임당 1회가 아니라 **캐시되지 않은 실제 조건 평가 직전**이며 AND 단락 평가에서 생략될 수 있습니다.

### 종료와 중단은 다릅니다

- 기존 `enabled=false`는 **일시 중지**입니다. Exit를 새로 호출하지 않으며 활성화 시 재개합니다.
- 기존 `ResetToInitialState(true, false)`는 이전 Exit 없이 새 실행 상태를 만듭니다. Coordinator별 기존 정책을 그대로 유지합니다.
- 일반 전환/명시적 Reset 종료는 기존 `OnStateExit()`를 유지합니다.
- 새로운 `StopMachine()` / `Interrupted` / `Stopped`는 `OnInterrupted(reason)` 경로입니다. **기존 행동에 취소 처리가 자동 추가된 것은 아닙니다.** 완료 효과를 갖는 Exit를 취소 시 실행하면 실제 공격/감염 등이 잘못 확정될 수 있어 기본 취소 콜백은 비어 있습니다.
- 따라서 기존 전환의 Exit Reason은 `Transition` 그대로 두세요. 새 중단 정책을 쓰려면 해당 행동의 리소스 해제·공격 취소를 먼저 구현하고 테스트해야 합니다.

## 새로운 네이티브 계층을 만들 때

현재 Enemy Base의 실제 `Mode -> Alive -> 행동` 관계는 기존 Coordinator 기반으로 표시됩니다. 임의의 Combat/Exploration 그룹을 만들어 기존 실행을 바꾸지 않았습니다.

새 테이블에서는 상태 우클릭 `다음 계층 상태 만들기...`로 실제 하위 상태를 생성할 수 있습니다. 첫 자식은 초기 자식으로 등록됩니다. `시작 상태`와 `SO 내부 계층 (고급)`은 실제 실행 데이터입니다. 이름 뒤 계층 숫자는 표시용이며 이 실행 데이터를 수정하지 않습니다.

- 네이티브 계층은 바깥 -> 안쪽 진입, 안쪽 -> 바깥 종료.
- 부모 전환을 먼저 평가하고 한 Tick에서 하나의 전환만 수행.
- 형제 전환은 공통 부모를 다시 진입하지 않음. 자기 전환은 재진입.
- 기존 여러 MonoBehaviour 실행기를 단일 네이티브 계층으로 합치면 프레임 호출 순서가 달라질 수 있으므로 자동 변환하지 않음.

## 검사와 한계

Unity: `Tools > Authoring > Verification > Run HFSM Compatibility Tests`.

보고서: `Library/HfsmRefactor-20260929/unity-verification.txt`, `ui-verification-v*.txt`, `graph-level-ui-v*.txt`.

`HfsmGraphPresentationTests`는 메모리 SO와 표시용 창으로 계층 경계·선 필터·기본 화살표 표시·1~3계층 이름 표기·행 보존·패널 폭·긴 이름의 줄바꿈·번호 배지의 겹침 방지·직각 경로와 카드 회피를 검사합니다. `HfsmGraphUiVerificationRunner.Run()`은 Player·일반 적·보스 11개 프리팹을 읽기만 하며 별도 창에서 조작을 확인합니다. 글로벌 Undo·씬 객체 생성·프리팹/씬 저장은 하지 않습니다. 2026-10-02 v4 검사: 359개 검사항목 통과 / 실패 0. 실제 Alive의 원래 전환마다 별도 경로 유지, 카드 관통 방지, 전체 화살표와 선택 관련 번호 표시, 경로 캐시 재사용, 확대/축소 버튼, 계층 번호, 패널과 원본 데이터 보존을 확인했습니다. 번호 배지 표시가 줄어 v3보다 반복 배지 검사항목 수도 줄었습니다. 별도 관리 메모리의 경로 검사 14개와 실행기 호환성 13개도 통과했습니다. 임시 자동 실행 파일은 검사 후 Assets에서 제거합니다. 전체 호환성 메뉴에는 글로벌 Undo 테스트가 있으므로 중요한 작업 씬이 열린 상태에서 자동 실행하지 마세요.

PowerShell (프로젝트 루트):

```powershell
node Tools/Hfsm/compile.cjs
node Tools/Hfsm/run-managed.cjs tests
node Tools/Hfsm/run-managed.cjs routing
node Tools/Hfsm/run-managed.cjs extract --verify
node Tools/Hfsm/baseline.cjs
```

- 도구는 이 체크아웃의 Unity 설치 경로와 Bee 응답 파일을 사용합니다. Unity 버전/경로가 바뀌면 경로를 갱신해야 합니다.
- `extract --verify`는 초기 원본 스냅샷과 기존 메서드/필드를 대조합니다. 스냅샷은 Library에 있습니다. `--format`을 추가한 경우에만 소스를 정규화합니다. 추출 자체를 재실행하는 명령은 일상 작업용이 아닙니다.
- `baseline.cjs`는 첫 실행 때 해시를 기록하고 이후 비교합니다. Library 삭제 후 새 기준을 만들면 과거 무변경 증거가 복원되는 것은 아닙니다.
- 자동 검사가 통과해도 실제 RUNScene의 NavMesh/애니메이션/발사체/보스 패턴을 모두 재현한 것은 아닙니다. 릴리즈 전에 순찰 -> 추적 -> 공격 -> 사망 -> 재활성화와 Player/각 보스의 Play Mode 회귀 확인이 남아 있습니다.

## 2026-10-02 · 캐릭터 루트 컴포넌트 통합

EnemyBasePrefab 및 Player는 `ParentStateMachine + ChildStateMachine + StateMachineCoordinator`를
**`HfsmStateMachine` 한 컴포넌트**로 교체합니다. Enemy Base를 상속하는 일반 적·Butcher·보스도 같은 연결을 상속합니다.
상태/전환/공유 규칙 SO의 GUID와 내용은 변경하지 않습니다. 기존 루트 컴포넌트의 fileID도 보존합니다.
WIP Mutarus/기타 구형 생물 프리팹은 아직 기존 실행기·Coordinator를 사용합니다. 남은 사용처가 있어 구형 스크립트/meta를 삭제하지 않습니다.

### 사용법

1. 캐릭터 루트의 **HFSM State Machine → HFSM 그래프 열기**를 누릅니다. 기존 HFSM Transition Graph 창을 사용하며 새 Tools 메뉴를 만들지 않습니다.
2. 최상위에서 `Spawning_Shooter_1 / Alive_Shooter_1 / Dead_Shooter_1`을 봅니다. Alive 더블클릭으로 2계층에 들어갑니다.
3. 상태 클릭 → 기존 행동·실제 전환 순서를 확인합니다. 공유 규칙 정리와 원본 화살표 보기는 그대로 사용할 수 있습니다.
4. **계층 연결 설정 보기**에서 시작 상태·Alive 안의 행동 테이블·재진입 방식을 관리합니다.
5. Play Mode에서 루트 Inspector와 그래프에 `Alive_1 > Patrol_2` 같은 실제 활성 경로가 표시됩니다. 상태 파일 이름에 `_1/_2`를 붙여 GUID나 파일을 바꾼 것은 아닙니다.

그래프 SO 위치:

- `Assets/ScriptableObject/StateMachine/Shooter/EnemyShooter/EnemyShooter_HFSM.asset`
- `Assets/ScriptableObject/StateMachine/Shooter/Player/Player_HFSM.asset`

그래프 SO는 **계층 연결 지도**입니다. 전환 테이블을 복제하지 않고 기존 Mode/Alive 테이블을 참조합니다.
1계층 시작은 Enemy=Spawning, Player=Alive이며, Alive 내부 시작은 각각 기존 Idle입니다.
내부 연결의 `OwnerScope=0`은 최상위 실행 단위입니다. 배열 i번째 연결은 i+1번 실행 단위이며,
앞에 정의된 실행 단위의 상태 안으로만 연결합니다. 따라서 3계층 이상도 연결 가능하고 순환 소속은 검증 오류입니다.
각 실행 단위 내 native SO 계층도 지원합니다. 실제 상태 깊이를 더해 표시하며 시작 행동은 해당 단위의 테이블에 있어야 합니다.

### 동작 보존 규칙

| 단계 | 실행 시점 | 역할 |
| --- | --- | --- |
| 1계층 검사/행동 | -200 | 기존 Parent Update와 같은 위치. Spawning/Alive/Dead 전환 후 목적지 행동 실행 |
| 내부 활성 연결 판정 | 0 | 기존 Coordinator와 같은 위치. 소속 상태가 활성인지 읽고 Suspend/Resume만 결정 |
| 활성 내부 검사/행동 | +200 | 기존 Child Update와 같은 위치. 원래 우선순위·작성 행 순서로 첫 성공 전환 선택 |

세 단계를 한 Update로 당기면 다른 전투·입력 MonoBehaviour에 대한 실행 순서가 바뀝니다.
그래서 `HfsmPhaseScheduler`가 **런타임 공용 서비스 한 개**로 phase 0/+200을 수행합니다.
캐릭터 프리팹에는 Coordinator/추가 드라이버를 붙이지 않습니다. 서비스는 자동 생성·등록되며 씬/프리팹에 저장하지 않습니다.

- 단위당 한 Tick에 전환 하나입니다. 1계층과 2계층은 기존처럼 같은 프레임에 각각 전환할 수 있습니다.
- 사망 전환은 -200에서 먼저 처리하고, 0에서 내부를 비활성화하므로 그 프레임 +200에서 공격을 새로 검사/실행하지 않습니다.
- **기존 비활성화는 Exit/Cancel이 아니라 Suspend**입니다. 사망 행동의 기존 취소 처리도 그대로 두며 내부 Attack의 Exit을 새로 호출하지 않습니다.
- `이전 행동 이어서`는 재활성화 시 추가 Enter 없이 이전 상태/행동을 이어갑니다. Enemy/Player가 이 설정입니다.
- `시작 행동부터`는 기존 no-exit reset 정책처럼 새 실행 인스턴스를 만들고 시작 행동으로 진입합니다. 현재 이전된 프리팹에서는 사용하지 않습니다.
- explicit `StopMachine(reason)`만 내부 → 바깥 순서로 해당 종료 사유를 전달합니다. `ResetToInitialState(..., exitCurrentState: true)`도 내부부터 종료합니다.
- 조건은 필요한 행에서만 준비/평가합니다. 프레임 시작에 후보 조건을 전부 읽는 새 BT 평가나 새 공격 우선순위를 추가하지 않습니다.
- SO 작성 값은 공유하지만 **상태·행동·조건 캐시·진입 시간은 캐릭터 × 실행 단위마다 별개**입니다. 같은 SO가 두 단위에 있으면 실행 인스턴스도 둘입니다.
- 비활성화 기간에도 기존 `Time.time` 기반 진입 경과 시간은 계속 흐릅니다. 새 일시정지 시계를 도입하지 않습니다.
- 새로운 3계층 연결도 phase 0에서 직전 내부 상태를 기준으로 활성 여부를 판정합니다. +200에서 바뀐 2계층 상태는 다음 phase 0에 반영됩니다. 기존 다중 Coordinator 실행 시점을 보존하기 위한 규칙입니다.

### 코드를 고칠 위치

| 변경 목적 | 파일 |
| --- | --- |
| 캐릭터에 붙는 단일 호스트 / 초기화 / 전체 중단 | `Assets/Scripts/StateMachine/Core/HfsmStateMachine.cs` |
| 내부 계층별 상태·타이머·캐시·재진입 | `Core/HfsmScopeRuntime.cs` |
| Unity 프레임 단계 | `Core/HfsmPhaseScheduler.cs`, `Core/HfsmGatePhase.cs`, `Core/HfsmBehaviourPhase.cs` |
| 계층 연결 데이터·검증 | `ScriptableObjects/HfsmDefinitionSO.cs` |
| 순수 Enter/Tick/Exit 알고리즘 | `Core/HfsmRuntime.cs` (이 통합 작업에서는 변경 없음) |
| 기존 SO → 계층별 실행 객체 변환 | `ScriptableObjects/StateGraphCompiler.cs` |
| 행동/조건에서 현재 소속 계층 읽기 | `Core/StateMachineContext.cs`, `Core/IHfsmScope.cs` |
| 그래프 투영·Inspector | `Editor/HfsmGraphModel.cs`, `Editor/HfsmStateMachineEditor.cs`, `Editor/HFSMTransitionGraphWindow.*.cs` |

기존 `Awake(StateMachine)`는 유지됩니다. 컴포넌트 조회는 기존 코드 그대로 사용할 수 있습니다.
새 행동/조건이 현재 계층 상태·시간을 읽어야 하면 **`Context.CurrentState`, `Context.ElapsedSeconds`, `Context.Scope.Level`**을 사용하세요.
`Context.Owner.CurrentStateSO`는 호스트의 1계층 상태입니다. 내부 Patrol/Attack 상태와 혼동하지 마세요.
캐릭터 전체 조회는 `GetActiveState(계층)`, `CurrentLeafState`, `ActiveStatePath`를 사용합니다.
Player의 공격/투척은 여전히 기존 입력·무기 컴포넌트 소유입니다. Player Alive에는 Idle/Move/Dash/Knockback만 있으며 적 공격 HFSM으로 합치지 않습니다.

### 검증과 제한

`HfsmUnifiedHostTests`는 구형 실행 순서와 새 실행 순서의 추적 비교, 같은 프레임 전환,
사망 선점, Suspend/Resume, no-exit 재진입, explicit Stop/Reset 종료 순서,
계층/캐릭터별 인스턴스와 조건 캐시 분리, 3계층 및 native 계층, 저장된 변형 프리팹 연결을 검사합니다.
`HfsmUnifiedVerificationRunner.Run()`은 이 테스트와 기존 실행기·공유 규칙·공격 선택·그래프 검사를 수행합니다.
전역 Undo 테스트는 자동 실행하지 않으며 실제 작업 씬의 멤버/dirty 상태와 작성 파일 보존도 대조합니다.

증거: `Library/HfsmUnified-20261002/unified-tests.txt`, `graph-ui.txt`, `asset-audit.txt`.
`before-prefab-0.yaml` / `before-prefab-1.yaml`은 **이번 이전 직전 저장본**입니다. Git HEAD가 아니라 사용자의 최신 저장 내용을 기준으로 합니다.
`node Tools/Hfsm/verify-unified.cjs`는 기존 파일 중 기본 프리팹 두 개만 달라졌는지 검사합니다.
`--baseline`은 최초 이전 전에만 사용합니다. 이후 기준을 다시 찍어 보존 증거를 덮어쓰지 마세요.

EditMode 검사는 실제 NavMesh 이동·발사체·애니메이션 교전을 대체하지 않습니다.
실제 Play Mode에서 일반 적/Butcher/보스 및 Player의 사망·풀 재활성화·넉백/대시 회귀 확인은 별도로 필요합니다.

### 2026-10-02 · 내부 규칙 카드 가독성

내부 공유 규칙·개별 예외·목적지 상태에도 최상위 상태와 같은 명시적 사각 배경/테두리를 그립니다.
그래프의 밝은 글자색은 Unity 테마에서 가져오지 않습니다. 최상위 상태 제목에도 같은 기준을 적용합니다.
표시용 줄바꿈/높이만 조정하며 실제 SO 이름·전환 행·검사 순서·실행 계층은 바꾸지 않습니다.

- `Editor/HfsmGraphPresentation.cs`: 카드 색상/글자색/테두리 공통 정의.
- `Editor/HFSMTransitionGraphWindow.Rules.cs`: 내부 규칙 지도 카드의 배치/표시/기존 선택 동작.
- `Editor/HFSMTransitionGraphWindow.Canvas.cs`: 최상위 상태 카드 표시.
- `graph-ui-readability-v2.txt`: 기존 클릭/레이아웃 268개 통과. 화면 밖 조건 펼치기 버튼 클릭에서 실패.
- `unified-tests-v2.txt`: 101개 통과. 메모리 SO의 Undo 기록이 열린 씬의 dirty 표시를 바꾼 실패를 보존함.
- 메모리 SO의 Bind/Detach는 전역 Undo를 사용하지 않도록 분리했습니다. 실제 에셋의 Undo/dirty 동작은 유지합니다.
- v3 검사 결과는 `unified-tests-v3.txt` / `graph-ui-readability-v3.txt`에 별도 기록합니다. **아직 실행 완료되지 않았으므로 통과로 간주하지 않습니다.** 일회 자동 실행 파일은 백팩 작업과 검사가 섞이지 않게 정리했습니다. 재검사는 저장 후 Play/Prefab Mode 밖에서 재사용 가능한 두 Runner의 `Run()`으로 명시적으로 실행합니다.

기존 이전 기준과의 파일 감사에는 이후 저장된 Scraper 외형 Config/프리셋도 차이로 잡힙니다.
이 차이를 지우거나 이전 기준을 덮어쓰지 않습니다. 각 Unity 검사는 실행 직전 파일·씬 상태를 따로 비교합니다.
