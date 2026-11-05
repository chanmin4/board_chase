const fs = require('fs'), assert = require('assert'); let checks = 0;
const read = p => fs.readFileSync(p, 'utf8').replace(/\r/g, '');
const check = (v, m) => { checks++; assert(v, m); };
const catalog = read('Assets/Resources/ValuableCategories.asset');
const entries = [...catalog.matchAll(/  - id: (.+)\n    label: (.+)/g)].map(m => ({ id: m[1], label: m[2] }));
check(entries.length === 8 && new Set(entries.map(e => e.id)).size === 8, 'eight unique stable category IDs');
for (const name of ['도구','건설자재','군용물품','의료물품','생활품']) check(entries.some(e => e.label === name), 'requested category ' + name);
const ids = new Set(entries.map(e => e.id)), counts = Object.fromEntries(entries.map(e => [e.label,0])); counts['미분류'] = 0;
const folder = 'Assets/ScriptableObject/GameScene/GameItem/Valuable';
for (const name of fs.readdirSync(folder).filter(n => n.endsWith('.asset'))) {
  const text = read(folder + '/' + name), category = text.match(/^  _valuableCategoryId: (.+)/m)?.[1];
  check(text.includes('guid: 84f6310988464062bc8bf5b6ba2cf59a'), name + ' keeps original Valuable script');
  check((text.match(/^  _guid: [a-f0-9]{32}$/gm) || []).length === 1, name + ' has one runtime item identity');
  check(!category || ids.has(category), name + ' category resolves');
  counts[category ? entries.find(e => e.id === category).label : '미분류']++;
}
check(Object.values(counts).reduce((a,b) => a+b,0) === 45, 'all original Valuable items accounted for');
check(counts['미분류'] === 1, 'ambiguous Cobweb deliberately unclassified');
const player = read('Assets/Prefabs/UI/GamePlay/PanelHub/Inventory/PlayerInventoryPanel.prefab');
const docs = player.split(/(?=^--- !u!)/m).filter(s => s.startsWith('--- !u!'));
const roots = docs.filter(d => d.includes('  m_Name: SortCategory\n'));
check(roots.length === 2, 'pocket and bag authored category buttons');
for (const root of roots) {
  const id = root.match(/^--- !u!1 &(\d+)/)[1];
  const components = docs.filter(d => d.includes('m_GameObject: {fileID: ' + id + '}'));
  const button = components.find(d => d.includes('m_OnClick:'));
  check(button?.includes('m_MethodName: SortPlayerInventoryByCategory\n'), 'persistent category handler');
  check(button?.includes('m_TargetAssemblyTypeName: PlayerInventoryUI, Assembly-CSharp'), 'player-owned callback');
  check(/m_MethodName: SortPlayerInventoryByCategory\n        m_Mode: 1\n/.test(button), 'no-argument callback mode');
  check(components.some(d => d.includes('m_AnchoredPosition: {x: -156, y: -1}')), 'button does not overlap existing sorts');
}
for (const [path, symbol] of [
 ['Assets/Scripts/Presentation/UI/Gameplay/PanelHub/PlayerTooltipUI.cs','ValuableCategoryLabel'],
 ['Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/PlayerItemDetailPanelUI.cs','ValuableCategoryLabel'],
 ['Assets/Editor/LootProgressionEditorWindow.cs','row.CategoryKey != lastCategory'],
 ['Assets/Editor/LootMakerItemView.cs','.GroupBy(ItemMakerWindow.LootCategoryKey)'],
 ['Assets/Editor/ItemMakerWindow.cs','ValuableCategoryEditorWindow.DrawAssignment'],
 ['Assets/Editor/ItemMakerWindow.Valuables.cs','Undo.CollapseUndoOperations(group)']
]) check(read(path).includes(symbol), path + ' integration');
console.log('PASS ' + checks + ' Valuable asset/prefab/source contracts. Not native Unity import.');
console.log(JSON.stringify(counts));
