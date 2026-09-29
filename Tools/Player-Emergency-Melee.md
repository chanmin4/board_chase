# Player emergency melee

## Current behavior

- Empty active weapon slot: attack input uses the existing unarmed swing.
- Ranged weapon with rounds loaded: normal shooting.
- Empty magazine with reserve rounds for the selected ammo: existing automatic reload takes priority.
- Empty magazine without reserve rounds for the selected ammo: attack input swings the equipped gun.
- Other ammo types remain manually selectable; this change does not automatically switch ammo types.
- Existing melee-input binding follows the same eligibility rules. Pause, blocked input, dash, reload and active usable-item gates remain in place.
- Default melee damage is **5 HP**, range **2 world units**, cooldown **0.8 seconds**. Existing hit zone, swing animation, sound and particles are reused; no new weapon art is required.

## Tuning

- Damage: `Assets/ScriptableObject/GameScene/Character/Shooter/Player/Config/PlayerBaseStatsConfig.asset` -> **Emergency Melee / Melee Damage**.
- Range, arc, timing and effects: `Assets/ScriptableObject/GameScene/Character/Shooter/Player/Config/PlayerEmergencyMelee.asset`.
- Player melee damage is read from player stats, not the profile's legacy Damage field, equipped gun damage, or bullet damage.
- The runtime snapshot and player stats panel expose Melee Damage. `PlayerStatId.MeleeDamage` is appended as ID 37; existing serialized IDs are unchanged.
- No melee upgrades or melee-specific kill quests were added. Those remain a separate design decision. Existing hit/death events still fire; weapon-specific quest attribution has not been redesigned for gun bashes.

## Damage contract

Emergency melee bypasses armor and incoming damage multipliers (including reductions/amplification), without damaging armor durability. Death, invulnerability and dodge gates are preserved. Ordinary bullets and NPC melee continue through the existing damage pipeline.

## Dedicated melee weapons: WIP, not deleted

- Item Maker has a **WIP / Melee Weapons** category and **WIP Melee Weapon** template.
- The current dedicated melee ItemSO, CombatKnife, is stored in `Assets/ScriptableObject/GameScene/GameItem/Weapon/WIP/` with its original GUID.
- Existing model, view prefab, SO class, legacy melee profile and NPC equipment references are retained.
- CombatKnife is removed from the player's starting second weapon slot and authored normal loot pools. NPC melee weapons are not offered as corpse equipment loot.
- Player inventory equip paths reject dedicated melee weapons; legacy pickups can still be stored without auto-equipping or deleting them.
- Old saves/catalog entries are not erased. If an old save has a knife equipped, unequip it to use fists. Existing NPC melee behavior is unchanged.

## Verification

The runtime assembly and changed Item Maker/editor-test sources compile. Nineteen regression cases pass in an isolated Unity 6000.0.47f1 batch-mode project using the compiled production runtime assembly: fallback eligibility, actual magazine/reserve combinations, startup readiness, independent player damage, armor and damage-multiplier bypass, ordinary armor damage, invulnerability, death and non-positive damage.

Source tests: `Assets/Tests/EditMode/Editor/PlayerEmergencyMeleeTests.cs`.

The active game project was not operated through the UI. Actual Play Mode swing appearance, animation timing and range feel still need an in-game check against a nearby enemy.
