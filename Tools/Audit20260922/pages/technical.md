# 기술 부록 | 현재 Unity·Assets·코드 감사 (2026-09-22)

검사 기준: 2026-09-22 / E:/github/board_chase/board_chase. 현재 파일의 정적 연결·핵심 실행 경로 기준이며, 별도 표시 없는 항목은 이번 감사에서 플레이 테스트한 결과가 아닙니다.

## 감사 기준

대상 체크아웃은 E:/github/board_chase/board_chase 하나다. 다른 체크아웃, 과거 Notion 설명, 이전 대화의 완료 주장보다 이번 현재 파일을 우선했다. 프로덕션 스크립트·씬·프리팹·SO는 이번 작업에서 패치하지 않았다. 감사 스크립트와 결과만 Tools/Audit20260922 아래에 생성했다.

## 정적 전수 범위

- Assets 전체 파일 69,188개: meta 35,792개 포함, 비-meta 33,396개.
- C# 1,221개: 자사 검사 범위 Assets/Scripts·Editor·Tests 866개, 168,936행. 외부 패키지·지원·생성 등 나머지 355개는 별도 인덱스.
- 프리팹 8,689개, 씬 127개, .asset 1,386개, 머티리얼 744개. 공급사 데모/템플릿/WIP도 포함한 파일 수이지 실사용 콘텐츠 수가 아니다.
- 텍스트 직렬화 파일 10,986개에서 GUID·m_Script와 파일 간 참조를 추출했다.
- 자사 스크립트별 경로·행수·SHA256·타입·메서드·소스 이름 참조·직렬화 자산·지정 씬 참조 그래프를 만들었다.
- GUID 형식 불량 meta 0, 중복 GUID 0, 자산이 없는 고아 meta 타깃 0. YAML 전체 문법/Unity import 성공을 뜻하지 않는다.
- 미해결 GUID 참조 쌍 1,433개: 공급사/WIP/업그레이더 메타 포함. ‘현재 플레이 오류 1,433개’라고 읽지 않는다. 기능 필드와 실제 루트 연결을 구분해 우선순위를 정했다.

## 수동으로 이어 본 핵심 경로

부팅/씬 준비, 런 수명주기, 홀수 격자/탈출, 방/레이드 시간, 가스/감염, 피해/근접, AI/장비, 인벤토리/루팅, 상점/퀘스트/창고, 저장, UI/현지화, 오디오, 소켓, 에디터 자동화 경로를 관련 코드와 자산으로 대조했다. 866개 전체를 한 줄씩 수동 실행·리뷰한 것으로 과장하지 않는다.

## 이번 실행 검증

Unity 설치의 Roslyn 및 현재 Bee response file로 Assembly-CSharp와 Assembly-CSharp-Editor 컴파일을 감사 output으로 별도 실행했다. 둘 다 종료 코드 0. 런타임 경고 15개(TMP 구식 API/미사용 필드 등), 에디터 출력 경고 없음.

Player 빌드, 모든 테스트 재실행, 전 자산 임포트, 전 프리팹 육안 검사, 모든 오디오 청취, 모든 텍스트 화면 검사는 이번에 수행하지 않았다. 현재 열린 Unity 씬을 저장/전환하거나 실세이브를 초기화하지 않았다.

## 발견한 우선 문제

Build Settings 전 씬 비활성, 메뉴 Addressables 이전 주소/참조, RuntimeContentCatalog의 잘못된 YAML와 누락 GUID, 초기 장비/상점 누락 참조, DefaultVolumeProfile 테스트 컴포넌트 잔여, 루팅 중간 저장 일관성 위험이 있다. 아래 빌드·테스트 문서에서 증거와 후속 확인을 분리한다.

## 문서와 실제 파일의 관계

설명 본문은 동작/정책, 이 기술 부록은 소유권/파일/검증, 스크립트 카탈로그는 각 파일별 추출 증거다. 변경 후 인덱스 해시가 바뀌면 이 감사는 과거 스냅샷이 되므로 해당 시스템 문서를 다시 갱신한다.

## 코드·데이터 근거

- `E:/github/board_chase/board_chase/ProjectSettings/ProjectVersion.txt`
- `E:/github/board_chase/board_chase/ProjectSettings/EditorBuildSettings.asset`
- `E:/github/board_chase/board_chase/Packages/manifest.json`
- `E:/github/board_chase/board_chase/Tools/Audit20260922/inventory.cjs`
- `E:/github/board_chase/board_chase/Tools/Audit20260922/supplement.cjs`