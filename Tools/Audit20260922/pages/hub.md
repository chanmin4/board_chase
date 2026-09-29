# Board Chase | 현재 프로젝트 기준서 (2026-09-22)

검사 기준: 2026-09-22 / E:/github/board_chase/board_chase. 현재 파일의 정적 연결·핵심 실행 경로 기준이며, 별도 표시 없는 항목은 이번 감사에서 플레이 테스트한 결과가 아닙니다.

## 이 문서가 기준이다

현재 체크아웃을 다시 조사해 기존 아키텍처/브레인스토밍/옛 시스템 설명을 교체했다. 과거 페인트 점유 중심 기획, 전역 감염통제력·숫자 지갑·메타 구매를 현재 게임 핵심으로 쓰지 않는다. 다만 Unity에 실제 남은 연결은 WIP/은퇴 경계 문서에 사실대로 기록한다.

이 페이지는 앞으로 Notion에서 변경안을 정하고 Unity에서 구현할 때의 출발점이다. 본문은 플레이 흐름과 시스템 규칙, 기술 부록은 코드·자산·빌드·검증, 파일 인덱스는 조사 증거다.

## 현재 게임의 흐름

Home 준비 → 스테이지 선택 → 시작 허브(-1,0) → 홀수 격자 레이드 → 방 전투/수색/장비 관리 → 일반 탈출 또는 전력 해치 → Home 귀환/퀘스트·창고 진행. 사망은 정상 귀환과 다른 런 정리 경로다.

## 읽기 순서

<mention-page url="https://www.notion.so/3243fd19ccd680ad8f19e798478236bd"/>
<mention-page url="https://www.notion.so/2523fd19ccd68153acddeb0282d0f20a"/>
<mention-page url="https://www.notion.so/3a43fd19ccd68121b07cd3eb0cbec126"/>
<mention-page url="https://www.notion.so/3e33fd19ccd6813a9320efd1239af166"/>
<mention-page url="https://www.notion.so/3e33fd19ccd681d78c32ffab02a3443b"/>
<mention-page url="https://www.notion.so/3e33fd19ccd6818db5b1d6808f51f8b6"/>
<mention-page url="https://www.notion.so/3e33fd19ccd6810a84f3da728af97b66"/>
<mention-page url="https://www.notion.so/3e33fd19ccd6815da3cbfad9ca0271e8"/>
<mention-page url="https://www.notion.so/2523fd19ccd6811ea9dac519aefc1ddd"/>
<mention-page url="https://www.notion.so/3e33fd19ccd6818182ead21a4ac14564"/>
<mention-page url="https://www.notion.so/3e33fd19ccd681a7acdde97473eefbf7"/>
<mention-page url="https://www.notion.so/3e33fd19ccd6818a8a73ca7a4e3acde4"/>
<mention-page url="https://www.notion.so/2523fd19ccd681d5aa9bc5b9880f119d"/>
<mention-page url="https://www.notion.so/3d03fd19ccd6818a9104c0aef475e63f"/>
<mention-page url="https://www.notion.so/3e33fd19ccd681128a8eeeb34b247ef8"/>

## 현재 상태를 읽는 네 가지 구분

- 코드·데이터 확인: 현재 소스와 SO/프리팹의 경로를 대조했다. 플레이 승인과는 다름.
- 기반만 있음/미연결: 클래스와 설정은 있으나 실제 자산 연결이 비어 있거나 확인되지 않음.
- WIP/은퇴: 코드가 남았지만 현재 정책의 메인 실행 경로가 꺼져 있음.
- 검증 필요: 테스트/실빌드/실제 화면·청취를 아직 통과했다고 말할 수 없음.

## 핵심 상태 요약

레이드 격자·중앙 해치·발전/탈출·이중 시간·개인 감염·다층 방탄·비상 근접·물리 화폐·인벤토리·퀘스트/저장 기반이 있다. 반면 새 퀘스트 라인, 역할별 전용 방, 소켓 리그 실연결, 실더 보스, 특별 탈출, 번역/오디오 최종 마감은 완료로 볼 수 없다.

## 가장 먼저 볼 위험

현재 Build Settings의 씬 전부 비활성, 메뉴 Addressables 이전 참조, RuntimeContentCatalog의 YAML/누락 GUID, 초기 장비/상점 참조, DefaultVolumeProfile 잔여 테스트 타입, 루팅 이동 중간 저장 위험을 확인했다. 세부 콘텐츠를 더 쌓기 전에 이 기준선을 안정화하는 것이 우선이다.

## 점검 범위와 한계

Assets C# 1,221개 중 자사 866개(168,936행)의 정적 인덱스와 전체 자산 GUID 연결을 만들고 핵심 동작 경로를 수동 대조했다. 외부/지원 355개는 별도 목록이다. 런타임/에디터 C# 컴파일은 통과했다. 모든 소스를 한 줄씩 수동 검증하거나 모든 씬을 플레이·모든 프리팹을 눈으로 검사한 작업은 아니다. Player 빌드와 테스트 전체 재실행도 미실행이다.

## 앞으로의 변경 규칙

현재 동작을 수정하기 전에 변경안/소유자/저장 영향/승인 테스트를 기록한다. 구현과 자산 연결 후 실제 검증 결과를 남기고 이 기준서를 갱신한다. 과거 완료율이나 콘셉트 이미지를 실제 Unity 적용 증거로 대신하지 않는다.

## 코드·데이터 근거

- `E:/github/board_chase/board_chase/Tools/Audit20260922/output/summary.json`
- `E:/github/board_chase/board_chase/Tools/Audit20260922/output/supplement.json`