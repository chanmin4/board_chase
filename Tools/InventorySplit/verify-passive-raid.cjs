// Read-only authored data and cross-surface contracts. No Unity import or Play Mode claims.
const fs=require('fs');let checks=0;
const read=p=>fs.readFileSync(p,'utf8').replace(/\r/g,'');
const check=(ok,why)=>{checks++;if(!ok)throw Error(why);};
const rules=read('Assets/Resources/PassiveModuleRules.asset');
function levels(key){const raw=rules.match(new RegExp('^  '+key+': ([a-f0-9]+)$','m'))?.[1];check(!!raw,'Missing packed int levels: '+key);const buffer=Buffer.from(raw,'hex');return Array.from({length:buffer.length/4},(_,i)=>buffer.readInt32LE(i*4));}
check(JSON.stringify(levels('combatSlotLevels'))==='[1,5,10,15]','Combat unlock tuning changed');
check(JSON.stringify(levels('survivalSlotLevels'))==='[1,7,12,18]','Survival unlock tuning changed');
check(JSON.stringify(levels('drawCountLevels'))==='[1,10,20]','Preparation draw count tuning changed');
check(rules.includes('changeCooldownSeconds: 10'),'Existing change cooldown not preserved');
check(rules.includes('unifiedModule: {fileID: 11400000, guid: ac2210260000000000000000000000c8'),'No unified material reference');
const draws=rules.slice(rules.indexOf('  startingDraws:'),rules.indexOf('  recyclingSeconds:')).split(/(?=^  - label:)/m).slice(1);
check(draws.length===3&&draws[2].includes('enabled: 0'),'High draw tier accidentally enabled');
check(draws[0].includes('moduleCost: 10')&&draws[1].includes('moduleCost: 25'),'Draw costs not authored');
const catalog=read('Assets/Resources/RuntimeContentCatalog.asset');
const defs=new Map();
for(const file of fs.readdirSync('Assets/Resources/PassiveModules').filter(f=>/^Module_.*\.asset$/.test(f))){
 const path='Assets/Resources/PassiveModules/'+file,text=read(path),guid=read(path+'.meta').match(/^guid: (\w+)/m)[1];
 const category=+(text.match(/^  _category: (\d+)/m)?.[1]??-1);
 check(category===0||category===1,'Missing category '+file);check(catalog.includes(guid),'Unregistered module '+file);
 defs.set(guid,{file,text,category,rarities:[...text.matchAll(/^  - rarity: (\d+)/gm)].map(m=>+m[1])});
}
const starting=rules.slice(rules.indexOf('  startingPool:'),rules.indexOf('  startingDraws:'));
const ids=[...starting.matchAll(/guid: (\w+)/g)].map(m=>m[1]);
check(ids.length===19&&new Set(ids).size===19,'Starting pool duplicate/missing content');
for(const id of ids){check(defs.has(id),'Unknown starting candidate '+id);check(defs.get(id).file!=='Module_Empty.asset','Empty module in starting draw');}
for(const [table,category]of [['Armory',0],['Medical',1],['Vault',null]]){
 const text=read(`Assets/ScriptableObject/GameScene/Stage/RaidLoot/RaidLoot_${table}.asset`);
 for(const tier of text.split(/(?=^    - rarity:)/m).slice(1)){
  const rarity=+tier.match(/rarity: (\d+)/)[1];let manualTotal=0;
  for(const entry of tier.split(/(?=^      - item:)/m).slice(1)){
   const guid=entry.match(/guid: (\w+)/)?.[1],definition=defs.get(guid);
   if(!definition)continue;
   check(category==null||definition.category===category,'Wrong supply category '+table+'/'+definition.file);
   check(!definition.text.includes('_randomStatsEnabled: 1')||definition.rarities.includes(rarity),'Unsupported passive rarity creates empty loot '+table+'/'+definition.file+'/'+rarity);
   check(entry.includes('useManualShare: 1'),'Passive supply lost explicit share');
   check(entry.includes('manualSharePercent: 0.5'),'Unexpected passive supply tuning');manualTotal+=.5;
  }
  check(manualTotal>0&&manualTotal<100,'Invalid passive supply total '+table+'/'+rarity);
 }
}
const passive=read('Assets/Scripts/Gameplay/Entities/Shooter/Player/Passive/PlayerPassiveInventoryRuntime.cs');
check(passive.includes('MaximumSlotCount = 8'),'Runtime maximum differs from UI');
check(passive.includes('source.passiveSlotSchemaVersion >= 2 && index < source.equippedPassiveGuids.Count'),'Legacy migration pre-binds copies twice');
const capture=read('Assets/Scripts/Gameplay/Entities/Shooter/Player/Inventory/PlayerInventoryRuntime.RunPersistence.cs');
check(capture.includes('slotIndex < _items.Count; slotIndex++'),'Pending overflow excluded from save');
check(capture.includes('entry.slotIndex >= 4096'),'Saved overflow loses bounded restore guard');
const hatch=read('Assets/Scripts/Gameplay/Progression/Stage/StageProgressionManager.HatchProgression.cs');
check(!hatch.includes('EndPassiveRaid(')&&!hatch.includes('startingPassives = new'),'Hatch resets raid passives/preparation');
const end=read('Assets/Scripts/Gameplay/Entities/Shooter/Player/Inventory/PlayerInventoryRuntime.PassiveRaid.cs');
check(end.includes('passive.EverEquipped')&&end.includes('startingPassives = new'),'Extraction cleanup/counter reset missing');
console.log(`PASS ${checks} raid passive category/unlock/preparation/catalog/loot/save/lifecycle source contracts. Not Unity import, native ES3 or Play Mode.`);
