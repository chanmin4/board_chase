# 오디오 밑작업 연결 (2026-09-09)

## 이번 범위

기존 AudioCueSO -> AudioCueEventChannelSO -> AudioManager -> SoundEmitter 풀을 유지했다.
별도 AudioSource 재생 시스템/에디터 마이그레이션 버튼은 추가하지 않았다.
대상은 믹서, UI/인벤토리, 수색, 거점 NPC/퀘스트 연결이다.
총알 표면별 타격음은 `Tools/ObstacleAudio/README.md`를 참고한다.

## 믹서 조사와 실제 출력

프로젝트 Assets/Packages와 별도 보관 폴더까지 검색했을 때 `.mixer`는
`Assets/Audio/Audio/Mixer/Master.mixer` 하나였다.
현재 `PersistenceManager.unity`의 `AudioManager`와 예전 `AudioMaster.prefab` 모두 같은 GUID를 참조했다.
기존 Mixer GUID/기존 Master 그룹 ID는 유지했다.

```text
Master                  MasterVolume
├─ Music                MusicVolume
└─ SFX                  SFXVolume
   ├─ UI
   ├─ Voice
   └─ Ambience
```

- `Music_Config`는 Music, `UISFX_Config`는 UI, 일반/플레이어/적 SFX 설정은 SFX로 출력한다.
- `NPCVoice_Config`는 Voice, `Search_Config`는 SFX로 출력한다.
- UI/Voice/Ambience는 SFX의 자식이므로 기존 효과음 슬라이더로 함께 조절된다. 별도 음성/환경 슬라이더는 만들지 않았다.
- 기존 AudioMaster의 Music/SFX 그룹과 파라미터 이름도 동일하게 맞췄다. 이 구형 프리팹을 새로 씬에 배치하지는 않았다.
- 볼륨 적용을 Start 이후로 옮기고 저장된 기기 설정을 읽는다. 0~1 값은 dB로 변환하고 0은 -80dB로 처리한다.
- SoundEmitter 풀과 기존 거리/방향 보정 설정은 유지했다.

## 수정하는 곳

| 원하는 변경 | 위치 |
|---|---|
| 실제 WAV/OGG 교체 | Tools > Authoring > Audio Cue Library에서 아래 Cue 이름 검색 -> Clip Groups |
| UI 종류별 Cue 배정 | PersistenceManager 씬 > AudioManager > UIAudio |
| 수색 단계별 Cue 배정 | 같은 UIAudio 오브젝트의 InteractionSearchAudio |
| NPC별 인사/종료/선택 반응 | HomeQuestNpc / HomeItemNpc / HomeStatsNpc 프리팹의 Conversation Audio |
| 공통 NPC 프로필 | Assets/ScriptableObject/Audio/NpcAudioProfile_Default.asset |
| 퀘스트별 수락/완료 예외음 | QuestDefinitionSO의 Accepted Audio Cue / Completion Audio Cue |

퀘스트 예외음이 비어 있거나 재생 가능한 클립이 없으면 UIAudio의 공통 알림음을 사용한다.
NPC마다 다르게 하려면 기본 프로필을 복제해 각 프리팹에 배정하면 된다.

## UI/인벤토리 정책

- UIAudio의 Auto Bind Selectables가 실제 플레이 중 활성 Selectable에 기존 UIButtonAudioEventEmitter를 연결한다.
  런타임 생성 버튼/탭/드롭다운도 포함하며 프리팹의 배치/크기/폰트는 바꾸지 않는다.
- Canvas 렌더 전 프레임당 한 번, 할당을 재사용하는 Selectable 목록으로 검사한다. InputField는 제외한다.
- 마우스 클릭 및 키보드/컨트롤러 Submit을 지원한다. 이동/호버는 0.08초 제한을 둔다.
- 인벤토리 셀과 장비 셀처럼 Button이 아닌 슬롯의 선택/호버도 별도 연결했다.
- 인벤토리/장비/퀵슬롯의 이동/장착/해제/분할은 UI 명령의 반환 결과로 재생한다.
  저장 불러오기, 슬롯 Refresh, 보상 지급 같은 내부 데이터 갱신에는 이 UI 효과음을 넣지 않았다.
- 구매/판매/수리 실패 및 전리품 이동 실패에는 Denied를 사용한다.
- 같은 프레임에는 결과음이 일반 클릭/호버/패널음보다 우선한다. 퀘스트 알림이 가장 우선이다.
  NPC 음성은 별도 위치 기반 재생이므로 알림음과 함께 들릴 수 있다.
- 특정 Selectable의 자동 소리를 끄려면 UIButtonAudioEventEmitter를 미리 붙여 비활성화한다.

## 수색

`InteractionCastBarEventChannel`의 Loot 캐스팅만 관찰한다. Reload/Consumable 캐스팅에는 재생하지 않는다.

1. 시작: Search_Start + Search_Loop 한 번 시작
2. 진행 업데이트: 새 반복음을 만들지 않음
3. 진행 100%: 반복 정지 + Search_Complete
4. 중도 취소: 반복 정지 + Search_Cancel
5. 일시정지: 반복 정지, 재개하면 수색 중일 때만 다시 시작
6. 컴포넌트 비활성/대상 제거: 소유한 반복 키 정리

기존 루팅 열기/줍기/열쇠 피드백은 유지했다. 수색 반복음은 AI 청각 이벤트를 추가하지 않는다.
거리/취소 조건 등 수색 게임플레이 규칙은 기존 InteractionManager가 계속 소유한다.

## NPC/퀘스트

- 거점 NPC와 상호작용해 미션 창을 열면 인사.
- 미션 페이지를 떠나거나 창을 닫으면 종료 반응. NPC가 비활성화되면 남은 음성 정지.
- NPC의 제안을 수락하면 성공/실패 반응.
- 다음 음성/대사로 교체할 때 이전 재생 키를 정지해 겹치지 않게 한다.
  향후 대화 구현에서는 HomeQuestNpc.PlayDialogueCue를 사용 가능하다.
- 퀘스트 수락/새 완료 시 알림. 저장된 완료 상태 복원/미션 목록 Refresh/보상 재시도에서는 재생하지 않는다.
- 완료된 퀘스트의 DebugComplete 반복 요청도 중복 알림을 만들지 않는다.
- 이것은 대화 **오디오 연결**이며 대사 분기/자막/더빙 콘텐츠를 새로 구현한 것은 아니다.

## 임시 Cue 목록

- UI_Navigate, Quest_Accepted, Quest_Completed
- Search_Start, Search_Loop, Search_Complete, Search_Cancel
- NPC_Greeting_Temp, NPC_Farewell_Temp, NPC_Accept_Temp, NPC_Reject_Temp

기존 Universal Sound FX의 짧은 알림음/지퍼 소리를 임시로 연결했다.
NPC Cue는 녹음된 대사가 아닌 비언어 확인음이며, Search_Loop는 최종 수색 Foley가 아닌 임시 지퍼 루프다.
최종 클립과 음량/거리 밸런스는 Unity에서 들어 보며 교체해야 한다.
환경 반복음용 AudioLoopEmitter 자체는 기존 구현을 유지했다. 임의의 기계/발전기 소리를 씬 전체에 추가하지 않았다.

## 검증

- Runtime Assembly-CSharp 컴파일: 오류 0, 기존 경고 38.
- 추가 EditMode 테스트 코드 별도 컴파일: 오류 0, 경고 0.
- `Tools/Audio/VerifyAudioGroundwork.ps1`: YAML/믹서 그룹/출력/외부 GUID/Cue Library/씬 연결 검사 통과.
- 테스트 코드 컴파일과 실제 Unity 테스트 실행은 다르다. 이 작업에서는 Unity 화면 조작, EditMode 테스트 실행, 청음은 하지 않았다.

Unity에서 `AudioGroundworkTests`를 실행하고 다음 플레이 경로를 확인할 것:

1. Master/Music/SFX를 각각 0으로 내려 독립 음소거 확인 -> 재시작 후 저장값 확인.
2. 일반 메뉴, 동적 퀘스트 버튼, 마우스/키보드 이동, 드롭다운, 비활성 버튼 확인.
3. 아이템 이동/분할/장착/해제/퀵슬롯 성공과 가득 찬 인벤토리/잘못된 장착 실패 비교.
4. 전리품 양방향 이동, 상점 구매/판매/수리 성공과 실패 비교.
5. 수색 시작/완료/거리 이탈 취소/ESC 일시정지/씬 전환에서 반복음이 남지 않는지 확인.
6. 세 NPC 인사/수락/창 닫기와 퀘스트 완료 확인 -> 저장 재접속에서 완료 알림이 반복되지 않는지 확인.

전체 경로 검증에는 기존 부트 흐름을 통해 PersistenceManager 씬이 로드돼야 한다.
단독 프리팹 미리보기에는 공통 재생 매니저가 없으므로 그 자체로 작동/청음 결과를 판단하면 안 된다.
