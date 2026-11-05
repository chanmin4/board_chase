# Game systems integration — approved 2026-10-26

Source: 콘텐츠 수정·추가 계획, especially 추가사항, plus user's follow-up.
No Notion edits, no authored questline, no save reset, no unrelated checkout rollback.

## Decisions
- One Game System Tool including XP/levels; reuse Character & Status Maker and permanent upgrade/stat reward conventions.
- Timed recycling only; remove right-click dismantle. Numerical reroll remains HOME only.
- HOME permits passive equip/unequip/replace/reroll; raids prohibit these. Binding survives unequip.
- T function check remains movement-permitted, exempt from general interaction movement cancellation.
- Free linked-sector travel must not force-clear rooms, reset HFSM or respawn existing enemies. Real extraction stays separate.

## Checklist
- [x] Integrated authoring, XP event/settlement/dedup/save, usage gates, level bonuses and UI
- [x] Independent recycling timers, persistence/death/cancel/space handling, UI and recipes
- [x] Passive ranges/steps/fixed effects/penalties/runtime stats and HOME restriction
- [x] HFSM-preserving travel implementation, cooldown/cast/cancellation, visited-map policy, boss candidates
- [x] Infection gauge/stacks/ticks/event sources/treatment, virus rooms and infected enemies
- [x] Cover penetration/armor, alert/patrol/reinforcement, ratios/rarity colors
- [x] Preflighted warehouse quest rewards, acceptance grants, safe HOME spawn, loot authoring
- [x] Isolated runtime/editor compiler, managed regression and asset probes
- [ ] Native Unity import/EditMode execution and Play Mode checks (not run)

Unspecified numbers remain editable temporary defaults. Existing source assets stay authoritative.

Implementation and verification boundaries: Game-Systems-2026-10-26-Report.md.
