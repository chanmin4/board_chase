# Shop Maker: editor-only workflow redesign

## Ownership / compatibility

- Keep ShopProgressionTreeWindow class, file meta and existing asset/save IDs. The only shop editor menu is Tools > Authoring > Shop Maker (legacy menu alias removed on 2026-09-30).
- ItemSO owns common PurchasePrice (also used by selling/repair). Existing per-product overrides and barter remain explicit.
- ShopConfigSO owns product registration, stock, weights, unlock groups and prerequisite links.
- QuestDefinitionSO's existing ShopUnlock reward is the only quest-to-group source of truth.
- Append new quest rewards; never reorder or silently delete rewards (reward-granted saves use array indices).
- Do not alter runtime algorithms, production assets, save files, prefabs or scenes during tool installation.

## Author workflow

1. Tools > Authoring > Shop Maker. Register persistent ItemSOs by picker or multi-drag.
2. Browse collapsible categories and rarity-grouped cards, ordered rarity/category/name.
3. Edit ItemSO base price, optional shop override/barter, stock and appearance weight.
4. Choose initially available or quest-gated. Attach product to a group, and link a QuestSO reward to that group.
5. Group view shows quest -> group -> products. Existing prerequisite groups remain editable with cycle prevention.
6. Open the exact quest reward in Quest Maker or open a reward's group in Shop Maker.
7. Check cross references/payment, Save touched assets only. Undo is supported; viewing never saves.

Multiple groups granting a product are OR. Multiple prerequisite groups are AND. Unlocking adds eligibility for future offer rolls, not guaranteed immediate display. Existing saved offers are not rerolled.

## Files

- Assets/Editor/ShopProgressionTreeWindow.cs: window, category/card browser, splitters, save tracking.
- Assets/Editor/ShopProgressionTreeWindow.Workbench.cs: product price/stock and unlock authoring UI.
- Assets/Editor/ShopMakerAuthoring.cs: registration, safe append-only quest linking, group relationships, read-only validation.
- Assets/Tests/EditMode/Editor/ShopMakerTests.cs: in-memory data tests, no production asset writes.
- Tools/ShopMaker/compile.cjs: Unity-response-file compilation to Library only.

## Verification

Compile with `node Tools/ShopMaker/compile.cjs`. In Unity, use Tools > Authoring > Verification > Run Shop Maker Tests.
Checks cover fresh defaults, duplicate registration, IDs, quest reward preservation, OR/AND link shape, cycle rejection, price override/barter preservation, sorting source indices, unlink/removal scope, validation read-only behavior, Undo, and live GUI rendering/clicks.

2026-09-29 result: Editor compilation passed. Unity verification: 23 memory-data tests + 7 UI checks = 30 passed / 0 failed. Report: Library/ShopMakerVerification/unity-tests.txt. The temporary automatic verification trigger was removed; the explicit verification menu remains available.
