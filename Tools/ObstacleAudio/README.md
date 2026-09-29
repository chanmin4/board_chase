# 장애물 총탄 피격음

장애물 프리팹의 `Obstacle Surface > Surface`만 선택한다. 소리는 프로젝트의
`Assets/Resources/BulletImpactAudioCatalog.asset`에서 표면별로 공통 관리한다.
별도 씬 오브젝트나 장애물별 AudioSource/이벤트 연결은 필요하지 않다.
실제 재생은 기존 PlaySFX 채널과 AudioManager의 SoundEmitter 풀을 사용한다.

## 현재 적용

`Assets/Prefabs/GameScene/Sector/SectorObject/ObstacleObject`의 23개 프리팹:

| 표면 | 프리팹 | 공통 Cue |
| --- | --- | --- |
| Metal | Barrel 2개, MetalBox 3개, ShippingContainer 6개 | MetalImpact |
| Wood | Box 2개, Pallet 4개 | WoodImpact |
| Concrete | Fountain, RailwayPart | ConcreteImpact |
| Sand | Sand 2개 | WhooshImpact |
| Rubber | Wheel 2개 | RubberImpact |

복합 물체는 주된 외관을 기준으로 분류했다. 필요한 경우 Surface를 바꾸면 된다.
Wood/Concrete는 설치된 Universal Sound FX의 짧은 나무/돌 충격음을 초기값으로
연결했고, Rubber는 전용 타이어 녹음 대신 둔탁한 일반 충격음을 임시 사용한다.
Metal/Sand는 기존 연결을 재사용했다. 음색과 음량은 실제 플레이로 최종 조정한다.
새 Cue 3개는 기존 Audio Cue Library에도 등록했다.

## 새 장애물 설정

1. 프리팹 루트에 `Obstacle Surface` 컴포넌트를 추가하고 Surface를 선택한다.
2. 총알에 맞아야 하는 Collider의 Layer를 `SectorStructure`로 설정한다.
3. Collider가 자식에 있으면 가장 가까운 부모의 Surface를 사용한다.
4. 같은 오브젝트에 기존 `ObstacleAudio`를 중복으로 붙이지 않는다.

이번 폴더에서는 기존 ObstacleAudio를 교체했다. 다른 폴더에서 사용하는
기존 ObstacleAudio 스크립트와 이벤트 동작은 유지된다.
Fountain_01, Pallet_02, RailwayPart_01은 Default Layer에서 SectorStructure로
변경했다. 메시, 머티리얼, Transform, Collider 형태는 유지했다.

## 적용 범위

BasicBullet의 실제 Collider 충돌에서만 실행한다. 플레이어/적 모두 동일한
경로이며 Light/Medium/Heavy/Sniper/Shotgun의 모든 티어에 적용한다.
Rocket은 BasicBullet을 사용하더라도 제외한다. 특수탄(주입/페인트/장판),
칼, 투척물, 폭발, 사거리·수명 만료에는 이 피격음을 실행하지 않는다.
표면 컴포넌트가 없는 적이나 다른 물체의 피격음도 변경하지 않는다.

재생 위치는 장애물 중심이 아니라 SphereCast의 실제 hit.point다.
AI의 소음 감지 이벤트, 총탄 피해 및 소모 처리는 별도로 유지한다.
Default/누락/빈 Cue/Looping Cue에는 공통 Default Cue를 사용한다.

`Minimum Interval Per Obstacle` 기본값은 0.04초다. 같은 장애물에 동시에
맞는 산탄 소리만 묶으며, 다른 장애물의 소리는 차단하지 않는다.
0으로 바꾸면 모든 명중마다 재생한다. 이 설정은 피해나 AI 청각을 줄이지 않는다.

## 확인

EditMode 테스트: `ObstacleBulletImpactAudioTests`.
탄종 필터, 자식 Collider 표면 조회, 접촉 위치, 산탄 중복 제한, 실패 시 재시도,
기본음 처리, 전체 프리팹 및 공통 목록 연결을 검사한다.

실제 씬에서는 AudioManager/AudioListener가 있는 상태로 금속과 나무 장애물을
쏴서 음색/위치 차이를 확인하고, 샷건 및 로켓 제외 동작을 확인한다.
