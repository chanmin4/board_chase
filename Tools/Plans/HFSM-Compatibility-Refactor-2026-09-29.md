# HFSM compatibility refactor

## Contract (approved scope: behavior-preserving refactor)

- Keep every existing MonoScript GUID, serialized field, prefab and state/table asset reference.
- Existing tables: first authored source is the entry, first satisfied row wins, one transition per tick, destination updates in that tick, self transitions re-enter.
- Existing coordinator scheduling remains parent (-200), coordinator (0), child (+200). Do not flatten nested boss clocks or change their reset/resume decisions.
- Disabling an existing machine suspends it WITHOUT synthesizing Exit callbacks. Legacy reset(false) intentionally abandons the previous instance without Exit. New explicit stop/reset APIs distinguish interruption from transition; do not silently change legacy calls.
- Legacy condition preparation remains at the same lazy, cached evaluation boundary. Move mutating preparation out of boolean predicates without collapsing multiple awareness updates into one or eagerly evaluating short-circuited conditions.
- Serialized controller state stays on its existing component. Extract behavior into focused collaborators, retain facade methods for actions, diagnostics and other callers.
- UI layout is editor-only data. Moving graph nodes never changes transition order. Runtime data remains the existing SO assets and coordinator bindings.

## Work sequence

1. Record source/asset baseline; add characterization traces for flat scheduling, guard short circuit/cache, reset and suspend contracts.
2. Introduce a Unity-independent hierarchical runtime and bridge existing State / StateMachine into it. Explicit entry and stable priority default to legacy values. Add hierarchy descriptors for existing coordinator links.
3. Extract EnemyShooterCombatController domains while retaining method bodies/call ordering and serialized identity. Separate condition preparation from reads.
4. Graph: actual prefab/instance hierarchy, collapsible/navigable child tables, draggable nodes, selected transitions, reconnect/delete, priorities, live state path, resizable panels, persisted layout, validation.
5. Compile runtime/editor/gameplay assemblies; run behavioral/editor tests; compare authored asset hashes. Report separately static/managed tests versus in-Unity/play verification.

## Explicit non-goals

- No new Combat/Exploration hierarchy imposed on authored AI: visualize actual Alive/Mode/boss bindings. Authors may create new hierarchy explicitly later.
- No automatic success effects on cancellation, new death behavior, sensor throttling, combat rebalance, prefab/scene saves, or bulk asset migration.
- Do not treat WIP creatures as unused without a reference audit.

## Verification checklist

- Flat trace matches old loop, including self transitions and updating destination.
- Parent-to-child enter and child-to-parent exit, sibling LCA, interrupt/stop/reset, suspend/resume.
- Cached shared conditions retain expected short-circuit/order; empty guards remain non-triggering unless explicitly Always.
- Existing tables preserve entry and all authored transition/action/condition sequences.
- Multiple agents do not share runtime instances.
- Hierarchy adapter preserves coordinator scheduling/defaults/reset-on-enable, detects cycles for visualization.
- Graph edits preserve Undo, stable priority ties, entry independence, asset identity, and no mutation while displaying/debugging.
- Controller extraction preserves serialized fields/public methods and delegates each former body to one owner.

## Implemented

- A Unity-independent `HfsmRuntime` / `HfsmNode` is now the execution core behind the existing `StateMachine` component. Existing SO definitions compile through `StateGraphCompiler`. The old component scripts and serialized fields retain their identities.
- Explicit entry, stable priority, optional native hierarchy and exit reason are additive. Existing tables have none of these overrides and retain their original semantics.
- Both coordinators publish their actual hierarchy via `IStateMachineHierarchySource`; their child scheduling and reset/resume behavior are unchanged. This covers Enemy Base, Player and existing multi-machine boss/creature layouts without rewriting their prefabs.
- The original enemy controller facade is 424 lines (previously approximately 4,900). Behavior is delegated to ten focused domain objects in partial source files; queries and stateless utilities are separate. The original component intentionally still owns shared runtime fields/config references for serialization and caller compatibility.
- Condition preparation is explicit and lazy. It has NOT been changed to once-per-frame sensor updates: that would change existing behavior.
- The graph displays actual coordinator hierarchies and optional native sub-states, supports folding, inner-scope navigation, transition authoring, live state highlighting, persisted layout, resizable panels, Undo and scoped asset saving.
- New Action/Condition script templates now use the actual `VSplatter.StateMachine` namespace. Action templates separate cancellation cleanup from normal completion.

## Verification evidence

- Four assemblies compile against this checkout's Unity response files: `VSplatter.StateMachine`, `VSplatter.StateMachine.Editor`, `Assembly-CSharp`, `Assembly-CSharp-Editor`. Existing unrelated TMP obsolete warnings remain.
- Managed core: **13 passed / 0 failed**, including 40 seeded comparisons (3,200 ticks) with the original flat execution loop.
- In-Unity initial verification: **30 passed / 0 failed**; compiler compatibility, condition caching/short-circuiting, lifecycle policies, authoring and existing Enemy Base/Player hierarchy included. An additional Undo/Redo regression test is included in the final suite; see the latest report for its result.
- A separate editor window successfully displayed Enemy Base's actual Alive hierarchy, toggled/restored its fold state, and selected a state. The final verification also checks selection survives a model rebuild.
- Roslyn token comparison: **all 153 original controller method bodies and all original field declarations preserved**, allowing only instance-owner qualification in extracted domain bodies. This proves extraction fidelity, not physics/AI integration behavior.
- Hash comparison: **8,692 existing prefab/state/table/meta files, zero differences** at verification. No production scene/prefab saves or automatic content migration.
- Reports and original snapshots: `Library/HfsmRefactor-20260929/`. These are local verification outputs, not shipped gameplay assets.

## Remaining manual integration checks

The editor/managed tests do not simulate a complete RUNScene encounter. Before treating this as a gameplay release, run the same encounters as the pre-refactor build:

1. Enemy Base/Raider: spawn -> patrol -> investigate -> chase -> ranged/melee/throwable attacks.
2. Kill during attack, cover movement and investigation; verify old cancellation/death behavior and no extra projectiles/completion effects.
3. Disable/re-enable and pool reuse; verify resume versus the coordinator's explicit reset-on-enable behavior.
4. Each authored boss and Player: multi-layer transitions, death, revive/respawn and one-shot effects.
5. Use a **scene instance** in the graph during Play Mode to inspect active scopes. Prefab assets have no running state.

No claim of full Play Mode/NavMesh/physics parity is made from the unit tests alone.

See `Tools/Hfsm/README.md` for daily authoring and code extension guidance.
