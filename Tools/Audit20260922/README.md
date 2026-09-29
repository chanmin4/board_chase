# Board Chase 현재 프로젝트 감사 · 2026-09-22

프로덕션 Unity 파일을 수정하지 않고 현재 소스/자산의 상태를 점검해 Notion 기준서를 교체한 작업입니다.

- 기준서: https://www.notion.so/39e3fd19ccd68174813ccc0cf073a89b
- 기술 감사: https://www.notion.so/3d03fd19ccd6818a9104c0aef475e63f
- 남은 작업: https://www.notion.so/2523fd19ccd681d5aa9bc5b9880f119d
- 스크립트/첨부: https://www.notion.so/3e33fd19ccd681128a8eeeb34b247ef8

## 결과

기존 17개 시스템 문서를 현재 본문으로 교체하고 새 분야 문서 9개를 작성했습니다. 총 26개 페이지를 fetch로 재확인했습니다. 기존 하위 페이지 10개와 사운드 DB 2개를 보존했습니다. 기준서에는 새 하위 페이지 9개와 전체 분야 링크 15개가 있습니다. 첨부는 모듈별 Markdown 7개와 감사 원본 ZIP 1개입니다.

## 범위

Assets C# 1,221개를 목록화하고 자사 Scripts/Editor/Tests 866개(168,936행)의 타입·함수·참조·해시를 추출했습니다. 직렬화 YAML 10,986개와 GUID 연결을 검사하고 핵심 실행 경로를 수동 대조했습니다. 외부/지원/생성 355개는 별도 파일 인덱스입니다.

Unity 설치 Roslyn과 기존 Bee rsp로 Assembly-CSharp 및 Assembly-CSharp-Editor를 감사 output에 별도 컴파일해 둘 다 종료 0을 확인했습니다. 런타임 경고 15개, 에디터 출력 경고 0개입니다. Player 빌드, 전체 테스트 재실행, 모든 씬 플레이, 모든 모델 육안 검사, 전체 오디오 청취는 미실행입니다. 전체 소스를 한 줄씩 수동 리뷰한 것으로 읽으면 안 됩니다.

## 파일

- inventory.cjs / supplement.cjs: 읽기 전용 감사 및 output 생성.
- docs.cjs / technical-docs.cjs: 현재 기준 문서 원본 및 로컬 Markdown 생성.
- notion-page-map.json / notion-verification.json: 게시한 페이지 ID와 실제 조회 검증.
- pages/: 읽기 쉬운 로컬 문서.
- output/: 정적 인덱스/참조/누락/표·Cue 검사 결과. DLL은 별도 컴파일 산출물입니다.
- BoardChase-current-audit-20260922.zip: Notion에 올린 텍스트 감사 자료 묶음. 게임 에셋 원본이나 사용자 세이브를 담지 않습니다.
- upload.cjs: Notion이 발급한 짧은 수명의 URL로 승인된 감사 파일을 한 번 POST하는 보조기. 자격 정보는 파일로 저장하지 않습니다.

현재 기준서와 작업 제안은 구분합니다. 이번 감사에서 발견한 부팅/카탈로그/저장 일관성/프리팹 연결 문제는 Unity에 자동 패치하지 않았습니다.
