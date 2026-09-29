# Scripts architecture

## Purpose

`Assets/Scripts` is organized by responsibility and execution boundary rather than by historical feature names. The hierarchy is intended to make ownership and dependency direction obvious while preserving the project's existing runtime behavior.

This reorganization is structural only. Existing type names, namespaces, public APIs, serialized fields, event flows, execution order, source logic, and script GUIDs remain unchanged. Most scripts still compile into Unity's default `Assembly-CSharp` or `Assembly-CSharp-Editor`; only the reusable StateMachine framework keeps explicit asmdef boundaries.

## Top-level responsibilities

| Path | Responsibility | `.cs` files |
|---|---|---:|
| `Authoring` | Inspector extensions, Editor windows, placement and generation tools | 18 |
| `Core` | Events, input, persistence, scene flow, runtime anchors, and common foundations | 97 |
| `Diagnostics` | Runtime observation and debug probes | 1 |
| `Gameplay` | Combat, entities, items, interaction, progression, and world rules | 413 |
| `Presentation` | Audio, camera, HUD, menus, and other user-facing presentation | 142 |
| `Scenes` | Scene-specific bootstrap and composition | 2 |
| `StateMachine` | Reusable state-machine framework with explicit asmdefs | 24 |
| `WIP` | Quarantined legacy, inactive, or deferred code | 40 |

`Camera`, `Effect`, `RiskSelectionScene`, and `System` are empty remnants of the former hierarchy. New scripts must not be added there. Their folder assets can be removed later only after a Unity asset import check confirms that no external tooling relies on them.

## Gameplay ownership

The `Gameplay` root contains only cross-domain coordinators such as `GameManager`, `GameStateSO`, `MaskRenderManager`, and `SpawnSystem`. A generic `Manager` folder is not an ownership boundary; managers live beside the domain they coordinate.

```text
Gameplay (413)
├─ Combat (9)
│  ├─ Infection (3)
│  ├─ PaintMark (5)
│  └─ Projectiles (1)
├─ Entities (297)
│  ├─ Animation (1)
│  ├─ Common (72)
│  ├─ Creature (136)
│  └─ Shooter (88)
├─ Interaction (7)
├─ Items (31)
├─ Progression (15)
└─ World (50)
```

### Combat

`Gameplay/Combat` owns combat mechanics that are not exclusive to one entity type.

- `Infection` owns infection control runtime behavior and shared rules.
- `PaintMark` owns paint-mark state, mask-render data, and related visualization contracts.
- `Projectiles` owns the shared projectile root and projectile registration lifecycle.

Entity decision-making belongs in `Entities`. Weapon, ammunition, projectile-instance, and throwable definitions belong in `Items`.

### Entities

`Gameplay/Entities` owns actors, their state, capabilities, AI, and lifecycle.

- `Common`
  - `Core`: damage, invulnerability, drops, and component resolution.
  - `Config` and `Definitions`: shared entity data.
  - `Equipment`, `KnockBack`, and `StatusEffects`: reusable actor capabilities.
  - `StateMachine/Actions` and `StateMachine/Conditions`: game-domain state actions and conditions.
- `Creature`
  - shared creature-enemy combat and AI;
  - `CommonEnemyStateMachine`;
  - `NamedEnemy` boss and named-pattern behavior;
  - `NormalEnemy` variants such as Chaser, Roller, Spitter, and Worm;
  - creature-specific audio, effects, melee, projectiles, and configuration.
- `Shooter`
  - `Common`: shooting, equipment, stats, effects, vision, and shared combat state;
  - `Enemy`: enemy-shooter controllers, effects, state machine, configuration, and Raider implementation;
  - `Player`: player input, aiming, shooting, dash, inventory, passives, upgrades, and effects.
- `Animation/LegacySciFi` keeps existing animation compatibility code. It is not a destination for new general-purpose animation code.

`Gameplay/Entities/Common/StateMachine` contains game-specific behaviors. It is separate from the reusable framework under top-level `Assets/Scripts/StateMachine`.

### Items

`Gameplay/Items` owns item definitions and item runtime behavior.

- `Definitions`: common `ItemSO` types such as consumables, currency, and passives.
- `Weapons/Definitions`: weapon and ammunition data.
- `Weapons/Runtime`: bullet, weapon-view, throwable, and area-effect runtime behavior.
- `World`: pickups, visual rotation, spawning, and world-to-inventory transfer.

The empty `Gameplay/Items/CharacterItem` folder is a former-layout remnant and is not a destination for new code.

### Interaction

`Gameplay/Interaction/Core` owns the interaction contract and manager. `Gameplay/Interaction/QTE` owns QTE bases and concrete QTE modes.

### Progression

`Gameplay/Progression` owns run progression and long-term progression rules.

- `Difficulty`: difficulty runtime and definitions.
- `Level`: kill reward and level progress.
- `Meta`: meta upgrades and roguelike currency.
- `Risk`: risk definitions, selection sets, and session state.
- `Stage`: stage flow and progression managers.

`Stage/LegacyDefinitions` is compatibility or removal-review territory. New stage definitions should use the active stage ownership model.

### World

`Gameplay/World/Sectors` owns the generated stage world.

- the sector root owns occupancy, painting, cleanup, enemy spawning, and `SectorRuntime`;
- `Runtime` owns occupancy, portal, and sector-state managers;
- `Portal` owns sector edges, directions, and connections;
- `StageMap` owns room nodes, room types, stage generation, and sector instantiation;
- `Named_Boss` owns named and boss-sector presentation flow;
- `Shop` and `Treasure` own special room interaction and reward behavior;
- `ScriptableObject` owns active stage, sector, run-flow, and preset data.

## Presentation ownership

`Presentation` contains code whose primary responsibility is what the player sees or hears.

- `Audio` owns audio playback, audio event routing, and audio definitions.
- `Camera` owns camera-facing presentation controllers.
- `UI/Common` owns reusable UI infrastructure.
- `UI/Gameplay` owns the HUD, panels, inventory, shop, loot, maps, status displays, vision overlays, and world-space gameplay UI.
- `UI/Menu` owns non-gameplay menu presentation.

Gameplay code should not directly own concrete UI or audio widgets. It exposes state or events; presentation code observes those public contracts.

## Core ownership

`Core` contains application-wide facilities that do not express a combat, world, or UI rule.

- `Anchors`: runtime scene anchors.
- `Application`: pause and application-level coordination.
- `Events`: event channels and neutral messages.
- `Foundation`: shared base types and low-level helpers.
- `Input`: input reading and input contracts.
- `Persistence`: save/load infrastructure.
- `SceneFlow`: scene loading and transitions.

Some existing typed event channels refer directly to gameplay or presentation types. That is a pre-existing coupling, not a template for new dependencies. Prefer neutral DTOs or place a new contract next to the domain that owns it.

## Dependency direction

`A -> B` means that A may depend on B.

```text
StateMachine.Runtime -> Unity / external packages
Core                 -> Unity / external packages
Gameplay             -> Core + StateMachine.Runtime
Presentation         -> Core + Gameplay public state and contracts
Scenes               -> Core + Gameplay + Presentation
Diagnostics          -> observed runtime groups
Authoring(Editor)    -> authored runtime groups

WIP -> active code: temporarily allowed during recovery
active code -> WIP: forbidden
```

Most folders share `Assembly-CSharp`, so this direction is a maintenance rule rather than a compiler-enforced rule. New code should follow it even when the compiler would allow the reverse dependency.

## StateMachine asmdef exception

Top-level `StateMachine` preserves two independent assemblies:

- `VSplatter.StateMachine`: reusable runtime framework with no dependency on project gameplay types;
- `VSplatter.StateMachine.Editor`: Editor-only tooling that references the runtime framework.

Consequently, StateMachine runtime code cannot depend on `Core`, `Gameplay`, or `Presentation` types from `Assembly-CSharp`. `Assembly-CSharp` may use the auto-referenced StateMachine runtime.

Execution-boundary labels used by source headers are:

- general runtime: `Runtime (Assembly-CSharp)`;
- general `Editor` folders: `Editor only (Assembly-CSharp-Editor)`;
- StateMachine runtime: `Runtime (VSplatter.StateMachine)`;
- `StateMachine/Editor`: `Editor only (VSplatter.StateMachine.Editor)`.

## Authoring, diagnostics, and scene composition

- `Authoring` is Editor-only and may inspect or generate runtime assets. Runtime code must never depend on it.
- `Diagnostics` observes runtime systems but must not become the owner of gameplay behavior.
- `Scenes` composes already-owned systems for a specific scene. Shared behavior must be promoted to its owning Core, Gameplay, or Presentation domain.

## WIP policy

`WIP/Legacy` is a quarantine for code that is unused by active production paths but still requires deletion review.

- Do not implement new features in WIP.
- Active production code must not acquire new references to WIP types.
- Promote a reused script to the correct active domain and move its `.meta` with it.
- Before deletion, check prefab, scene, ScriptableObject, and Addressables GUID references.
- WIP is not a compile exclusion. Non-Editor WIP scripts still compile into `Assembly-CSharp`.
- Other folders named `Legacy`, such as HUD compatibility code, are likewise not automatically excluded from compilation.

### Latest deletion review (2026-08-14)

The current WIP audit found no active script directly referencing WIP types. Fourteen scripts were removed only after both checks passed: no non-WIP C# symbol usage and no prefab, scene, ScriptableObject, or asset GUID reference. The removed group was limited to empty, fully commented, or explicitly `NotUsing` code:

- legacy `CollectableItem`, `PistolWeaponSO`, empty `SpecialBullet`, `GameSceneManager`, `IsPlayerInCurrentSectorConditionSO`, and `RaiderAudio`;
- unreferenced legacy Board, HUD, Tile, and pollution helper scripts.

The remaining WIP files are intentionally retained because at least one of the following is true: a legacy scene/prefab still serializes the script GUID, the Editor tool is still usable, or active inventory UI still calls its compatibility API. In particular, `PlayerInventoryRuntime.LegacyAmmoSlotCompatibility` cannot be removed until the Player Inventory UI and Quick Slot UI stop calling its `Try*AmmoSlot` methods. Do not remove it just because the folder name says Legacy.

## Structural-refactor invariants

1. Move an existing `.cs` only together with its existing `.cs.meta`.
2. Never regenerate an existing script GUID.
3. Only newly created folders receive new folder metas and GUIDs.
4. Do not change declared type names, namespaces, public APIs, serialized fields, event flow, execution order, or runtime logic as part of a structural pass.
5. Filename alignment must not rename the declared type.
6. Stop on destination or meta conflicts rather than overwriting.
7. Verify the same script count and GUID ownership before and after movement.
8. Verify source SHA-256 by GUID before and after movement.
9. Treat behavior changes as a separate patch and review.

Role-header insertion is also a separate mechanical pass. The tool preserves BOM, newline style, and every original source byte after the inserted comment.

## Role-header convention

Every `.cs` begins with:

```csharp
/*
 * Role hierarchy: Gameplay > Entities > Common > Core
 * Primary responsibility: Describes the type's focused responsibility.
 * Execution boundary: Runtime (Assembly-CSharp)
 */
```

- `Role hierarchy` mirrors the current directory using ` > `.
- `Primary responsibility` is a one-sentence discovery aid.
- `Execution boundary` records the Unity assembly boundary.

The responsibility sentence is generated from the primary declaration and path heuristics. It is navigation metadata, not an API contract, and can be refined manually when a more precise description is known. The header generator skips files that already contain a role header; after a later move, update the recorded hierarchy and boundary to match the destination.

## Verification snapshot

File-level verification on 2026-08-14:

- `.cs` files: 788;
- adjacent `.cs.meta` files: 788;
- readable unique script GUIDs: 788;
- missing script metas: 0;
- duplicate script GUIDs: 0;
- role headers: 788/788;
- role-path mismatches: 0;
- execution-boundary mismatches: 0;
- WIP scripts: 27;
- explicit asmdefs below `Assets/Scripts`: 2.

Execution-boundary distribution:

- `Runtime (Assembly-CSharp)`: 691;
- `Editor only (Assembly-CSharp-Editor)`: 22;
- `Runtime (VSplatter.StateMachine)`: 15;
- `Editor only (VSplatter.StateMachine.Editor)`: 9.

The reproducible checks live in `Tools/ScriptRefactor`. They validate file structure, source integrity, GUIDs, and role headers. Unity reimport, compilation, PlayMode, and player builds are separate verification steps and were intentionally not run as part of this structural pass.
