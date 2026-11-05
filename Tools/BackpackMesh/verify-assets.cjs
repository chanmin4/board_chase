// Read-only verification against the user's latest saved backpack assets.
const fs = require('fs'), crypto = require('crypto');
const folder = 'Library/BackpackNoStraps-20261002';
const before = JSON.parse(fs.readFileSync(folder + '/before.json', 'utf8'));
const moves = JSON.parse(fs.readFileSync(folder + '/migration.json', 'utf8'));
const models = JSON.parse(fs.readFileSync(folder + '/mesh-verification.json', 'utf8'));
const hash = p => crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');
const norm = s => s.replace(/\r\n/g, '\n').trimEnd();
const lines = []; let passed = 0, failed = 0;
function check(ok, label) { lines.push((ok ? 'PASS ' : 'FAIL ') + label); ok ? passed++ : failed++; }
for (const b of before) {
  const m = moves.find(x => x.level === b.n), model = models.find(x => x.level === b.n);
  check(hash(m.wipPrefab) === b.hashes[b.prefab], `LV${b.n}: original prefab bytes preserved in WIP`);
  check(hash(m.wipPrefab+'.meta') === b.hashes[b.prefab+'.meta'], `LV${b.n}: original GUID/meta preserved in WIP`);
  check(hash(b.model) === b.hashes[b.model] && hash(b.model+'.meta') === b.hashes[b.model+'.meta'], `LV${b.n}: original source FBX/importer unchanged`);
  check(hash(b.derived) === model.output_sha256 && model.all_retained_geometry_uv_normals_colors_and_unrelated_nodes_preserved,
    `LV${b.n}: reviewed face deletion matches structurally verified mesh`);
  const originalMeshGuid = b.prefabText.match(/m_Mesh:.*guid: (\w+)/)[1];
  check(norm(fs.readFileSync(b.prefab,'utf8')) === norm(b.prefabText.replace(originalMeshGuid,m.modelGuid)),
    `LV${b.n}: copied prefab differs only in MeshFilter reference (no pivot/transform/component/material changes)`);
  const item = fs.readFileSync(b.item,'utf8');
  check(norm(item) === norm(b.itemText.replace(/(_equippedVisualPrefab:.*guid: )\w+/, '$1'+m.prefabGuid)),
    `LV${b.n}: Item SO changed only equipped model; saved pose, world model and all gameplay values preserved`);
  check(norm(fs.readFileSync(b.derived+'.meta','utf8').replace(/^guid: \w+/m,'')) === norm(b.modelMeta.split('AssetOrigin:')[0].replace(/^guid: \w+/m,'')),
    `LV${b.n}: derived mesh inherits original import scale/axes/material/normal settings`);
}
const wanted = new Set(moves.flatMap(m => [m.originalGuid,m.prefabGuid,m.modelGuid]));
const counts = new Map();
function walk(dir) {
  for (const e of fs.readdirSync(dir,{withFileTypes:true})) {
    const p = dir+'/'+e.name;
    if(e.isDirectory()) walk(p);
    else if(e.name.endsWith('.meta')) {
      const guid = fs.readFileSync(p,'utf8').match(/^guid: (\w+)/m)?.[1];
      if(wanted.has(guid)) counts.set(guid,(counts.get(guid)||0)+1);
    }
  }
}
walk('Assets');
for(const guid of wanted) check(/^[a-f0-9]{32}$/.test(guid) && counts.get(guid) === 1, 'Unique original/active/model GUID: '+guid);
check(models.find(m=>m.level===4).removed_polygons === 0, 'LV4 has no shoulder straps; sleeping-roll lashings are preserved');
lines.push(`${passed} passed / ${failed} failed. Mesh + serialized asset checks, not Unity imported mesh/render checks.`);
fs.writeFileSync(folder+'/asset-verification.txt',lines.join('\n')+'\n');
console.log(lines.join('\n')); process.exitCode = failed ? 1 : 0;
