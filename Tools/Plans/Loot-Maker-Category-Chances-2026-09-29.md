# Loot Maker: 등급 → 카테고리 → 아이템 확률

## 사용법

1. 상자를 선택하고 `Loot Items`에서 원하는 등급을 펼친다.
2. `Custom category chances`를 켠다. 설정은 해당 상자의 독립 테이블과 해당 등급에만 적용된다.
3. 각 카테고리의 `Category Weight`를 조절한다. 예: Armor 70, Passive 30이면 해당 등급 안에서 각각 70%, 30%다. 숫자는 상대 비중이며 합계가 반드시 100일 필요는 없다.
4. 카테고리를 펼쳐 아이템을 편집한다. Custom 모드에서는 자동 아이템이 카테고리 안의 남은 비중을 나눠 갖고, 개별 수동 확률도 그 카테고리 안에서 계산된다.
5. `This box: ...% per filled slot`에서 최종 확률을 확인하고 `Save`한다. 등급 비중 자체는 기존 `Box Settings`에서 조절한다.

카테고리의 아이템 목록을 접어도 비중 입력과 실제 환산 확률은 보인다. 기존 등급/카테고리 접기와 이름 정렬은 유지한다.

## 추첨 규칙

- 생성할 칸 수를 정한 뒤, 각 칸마다 등급 → 그 등급의 카테고리 → 그 카테고리의 아이템을 각각 새로 추첨한다. 앞 단계가 다음 단계의 후보를 정하는 조건부 추첨이며, 칸 사이에는 복원 추출이므로 같은 아이템이 다시 나올 수 있다.
- 선택된 칸에 별도의 실패 확률을 다시 적용하지 않는다.
- 예: Normal 80% × Armor 70% × Armor A 25% = 전체 생성 칸 기준 Armor A 14%.
- 후보가 없거나 카테고리 비중이 0인 그룹은 제외하고 나머지를 재정규화한다. 아이템 비중이 전부 0인 그룹도 제외한다.
- 해당 등급에 유효 카테고리가 하나도 없으면 그 등급을 제외한다. 전체 후보가 없으면 잘못된 설정으로 생성에 실패하며, 이를 정상적인 빈 상자 결과로 저장하지 않는다. 사용자가 지정한 생성 칸 범위의 최소 0 설정은 그대로 지원한다.
- 아직 비중을 설정하지 않은 새 카테고리는 Custom 모드에서 비중 1을 사용한다. 아이템이 사라진 카테고리의 저장된 규칙은 후보가 생기기 전까지 무시한다.
- 카테고리 분류는 Item Maker와 동일한 런타임 분류 키를 사용한다. 무기/탄약은 기존 세부 분류 기준이다.

## 기존 데이터와 호환

- 기존 테이블은 기본 `Auto` 상태다. 보기만 해서는 새 규칙을 만들거나 에셋을 수정하지 않는다.
- Auto는 기존 등급 내 아이템별 확률의 합을 카테고리 비중으로 계산해, 기존 아이템의 최종 확률을 유지한다. 아이템 수가 많은 카테고리가 더 자주 선택되는 기존 동작도 유지된다.
- Custom을 처음 켤 때 현재 카테고리별 환산 비중을 초기값으로 넣는다. 이미 작성한 비중은 Auto/Custom을 전환해도 보존된다.
- **수동 아이템 비중이 있으면 Custom 활성화 후 확인이 필요하다.** Auto의 등급 전체 기준에서 Custom의 카테고리 내부 기준으로 해석이 달라지며, 기존 수동 값 자체를 자동으로 고치지는 않는다. UI 안내와 최종 아이템 확률을 확인한다.
- 카테고리 추첨에 난수를 추가로 쓰므로 같은 시드에서 새로 생성되는 구체적인 결과 순서는 이전 버전과 다를 수 있다. 이미 생성해서 저장된 상자를 다시 굴리는 변경은 없다.
- 실제 상자들의 목록/수량/등급 비중/칸 수를 일괄 수정하거나 다시 마이그레이션하지 않았다. 테이블 독립 소유 구조를 유지한다.
- `Copy Table` / `Paste Table`의 깊은 복사에는 카테고리 규칙도 포함된다. 다른 상자와 공유 연결을 만들지 않으며, 대상 상자의 크기와 등급 비중은 그대로다.
- 설정은 `LootObjectPool`에 저장한다. Enemy Drops의 `LootStagePool` 구조나 작성 화면에는 추가하지 않았다.

## 검증

- Unity 실제 실행: **82 checks passed**, 실패 0.
- 임시 검사 코드 제거 후 런타임/Editor 재컴파일 성공. 기존 TMP obsolete/unused-field 경고만 남았다.
- 현재 저장 에셋 별도 읽기 전용 연결 검사: 상자 27개, 고유 테이블 27개, family 연결 0개. 기존 `verify-independent-tables.cjs`의 과거 마이그레이션 스냅샷 비교는 MilitaryCase_01_WornWhite의 현재 이름 `MillitaryBox02`와 옛 이름 `MillitaryBox01` 차이로 중단되어, 최신 데이터의 연결 구조만 별도로 검사했다. 현재 이름을 되돌리거나 스냅샷을 갱신하지 않았다.
- 기존 LootMakerTests, LootMakerItemViewTests 및 신규 LootCategorySelectionTests를 함께 실행했다.
- 신규 검증: Auto 확률 보존, 조건부 확률, 테이블/등급 간 독립, 빈 후보/0 비중, 비정상 부동소수값, 매 칸 별도 추첨, 10,000회 분포, Undo/Redo, 깊은 복사, 임시 에셋 저장/재임포트, Item Maker와 분류 일치.
- 별도 검사 창: 토글 클릭, 카테고리 70:30 미리보기, 카테고리를 접어도 비중 필드 표시, 최종 아이템 확률, Auto 복귀와 Custom 값 유지, 창 재열기를 확인했다. 비중 숫자는 공통 작성 API로 변경했으며 키보드 타이핑 자체는 자동화하지 않았다.
- 실제 상자, 아이템, 프리팹, 작업 씬은 검사에서 변경하거나 저장하지 않았다. 임시 직렬화 테스트 폴더도 정리했다.
- 결과: `Library/LootCategoryChances-20260929/result-v1.txt`.
- 임시 실행 파일과 meta는 Assets에서 제거했다. 실행 소스는 `Tools/LootMaker/verify-loot-category-chances.cs.txt`에 보관하며 자동 실행되지 않는다.

## 구현 위치

- `Assets/Scripts/Gameplay/Items/Loot/LootItemCategories.cs`: 런타임/Item Maker 공통 카테고리.
- `Assets/Scripts/Gameplay/Items/Loot/LootCategorySelection.cs`: 카테고리 규칙, 조건부 확률, 추첨.
- `Assets/Scripts/Gameplay/Items/Loot/LootTableSO.cs`: 등급 선택과 생성 칸별 통합.
- `Assets/Editor/LootMakerAuthoring.cs`: Undo 가능한 토글/비중 작성.
- `Assets/Editor/LootProgressionEditorWindow.cs`: 확률 입력, 접기, 최종 확률 표시.
- `Assets/Tests/EditMode/Editor/LootCategorySelectionTests.cs`: 회귀 검증.
