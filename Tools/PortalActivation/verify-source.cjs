// Read-only authoring and wiring checks; native coroutine/physics tests are separate.
const fs = require('fs'), assert = require('assert');
const read = f => fs.readFileSync(f, 'utf8');
const walk = d => fs.readdirSync(d, {withFileTypes:true}).flatMap(e => e.isDirectory() ? walk(d+'/'+e.name) : [d+'/'+e.name]);
let checks = 0;
function check(ok, label) { checks++; assert(ok, label); }
const root='Assets/ScriptableObject/GameScene/Character/Shooter/EnemyShooter/';
const assignments={
 'NormalEnemy/Scraper/ScraperShooterConfig.asset':1, 'NormalEnemy/Butcher/ButcherShooterConfig.asset':1,
 'Boss/PunisherConfig.asset':1, 'NormalEnemy/Raider/RaiderShooterConfig.asset':2,
 'Boss/Stage2SniperBossConfig.asset':2, 'BossGuard/Stage2BossGuardConfig.asset':2,
 'NormalEnemy/Agent/AgentShooterConfig.asset':3, 'Boss/Stage3RocketBossConfig.asset':3,
 'BossGuard/Stage3BossGuardConfig.asset':3
};
for(const [f,n] of Object.entries(assignments)) check(read(root+f).includes('  _faction: '+n), f);
for(const f of walk('Assets/Scripts').filter(f=>f.endsWith('.cs'))) check(!read(f).includes('ShooterFaction.Enemy'), f+' retired enum reference');
const rules=read('Assets/Resources/SectorTravelRules.asset');
check(/requirePortalActivation: 1/.test(rules)&&/portalActivationCast: 5/.test(rules)&&/portalCast: 5/.test(rules),'two five-second casts');
const manager=read('Assets/Scripts/Gameplay/World/Sectors/Runtime/SectorPortalManager.cs');
check(manager.includes('HasActivatedPortals(source)')&&!manager.includes('HasActivatedPortals(target)'),'source-only gate');
check(!manager.includes('_nextLinkedTravelTime')&&!manager.includes('travel.portalCooldown'),'no cooldown');
check(manager.includes('source.IsCleared;'),'extraction clear condition retained');
const interaction=read('Assets/Scripts/Gameplay/Interaction/Core/InteractionManager.cs');
check(interaction.includes('if(target!=null&&target.IsPeriodicStatusDamage)return;'),'status ticks do not interrupt');
check(interaction.includes('case InteractionType.Portal:')&&interaction.includes('interaction.type == InteractionType.PortalActivation'),'both cast paths wired');
const device=read('Assets/Scripts/Gameplay/World/Sectors/Portal/SectorPortalActivator.cs');
check(!/Instantiate\(|new GameObject\(|AddComponent</.test(device),'device never generates geometry at runtime');
const prefab='Assets/Prefabs/GameScene/Sector/SectorObject/InteractableObject/PortalActivationDevice.prefab';
const prefabText=read(prefab),guid=f=>read(f+'.meta').match(/^guid: (\w+)/m)[1];
check(prefabText.includes('guid: '+guid('Assets/Scripts/Gameplay/World/Sectors/Portal/SectorPortalActivator.cs')),'script GUID');
check(prefabText.includes('m_Name: Visual')&&prefabText.includes('m_Name: TemporaryPanel_ReplaceThis'),'replaceable appearance');
check(prefabText.includes('m_IsTrigger: 1')&&prefabText.includes('m_Layer: 8'),'interaction layer and trigger');
for(const file of [prefab,'Assets/Prefabs/GameScene/Sector/Bases/Sector_Base.prefab']){
 const text=read(file),ids=[...text.matchAll(/^--- !u!\d+ &(-?\d+)/gm)].map(m=>m[1]);
 check(ids.length===new Set(ids).size,file+' unique local IDs');
 for(const m of text.matchAll(/\{fileID: (-?\d+)\}/g)) check(m[1]==='0'||ids.includes(m[1]), file+' local reference '+m[1]);
}
const base='Assets/Prefabs/GameScene/Sector/Bases/Sector_Base.prefab';
check(read(base).includes('guid: '+guid(prefab)),'base contains nested authored device');
const all=walk('Assets/Prefabs/GameScene/Sector').filter(f=>f.endsWith('.prefab'));
const byGuid=new Map(all.filter(f=>fs.existsSync(f+'.meta')).map(f=>[guid(f),f]));
function inherits(file,target,visited=new Set()){
 if(file===target)return true;
 if(visited.has(file))return false;visited.add(file);
 const refs=[...read(file).matchAll(/m_SourcePrefab: \{fileID: 100100000, guid: (\w+)/g)].map(m=>byGuid.get(m[1])).filter(Boolean);
 return refs.some(f=>inherits(f,target,visited));
}
const rooms=all.filter(f=>f.includes('/Rooms/'));
for(const f of rooms)check(inherits(f,base),f+' inherits device from Sector_Base');
console.log('PASS '+checks+' source/asset checks; '+rooms.length+' room prefabs inherit the authored device. Not Play Mode.');
