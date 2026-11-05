// Read-only migration planner. Emits an apply_patch plan; never writes/deletes project files itself.
// Six redundant configs are mapped to the existing canonical GUIDs. Distinct bosses/guards are not merged.
const fs = require('fs');
const root = 'Assets/ScriptableObject/GameScene/Character/Shooter/EnemyShooter/NormalEnemy';
const pairs = [
 ['Scraper/ScraperStage2ShooterConfig','Scraper/ScraperShooterConfig'],
 ['Scraper/ScraperStage3ShooterConfig','Scraper/ScraperShooterConfig'],
 ['Raider/RaiderStage2ShooterConfig','Raider/RaiderShooterConfig'],
 ['Raider/RaiderStage4ShooterConfig','Raider/RaiderShooterConfig'],
 ['Raider/RaiderUpgradeShooterConfig','Raider/RaiderShooterConfig'],
 ['Agent/AgentStage4ShooterConfig','Agent/AgentShooterConfig']
];
const read = p => fs.readFileSync(p,'utf8').replace(/\r\n/g,'\n');
const guid = p => read(p+'.meta').match(/^guid: ([a-f0-9]{32})$/m)[1];
const walk = d => fs.readdirSync(d,{withFileTypes:true}).flatMap(e=>e.isDirectory()?walk(d+'/'+e.name):[d+'/'+e.name]);
const removed = pairs.map(([p])=>root+'/'+p+'.asset').concat([
 root+'/Raider/RaiderStage3LoadoutTable.asset', root+'/Raider/RaiderStage3Loadout.asset', root+'/Raider/RaiderUpgradeCustomize.asset'
]);
const map = new Map(pairs.map(([a,b])=>[guid(root+'/'+a+'.asset'),guid(root+'/'+b+'.asset')]));
for (const p of removed) {
 if(!fs.existsSync(p)||!fs.existsSync(p+'.meta'))throw Error('Missing exact migration target: '+p);
 if(/_(?:fixedSpawnLook|spawnLookPool): \{fileID: (?!0\})/.test(read(p)))throw Error('New appearance binding must be preserved before consolidation: '+p);
}
const sources = new Map(walk('Assets').filter(p=>/\.(asset|prefab|unity|controller|overrideController|json)$/.test(p)).map(p=>[p,read(p)]));
const changes = new Map();
for(const [p,text] of sources) {
 if(removed.includes(p))continue;
 let next=text; for(const [a,b] of map)next=next.replaceAll(a,b);
 if(next!==text)changes.set(p,next);
}
const loot='Assets/ScriptableObject/GameScene/GameItem/LootProgressionConfig.asset';
const oldLoot=read(loot), upgrade=oldLoot.match(/^  - sourceId: raider_upgrade\n[\s\S]*?(?=^  - sourceId:|^  _)/m)?.[0];
if(!upgrade || /- item:/.test(upgrade))throw Error('Upgrade loot source changed; review before removing');
changes.set(loot,oldLoot.replace(upgrade,'')); // Do not create two lookup entries for the same canonical config.
for(const [file,title,description] of [
 ['Campaign_17_Raider_Upgrade','Campaign 17 - Raider Suppression','Template: defeat Raider enemies.'],
 ['Campaign_18_Upgrade_Accuracy','Campaign 18 - Raider Accuracy','Template: land ranged hits on Raider enemies.']
]) {
 const p='Assets/ScriptableObject/GameScene/Quest/'+file+'.asset';
 changes.set(p,(changes.get(p)||read(p)).replace(/^  _displayName:.*$/m,'  _displayName: '+title).replace(/^  _description:.*$/m,'  _description: '+description));
}
// Equipment references survive without duplicating monster identity/stats/AI. Upgrade-specific stage 3 gear is deliberately retired.
for(const [base,entries] of [
 ['Scraper/ScraperShooterConfig',[[2,'Scraper/ScraperStage2LoadoutTable'],[3,'Scraper/ScraperStage3LoadoutTable']]],
 ['Raider/RaiderShooterConfig',[[2,'Raider/RaiderStage2LoadoutTable'],[4,'Raider/RaiderStage4LoadoutTable']]],
 ['Agent/AgentShooterConfig',[[4,'Agent/AgentStage4LoadoutTable']]]
]) {
 const p=root+'/'+base+'.asset', text=read(p);
 if(text.includes('_stageLoadouts:'))throw Error('Already migrated or custom stage rules exist: '+p);
 const rows='  _stageLoadouts:\n'+entries.map(([stage,path])=>'  - stage: '+stage+'\n    loadout: {fileID: 11400000, guid: '+guid(root+'/'+path+'.asset')+', type: 2}\n').join('');
 changes.set(p,text.replace(/^(  _loadoutTable:.*\n)/m,'$1'+rows));
}
// Restore the missing real rig on the single shared Visual_Shooter, with no default look or transform changes.
const visual='Assets/Prefabs/GameScene/Shooter/Visual_Shooter.prefab'; let vt=read(visual);
if(vt.includes('0dce341d25b343f68c5c3aed21d1ba89'))throw Error('Visual rig already exists; review instead of adding a duplicate');
const transforms={};
for(const block of vt.split(/^--- /m))if(block.startsWith('!u!1 ')){
 const name=block.match(/^  m_Name: (.*)$/m)?.[1], id=block.match(/component: \{fileID: (\d+)\}/)?.[1]; if(name&&id)transforms[name]=id;
}
if(!transforms.Meshes || !transforms.GameplayColliderRoot)throw Error('Unsafe visual hierarchy');
const rigId='920260930100000001'; if(vt.includes(rigId))throw Error('Rig fileID collision');
vt=vt.replace('  - component: {fileID: 6699817276302984180}\n','  - component: {fileID: 6699817276302984180}\n  - component: {fileID: '+rigId+'}\n');
const fields={_bodyVisualRoot:'Meshes',_gameplayColliderRoot:'GameplayColliderRoot',_armorSocket:'ArmorSocket',_rightHandSocket:'RightHand_WeaponSocket',_leftHandSocket:'LeftHand',_hairSocket:'HairSocket',_eyesSocket:'EyesSocket',_mouthSocket:'MouthSocket',_faceSocket:'FaceSocket',_bodySocket:'BodySocket',_feetSocket:'FeetSocket'};
vt+='--- !u!114 &'+rigId+'\nMonoBehaviour:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: 8470056084066338813}\n  m_Enabled: 1\n  m_EditorHideFlags: 0\n  m_Script: {fileID: 11500000, guid: 0dce341d25b343f68c5c3aed21d1ba89, type: 3}\n  m_Name: \n  m_EditorClassIdentifier: \n';
for(const [field,name] of Object.entries(fields))vt+='  '+field+': {fileID: '+(transforms[name]||0)+'}\n';
vt+='  _maskSocket: {fileID: 0}\n  _backSocket: {fileID: 0}\n  _defaultCustomize: {fileID: 0}\n  _characterCustomize: {fileID: 0}\n  _defaultAppearanceProfile: {fileID: 0}\n'; changes.set(visual,vt);
for(const p of removed){const g=guid(p);for(const [other,text] of sources)if(!removed.includes(other)&&(changes.get(other)||text).includes(g))throw Error('Reference still exists: '+p+' <- '+other);}
let patch='*** Begin Patch\n';
for(const [p,next] of changes){
 const old=read(p).trimEnd().split('\n'), rows=next.trimEnd().split('\n');
 let start=0;while(start<old.length&&start<rows.length&&old[start]===rows[start])start++;
 let end=0;while(end<old.length-start&&end<rows.length-start&&old[old.length-1-end]===rows[rows.length-1-end])end++;
 const a=Math.max(0,start-2), oldEnd=Math.min(old.length,old.length-end+2), newEnd=Math.min(rows.length,rows.length-end+2);
 patch+='*** Update File: '+p+'\n@@\n'+old.slice(a,oldEnd).map(l=>'-'+l).join('\n')+'\n'+rows.slice(a,newEnd).map(l=>'+'+l).join('\n')+'\n';
}
for(const p of removed)patch+='*** Delete File: '+p+'\n*** Delete File: '+p+'.meta\n';
patch+='*** End Patch';
console.log(JSON.stringify({changed:[...changes.keys()],removed,patch}));
