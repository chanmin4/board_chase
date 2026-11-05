const fs=require('fs'), assert=require('assert');
let checks=0;const check=(v,m)=>{checks++;assert(v,m);};
const catalog=fs.readFileSync('Assets/Resources/RuntimeContentCatalog.asset','utf8');
const rules=fs.readFileSync('Assets/Resources/PassiveModuleRules.asset','utf8');
const systems=fs.readFileSync('Assets/Resources/RecyclingRules.asset','utf8');
for(const name of fs.readdirSync('Assets/Resources/PassiveModules').filter(x=>x.endsWith('.asset'))){
 const p='Assets/Resources/PassiveModules/'+name,text=fs.readFileSync(p,'utf8');
 const id=fs.readFileSync(p+'.meta','utf8').match(/^guid: (\w+)/m)[1];
 check(text.includes('_guid: '+id),'Save GUID mismatch: '+name);
 check(catalog.includes('guid: '+id),'Missing catalog item: '+name);
 check(name.startsWith('TestModule_')||rules.includes('guid: '+id)||systems.includes('guid: '+id),'Unused rule asset: '+name);
 if(name.startsWith('TestModule_')){
  check(text.includes('_randomStatsEnabled: 1'),'No random authoring: '+name);
  const rarities=[...text.matchAll(/^  - rarity: (\d+)/gm)].map(x=>+x[1]);
  check(new Set(rarities).size===5 && [0,1,3,2,4].every(x=>rarities.includes(x)),'Missing rarity rows: '+name);
 } else if(name.startsWith('Module_')) {
  check(text.includes('_maxStack: 1'),'Modules must be unique inventory items: '+name);
  const tiers=[...text.matchAll(/^  - rarity: (\d+)/gm)].map(x=>+x[1]);
  check((name==='Module_Ricochet.asset'?[3,2]:[0,1,3,2]).every(x=>tiers.includes(x)),'Missing authored module rarity: '+name);
  check(/_effectGroup: \w+/.test(text),'Missing effect-group identity: '+name);
 } else check(text.includes('_maxStack: 999'),'Parts must stack: '+name);
}
const quest=fs.readFileSync('Assets/Scripts/Gameplay/Progression/Quests/QuestRuntime.cs','utf8');
check((quest.match(/state\.TryComplete\(/g)||[]).length===1,'Automatic quest completion reintroduced');
const jam=fs.readFileSync('Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerAimController.Malfunction.cs','utf8');
check(jam.includes('Time.time + 5f'),'Function check must last 5 sec');
check(jam.includes('SetWeaponJammed(_functionCheckSlot, false)'),'No completion unlock');
check(!jam.includes('Durability =')&&!jam.includes('RepairWeaponToFull'),'Function check must not repair durability');
const hover=fs.readFileSync('Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/EnemyPassiveHoverUI.cs','utf8');
check(!/class EnemyPassiveHoverUI[^\{]*(IDrag|IClick|IPointerClick)/.test(hover),'Enemy passive must be read only');
const weapon=fs.readFileSync('Assets/Scripts/Gameplay/Items/Weapons/Definitions/RangedWeaponSO.cs','utf8');
check(/_durabilityLossPerFire\s*=\s*0\.05f/.test(weapon),'Default wear must be .05 durability per successful shot');
const enemyShoot=fs.readFileSync('Assets/Scripts/Gameplay/Entities/Shooter/Enemy/Common/EnemyShooterShoot.cs','utf8');
check(!enemyShoot.includes('ApplyCurrentWeaponFireWear('),'Enemy shot must not consume weapon durability');
check(!enemyShoot.includes('CheckWeaponMalfunction(')&&!enemyShoot.includes('MalfunctionChance('),'Enemy shot must not roll a malfunction');
const equipment=fs.readFileSync('Assets/Scripts/Gameplay/Entities/Common/Equipment/EntityEquipmentRuntime.cs','utf8');
check(equipment.includes('if (IsEnemyWeaponOwner) return false;'),'Shared wear API must refuse enemy wear');
check(equipment.includes('value && !IsEnemyWeaponOwner'),'Enemy jam flags must be rejected');
console.log(`PASS ${checks} authored asset/source contract checks. Not a Unity import or Play Mode test.`);
