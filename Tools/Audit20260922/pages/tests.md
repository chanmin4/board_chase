# 기술 C. 검증 기록·승인 테스트·남은 범위

검사 기준: 2026-09-22 / E:/github/board_chase/board_chase. 현재 파일의 정적 연결·핵심 실행 경로 기준이며, 별도 표시 없는 항목은 이번 감사에서 플레이 테스트한 결과가 아닙니다.

## 이번 감사에서 수행

- Assets 파일 목록/확장자, meta GUID 형식·중복·실제 대상 존재 확인.
- 직렬화 GUID/m_Script 연결 및 7개 게임 씬 루트 참조 그래프 생성.
- 자사 스크립트 866개 타입/메서드/참조/해시 인덱스, 외부/지원 355개 파일 인덱스.
- 대표 실행 경로와 Stage/Item/Quest/Shop/Audio/Localization SO 대조.
- Unity 설치의 Roslyn + 현재 Bee rsp로 런타임 및 에디터 어셈블리 별도 컴파일: 종료0. 런타임 경고15, 에디터 경고 출력0.
- 실세이브 삭제, 프로덕션 코드 수정, 열린 씬 저장, 임의 에디터 복구 도구 실행은 하지 않음.

## 존재하지만 이번에 재실행하지 않은 테스트 소스 10개

RunLifecycleAndSaveTests, RaidRuntimeContractTests, RaidMapFoundationTests, PlayerEmergencyMeleeTests, ObstacleBulletImpactAudioTests, ItemMakerWeaponCategoryTests, ItemMakerBatchTests, ItemHandAndCategoryTests, InventorySortUIOwnershipTests, AudioGroundworkTests.

테스트 파일 존재와 이번 테스트 통과는 다르다. 과거 대화의 통과 개수를 이번 결과로 재사용하지 않는다.

## 앞으로의 승인 게이트

1. Boot: 깨끗한 Player 실행에서 메뉴→Home→RUN→귀환→재출정. Missing Reference 및 무한 로딩 없음.
2. Raid: 5스테이지 여러 시드에서 크기/중앙/사분면/최소거리/보스 중복 없음. 저장 후 배치 안정.
3. Extract: 시작 귀환·일반 탈출·배터리 부족/성공·해치 잠김/개방·특별 탈출 비활성.
4. Combat: 실드/방탄복/기본 방탄 순서, 파손, 감염≥HP, 무기 없음/탄 없음 근접, 사용 중단.
5. Inventory: 수량/내구/장착 유지, 정렬, 드래그/거래 충돌, 루팅 수령 중 저장 중복 방지.
6. Progress: 킬/명중/수집 필터, 보상 1회성, 해금 유지, 창고 확장 후 데이터 보존.
7. Save: 구버전·잘린 본파일·백업 실패·누락 GUID·사망·강제종료 경계. 테스트용 저장으로 실행.
8. UI: KO/EN·폰트·길이·키 변경·설정 재시작·오버레이 입력 복귀·중복 정렬 버튼 없음.
9. Audio: Master/Music/SFX 실제 적용, 수색 취소 루프 정리, 표면 Cue, NPC 임시 클립 교체.
10. Visual/Build: 실제 리그/장비 장착, 렌즈·머티리얼, 13×13 성능, URP Player 출력.

## 결과 기록 양식

테스트명 / 기준 문서 / Git 또는 파일 해시 / 씬·seed·stage / 입력 / 예상 / 실제 / 로그·스크린샷 / 통과 여부를 남긴다. 문제를 수정하지 않았으면 검증 실패 또는 미검증으로 유지한다. 특히 P12 성격의 검증 항목은 자동 패치 작업으로 바꾸지 않는다.

## 정적 인덱스의 한계

이름 기반 소스 참조는 주석/동명 토큰을 포함할 수 있다. 직렬화 그래프는 비활성 객체도 포함하며 동적 로딩을 모두 증명하지 못한다. 이 인덱스로 파일별 조사 위치를 찾고 실제 기능 판정은 소유권·생성 경로·플레이 결과로 확정한다.

## 코드·데이터 근거

- `E:/github/board_chase/board_chase/Assets/Tests/EditMode/Editor/RunLifecycleAndSaveTests.cs`
- `E:/github/board_chase/board_chase/Assets/Tests/EditMode/Editor/RaidRuntimeContractTests.cs`
- `E:/github/board_chase/board_chase/Assets/Tests/EditMode/Editor/RaidMapFoundationTests.cs`
- `E:/github/board_chase/board_chase/Assets/Tests/EditMode/Editor/PlayerEmergencyMeleeTests.cs`
- `E:/github/board_chase/board_chase/Assets/Tests/EditMode/Editor/InventorySortUIOwnershipTests.cs`
- `E:/github/board_chase/board_chase/Assets/Tests/EditMode/Editor/AudioGroundworkTests.cs`