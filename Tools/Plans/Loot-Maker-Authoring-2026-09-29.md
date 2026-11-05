# Loot Maker: 프리팹 등록 / 테이블 복사 (2026-09-29)

> 후속 변경: 모든 상자의 공유 연결을 해제했다. 아래 공유 목록/Make Independent/Paste as New 설명은 이전 구현 기록이다. 현재 사용법과 검증은 [개별 테이블 패치](Loot-Maker-Independent-Tables-2026-09-29.md)를 기준으로 한다.

## 사용법

메뉴는 `Tools > Authoring > Loot Maker`, 기존 Container Loot / Enemy Drops 구분도 유지한다.

### 기존 상자 작성

1. Medical 같은 분류를 누르고 상자 카드를 선택한다.
2. 상단 `SHARED TABLE`은 여러 상자가 같은 테이블을 사용한다는 뜻이다. 한 상자만 바꾸려면 먼저 `Make Independent`를 누른다. `OWN TABLE`은 현재 등록된 상자 데이터 하나만 사용하는 테이블이다.
3. `Loot Items`에 Item Maker / Project의 ItemSO를 드래그한다. 이 영역은 내용물 아이템용이며 상자 프리팹 등록 영역과 다르다.
4. 아이템 수량과 필요 시 개별 비율을 편집한다. 상자 칸 수 / 크기 / 추첨 횟수 / 추첨 성공률은 `Box Settings`에서 별도로 편집한다.
5. 상단 `Save`로 저장한다.

### A 테이블을 복사하여 B에서 일부만 수정

1. A 선택 → `Copy Table`.
2. B 선택 → `Paste as New`.
3. B의 새 테이블에서 아이템을 추가/삭제하거나 수량을 변경한다. A와 기존 공유 상자는 바뀌지 않는다.

복사는 아이템 참조, rarity 행, 수량, 수동 share 설정, 기본 추첨 횟수를 포함한다. 아이템 SO 자체와 Box Settings(칸 수, override 횟수, 성공률)는 복사하지 않는다. 클립보드는 창 안의 메모리 스냅샷이며 원본이 이후 바뀌어도 복사 당시 내용이 유지된다. 창 닫기 / 스크립트 재컴파일 시 클립보드는 해제된다.

- `Make Independent`: 현재 테이블의 독립 복사본을 선택한 상자에만 연결한다. 카테고리와 상자별 수치는 유지한다.
- `Use Shared List...`: 목록 선택 후 `Link this box to selected list`로 명시적으로 공유한다. 확인창을 거친다. 예전 테이블은 삭제하지 않는다.
- `Paste over current table...`: 새 테이블을 만들지 않고 현재 테이블 내용을 교체한다. 사용하는 모든 상자에 영향을 주므로 개수와 경고를 확인해야 한다. 기본 동작인 `Paste as New`와 구분한다.
- 테이블 편집과 목록 연결 변경은 Undo를 지원한다. 독립 복사 후 연결을 Undo해도 생성된 테이블/목록 에셋은 재사용할 수 있도록 남겨 둔다.
- 같은 이름으로 여러 번 복제해도 고유 파일 경로와 목록 이름을 사용하며 기존 에셋을 덮어쓰지 않는다.

### 새 상자 프리팹 등록

1. 복사 출발점으로 쓸 기존 상자/목록을 선택한다. 원하는 기본 목록이 없으면 분류만 선택해도 된다.
2. 가운데 목록 위의 `Drop loot-box prefabs here`에 Project의 프리팹을 드래그한다. 여러 개 동시 등록 가능. 또는 Object Field 선택 → `+ Register`.
3. 새로운 상자는 선택한 목록을 독립 복사하고 자기 LootObjectSO를 만든다. 선택 목록이 없으면 복제 프리팹의 기존 목록을 복사하거나 빈 목록으로 시작한다.
4. 이미 등록된 동일 프리팹은 기존 데이터를 수정하지 않고 선택만 한다.
5. 복제/바리언트가 부모 상자의 LootObjectSO를 물려받았으면 새 기록과 테이블을 만들어 분리한다. 부모 프리팹, 부모 기록, 기존 테이블, 바리언트 상속은 유지한다.

등록 대상은 **이미 LootableObject 1개와 같은 오브젝트의 StructureLootInventoryRuntime을 가진 .prefab 에셋**이다. Raw FBX, 씬 오브젝트, 루팅 기능이 없는 모델, 여러 컨테이너를 한꺼번에 담은 프리팹은 설명을 표시하고 거절한다. UI 채널/콜라이더/상호작용까지 임의 생성하는 범용 프리팹 제작 기능은 아니다. 현재 편집 중인 Prefab Mode의 프리팹은 닫고 등록한다.

등록은 원본 prefab의 LootableObject 연결을 저장하는 명시적인 파일 작업이다. 변경 전 프리팹은 `Library/LootMakerRegistrationBackups/`에 보관한다. 실패 시 파일 복구와 이번에 생성한 에셋 정리를 시도한다. 이 폴더는 캐시 삭제 시 없어지므로 영구 백업이 아니다. 테이블 편집은 기존 Save 흐름을 유지한다.

## 상자 카테고리 드래그 이동 (2026-09-29 후속)

- 가운데 상자 카드(예: `Ammo_big`)를 누른 채 왼쪽 `Ammo`, `Medical`, `Safe` 등의 카테고리 버튼으로 끌어서 놓는다. 드롭 가능한 버튼은 녹색으로 강조된다. 클릭만 하면 기존처럼 상자를 선택한다.
- 놓으면 해당 분류로 화면이 전환되고 이동한 상자가 선택된다. 검색어는 초기화하며 카테고리별 개수도 갱신한다. 변경은 즉시 저장한다. `Ctrl+Z` / Redo 지원; Undo 후에는 상단 Save로 되돌린 상태를 저장한다.
- `All Containers`와 `Unassigned`는 조회 필터라 드롭 대상이 아니다. 분류를 없애서 보관하려면 `Other`로 옮긴다. 아직 분류되지 않은 상자도 카테고리로 옮길 수 있으며 테이블이 없으면 별도로 작성해야 한다.
- 카테고리는 **상자별 분류**다. 내부적으로 `LootObjectSO`의 명시적인 category override만 변경하며, 공유 `LootObjectTypeSO`나 `LootTableSO`를 바꾸지 않는다. 같은 목록을 공유하는 형제 상자는 그대로이며 이동 후에도 목록 공유는 유지된다. 아이템 편집까지 분리하려면 기존 `Make Independent`를 사용한다.
- 기존 상자는 별도 마이그레이션 없이 가족의 분류를 기본값으로 사용한다. 드래그로 분류한 상자는 이후 공유 목록을 연결/복제해도 지정한 분류를 유지한다. `Unassigned`는 기본 분류도 명시적인 분류도 없는 상자를 뜻하며, 빈 루팅 테이블 여부와는 별개다.
- 프리팹/에셋 파일의 경로·이름·GUID, 아이템 목록, 전체 칸·생성 칸 범위·등급 비율, 모델·잠금 조건은 바뀌지 않는다. 새 테이블을 만들거나 자동으로 Ammo 아이템을 채우는 기능이 아니다.
- Project의 `LootObjectSO` 에셋도 카테고리 드롭 대상으로 지원한다. 새 **프리팹** 등록은 기존 가운데 상단 등록 영역을 사용한다. Play Mode / 컴파일 중 / 읽기 전용 에셋은 이동하지 않는다.
- 검증: runtime/editor 컴파일 통과. 실제 Unity에서 **79개 통과 / 실패 0** (기존 71개 + 분류 관련 8개). 분류 이동·공유 데이터 및 생성 설정 보존·Undo/Redo·미분류 이동·같은 분류 및 잘못된 입력·목록 교체 후 분류 유지·저장/재로드와 GUID 보존을 검사했다. 보고서: `Library/LootMakerAuthoring-20260929/category-tests-v1.txt`. 미저장 씬 때문에 첫 검사에서 씬 추가가 거부되어 작업 씬과 별개인 미리보기 씬으로 보정 후 실행했다. 임시 검사 파일과 테스트 에셋은 제거했다. 실제 마우스를 통한 전체 드래그 동작은 자동 검증 범위에 포함되지 않는다.

## 중복 루팅 오브젝트 삭제 (후속)

- 오른쪽 `Select Asset / Open Prefab` 옆 `Delete...`는 선택한 **LootObjectSO 카탈로그 기록**을 삭제한다. 같은 사진/프리팹을 사용하더라도 다른 기록일 수 있으므로 실제 에셋 경로를 표시한다. 모델 프리팹, LootObjectTypeSO, LootTableSO, ItemSO는 삭제하지 않는다.
- 사용처 검사를 마치면 삭제 패널을 연다. 미사용 기록은 `Delete Record...` → 경로 확인 → 삭제한다. 실제 파일과 meta는 휴지통으로 보내며 목록을 갱신한다.
- 사용 중인 중복은 `Replace uses with / Existing records`에서 대체 LootObjectSO를 고른 뒤 `Replace Links + Delete...`로 명시적으로 확인해야 한다. 프리팹이 현재 사용하는 다른 기록이 있으면 추천값으로 표시하지만 확인 전에는 변경하지 않는다. 목록에는 에셋 이름과 `[same prefab]`를 함께 표시한다.
- 기존 사용처는 대체 기록의 **아이템 목록·칸 수·등급 확률**을 사용한다. 옛 루팅 테이블을 병합하거나 덮어쓰지 않는다. 기존 테이블 자체는 재사용할 수 있도록 그대로 남는다.
- 사진의 `RaidContainer_Medical / Armory / Vault`는 같은 프리팹을 보여주지만 Stage1~5의 특수방 설정에서 실제 사용 중이다. 단순히 미사용 중복으로 취급해서 삭제하지 않는다. 패치 설치만으로 이 기록들을 삭제하거나 스테이지 설정을 변경하지 않는다.
- 저장된 에셋/프리팹의 직접 참조와 로드된 미저장 참조를 검사한다. 사용처가 있으면 대체값 없는 삭제를 거부한다. 씬 참조, 미저장 사용처, 읽기 전용 사용처, GUID 문자열 등 자동 이전이 안전하지 않은 참조는 경로를 보여주고 수동 정리를 요청한다. 씬을 자동으로 열거나 저장하지 않는다. 최종 확인 시 다시 검사한다.
- 삭제 직전 `Library/LootMakerDeletedRecords/<시각-고유ID>/`에 원본 기록·meta와 변경할 참조 파일·meta, 원래 경로를 기록한 `manifest.json`을 보관한다. 참조 저장 또는 휴지통 이동이 실패하면 이 파일들로 되돌린다. 휴지통/Library 백업은 영구 버전 관리 대체물이 아니며 Library 삭제 시 백업이 사라진다.
- **파일 삭제는 Ctrl+Z 작업이 아니다.** 복구하려면 휴지통의 기록과 meta, 연결을 변경한 경우 manifest에 기재한 원본 참조 파일까지 백업/Git으로 복원해야 한다. UI에도 이 차이를 명시한다.
- 검증: editor 컴파일 통과. 실제 Unity에서 **85개 통과 / 실패 0** (이전 79개 + 삭제 관련 6개). 임시 중복 삭제, 백업과 meta 보존, 사용 중 삭제 거절, 프리팹·복사한 스테이지 연결 이전 및 재로드, 미저장 및 GUID 문자열 참조 보호, 모델·테이블 보존을 검사했다. 보고서: `Library/LootMakerAuthoring-20260929/delete-tests-v1.txt`. 실제 중복 기록은 삭제하지 않았고 Stage1~5 원본 파일의 SHA256이 검사 전후 동일함을 확인했다. 임시 실행 스크립트/메타와 테스트 에셋은 정리했다. 실제 마우스로 삭제 UI의 모든 동작을 자동 검사한 것은 아니다.

### 삭제 확인 UI 무반응 수정 (같은 날 후속)

- 증상: `Delete...` 클릭 후 사용처 검사 진행 표시만 지나가고 확인 UI가 나타나지 않음. 기존 구현은 `delayCall`의 검사 완료 후에만 `_deleteBox`를 설정했고, 선택 상태가 달라지면 조용히 반환했다. 같은 카드 재선택도 리뷰 상태를 무조건 지웠다.
- 수정: 버튼 클릭 즉시 대상이 고정된 `DELETE REVIEW` 모드로 전환한다. 기존 아이템 편집 영역 대신 스크롤 가능한 확인 영역을 사용하여 검사 중, 검사 완료, 취소/오류, 참조 목록과 차단 이유를 표시한다. 오류는 사라지는 알림 대신 이 영역에 남기고 `Retry reference check`를 제공한다.
- 검사와 최종 확인 예약은 IMGUI 밖의 일회성 `EditorApplication.update` 콜백으로 처리한다. 같은 카드 재선택은 리뷰를 유지한다. 다른 카드 선택/취소/창 닫기는 예약 콜백을 해제하고 요청 번호를 바꾸어 이전 검사 결과가 새 화면을 덮지 않게 한다.
- 사용법: `Delete...` → `Check complete` 확인 → 필요한 경우 `Replace uses with` 선택 → `Replace Links + Delete...` 또는 `Delete Record...` → 최종 확인. `Cancel`은 편집으로 복귀하며 아무 파일도 변경하지 않는다. 모델 프리팹과 테이블을 보존하는 삭제 범위와 마지막 재검사는 기존 그대로다.
- 실제 Unity 검증: 새 상태 회귀 테스트 **4개 통과**, 별도 창에서 `SendEvent`의 MouseDown/MouseUp으로 `RaidContainer_Vault` 카드 선택 → Delete → 결과 영역의 실제 Repaint 확인 → Cancel까지 통과했다. 참조 **5곳 / 차단 0개**, Cancel 뒤 리뷰 해제를 확인했다. 최종 삭제는 실행하지 않았다. 이전 데이터 삭제 테스트 85개 결과와 별도로 기록한다.
- 실제 금고/의료 기록, 금고 프리팹·루팅 테이블, Stage1~5 및 각 meta **18개 파일**이 검사 전후 동일했다. 보고서: `Library/LootMakerAuthoring-20260929/delete-ui-probe-v2.txt`. 임시 실행 스크립트/메타는 제거했고, 재현 소스만 `Tools/LootMaker/verify-delete-ui.cs.txt`에 보관한다. 이전 v1 GUI 프로브는 pending 상태로 timeout이어서 성공 증거로 사용하지 않는다.

## 이름 편집

오른쪽 선택 상자 영역의 Select Asset / Open Prefab 아래에서 이름을 편집한다. Loot Items / Box Settings 어느 탭에서도 보인다.

- `Prefab Name` → `Rename`: 카드에 표시되는 실제 프리팹 파일 이름을 같은 폴더 안에서 즉시 변경한다. AssetDatabase.RenameAsset을 사용하여 GUID, LootObjectSO 연결, 씬 참조, 바리언트 부모 관계를 유지한다. 선택과 현재 탭은 유지하고 기존 검색어는 지워 새 이름이 숨지 않도록 한다.
- `Game Title`: 인게임 표시 이름의 fallback. 입력 후 Enter 또는 필드 이동으로 적용하고 상단 Save로 저장한다. Undo 지원. Localized Display Name이 지정돼 있으면 번역 문자열이 우선한다.
- 공유 테이블/가족 이름이나 LootObjectSO 파일 이름은 함께 변경하지 않는다. 프리팹 파일 이름 변경은 즉시 저장되는 파일 작업이며 되돌리려면 이전 이름으로 다시 Rename한다.
- 이름 충돌, 경로 입력, 잘못된 파일명, Windows 예약 이름, 없는 프리팹, Packages/모델/씬 오브젝트를 거부한다. Play Mode 중에는 비활성화하고 Prefab Mode에서는 저장 후 나오도록 안내한다.
- 실제 프로젝트의 상자 이름은 패치가 자동 변경하지 않는다. 사용자가 Rename을 누른 대상만 변경한다.
- 검증: 편집기 코드 컴파일 통과. 사용자의 Refresh 이후 실제 Unity에서 **71개 통과 / 실패 0** (기존 59개 + 이름 편집 12개). 대소문자 변경, 확장자 처리, GUID/바리언트/루팅 연결 보존, 충돌 및 잘못된 이름 거부, 표시 이름 Undo를 검사했다. 보고서: `Library/LootMakerAuthoring-20260929/rename-tests-v1.txt`. 임시 검사 스크립트/메타와 테스트 에셋은 정리했다. 실제 마우스 클릭 동작 전체를 자동 검증한 것은 아니다.

## 최신 생성 규칙

같은 날 후속 패치로 Roll / 성공률 대신 실제 생성 칸 수와 상자별 등급 비율을 적용했다. 현재 규칙과 59개 Unity 검증 결과는 `Loot-Maker-Slot-Generation-2026-09-29.md` 참고. 아래 Roll 관련 설명과 37개 검증은 이전 개편 기록이다.

## Quick Size Presets 후속 변경

- `Box Settings`의 5개 버튼: Very Small **3칸**, Small **4칸**, Medium **6칸**, Large **9칸**, Very Large **12칸**.
- 좁은 편집창에서도 읽을 수 있도록 3개 / 2개 두 줄로 배치한다.
- 이제 프리셋은 선택한 상자의 크기 이름과 슬롯 수만 변경한다. 루팅 테이블, Override Rolls, 추첨 횟수, 성공률 및 모델 크기는 유지한다. 수치는 아래 필드에서 별도 조정한다.
- 기존 에셋의 칸 수는 일괄 변경하지 않는다. 기존 enum ID도 유지한다. 프리셋을 명시적으로 눌렀을 때만 새 칸 수가 적용되며 Undo / Save 흐름은 동일하다.
- 2026-09-23 기록의 Medium 8칸 / Large 12칸 및 프리셋에 따른 확률 변경은 이전 동작이다.
- 후속 변경 검증: runtime / editor 재컴파일 통과. 5개 크기 × Override Rolls 켜짐/꺼짐의 수치 보존 테스트와 기존 enum / 저장 슬롯 호환성 테스트를 추가·수정했다. 아래 37개 실행 결과는 후속 변경 전 개편 시점의 결과이며, 이번 테스트는 아직 열린 Unity에서 재실행하지 않았다. enum과 테스트 특성 변경은 Hot Reload가 아닌 Assets → Refresh로 반영한다.

## 썸네일 보정

- LootObject 29개 `_worldPrefab`과 LootObjectType 7개 `_previewPrefab`의 `fileID: 100100000`을 해당 프리팹의 실제 root GameObject ID로 변경했다. GUID는 유지했다.
- 카드에 실제 프리팹 이름과 AssetPreview를 표시한다. 미리보기를 만드는 동안 Loading을 표시하며, 실패/누락 때 파란 기본 큐브를 실제 모델인 것처럼 표시하지 않는다.
- 변경 전 SO 파일과 보정 manifest는 `Library/LootMakerAuthoring-20260929/`에 보관한다.
- 기존 ItemSO, 테이블 내용/확률, 상자별 수치, 잠금 조건, Enemy Drops, 런타임 추첨/저장 규칙은 변경하지 않았다.

## 검증

- 실제 열린 Unity의 Assets → Refresh 이후 기존 25개 + 신규 12개, **37개 테스트 통과 / 실패 0**.
- 스냅샷 깊은 복사, 개별 수량/share 보존, 형제 상자 보존, 연결/덮어쓰기 Undo, 중복 이름 보호, 양방향 프리팹 등록, 재등록 무변경, 바리언트 부모 보존, 잘못된 입력 거절, 실제 카드 프리팹 root 참조를 검사했다.
- MedicalBox_verySmall / Small / Big의 실제 AssetPreview PNG 3개를 출력하고 확인했다. 모두 실제 의료상자 모델이며 파란 기본 아이콘이 아니다.
- runtime / editor 컴파일 통과. 기존 TMP obsolete / unused-field 경고는 별개다.
- 임시 검증 브리지와 테스트가 생성한 임시 Assets 폴더는 제거했다. 영구 테스트는 `LootMakerAuthoringTests.cs`로 유지한다.
- 이 검증은 모든 UI 마우스 동작이나 전체 레이드 플레이를 대신하지 않는다.

코드: `Assets/Editor/LootMakerAuthoring.cs`, `LootProgressionEditorWindow.ContainerAuthoring.cs`, 기존 `LootProgressionEditorWindow.cs`.
