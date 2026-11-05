// Read-only asset audit; all generated evidence is confined to Library.
const fs = require('fs'), crypto = require('crypto');
const out = 'Library/HfsmUnified-20261002';
const paths = ['Assets/Prefabs/GameScene/Shooter/EnemyShooter/EnemyBasePrefab.prefab',
 'Assets/Prefabs/GameScene/Shooter/Player/Player.prefab'];
const walk = dir => fs.readdirSync(dir, { withFileTypes: true }).flatMap(e => e.isDirectory() ? walk(dir + '/' + e.name) : [dir + '/' + e.name]);
const hash = p => crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');
const files = () => Object.fromEntries(walk('Assets').filter(p => /\.(asset|prefab|unity|meta)$/.test(p)).map(p => [p, hash(p)]));
fs.mkdirSync(out, { recursive: true });
if (process.argv.includes('--baseline')) {
 fs.writeFileSync(out + '/before.json', JSON.stringify(files(), null, 2));
 paths.forEach((p, i) => fs.writeFileSync(out + '/before-prefab-' + i + '.yaml', fs.readFileSync(p)));
 console.log('Saved authored-file baseline and two exact prefab backups to ' + out);
} else {
 const before = JSON.parse(fs.readFileSync(out + '/before.json')), after = files(), lines = []; let failed = 0;
 const check = (ok, label) => { lines.push((ok ? 'PASS ' : 'FAIL ') + label); if (!ok) failed++; };
 const changed = Object.keys(before).filter(p => before[p] !== after[p]);
 check(changed.length === 2 && paths.every(p => changed.includes(p)), 'Only the two approved base prefabs changed; existing SOs, scenes and meta bytes preserved');
 const rootId = '6915450752968060550';
 paths.forEach(p => {
  const s = fs.readFileSync(p, 'utf8');
  check(s.includes('guid: d26f1002000000000000000000000004') && s.includes('_definition:'), p + ': unified host connected');
  check(!/60a6b2c19999f504da354acd124867d5|71b601e08d84d324fbc1dff9f84f20c7|ecd6d1a63112ede4bac9a904854e4205/.test(s), p + ': old three components removed');
  check(s.includes('&4537753811123894838') && !/723617969344722977|8017175232211933994/.test(s), p + ': surviving component identity preserved, no dangling removed IDs');
  check(s.includes('&' + rootId), p + ': root identity preserved');
 });
 const metas = walk('Assets').filter(p => p.endsWith('.meta'));
 const allGuids = metas.map(p => [p, /guid: ([a-f0-9]+)/.exec(fs.readFileSync(p, 'utf8'))?.[1]]);
 for (const [p, guid] of allGuids.filter(([,g]) => g?.startsWith('d26f1002'))) check(guid.length === 32 && allGuids.filter(([,g]) => g === guid).length === 1, 'Valid unique GUID: ' + p);
 lines.push('Changed existing files: ' + changed.join(', '));
 lines.push('Added authored files: ' + Object.keys(after).filter(p => !before[p]).join(', '));
 lines.push(failed + ' failed'); fs.writeFileSync(out + '/asset-audit.txt', lines.join('\n'));
 console.log(lines.join('\n')); process.exitCode = failed ? 1 : 0;
}
