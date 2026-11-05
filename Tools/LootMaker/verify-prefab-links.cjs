const fs = require('fs');
const assert = require('assert');
const dir = 'Library/LootMakerAuthoring-20260929';
const manifest = JSON.parse(fs.readFileSync(dir + '/prefab-link-manifest.json', 'utf8'));
const normalize = text => text.replace(/\r\n/g,'\n').trimEnd() + '\n';
for (const entry of manifest) {
 const current = fs.readFileSync(entry.path,'utf8');
 const before = fs.readFileSync(dir + '/Before/' + entry.path,'utf8');
 assert.strictEqual(normalize(current),normalize(entry.expected),entry.path + ': unexpected concurrent change');
 const strip = text => normalize(text).replace(/(^  _(worldPrefab|previewPrefab): \{fileID: )\d+/gm,'$1ROOT');
 assert.strictEqual(strip(current),strip(before),entry.path + ': data beyond preview reference changed');
}
for (const p of ['Assets/Editor/LootMakerAuthoring.cs','Assets/Editor/LootProgressionEditorWindow.ContainerAuthoring.cs',
 'Assets/Tests/EditMode/Editor/LootMakerAuthoringTests.cs'])
 assert.match(fs.readFileSync(p+'.meta','utf8'),/^guid: [a-f0-9]{32}\s*$/m,p);
assert(!fs.existsSync('Assets/Editor/LootMakerAuthoringVerificationOnce.cs'));
assert(!fs.existsSync('Assets/Editor/LootMakerAuthoringVerificationOnce.cs.meta'));
assert(!fs.readdirSync('Assets').some(n=>n.startsWith('__LootMakerTests_')),'Temporary test assets remain');
console.log(`PASS ${manifest.length} exact preview-reference changes; all other recorded values preserved. Metadata and cleanup OK.`);
