// READ ONLY. Compare this migration's exact saved Unity YAML backup with resolved current rows.
// Intentionally limited to this flat Enemy Base schema; unknown/non-flat hierarchy fails closed.
const fs = require('fs'), path = require('path'), assert = require('assert');
const tablePath = 'Assets/ScriptableObject/StateMachine/Shooter/EnemyShooter/Enemy_ShooterBase_Alive.asset';
const backupPath = process.argv[2];
assert(backupPath && fs.existsSync(backupPath), 'Pass the exact pre-migration .asset backup path.');
const read = p => fs.readFileSync(p, 'utf8').replace(/\r/g, '');
const files = d => fs.readdirSync(d, { withFileTypes: true }).flatMap(e => e.isDirectory() ? files(d + '/' + e.name) : [d + '/' + e.name]);
const assets = new Map();
for (const p of files('Assets/ScriptableObject/StateMachine').filter(p => p.endsWith('.asset.meta'))) {
  const guid = read(p).match(/^guid: ([0-9a-f]{32})$/m)?.[1];
  assert(guid, 'Invalid meta GUID: ' + p); assert(!assets.has(guid), 'Duplicate GUID: ' + guid);
  assets.set(guid, p.slice(0, -5));
}
function ref(text, field, optional = false) {
  const value = text.match(new RegExp('^\\s*(?:- )?' + field + ': (\\{[^}]+\\})$', 'm'))?.[1];
  if (!value) { assert(optional, 'Missing reference ' + field); return '{fileID: 0}'; }
  return value;
}
const guidOf = r => r.match(/guid: ([0-9a-f]{32})/)?.[1];
function number(text, field) { return Number(text.match(new RegExp('^\\s*(?:- )?' + field + ': (\\d+)$', 'm'))?.[1] || 0); }
function name(text, field = 'm_Name') {
  const value = text.match(new RegExp('^  ' + field + ': (.*)$', 'm'))?.[1] || '';
  return value.startsWith('"') ? JSON.parse(value) : value;
}
function label(r) { const p = assets.get(guidOf(r)); assert(p, 'Missing asset reference: ' + r); return name(read(p)); }
function conditions(text) {
  return [...text.matchAll(/^\s*- ExpectedResult: (\d+)\n\s+Condition: (\{[^}]+\})\n\s+Operator: (\d+)$/gm)]
    .map(m => ({ result: Number(m[1]), condition: m[2], operator: Number(m[3]) }));
}
function rows(text) {
  const block = text.split(/^  _transitions:\n/m)[1]; assert(block, 'Missing transition array');
  return block.split(/\n  _[a-zA-Z]/)[0].split(/(?=^  - FromState:)/m).filter(s => s.trim()).map(s => ({
    from: ref(s, 'FromState'), to: ref(s, 'ToState'), conditions: conditions(s),
    priority: number(s, 'Priority'), always: number(s, 'Always'), exit: number(s, 'ExitReason'),
    shared: number(s, 'UseSharedRule') === 1, rule: ref(s, 'SharedRule', true)
  }));
}
const oldText = read(backupPath), currentText = read(tablePath), old = rows(oldText), raw = rows(currentText);
function payload(row) { const { shared, rule, ...value } = row; return value; }
function effective(row) {
  if (!row.shared) return payload(row);
  const p = assets.get(guidOf(row.rule)); assert(p, 'Missing shared definition: ' + row.rule);
  const text = read(p);
  assert(ref(text, 'm_Script').includes('fd5c76e55c7741b5934be5eb651154af'), 'Wrong shared rule script: ' + p);
  return { from: row.from, to: ref(text, '_toState'), conditions: conditions(text),
    priority: row.priority, always: number(text, '_always'), exit: number(text, '_exitReason') };
}
const resolved = raw.map(effective);
assert.deepStrictEqual(resolved, old.map(payload), 'Resolved execution data differs from the saved pre-migration baseline.');
assert.deepStrictEqual(raw.map(payload), old.map(payload), 'An inline snapshot/row identity changed.');
assert.strictEqual(ref(oldText, '_initialState', true), ref(currentText, '_initialState', true), 'Explicit entry changed.');
for (const t of [oldText, currentText]) assert(!t.includes('  _hierarchy:') || /^  _hierarchy: \[\]$/m.test(t), 'Unsupported/non-empty hierarchy');
assert.strictEqual(ref(oldText, 'm_Script'), ref(currentText, 'm_Script'), 'Table script changed.');
assert(fs.readFileSync(backupPath + '.meta').equals(fs.readFileSync(tablePath + '.meta')), 'Table meta/GUID changed.');
const sharedIds = [...new Set(raw.filter(r => r.shared).map(r => guidOf(r.rule)))];
assert.strictEqual(raw.length, 33); assert.strictEqual(raw.filter(r => r.shared).length, 24); assert.strictEqual(sharedIds.length, 6);
console.log('PASS: all 33 resolved rows AND inline snapshots exactly match the saved backup; entry/hierarchy/script/meta unchanged.');
console.log('33 rows / 24 shared usages / 6 shared definitions / 9 local exceptions / 15 display cards');
for (const id of sharedIds) {
  const text = read(assets.get(id)); const users = raw.filter(r => r.shared && guidOf(r.rule) === id);
  console.log((name(text, '_displayName') || name(text)) + ': ' + users.map(r => label(r.from)).join(', ') + ' -> ' + label(ref(text, '_toState')));
}
for (const source of [...new Set(raw.map(r => r.from))]) {
  console.log('\n' + label(source) + ':');
  raw.map((r, i) => ({ r, i })).filter(p => p.r.from === source).sort((a, b) => b.r.priority - a.r.priority || a.i - b.i).forEach(({ r, i }, rank) => {
    const e = resolved[i];
    console.log('  ' + (rank + 1) + '. #' + (i + 1) + ' [' + (r.shared ? 'shared' : 'local') + '] '
      + e.conditions.map((c, j) => (j ? (e.conditions[j - 1].operator === 0 ? ' AND ' : ' OR ') : '') + label(c.condition) + (c.result === 0 ? '' : ' == false')).join('')
      + ' -> ' + label(e.to));
  });
}
