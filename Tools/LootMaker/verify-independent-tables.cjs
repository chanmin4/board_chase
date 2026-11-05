// Read-only saved-asset audit. Run from the Unity project root.
const fs = require('node:fs');
const path = require('node:path');
const assert = require('node:assert/strict');

const root = 'Assets/ScriptableObject/GameScene/GameItem/Loot';
const read = p => fs.readFileSync(p, 'utf8').replace(/\r\n/g, '\n');
const guidOf = text => text.match(/^guid: ([a-f0-9]{32})$/m)?.[1];
const ref = (text, field) => text.match(new RegExp(`^  ${field}: \\{fileID: (-?\\d+)(?:, guid: ([a-f0-9]{32}), type: \\d+)?\\}$`, 'm'));
const field = (text, name) => text.match(new RegExp(`^  ${name}: (.*)$`, 'm'))?.[1];
const scriptGuid = guidOf(read('Assets/Scripts/Gameplay/Items/Loot/LootObjectSO.cs.meta'));
function filesUnder(folder, suffix) {
  return fs.readdirSync(folder, { withFileTypes: true }).flatMap(entry => {
    const file = folder + '/' + entry.name;
    return entry.isDirectory() ? filesUnder(file, suffix) : file.endsWith(suffix) ? [file] : [];
  });
}
const boxes = filesUnder('Assets', '.asset').filter(p => read(p).includes(`guid: ${scriptGuid}`));
const tablePaths = filesUnder(root, '.asset.meta');
const assetsByGuid = new Map(tablePaths.map(p => [guidOf(read(p)), p.slice(0, -5)]));
const usedTables = new Set();
const categories = ['Other', 'Ammo', 'Medical', 'Military Gear', 'Treasure Box', 'Safe', 'Backpack', 'Metal Box'];
const counts = new Map();
const rows = [];
for (const boxPath of boxes) {
  const box = read(boxPath);
  assert.equal(ref(box, '_lootObjectType')?.[1], '0', `Legacy family remains: ${boxPath}`);
  assert.equal(field(box, '_overrideCategory'), '1', `Category is inherited: ${boxPath}`);
  const tableGuid = ref(box, '_lootTable')?.[2];
  assert.ok(tableGuid && assetsByGuid.has(tableGuid), `Missing own table: ${boxPath}`);
  assert.ok(!usedTables.has(tableGuid), `Shared table remains: ${boxPath}`);
  usedTables.add(tableGuid);
  const tablePath = assetsByGuid.get(tableGuid);
  const table = read(tablePath);
  const items = [...table.matchAll(/^      - item: \{fileID: (?!0[,}])[^\n]+/gm)].length;
  const category = categories[Number(field(box, '_category'))];
  assert.ok(category, `Unknown category: ${boxPath}`);
  counts.set(category, (counts.get(category) || 0) + 1);
  const beforePath = 'Library/LootOwnership-20260929/before-records/' + path.basename(boxPath);
  if (fs.existsSync(beforePath)) {
    const before = read(beforePath);
    for (const key of ['_worldPrefab', '_displayName', '_lootSlotCount', '_sizeClass'])
      assert.equal(field(box, key), field(before, key), `Changed ${key}: ${boxPath}`);
  }
  rows.push({ box: path.basename(boxPath, '.asset'), category, items, table: path.basename(tablePath) });
}
assert.ok(boxes.length > 0, 'No authored boxes found');
console.log(JSON.stringify({ boxes: boxes.length, uniqueTables: usedTables.size, familyLinks: 0, categories: Object.fromEntries(counts), rows }, null, 2));
