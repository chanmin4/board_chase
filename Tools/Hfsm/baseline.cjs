const fs=require('fs'),crypto=require('crypto'),cp=require('child_process');
const out='Library/HfsmRefactor-20260929';fs.mkdirSync(out,{recursive:true});
const walk=d=>fs.readdirSync(d,{withFileTypes:true}).flatMap(e=>e.isDirectory()?walk(d+'/'+e.name):[d+'/'+e.name]);
const paths=[...walk('Assets/ScriptableObject/StateMachine'),...walk('Assets/Prefabs/GameScene')];
const current=Object.fromEntries(paths.filter(p=>/\.(asset|prefab|meta)$/.test(p)).map(p=>[p,crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex')]));
const manifest=out+'/authored-assets-baseline.json';
if(!fs.existsSync(manifest)) {fs.writeFileSync(manifest,JSON.stringify(current,null,2));console.log('Recorded',Object.keys(current).length,'asset/meta hashes');}
else {const old=JSON.parse(fs.readFileSync(manifest));const changed=[...new Set([...Object.keys(old),...Object.keys(current)])].filter(p=>old[p]!==current[p]);console.log('Asset differences:',changed);if(changed.length)process.exitCode=1;}
for(const p of ['Assets/Scripts/Gameplay/Entities/Shooter/Enemy/StateMachine/EnemyShooterCombatController.cs','Assets/Scripts/StateMachine/Core/StateMachine.cs','Assets/Scripts/StateMachine/Core/State.cs','Assets/Scripts/StateMachine/Core/StateCondition.cs']){
 const dest=out+'/original-'+p.split('/').pop();
 if(!fs.existsSync(dest))fs.writeFileSync(dest,cp.execFileSync('git',['show','HEAD:'+p]));
}
