// Generates (does not apply) a narrowly scoped preview-reference patch and recovery copies.
const fs = require('fs');
const path = require('path');
const root = process.cwd();
const recovery = 'Library/LootMakerAuthoring-20260929';
const files = dir => fs.readdirSync(dir, {withFileTypes:true}).flatMap(e => {
 const p=path.join(dir,e.name).replace(/\\/g,'/'); return e.isDirectory()?files(p):[p];
});
const metas = new Map();
for(const p of files('Assets/Prefabs/GameScene').filter(p=>p.endsWith('.prefab.meta'))) {
 const m=fs.readFileSync(p,'utf8').match(/^guid: ([a-f0-9]{32})\s*$/m);
 if(m)metas.set(m[1],p.slice(0,-5));
}
function rootId(guid, seen=new Set()) {
 if(seen.has(guid))throw Error('Prefab cycle: '+guid); seen.add(guid);
 const p=metas.get(guid); if(!p)throw Error('Missing prefab: '+guid);
 const text=fs.readFileSync(p,'utf8');
 const blocks=text.split(/(?=^--- !u!)/m);
 const transform=blocks.find(b=>/^--- !u!4 /m.test(b)&&/^  m_Father: \{fileID: 0\}/m.test(b));
 if(transform) return BigInt(transform.match(/^  m_GameObject: \{fileID: (\d+)\}/m)[1]);
 const inst=blocks.find(b=>/^--- !u!1001 /m.test(b)&&/^    m_TransformParent: \{fileID: 0\}/m.test(b));
 if(!inst)throw Error('No root: '+p);
 const source=inst.match(/m_SourcePrefab: \{fileID: 100100000, guid: ([a-f0-9]{32})/)[1];
 return rootId(source,seen)^BigInt(inst.match(/^--- !u!1001 &(\d+)/m)[1]);
}
const patch=['*** Begin Patch']; const manifest=[];
for(const p of [...files('Assets/ScriptableObject/GameScene/GameItem/Loot'),...files('Assets/ScriptableObject/GameScene/Stage/RaidLoot')].filter(p=>p.endsWith('.asset'))) {
 const original=fs.readFileSync(p,'utf8'); let changed=original;
 for(const m of original.matchAll(/^  _(worldPrefab|previewPrefab): \{fileID: 100100000, guid: ([a-f0-9]{32}), type: 3\}\r?$/gm)) {
  const line=m[0].trimEnd(); const id=rootId(m[2]).toString();
  const replacement=line.replace('fileID: 100100000,','fileID: '+id+',');
  if(changed===original)patch.push('*** Update File: '+p);
  patch.push('@@','-'+line,'+'+replacement);
  changed=changed.replace(line,replacement);
 }
 if(changed!==original) {
  const backup=path.join(recovery,'Before',p);
  fs.mkdirSync(path.dirname(backup),{recursive:true});
  if(!fs.existsSync(backup))fs.copyFileSync(p,backup);
  manifest.push({path:p,expected:changed});
 }
}
patch.push('*** End Patch');
fs.mkdirSync(recovery,{recursive:true});
fs.writeFileSync(recovery+'/prefab-link-manifest.json',JSON.stringify(manifest,null,2));
process.stdout.write(patch.join('\n'));
