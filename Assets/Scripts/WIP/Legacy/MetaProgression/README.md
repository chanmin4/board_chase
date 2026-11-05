# Retired meta progression

Retired on 2026-10-28. This is not an active character-growth system.

- Scripts, source catalog, event assets and the dedicated stats prefab were moved here **with their original .meta GUIDs**. Existing prefab references are not broken.
- Compatibility types still compile so old serialized components and `Save._metaUpgrades` / `MetaUpgradeSaveData` / enum values can deserialize. Do not remove or renumber these while old saves need support.
- Purchase requests remain false, levels remain zero, preview snapshots are empty, and catalog modifier application is inert. Original catalog calculations are retained behind `#if false` for WIP reference only.
- Old UI entry points cannot open purchasing UI or compute old stats. Main-menu entry is forcibly off; the catalog and event channel have no CreateAssetMenu entries.
- Game System Tool no longer discovers or offers the legacy catalog type.
- Current growth comes from `SaveSystem.Active.Profile.QuestStatRewards` and `Profile.experience`; PlayerStatsRuntime combines those with authored base stats, equipment and equipped passives. PlayerStatsConfigSO is not a per-player save.
- `Tools/CombatModules/compile.cjs` includes only this specific WIP compatibility folder, not arbitrary WIP gameplay.

Passive common rules remain active outside WIP. Material and reroll cost live in PassiveModuleRules; timed passive recycling derives its recipe from that same material, its shared recycling time and the item's dismantle count. GameSystemRules retains processor capacity and the ten non-passive weapon/armor recipes. Existing queued recycling jobs retain their already-saved output and timer.

This retirement does not reset any profile, quest reward, XP, item, or run snapshot.
