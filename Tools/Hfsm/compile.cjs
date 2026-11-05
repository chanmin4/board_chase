// Compile against this checkout's Unity response files, without touching ScriptAssemblies.
const fs=require('fs'),path=require('path'),cp=require('child_process');
const root=process.cwd(), out='Library/HfsmRefactor-20260929';fs.mkdirSync(out,{recursive:true});
const unity='E:/Program Files/Unity/Hub/Editor/6000.0.47f1/Editor/Data';
const names=['VSplatter.StateMachine','VSplatter.StateMachine.Editor','Assembly-CSharp','Assembly-CSharp-Editor'];
const walk=d=>fs.readdirSync(d,{withFileTypes:true}).flatMap(e=>e.isDirectory()?walk(d+'/'+e.name):[d+'/'+e.name]);
const extras={
 'VSplatter.StateMachine':walk('Assets/Scripts/StateMachine').filter(p=>p.endsWith('.cs')&&!p.includes('/Editor/')),
 'VSplatter.StateMachine.Editor':walk('Assets/Scripts/StateMachine/Editor').filter(p=>p.endsWith('.cs')),
 'Assembly-CSharp':walk('Assets/Scripts/Gameplay/Entities/Shooter/Enemy/StateMachine').filter(p=>p.endsWith('.cs')),
 'Assembly-CSharp-Editor':[
  ...walk('Assets/Editor'),...walk('Assets/Scripts/Authoring/Editor'),...walk('Assets/Tests/EditMode/Editor')
 ].filter(p=>p.endsWith('.cs'))
};
for(const name of names){
 let rsp=fs.readFileSync('Library/Bee/artifacts/1900b0aE.dag/'+name+'.rsp','utf8')
  .replace(/^-out:.*$/m,`-out:"${out}/${name}.dll"`).replace(/^-refout:.*\r?\n/m,'')
  .replace(/^[-/]analyzer:.*\r?\n/gm,'').replace(/^[-/]additionalfile:.*\r?\n/gm,'')
  .split(/\r?\n/).filter(l=>!(l.startsWith('"Assets/')&&l.endsWith('.cs"')&&!fs.existsSync(l.slice(1,-1)))).join('\n');
 for(const dep of names.slice(0,names.indexOf(name))) rsp=rsp.replace(new RegExp('-r:"[^"\\n]*[/\\\\]'+dep.replaceAll('.','\\.')+'(?:\\.ref)?\\.dll"','g'),`-r:"${out}/${dep}.dll"`);
 for(const p of extras[name])if(!rsp.includes('"'+p+'"'))rsp+='\n"'+p+'"';
 fs.writeFileSync(`${out}/${name}.rsp`,rsp);
 const result=cp.spawnSync(unity+'/NetCoreRuntime/dotnet.exe',[unity+'/DotNetSdkRoslyn/csc.dll','@'+out+'/'+name+'.rsp'],{encoding:'utf8',maxBuffer:20*1024*1024,windowsHide:true});
 fs.writeFileSync(`${out}/${name}.compile.log`,(result.stdout||'')+(result.stderr||'')+(result.error||''));
 console.log(name,'exit='+result.status);
 if(result.error||result.status!==0){console.log((result.stdout||'')+(result.stderr||'')+(result.error||''));process.exit(1);}
 console.log((result.stdout||'').split('\n').filter(l=>l.includes('warning')).slice(0,4).join('\n'));
}
// Compile the user-facing script templates too; generated sources stay outside Assets.
let templateRsp=fs.readFileSync(`${out}/VSplatter.StateMachine.rsp`,'utf8')
 .split(/\r?\n/).filter(l=>!/^".*\.cs"$/.test(l.trim())).join('\n')
 .replace(/^-out:.*$/m,`-out:"${out}/Hfsm.TemplateSmoke.dll"`);
templateRsp+=`\n-r:"${out}/VSplatter.StateMachine.dll"`;
for(const kind of ['Action','Condition']){
 const runtime=`Hfsm${kind}TemplateSmoke`;
 const source=fs.readFileSync(`Assets/Scripts/StateMachine/Editor/Templates/State${kind}.txt`,'utf8')
  .replaceAll('#SCRIPTNAME#',runtime+'SO').replaceAll('#RUNTIMENAME#',runtime).replaceAll('#RUNTIMENAME_WITH_SPACES#',runtime);
 const file=`${out}/${runtime}.cs`;fs.writeFileSync(file,source);templateRsp+=`\n"${file}"`;
}
fs.writeFileSync(`${out}/templates.rsp`,templateRsp);
const templates=cp.spawnSync(unity+'/NetCoreRuntime/dotnet.exe',[unity+'/DotNetSdkRoslyn/csc.dll','@'+out+'/templates.rsp'],{encoding:'utf8',windowsHide:true});
fs.writeFileSync(`${out}/templates.compile.log`,(templates.stdout||'')+(templates.stderr||'')+(templates.error||''));
console.log('Action/Condition templates exit='+templates.status);
if(templates.error||templates.status!==0){console.log((templates.stdout||'')+(templates.stderr||'')+(templates.error||''));process.exit(1);}
