// Build/run a small managed verification or transformation tool with Unity's bundled .NET.
const fs=require('fs'),cp=require('child_process'),path=require('path');
const unity='E:/Program Files/Unity/Hub/Editor/6000.0.47f1/Editor/Data';
const out='Library/HfsmRefactor-20260929';fs.mkdirSync(out,{recursive:true});
const mode=process.argv[2]||'extract';const name=mode==='extract'?'ExtractCombatDomains':mode==='routing'?'ManagedRoutingTests':mode==='combat'?'ManagedCombatDecisionTests':'ManagedTests';
const shared=unity+'/NetCoreRuntime/shared/Microsoft.NETCore.App';const version=fs.readdirSync(shared).sort().pop();
let sources=mode==='extract'?['Tools/Hfsm/ExtractCombatDomains.cs']:mode==='routing'?['Tools/Hfsm/ManagedRoutingTests.cs','Assets/Scripts/StateMachine/Editor/HfsmGraphRouting.cs']:mode==='combat'?['Tools/Hfsm/ManagedCombatDecisionTests.cs','Assets/Scripts/Gameplay/Entities/Shooter/Enemy/StateMachine/EnemyShooterCombatDecisionPolicy.cs','Assets/Tests/EditMode/Editor/HfsmCombatDecisionPolicyTests.cs']:['Tools/Hfsm/ManagedTests.cs','Assets/Scripts/StateMachine/Core/HfsmNode.cs','Assets/Scripts/StateMachine/Core/HfsmRuntime.cs','Assets/Tests/EditMode/Editor/HfsmRuntimeTests.cs'];
const refs=fs.readdirSync(shared+'/'+version).filter(f=>/^(System\.|Microsoft\.CSharp|mscorlib|netstandard).*\.dll$/.test(f)&&!f.includes('.Native.')).map(f=>shared+'/'+version+'/'+f);
if(mode==='extract'){for(const f of ['Microsoft.CodeAnalysis','Microsoft.CodeAnalysis.CSharp']){refs.push(unity+'/DotNetSdkRoslyn/'+f+'.dll');fs.copyFileSync(unity+'/DotNetSdkRoslyn/'+f+'.dll',out+'/'+f+'.dll');}}
else if(mode==='routing'){const core=unity+'/Managed/UnityEngine/UnityEngine.CoreModule.dll';refs.push(core);fs.copyFileSync(core,out+'/UnityEngine.CoreModule.dll');}
else {const rsp=fs.readFileSync('Library/Bee/artifacts/1900b0aE.dag/Assembly-CSharp-Editor.rsp','utf8');const n=rsp.match(/-r:"([^"]*nunit.framework.dll)"/)[1];refs.push(n);fs.copyFileSync(n,out+'/nunit.framework.dll');}
fs.writeFileSync(out+'/'+name+'.rsp',['-nologo','-target:exe','-langversion:latest','-out:"'+out+'/'+name+'.dll"',...refs.map(r=>'-r:"'+r+'"'),...sources.map(s=>'"'+s+'"')].join('\n'));
let r=cp.spawnSync(unity+'/NetCoreRuntime/dotnet.exe',[unity+'/DotNetSdkRoslyn/csc.dll','@'+out+'/'+name+'.rsp'],{encoding:'utf8',windowsHide:true});console.log(r.stdout,r.stderr);if(r.error||r.status!==0){console.error(r.error||'Compiler failed');process.exit(1);}
fs.writeFileSync(out+'/'+name+'.runtimeconfig.json',JSON.stringify({runtimeOptions:{tfm:'net'+version.split('.')[0]+'.0',framework:{name:'Microsoft.NETCore.App',version}}}));
r=cp.spawnSync(unity+'/NetCoreRuntime/dotnet.exe',[out+'/'+name+'.dll',...process.argv.slice(3)],{encoding:'utf8',maxBuffer:1024*1024*20,windowsHide:true});console.log(r.stdout,r.stderr);if(r.error||r.status===null){console.error(r.error||'Runner failed to start');process.exit(1);}process.exit(r.status);
