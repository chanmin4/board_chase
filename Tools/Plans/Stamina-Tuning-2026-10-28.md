# 스태미나 수치 변경 (2026-10-28)

- 대시 소모 40 → 50.
- 달리기 초당 소모 15 → 20. 기존 deltaTime 연속 계산을 유지하므로 0.1초당 2에 해당하며 프레임률에 독립적이다.
- 마지막 소모 후 회복 대기 0.75 → 2초. 달리기 소모/대시 때마다 회복 시점을 다시 설정하는 기존 동작을 유지한다.
- 회복량 초당 15, 최대 플레이어 200/적 400, 이동 배율, 10% 입력 시작 제한은 변경하지 않았다. 12% 대시로 0이 되거나 승인된 달리기를 0까지 유지하는 규칙을 보존했다.
- 공통 ShooterCommonRules.asset과 소스 기본값을 함께 수정했다. 현재 캐릭터 설정은 공통 이동 규칙을 사용하므로 비활성 개별 복사값은 덮어쓰지 않았다. 임시 디버그 덮어쓰기는 계속 우선한다.

변경 파일: ShooterStaminaRuntime.cs, ShooterCommonRules.asset, StaminaInputTests.cs, Tools/GameSystems/VerifyStaminaInput.ps1.

실제 런타임 소스를 관리형 대역으로 실행한 입력·소모·회복 지연 검사 88개 통과. Unity 참조 분리 컴파일 런타임/에디터 오류 0개. Native EditMode/Play Mode 실행은 하지 않았다. dotnet build --no-restore는 기존 project.assets.json 부재(NETSDK1004)로 막히므로 분리 컴파일 결과와 구분한다. Notion·사용자 저장 파일은 수정하지 않았다.
