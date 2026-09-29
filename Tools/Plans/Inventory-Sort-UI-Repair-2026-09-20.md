# Inventory sorting UI ownership repair (2026-09-20)

## Cause

`PlayerInventoryUI.OnValidate` reached `EnsureSortButtons` while Unity was importing
the persistent UI prefab. The old generator created scene objects before trying to
parent them into the prefab asset. Unity rejected that parenting, leaving unbound
`SortCompact` / `SortRarity` roots in the currently active scene. Revalidation repeated
the creation. They were not a second, intentional inventory UI in RUNScene.

## Repair

- Author four buttons in `Assets/Prefabs/UI/GamePlay/PanelHub/Inventory/PlayerInventoryPanel.prefab`:
  `PlayerInventorySection/InventoryArea/{PocketSection,InventorySection}/Header/{SortCompact,SortRarity}`.
- `Canvas-GamePlay.prefab`, used by `Assets/Scenes/Manager/GamePlay.unity`, inherits those buttons.
- Persist each OnClick connection to the owning `PlayerInventoryUI.SortPlayerInventory(bool)`.
- Runtime code only refreshes the authored buttons; it never creates sort controls.
- Defer editor layout preview out of `OnValidate`; skip persistent prefab assets and foreign panel roots.
- Do not create missing section titles or pocket grids in edit mode.
- `InventorySortButtonMigration` removes only objects matching the old generator's exact
  signature in loaded RUNScene/GamePlay scenes. It preserves prefab-owned/wired buttons,
  supports Undo and marks modified scenes dirty without auto-saving.
- Leave sorting logic, item quantities, rarity order and inventory boundaries unchanged.

## Verification

- Unity 6000.0.47f1 editor imported the authored buttons and compiled runtime/editor assemblies.
- Direct Unity compiler check: runtime 0 errors (15 existing warnings); editor 0 errors, 0 warnings.
- Seven focused native-editor regression tests passed after the final prefab update:
  persistent bool arguments and header ownership, nested GamePlay prefab wiring,
  RUNScene exclusion, repeated validation without object creation, persistent-prefab
  safety, foreign-panel isolation and conservative legacy-object recognition.
- New script metadata GUIDs are valid 32-character GUIDs; new prefab local references resolve.
- Temporary native-editor verification harness was removed; regression tests remain in
  `Assets/Tests/EditMode/Editor/InventorySortUIOwnershipTests.cs`.
- No gameplay balance, scene loading flow or Input System package changes were made.
- Full Play Mode interaction/visual navigation was not exercised in this repair.
