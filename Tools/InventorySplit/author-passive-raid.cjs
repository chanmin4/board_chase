// Emits apply_patch input. Preauthors slots/profile changes; never writes Unity assets itself.
const fs = require('fs');
let next = 931029000000000001n;
const patches = [], manifest = [];
const parse = text => text.replace(/\r/g, '').split(/(?=^--- !u!)/m).map(raw => {
  const match = raw.match(/^--- !u!(\d+) &(\d+)/); return { raw, id: match?.[2], type: +match?.[1] };
});
function change(path, before, after) {
  before = before.replace(/\r/g, ''); after = after.replace(/\r/g, '');
  if (before === after) return;
  // Whole files are generated in memory, but patch only changed YAML documents.
  const a = parse(before), b = parse(after), byId = new Map(a.filter(d=>d.id).map(d=>[d.id,d]));
  if (a.length > 2) {
    for (const d of b.filter(d=>d.id)) {
      const old = byId.get(d.id);
      if (old && old.raw !== d.raw) patches.push(`*** Update File: ${path}\n@@\n${old.raw.trimEnd().split('\n').map(x=>'-'+x).join('\n')}\n${d.raw.trimEnd().split('\n').map(x=>'+'+x).join('\n')}`);
    }
    const added=b.filter(d=>d.id&&!byId.has(d.id));
    if(added.length){const last=a[a.length-1].raw.trimEnd().split('\n');patches.push(`*** Update File: ${path}\n@@\n${last.slice(-3).map(x=>' '+x).join('\n')}\n${added.map(d=>d.raw.trimEnd()).join('\n').split('\n').map(x=>'+'+x).join('\n')}`);}
  } else patches.push(`*** Update File: ${path}\n@@\n${before.trimEnd().split('\n').map(x=>'-'+x).join('\n')}\n${after.trimEnd().split('\n').map(x=>'+'+x).join('\n')}`);
}
const scriptGuid=fs.readFileSync('Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/PassiveInventorySlotUI.cs.meta','utf8').match(/^guid: (\w+)/m)[1];
for(const kind of ['Player','Enemy']){
  const path=`Assets/Prefabs/UI/GamePlay/PanelHub/Inventory/${kind}InventoryPanel.prefab`,before=fs.readFileSync(path,'utf8');
  const docs=parse(before),find=id=>docs.find(d=>d.id===id);
  const ui=docs.find(d=>d.type===114&&d.raw.includes(kind==='Player'?'  _passiveSlots:':'  _equipmentSectionRoot:'));
  const field=kind==='Player'?'_passiveSlots':'_passiveInspectSlots';
  let refs=ui.raw.match(new RegExp('  '+field+':\\n((?:  - \\{fileID: \\d+\\}\\n)+)'));
  refs=refs?[...refs[1].matchAll(/fileID: (\d+)/g)].map(m=>m[1]):docs.filter(d=>d.type===114&&d.raw.includes('guid: '+scriptGuid)).sort((a,b)=>{
    const name=d=>+(find(d.raw.match(/m_PrefabInstance: \{fileID: (\d+)/)[1]).raw.match(/value: PassiveSlot_(\d+)/)?.[1]??0);return name(a)-name(b);
  }).map(d=>d.id);
  if(refs.length!==5&&refs.length!==8)throw Error('Unexpected passive count '+kind+':'+refs.length);
  const last=find(refs[4]),instanceId=last.raw.match(/m_PrefabInstance: \{fileID: (\d+)/)[1];
  const template=docs.filter(d=>d.id===instanceId||d.raw.includes('m_PrefabInstance: {fileID: '+instanceId+'}'));
  const parent=find(instanceId).raw.match(/m_TransformParent: \{fileID: (\d+)/)[1];
  while(refs.length<8){
    const map=new Map(template.map(d=>[d.id,String(next++)]));
    const copied=template.map(d=>({ ...d,id:map.get(d.id),raw:d.raw.replace(/^--- !u!(\d+) &(\d+)/,(_,t,k)=>`--- !u!${t} &${map.get(k)}`).replace(/\{fileID: (\d+)\}/g,(all,k)=>map.has(k)?'{fileID: '+map.get(k)+'}':all).replace(/value: PassiveSlot_5/g,'value: PassiveSlot_'+(refs.length+1)) }));
    docs.push(...copied);refs.push(map.get(last.id));
    const rect=copied.find(d=>d.type===224),row=find(parent);
    row.raw=row.raw.replace('  m_Children:\n','  m_Children:\n  - {fileID: '+rect.id+'}\n');
  }
  for(let i=0;i<refs.length;i++){
    const component=find(refs[i]),instance=find(component.raw.match(/m_PrefabInstance: \{fileID: (\d+)/)[1]);
    const rect=docs.find(d=>d.type===224&&d.raw.includes('m_PrefabInstance: {fileID: '+instance.id+'}'));
    const source=rect.raw.match(/m_CorrespondingSourceObject: \{fileID: (\d+), guid: (\w+)/);
    const values={'m_AnchoredPosition.x':4+Math.floor(i/2)*63,'m_AnchoredPosition.y':-(8+(i%2)*112),'m_LocalScale.x':.58,'m_LocalScale.y':.58,'m_LocalScale.z':.58};
    for(const [key,value] of Object.entries(values)){
      const escaped=key.replace(/\./g,'\\.'),pattern=new RegExp('(    - target: \\{fileID: '+source[1]+', guid: '+source[2]+', type: 3\\}\\n      propertyPath: '+escaped+'\\n      value: )[^\\n]*');
      if(!pattern.test(instance.raw))throw Error('Missing transform override '+key);
      instance.raw=instance.raw.replace(pattern,'$1'+value);
    }
  }
  const rows=refs.map(id=>'  - {fileID: '+id+'}').join('\n')+'\n';
  const rx=new RegExp('  '+field+':\\n(?:  - \\{fileID: \\d+\\}\\n)+');
  ui.raw=rx.test(ui.raw)?ui.raw.replace(rx,'  '+field+':\n'+rows):ui.raw.replace('  _equipmentSectionRoot:','  '+field+':\n'+rows+'  _equipmentSectionRoot:');
  const ids=new Set();for(const d of docs.filter(d=>d.id)){if(ids.has(d.id))throw Error('Duplicate ID '+d.id);ids.add(d.id);}
  change(path,before,docs.map(d=>d.raw).join(''));manifest.push({prefab:kind,slots:refs.length});
}
const root='Assets/Resources/PassiveModules';const moduleFiles=fs.readdirSync(root).filter(f=>/^Module_.*\.asset$/.test(f));
const survival=new Set(['Armor','ArmorWear','Dash','Health','MoveSpeed','Stamina','Storage','VisionArc','AimPunch','Suppression','Experience','Empty']);
const startRefs=[];
for(const file of moduleFiles){
  const path=root+'/'+file,before=fs.readFileSync(path,'utf8'),name=file.slice(7,-6);
  const category=survival.has(name)?1:0;
  let after=before.includes('  _category:')?before.replace(/  _category: \d+/,'  _category: '+category):before.replace('  _randomStatsEnabled:','  _category: '+category+'\n  _randomStatsEnabled:');
  if(!after.includes('  _category:'))after=after.replace('  _effectGroup:','  _category: '+category+'\n  _effectGroup:');
  if(!after.includes('  _category:'))throw Error('No insert point '+file);
  change(path,before,after);
  const guid=fs.readFileSync(path+'.meta','utf8').match(/^guid: (\w+)/m)[1];
  if(name!=='Empty')startRefs.push(`  - {fileID: 11400000, guid: ${guid}, type: 2}`);
  manifest.push({passive:name,category});
}
const legacyRoot='Assets/ScriptableObject/GameScene/GameItem/PassiveItem';
for (const file of fs.readdirSync(legacyRoot).filter(f=>f.endsWith('.asset'))) {
  const path=legacyRoot+'/'+file,before=fs.readFileSync(path,'utf8');
  if(!before.includes('99ca85af63a94240aa97cd468d889997'))continue;
  const category=/^(Compass|FlashLight|Shield|FryingPan)/.test(file)?1:0;
  const after=before.includes('  _category:')?before.replace(/  _category: \d+/,'  _category: '+category):before.replace(/(  m_EditorClassIdentifier:[^\n]*\n)/,'$1  _category: '+category+'\n');
  change(path,before,after);manifest.push({legacyPassive:file,category});
}
const rulePath='Assets/Resources/PassiveModuleRules.asset',rules=fs.readFileSync(rulePath,'utf8');
let after=rules.replace('  recyclingSeconds:',`  homeOnlyPassiveChanges: 0\n  combatSlotLevels: 01000000050000000a0000000f000000\n  survivalSlotLevels: 01000000070000000c00000012000000\n  combatEffectMultiplier: 1.5\n  survivalEffectMultiplier: 1.5\n  unifiedModule: {fileID: 11400000, guid: ac2210260000000000000000000000c8, type: 2}\n  drawCountLevels: 010000000a00000014000000\n  startingPool:\n${startRefs.join('\n')}\n  startingDraws:\n  - label: 저등급\n    enabled: 1\n    requiredLevel: 1\n    moduleCost: 10\n    rarityWeights: [80, 20, 0, 0, 0]\n  - label: 중등급\n    enabled: 1\n    requiredLevel: 10\n    moduleCost: 25\n    rarityWeights: [50, 35, 15, 0, 0]\n  - label: 고등급 · 데모 제외\n    enabled: 0\n    requiredLevel: 20\n    moduleCost: 50\n    rarityWeights: [35, 30, 25, 10, 0]\n  recyclingSeconds:`);
// Rarity enum order in this project is Normal=0, Rare=1, Unique=3, Legendary=2, Mythic=4.
const yields={0:1,1:3,3:8,2:15,4:25};
if(rules.includes('  startingDraws:'))after=rules;
if(!after.includes('    moduleYield:'))after=after.replace(/(  - rarity: (\d+)\r?\n    parts:[^\n]*\r?\n    rerollCost: \d+)/g,(all,_row,r)=>all+'\n    moduleYield: '+yields[r]);
change(rulePath,rules,after);
const parts=root+'/PassiveParts_Normal.asset',partsBefore=fs.readFileSync(parts,'utf8');
change(parts,partsBefore,partsBefore.replace('  m_Name: PassiveParts_Normal','  m_Name: 통합 패시브 모듈'));
// Small explicit passive shares in the existing raid supply tables, not a new loot generator.
for(const [table,category] of [['Armory',0],['Medical',1],['Vault',null]]){
  const path=`Assets/ScriptableObject/GameScene/Stage/RaidLoot/RaidLoot_${table}.asset`,before=fs.readFileSync(path,'utf8').replace(/\r/g,'');
  const candidates=moduleFiles.filter(f=>f!=='Module_Empty.asset').filter(f=>category==null||+(fs.readFileSync(root+'/'+f,'utf8').match(/  _category: (\d+)/)?.[1]??0)===category);
  const entries=candidates.map(f=>({guid:fs.readFileSync(root+'/'+f+'.meta','utf8').match(/^guid: (\w+)/m)[1],text:fs.readFileSync(root+'/'+f,'utf8')}));
  let after=before.replace(/(    - rarity: (\d+)\n      chancePercent: [^\n]+\n      items:)(?: \[\])?\n([\s\S]*?)(?=    - rarity:|$)/g,(_,head,rarity,body)=>{
    let rows='';
    for(const entry of entries){
      const valid=!entry.text.includes('  _randomStatsEnabled: 1')||[...entry.text.matchAll(/^  - rarity: (\d+)/gm)].some(m=>m[1]===rarity);
      if(!valid)body=body.replace(new RegExp('      - item: \\{fileID: 11400000, guid: '+entry.guid+', type: 2\\}\\n(?:        [^\\n]*\\n)*'),'');
      else if(!body.includes(entry.guid))rows+=`      - item: {fileID: 11400000, guid: ${entry.guid}, type: 2}\n        minAmount: 1\n        maxAmount: 1\n        useManualShare: 1\n        manualSharePercent: 0.5\n`;
    }
    return head+'\n'+rows+body;
  });
  change(path,before,after);if(after!==before)manifest.push({lootTable:table,passiveCandidates:entries.length,sharePerCandidate:.5});
}
const selected = process.argv.find(x => x.startsWith('--part='));
process.stdout.write(JSON.stringify(selected ? {patch:'*** Begin Patch\n'+patches[+selected.split('=')[1]]+'\n*** End Patch'} : {manifest,parts:patches.map((p,index)=>({index,length:p.length,file:p.split('\n')[0]}))}));
