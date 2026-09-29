# 스크립트 전수 인덱스·감사 원본 | 1,221개

검사 기준: 2026-09-22 / E:/github/board_chase/board_chase. 현재 파일의 정적 연결·핵심 실행 경로 기준이며, 별도 표시 없는 항목은 이번 감사에서 플레이 테스트한 결과가 아닙니다.

## 파일별 조사 자료

이 페이지의 첨부 자료는 2026-09-22 현재 파일 스냅샷이다. 자사 소스 866개는 타입·메서드·직렬화 참조·코드 이름 참조·씬 참조 그래프·해시를, 나머지 355개는 경로·행수·해시를 기록했다. 총 Assets C# 1,221개다.

자사 분류는 Assets/Scripts, Assets/Editor, Assets/Tests 기준이다. 그 밖의 AssetSupportFile/EditorTool에도 커스텀 지원 코드가 섞일 수 있으므로 ‘나머지=전부 공급사 원본’이라고 단정하지 않는다. 외부/지원 영역의 구현 전체를 수동 정밀 리뷰한 것은 아니다.

## 인덱스 읽기

- Core: 씬 전환, 입력, 저장, 이벤트, 공통 서비스.
- Gameplay: 엔티티, 무기/아이템, 전투/감염, 섹터/맵, 퀘스트/진행.
- Presentation: HUD/패널/설정/지도, 카메라, 오디오.
- Authoring: 제작·검사·디버그 에디터 도구.
- StateMachine: 행동/조건/전이 공통 기반.
- Other: 위 모듈 밖 자사 소스와 Assets/Editor, Tests.
- External-and-generated: 공급사/지원/생성 소스 파일 목록.

## 첨부 원본 내용

scripts.json에는 전체 자사 파일의 자세한 참조와 해시가 있다. refs.json/guids.json은 자산 그래프와 GUID→경로, missing.json은 미해결 GUID 쌍, summary.json/supplement.json은 범위·KO/EN 표·Cue·우선 누락 목록이다. 사람이 읽는 모듈별 Markdown도 함께 제공한다.

참조 없음은 삭제 허가가 아니다. RequireComponent·AddComponent·Resources·Addressables·리플렉션·에디터 메뉴를 함께 본다. 참조 있음도 실제 활성 게임 콘텐츠라는 보증이 아니다. 기본 템플릿과 데모를 포함한 숫자를 ‘완성 아이템/기능 개수’로 사용하지 않는다.

## 업데이트 방법

현재 체크아웃에서 inventory.cjs와 supplement.cjs를 실행하면 Tools/Audit20260922/output에 감사 결과만 생성한다. 향후 새 날짜로 별도 스냅샷을 만들고 현재 문서의 근거/검증일을 갱신한다. 이 스크립트는 Unity 자산을 자동 복구하거나 삭제하지 않는다.

## 코드·데이터 근거

- `E:/github/board_chase/board_chase/Tools/Audit20260922/inventory.cjs`
- `E:/github/board_chase/board_chase/Tools/Audit20260922/supplement.cjs`
- `E:/github/board_chase/board_chase/Tools/Audit20260922/output/scripts.json`