// Compile only into Library using the references already configured by this project's Unity Editor.
const fs = require('fs'), cp = require('child_process');
const output = 'Library/SectorHatchVerification';
const unity = 'E:/Program Files/Unity/Hub/Editor/6000.0.47f1/Editor/Data';
const walk = d => fs.readdirSync(d, {withFileTypes:true}).flatMap(e => e.isDirectory() ? walk(d+'/'+e.name) : [d+'/'+e.name]);
fs.mkdirSync(output,{recursive:true});
const responses = walk('Library/Bee/artifacts');
for (const name of ['Assembly-CSharp','Assembly-CSharp-Editor']) {
  const source = responses.filter(p => p.endsWith('/'+name+'.rsp')).sort((a,b) => fs.statSync(b).mtimeMs-fs.statSync(a).mtimeMs)[0];
  if (!source) throw Error('Missing Unity response: '+name);
  let rsp = fs.readFileSync(source,'utf8').replace(/^-out:.*$/m,'-out:"'+output+'/'+name+'.dll"')
    .replace(/^-refout:.*\r?\n/m,'').replace(/^[-/]analyzer:.*\r?\n/gm,'').replace(/^[-/]additionalfile:.*\r?\n/gm,'')
    .split(/\r?\n/).filter(l => !(l.startsWith('"Assets/') && l.endsWith('.cs"') && !fs.existsSync(l.slice(1,-1)))).join('\n');
  const extras = name === 'Assembly-CSharp' ? ['Assets/Scripts/Gameplay/World/Sectors/StageMap/SectorHatch.cs',
    'Assets/Scripts/Gameplay/World/Sectors/StageMap/RaidExtractionPortalPolicy.cs',
    'Assets/Scripts/Presentation/UI/Gameplay/Overlay/UIEscapeRouter.cs',
    'Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/ShooterBodyCollisionMotor.cs',
    'Assets/Scripts/Diagnostics/Runtime/GameColliderOverlay.cs'] :
    ['Assets/Editor/SectorHatchEditor.cs','Assets/Tests/EditMode/Editor/SectorHatchTests.cs','Assets/Tests/EditMode/Editor/SectorHatchVerification.cs',
     'Assets/Tests/EditMode/Editor/RaidExtractionPortalTests.cs','Assets/Tests/EditMode/Editor/RaidExtractionPortalVerification.cs',
     'Assets/Tests/EditMode/Editor/UIEscapeTests.cs','Assets/Tests/EditMode/Editor/UIEscapeVerification.cs',
     'Assets/Tests/EditMode/Editor/HomeRunTransitionTests.cs','Assets/Tests/EditMode/Editor/HomeRunTransitionVerification.cs',
     'Assets/Tests/EditMode/Editor/MapVisionPortalVerification.cs',
     'Assets/Scripts/Authoring/Editor/DebugMonitorWindow.GameColliders.cs',
     'Assets/Tests/EditMode/Editor/GameColliderLabVerification.cs',
     'Assets/Tests/EditMode/Editor/PopupAimVerification.cs',
     'Assets/Tests/EditMode/Editor/CombatLabVerification.cs'];
  for (const file of extras) if (!rsp.includes('"'+file+'"')) rsp += '\n"'+file+'"';
  if (name.endsWith('-Editor')) rsp = rsp.replace(/-r:"[^"\n]*[/\\]Assembly-CSharp(?:\.ref)?\.dll"/g, '-r:"'+output+'/Assembly-CSharp.dll"');
  fs.writeFileSync(output+'/'+name+'.rsp',rsp);
  const result = cp.spawnSync(unity+'/NetCoreRuntime/dotnet.exe',[unity+'/DotNetSdkRoslyn/csc.dll','@'+output+'/'+name+'.rsp'],
    {encoding:'utf8',maxBuffer:20*1024*1024,windowsHide:true});
  fs.writeFileSync(output+'/'+name+'.compile.log',(result.stdout||'')+(result.stderr||'')+(result.error||''));
  console.log(name,'exit='+result.status,result.stdout||'',result.stderr||'',result.error||'');
  if (result.error || result.status !== 0) process.exit(1);
}
