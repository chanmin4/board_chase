# Shooter Customize — 실시간 외형 편집

## 사용법

메뉴는 기존 `Tools > Authoring > Shooter Customize Station` 그대로다.

1. 상단 Shooter에 편집할 프리팹을 넣는다. 호환되는 ShooterVisualRig 또는 Visual_Shooter 자식을 찾는다. 기본 Visual_Shooter는 창을 열면 자동 표시된다.
2. 기존 외형은 Look에서 선택한다. 새 외형은 편집 후 Save As로 만든다.
3. 왼쪽 Body / Hair / Eyes / Mouth / Face / Armor / Feet 등 부위를 선택한다.
4. 오른쪽에 악세사리 프리팹을 드래그하거나 + Add로 추가하고, 카드 선택 후 길이·폭·깊이·위치·회전을 조절한다. Body에서는 몸통 비율과 기본 몸 색상을 조절한다.
5. Save Look으로 ShooterCustomizeSO에 저장한다. 원본 시각 프리팹에 변경을 직접 Apply하지 않는다.

기존에 해당 Look을 참조하는 캐릭터는 저장된 변경을 사용한다. 새 Look을 만들었으면 사용할 캐릭터의 ShooterVisualRig > Default Customize에 연결하거나 Advanced > Export Visual Prefab으로 바인딩된 새 시각 프리팹을 만든다. Save Look 자체가 모든 슈터를 자동 교체하지는 않는다.

가운데 캐릭터를 왼쪽 드래그하면 회전, 휠은 확대/축소, 오른쪽/가운데 드래그는 이동이다. Front / Side / Back / Fit 버튼과 현재 부위의 Anchor 표시가 있다. 편집마다 자동으로 카메라를 재정렬하지 않아 크기 변화가 눈에 보인다. 크게 늘려 화면을 벗어나면 Fit을 누른다.

## 장비와 고정 외형 구분

- 왼쪽 Gas Mask / Goggles에서 GasMaskItemSO를 넣으면 착용 모습을 보여준다.
- 장비 프리팹·장착 소켓·위치를 편집해도 Save Equipment 전에는 해당 ItemSO를 수정하지 않는다.
- 고정 외형은 ShooterCustomizeSO, 가스마스크는 기존 GasMaskItemSO에 각각 저장한다. 장비 프리뷰가 고정 악세사리에 섞여 저장되지 않는다.
- Face 장착 시 Eyes/Mouth가 숨겨지고 해제 시 다시 보이며, Hair는 별개다. 런타임 ShooterVisualRig의 기존 가림 규칙을 미리보기에도 그대로 적용한다.
- 개별 악세사리의 occupiedRegions와 원본 프리팹 참조를 유지한다. 씬에서 생성된 복제물의 prefab 연결을 역추적하여 저장하던 방식은 제거했다.

## 안전성과 구현

- 작업 씬 대신 PreviewRenderUtility의 독립 preview scene을 사용한다. 사용자의 열린 씬에 오브젝트를 추가하거나 씬을 저장하지 않는다.
- Draft는 Editor 전용 작업 사본이다. 저장 전 수정은 프로젝트 asset에 기록되지 않으며, 전환/닫기 시 미저장 확인 및 Undo/Redo를 제공한다. 도메인 리로드 때 JSON으로 작업 사본을 복구한다.
- 창을 닫거나 Play Mode로 들어가면 카메라·렌더 텍스처·임시 오브젝트를 정리한다. Play Mode 중 편집은 잠근다.
- URP 호환 렌더링을 위해 PreviewRenderUtility.Render의 SRP 옵션을 사용한다. API 확인: [Unity 6000.0 PreviewRenderUtility 소스](https://github.com/Unity-Technologies/UnityCsReference/blob/6000.0/Editor/Mono/Inspector/PreviewRenderUtility.cs).
- Advanced에 소켓 전체 배치, 가림 영역, SO 선택 및 프리팹 Export를 모았다. 기존 클래스·메뉴·스크립트 GUID와 런타임 저장 형식은 유지한다.
- Export는 원본 프리팹을 덮어쓰지 않고 새 파일로만 만든다. Shooter 전체 안의 독립 에셋이 아닌 시각 자식을 선택했다면 Look 저장은 가능하지만 Export는 독립 시각 프리팹을 선택해야 한다.

## 기본 Visual_Shooter 충돌체 분리

기존 Root/Meshes/Capsule에 붙은 CapsuleCollider를 Root/GameplayColliderRoot로 옮겼다. Collider fileID `549841994017151066`, Layer 3, 로컬 위치 (0,1,0), 회전 0, 스케일 1, 중심 0, 반지름 0.7, 높이 1.4를 유지했다. 기존 메시와 충돌체의 초기 월드 변환도 같다.

따라서 BodyVisualRoot의 크기를 변경해도 해당 충돌체는 변하지 않는다. 그 외 프리팹은 자동 마이그레이션하지 않는다. 선택한 다른 프리팹에 여전히 몸통 메시 아래 충돌체가 있으면 몸통 크기 조절을 잠그고 경고한다. 런타임 데미지·충돌 계산 코드는 수정하지 않았다.

## 검증 및 한계

- 프로젝트의 Unity 6000.0.47f1 Roslyn 설정으로 전체 Editor + 새 테스트 코드 컴파일 성공.
- 별도 임시 Unity 프로젝트에서 12개 회귀 테스트 통과: 독립 씬/정리, 원본 보존, 반복 저장/로드, 프리팹 참조·소켓·가림 영역 보존, Face 가림, 장비 분리, 체형/충돌체 분리, 카메라 기준 유지, 소스 확인, 복구 JSON, Undo/Redo, 실제 asset 저장/재임포트, 장비 시각 필드만 저장, 기본 프리팹 충돌체 ID/변환/치수 유지.
- 1060×620 / 1280×760 / 1600×900에서 실제 창의 일반/장비 편집 GUI를 실행해 레이아웃 영역 초과와 Error/Assert가 없음을 확인했다.
- 임시 프로젝트에서 실제 Visual_Shooter 메시를 렌더하여 색/몸통 비율 변경과 정면·측면을 육안 확인했다. 이 검증은 기본 테스트 재질을 사용했으며 사용자 프로젝트의 URP 렌더러 설정·모든 커스텀 재질·실제 마우스 드래그 전체를 검증한 것은 아니다.
- 현재 기능은 Transform 기반 비율/배치 및 악세사리 장착이다. 메시 조각, 얼굴 모프, 블렌드셰이프, 애니메이션 제작 기능은 아니다. 비어 있는 부위에는 악세사리 프리팹을 넣어야 보이며, 소켓 이동은 해당 소켓의 장착물에 적용된다.
