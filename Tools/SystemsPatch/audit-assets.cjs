// Read-only structural audit. This is not a Unity YAML importer or Play Mode test.
const fs=require('fs'), path=require('path');
const walk=d=>fs.existsSync(d)?fs.readdirSync(d,{withFileTypes:true}).flatMap(e=>e.isDirectory()?walk(d+'/'+e.name):[d+'/'+e.name]):[];
const files=['Assets/Resources','Assets/ScriptableObject','Assets/Scripts'].flatMap(walk);
const byGuid=new Map();
for(const file of files.filter(x=>x.endsWith('.meta'))){
 const guid=fs.readFileSync(file,'utf8').match(/^guid: (\w+)/m)?.[1];
 if(guid)byGuid.set(guid,file.slice(0,-5));
}
let checks=0;
const check=(ok,message)=>{if(!ok)throw Error(message);checks++;};
const catalog=fs.readFileSync('Assets/Resources/CraftingCatalog.asset','utf8');
const recipes=[...catalog.matchAll(/^  - id: ([^\r\n]+)\r?\n([\s\S]*?)(?=^  - id:|^  materials:)/gm)];
const recipeIds=new Set(), missingTuning=[], missingSupply=[];
const materialsSection=catalog.split(/^  materials:/m)[1]||'';
const materials=[...materialsSection.matchAll(/guid: (\w+)/g)].map(m=>{
 const file=byGuid.get(m[1]);check(!!file,'Missing material asset '+m[1]);
 const text=fs.readFileSync(file,'utf8');
 return {file,rarity:+text.match(/^  _rarity: (\d+)/m)?.[1],category:text.match(/^  _valuableCategoryId: (\S+)/m)?.[1]};
});
const required={0:[0,0,0],1:[1,1,0],3:[3,3,1],2:[2,2,3,1],4:[4,4,2,3,1,0]};
for(const [,id,text] of recipes){
 check(!recipeIds.has(id),'Duplicate recipe '+id);recipeIds.add(id);
 const guid=text.match(/output:.*guid: (\w+)/)?.[1], file=byGuid.get(guid);
 check(!!file,'Missing recipe output '+id);
 const source=fs.readFileSync(file,'utf8'), rarity=+text.match(/moduleRarity: (\d+)/)?.[1];
 check(id==='module_'+source.match(/^  _guid: (\S+)/m)?.[1]+'_'+rarity,'Unstable recipe id '+id);
 if(/^  _randomStatsEnabled: 1$/m.test(source)&&!new RegExp('^  - rarity: '+rarity+'$','m').test(source))missingTuning.push(path.basename(file)+':'+rarity);
 const categories=[...text.matchAll(/^    - (\S+)/gm)].map(m=>m[1]);
 for(const tier of new Set(required[rarity]))if(!materials.some(x=>x.rarity===tier&&categories.includes(x.category)))missingSupply.push(id+':'+tier);
}
for(const file of ['CombatBalanceRules','CraftingCatalog','FactionDiscoveryRules','LootNoiseRules'].map(n=>'Assets/Resources/'+n+'.asset')){
 check(fs.existsSync(file+'.meta'),'Missing resource meta '+file);
 const script=fs.readFileSync(file,'utf8').match(/m_Script:.*guid: (\w+)/)?.[1];
 check(byGuid.has(script),'Missing script '+file);
}
for(let i=1;i<=8;i++){
 const text=fs.readFileSync('Assets/ScriptableObject/GameScene/GameItem/Armor/ArmorLV'+i+'.asset','utf8');
 check(+text.match(/_equipmentTier: (\d+)/)?.[1]===i,'Armor tier mismatch '+i);
 check(+text.match(/_armorClass: (\d+)/)?.[1]===[30,37,44,51,59,67,76,85][i-1],'Armor numeric mismatch '+i);
}
console.log(JSON.stringify({checks,recipes:recipes.length,materials:materials.length,missingEffectTuning:missingTuning,
 missingMaterialRequirements:missingSupply.length, affectedRecipes:new Set(missingSupply.map(x=>x.slice(0,x.lastIndexOf(':')))).size,
 note:'Unconfigured effect tiers/material pools are disabled at runtime; no guessed stats/items or rarity downgrades were added.'},null,2));
if(missingTuning.length||missingSupply.length)process.exitCode=2; // Authoring data remains, not a passed content-completeness check.
