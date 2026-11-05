# Loot Maker: occupied slots and per-box rarity weights

## Contract

- `LootObjectSO` owns Total Slots, Min Filled Slots, Max Filled Slots, and four relative rarity weights. Size presets only change capacity: 3 / 4 / 6 / 9 / 12. The effective fill range is constrained to `0 <= min <= max <= capacity`.
- `LootTableSO` owns eligible items, item quantity per slot, and within-tier shares. Sharing/copying a table does not change the box's capacity, fill range or rarity weights.
- First preparation chooses an inclusive random occupied-slot count. For each occupied slot, choose a populated tier using the box's positive weights, then choose an item using its within-tier share. There is no independent success/failure chance.
- Empty or disabled tiers are excluded. E.g. 60 / 30 / 8 / 2 with only Unique and Legendary candidates yields 80% / 20%. UI shows actual per-filled-slot tier and item probabilities. An item share is conditional within its tier, not a whole-container drop probability.
- One result goes into one randomly chosen distinct inventory slot. Duplicates do not auto-merge during generation. Quantity is capped to one MaxStack; it does not spill into more slots or world pickups. Normal inventory transfers/stacking are unchanged.
- An explicit 0..0 box can be empty even with no table candidates. If max > 0 but there are no eligible positive-weight candidates, preparation fails with a configuration warning and does not commit/consume a lock key as a valid empty result.
- Lock/key access remains. Legacy LockedLootProfile bonus rolls and rarity multipliers are NOT stacked on the new box contract. Author all slot-based container rewards in Box Settings. Legacy APIs/data remain for compatibility; Enemy Drops are not changed.

## Compatibility and authoring

- Existing assets are not blindly rewritten on load. Before first explicit edit/save, legacy override count or shared default count provides the initial fill range, capped to capacity. Old per-attempt success probability is retired, not translated to a hidden reduction.
- Initial per-box weights preserve the previous base 63 / 30 / 6 / 1. They can now be independently edited, including zero to disable a populated tier. Invalid negative/NaN/infinite weights resolve to zero.
- Save in Loot Maker freezes legacy ranges into versioned per-box fields. Changing table links freezes the prior range first. Replacing table contents freezes existing asset users before replacing old defaults. No startup editor provisioner is installed.
- Loot Maker and the ordinary LootObjectSO Inspector both use the new fields. Standard Undo and asset saving apply.
- Card selection is read-only. Total-slot changes and presets do not modify quality or shared item pools.
- Old generated raid containers remain authoritative: their item indices, quantities and conditions are restored without rerolling. Reopening no longer reapplies a smaller authored capacity, which previously could truncate restored items.

## Validation

Runtime/editor compilation passed after the core patch. Tests cover exact occupied counts, empty ranges, capacity clamps, duplicate stacks, per-stack quantity, empty-tier normalization, independent boxes, effective per-item chances, malformed weights, legacy conversion, Undo, deterministic placement, nonempty save restoration/capacity reduction, and absence of hidden lock rewards.

Actual Unity verification after the user saved and refreshed: **59 passed, 0 failed** (47 LootMakerTests cases + 12 authoring cases). Report: `Library/LootMakerAuthoring-20260929/slot-tests-v1.txt`. The temporary verifier used its own additive test scene and uniquely named temporary test assets; it did not save the user's scenes. The temporary source and meta were removed after verification. End-to-end mouse interaction and a full raid were not tested.

## Bandage example content

After the user's Save / Refresh confirmation, `LootObject_HealthBox_01` (MedicalBox_verySmall) was linked to the prepared independent Bandage Example family/table. Existing saved capacity was preserved (6); no size preset was silently applied. Set Min/Max Filled Slots to 1/2, normal/rare/unique/legendary weights 63/30/6/1. With only BandageLV1 (Normal, quantity 1..2) and BandageLV2 (Rare, quantity 1), actual per-filled-slot probabilities are about 67.74% / 32.26%. Other medical boxes and item definitions remain unchanged. This content link was checked statically after the 59-test code run.
