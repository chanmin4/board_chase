// Emits apply_patch operations only. Does not write files or touch Unity scenes.
const fs = require('fs');
const read = p => fs.readFileSync(p, 'utf8').replace(/\r/g, '');
const patches = [];
function change(file, old, next) {
  if (old === next) return;
  patches.push('*** Begin Patch\n*** Update File: ' + file + '\n@@\n' +
    old.trimEnd().split('\n').map(l => '-' + l).join('\n') + '\n' +
    next.trimEnd().split('\n').map(l => '+' + l).join('\n') + '\n*** End Patch');
}
const categories = {
  tools: 'Drill Hammer PipeRench Pliers Saw Scissor Screwdriver Shovel Spanner',
  construction: 'Cable FuelCan Screwbolt Tape',
  medical: 'Pillbox',
  household: 'AbandonedBoots Candle Canteen Eraser Fork Lighter MatchBox Pencil Spoon Stapler',
  electronics: 'BigBattery Calculator SmallBattery',
  food: 'Cake CannedFood Chicken Cookie Milk Mushroom Peanutbutter TomatoSoup Wine',
  collectibles: 'Crown EarthModel GoldCoin MarsModel MercuryModel MoonModel Ring Trophy'
};
const folder = 'Assets/ScriptableObject/GameScene/GameItem/Valuable';
for (const [category, names] of Object.entries(categories)) for (const name of names.split(' ')) {
  const file = folder + '/' + name + '.asset';
  if (!fs.existsSync(file)) throw Error('Missing authored item: ' + file);
  const old = read(file);
  // Never overwrite an author's explicit category on rerun.
  if (/^  _valuableCategoryId: .+/m.test(old)) continue;
  const anchor = old.match(/^  m_Name:.*$/m)[0];
  change(file, anchor, anchor + '\n  _valuableCategoryId: ' + category);
}
const file = 'Assets/Prefabs/UI/GamePlay/PanelHub/Inventory/PlayerInventoryPanel.prefab';
const source = read(file);
if (!source.includes('m_Name: SortCategory')) {
  const docs = source.split(/(?=^--- !u!)/m), map = new Map(docs.slice(1).map(d => [d.match(/^--- !u!\d+ &(\d+)/)[1], d]));
  const edits = new Map(), added = [];
  let nextId = 932029000000000000n;
  const roots = [...map.entries()].filter(([id, doc]) => doc.includes('  m_Name: SortRarity\n'));
  if (roots.length !== 2) throw Error('Expected two authored rarity buttons');
  for (const [id, doc] of roots) {
    const componentIds = [...doc.matchAll(/component: \{fileID: (\d+)\}/g)].map(m => m[1]);
    const rectId = componentIds.find(key => map.get(key).startsWith('--- !u!224'));
    const rect = map.get(rectId), parentId = rect.match(/m_Father: \{fileID: (\d+)\}/)[1];
    const childRectId = rect.match(/m_Children:\n  - \{fileID: (\d+)\}/)[1];
    const childGoId = map.get(childRectId).match(/m_GameObject: \{fileID: (\d+)\}/)[1];
    const childIds = [...map.get(childGoId).matchAll(/component: \{fileID: (\d+)\}/g)].map(m => m[1]);
    const ids = [...new Set([id, ...componentIds, childGoId, ...childIds])];
    const remap = new Map(ids.map(key => [key, String(++nextId)]));
    for (const key of ids) {
      let clone = map.get(key).replace(/&(\d+)|fileID: (\d+)/g, (m, a, b) => remap.has(a || b) ? (a ? '&' : 'fileID: ') + remap.get(a || b) : m);
      clone = clone.replace('m_Name: SortRarity', 'm_Name: SortCategory')
        .replace('m_MethodName: SortPlayerInventory\n', 'm_MethodName: SortPlayerInventoryByCategory\n')
        .replace('m_Mode: 6\n', 'm_Mode: 1\n')
        .replace(/m_text: .*/, 'm_text: 분류').replace(/[ \t]+$/gm, '');
      if (key === rectId) clone = clone.replace('m_AnchoredPosition: {x: -4,', 'm_AnchoredPosition: {x: -156,');
      added.push(clone);
    }
    const parent = edits.get(parentId) || map.get(parentId);
    edits.set(parentId, parent.replace('  m_Children:\n', '  m_Children:\n  - {fileID: ' + remap.get(rectId) + '}\n'));
  }
  for (const [id, next] of edits) change(file, map.get(id), next);
  // Anchor the append to the complete final document, avoiding ambiguous end-of-file context.
  const tail = docs[docs.length - 1];
  change(file, tail, tail.trimEnd() + '\n' + added.join(''));
}
if (process.argv.includes('--patches')) console.log(JSON.stringify(patches));
else console.log(JSON.stringify({ patches: patches.length, files: [...new Set(patches.map(p => p.match(/Update File: (.*)/)[1]))] }));
