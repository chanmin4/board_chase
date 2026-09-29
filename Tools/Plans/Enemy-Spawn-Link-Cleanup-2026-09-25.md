# 적 스폰 참조 / Unity 시작 시 반복 오류 정리 (2026-09-25)

## 원인과 적용

1. `BossConfigPrefabLinkProvisioner`는 이미 끝난 복구를 `[InitializeOnLoad]`로 매 세션 다시 실행했다. Punisher의 옛 `Butcher/Punisher.prefab` 경로를 찾으며 오류를 출력했다.
2. `StandardEnemyConfigPrefabLinkRepair`도 같은 방식으로 실행되며 존재하지 않는 `NormalEnemy/Scraper/Scraper.prefab`을 찾았다. 프리팹이 없을 때도 컴포넌트가 없다는 메시지를 출력했다.
3. 두 일회성 복구 스크립트와 `.meta`를 제거했다. 대체 자동 수정 스크립트는 추가하지 않았다. 현재 Punisher/Scraper 등 9개 Config의 프리팹 GUID와 EnemyShooter 컴포넌트 연결은 직접 검사했다.
4. Stage5Definition의 보스 Encounter는 기존 `Stage5TempBossEncounter` 에셋을 계속 사용한다. 이 에셋의 GUID와 경로를 유지하고 wave 참조만 `TempBossWave`에서 이미 존재하는 `Stage5BossWaveTemplate`으로 교체했다. Inspector 표시 이름은 `Stage 5 Boss Template Encounter`로 바꿨다.
5. 기존 `Stage5BossTemplateConfig`는 두 퀘스트(`RunMain_05_Stage5_Boss`, `Campaign_20_Stage5_Template`)의 목표와 동일하다. 최종 보스의 세부 설계는 여전히 TBD이며 이번 작업에서 신규 보스/보상/AI를 설계하지 않았다.

## 현재 Stage 5 연결

```text
Stage5Definition
  -> Stage5TempBossEncounter (기존 GUID 유지)
    -> Stage5BossWaveTemplate
      -> Stage5BossTemplateConfig
        -> Boss/Stage5BossTemplate.prefab : EnemyShooter
```

맵 규칙, 보스 확률, 스탯, 퀘스트 목표/보상, 씬 및 캐릭터 프리팹은 수정하지 않았다.

## 제거하지 않은 호환 데이터

`TempBossShooterConfig`, `TempBossGuardShooterConfig`, `TempBossWave`는 삭제하지 않았다. 옛 Config는 보스 프리팹의 초기 설정, 일부 퀘스트와 LootProgressionConfig에 참조가 남아 있어 단순 삭제하면 추가 Missing이 발생한다. 두 옛 Config와 `Stage4DemolitionGuardConfig`의 사라진 프리팹 참조는 별도의 레거시 콘텐츠 정리 대상이다. 이번 변경으로 Stage5의 활성 보스 Encounter는 옛 wave를 더 이상 사용하지 않는다.

## 검증

- Unity의 현재 컴파일 입력을 사용한 런타임 / 에디터 코드 컴파일 통과. 기존 TMP obsolete / 사용하지 않는 필드 경고만 존재.
- 정적 검사: 보스 6개 + Scraper 3개 스폰 GUID / 상속된 EnemyShooter 컴포넌트 local ID 일치.
- Stage5 Encounter -> wave -> config와 두 퀘스트 목표의 동일성 확인.
- 회귀 테스트 `Assets/Tests/EditMode/Editor/EnemySpawnLinkTests.cs`: 9개 스폰 연결, 5개 스테이지 보스 선택/전체 유효 스폰, Stage5 퀘스트 목표 일치, 완료된 복구 스크립트 부재. 총 16개 사례.
- 실제 열린 Unity에서 사용자의 Assets → Refresh 후 **16개 통과 / 실패 0**. 결과는 `Library/EnemyLinkCleanup-20260925/live-check.txt`. 임시 읽기 전용 검사 브리지는 결과 수집 후 코드와 meta 모두 제거했다. 영구 회귀 테스트만 유지한다.

## 복구 자료

삭제한 스크립트 2개와 meta, 변경 전 Stage5 Encounter는 `Library/EnemyLinkCleanup-20260925/`에 복사해 두었다. Library 캐시를 삭제하면 이 백업도 사라지므로 영구 백업이 아니다. 삭제한 자동 복구 스크립트를 그대로 복원하면 시작 시 옛 오류도 돌아온다.
