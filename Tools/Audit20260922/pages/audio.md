# 08. 오디오·믹서·Cue·표면 충돌음

검사 기준: 2026-09-22 / E:/github/board_chase/board_chase. 현재 파일의 정적 연결·핵심 실행 경로 기준이며, 별도 표시 없는 항목은 이번 감사에서 플레이 테스트한 결과가 아닙니다.

## 현재 재생 경로

게임 행동 → AudioCueEventChannelSO → 지속 AudioManager → SoundEmitter 풀 → AudioConfiguration의 mixer output → Master.mixer 순서다. AudioCueLibrary는 등록/검색/편집 카탈로그이며 모든 소리를 매 프레임 판단하는 별도 런타임 매니저가 아니다.

현재 프로젝트의 믹서는 Master.mixer 하나이며 Master/Music/SFX/UI/Voice/Ambience 그룹이 있다. 노출 설정은 MasterVolume/MusicVolume/SFXVolume이다. UI/Voice/Ambience 그룹이 존재한다고 설정 화면에 개별 볼륨 슬라이더가 있다는 뜻은 아니다.

## Cue 구성

AudioCueSO는 클립 그룹, 순차/랜덤 선택, 반복, gain, 기본 재생 설정을 가진다. AudioConfiguration은 2D/3D·거리·라우팅 등 재생 성격을 소유한다. SoundEmitter는 풀링되며 미리 준비하는 수량 기본값은 10이다.

이번 정적 검사에서 실제 AudioCue 폴더의 72개 Cue 모두 최소 하나의 오디오 클립 참조가 있고 그 GUID가 파일로 해석됐다. 템플릿은 별도다. 이것은 72개 소리를 실제 청취해 음량·품질을 승인했다는 뜻은 아니다.

## 총알 표면 충돌음

총알 충돌이 발생하면 맞은 collider에서 부모 방향으로 ObstacleSurface를 찾는다. 표면 타입은 Default/Metal/Wood/Concrete/Sand/Rubber. 공통 BulletImpactAudioCatalog에서 해당 표면 Cue와 설정을 고른다.

Layer는 충돌 대상을 걸러내는 역할이며 ‘Metal Layer이므로 금속 소리’라는 분류가 아니다. 렌더러 Material의 색/Shader를 자동 분석해 재질을 추측하지도 않는다. 장애물에는 표면 타입만 지정하고 사운드 목록은 공통 카탈로그에서 관리한다.

BasicBullet 지원 경로이며 Rocket은 제외된다. 칼/로켓 충격음을 이 일반 총알 표에 억지로 합치지 않는다. 같은 장애물 연타에 대한 짧은 재생 간격 제한(0.04초)이 있고 Cue/Channel 연결이 없으면 소리가 나지 않는다. ObstacleSurface가 붙은 프리팹은 정적 집계 23개이며 그중 지정 씬 참조 그래프 내 15개가 연결된다. 그래프 포함은 활성 스폰 보증이 아니다.

## UI·수색·NPC·퀘스트

UIAudio: 패널/선택/장착/거래 피드백. InteractionSearchAudio: 수색 진행과 중단. NPC 프로필과 퀘스트 이벤트: 인사/수락/거절/완료 등. 이들은 기존 Cue 체계를 사용한다.

NPC_Greeting_Temp 등의 authoringNotes는 실제 녹음 대사가 아니라 임시 비언어 알림이라고 명시한다. 사운드 연결 밑작업과 최종 대사·믹싱 완성을 구별한다.

## 제작 기준

새 소리는 클립 → Cue → 기본 재생 설정 → 이벤트 호출 위치 → 믹서 경로 → Audio Cue Library 등록 → 실제 상황 청취 순으로 연결한다. 큰 총격/연사 중 동시 발음, UI 일시정지, 수색 취소, 화면 전환 후 루프 잔존, NPC 중첩을 확인한다.

## 기존 음원 자료

아래 원본 자료 DB는 보존한다. 해당 DB의 모든 행이 현재 Unity에 연결된 음원이라는 의미는 아니다. 현재 구현의 기준은 위 경로와 Unity AudioCue 자산이다.

## 코드·데이터 근거

- `E:/github/board_chase/board_chase/Assets/Scripts/Presentation/Audio/AudioManager.cs`
- `E:/github/board_chase/board_chase/Assets/Scripts/Presentation/Audio/AudioData/AudioCueSO.cs`
- `E:/github/board_chase/board_chase/Assets/Scripts/Authoring/Editor/AudioCueLibraryWindow.cs`
- `E:/github/board_chase/board_chase/Assets/Scripts/Presentation/Audio/ObstacleSurface.cs`
- `E:/github/board_chase/board_chase/Assets/Scripts/Presentation/Audio/BulletImpactAudio.cs`
- `E:/github/board_chase/board_chase/Assets/Scripts/Presentation/Audio/InteractionSearchAudio.cs`
- `E:/github/board_chase/board_chase/Assets/Scripts/Presentation/Audio/UIAudio.cs`
- `E:/github/board_chase/board_chase/Assets/ScriptableObject/Audio/AudioCue/SFX/Interactable/NPC_Greeting_Temp.asset`