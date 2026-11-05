// Compile against this checkout's actual Unity response files without touching live ScriptAssemblies.
const fs = require('fs'), cp = require('child_process'), path = require('path');
const out = 'Library/CombatModulesVerification';
const unity = 'E:/Program Files/Unity/Hub/Editor/6000.0.47f1/Editor/Data';
fs.mkdirSync(out, { recursive: true });
const walk = d => fs.readdirSync(d, { withFileTypes: true }).flatMap(e => e.isDirectory() ? walk(d + '/' + e.name) : [d + '/' + e.name]);
const rspFiles = walk('Library/Bee/artifacts');
function compile(name) {
  const source = rspFiles.filter(p => p.endsWith('/' + name + '.rsp')).sort((a,b)=>fs.statSync(b).mtimeMs-fs.statSync(a).mtimeMs)[0];
  if (!source) throw new Error('Unity compiler response file missing: ' + name);
  let lines = fs.readFileSync(source, 'utf8').split(/\r?\n/).filter(l => !/^[-/](analyzer|additionalfile|refout):/.test(l));
  lines = lines.filter(l=>!(l.startsWith('"Assets/') && l.endsWith('.cs"') && !fs.existsSync(l.slice(1,-1))));
  lines = lines.map(l=>l.startsWith('-out:') ? `-out:"${out}/${name}.dll"` : l);
  if(name.endsWith('Editor')) lines = lines.map(l=>/^[-/]r:.*[/\\]Assembly-CSharp(?:\.ref)?\.dll"?$/.test(l) ? `-r:"${out}/Assembly-CSharp.dll"` : l);
  const candidates = name.endsWith('Editor') ? [...walk('Assets/Editor'), ...walk('Assets/Scripts/Authoring/Editor'), ...walk('Assets/Tests/EditMode/Editor')] : walk('Assets/Scripts').filter(p=>!p.includes('/Editor/'));
  for (const file of candidates.filter(p=>p.endsWith('.cs') && !p.includes('/WIP/Legacy/Gameplay/Player/VSplatterWeaponHolder.cs'))) {
    let directory = path.dirname(file), separateAssembly = false;
    while (directory !== '.' && directory !== 'Assets') {
      if (fs.readdirSync(directory).some(p=>p.endsWith('.asmdef'))) { separateAssembly=true; break; }
      directory=path.dirname(directory);
    }
    if(separateAssembly) continue;
    // These WIP types must still deserialize old prefab GUIDs and save enums, but their runtime entry points are inert.
    if (!name.endsWith('Editor') && (/Assembly-CSharp-Editor/.test(file) ||
        (file.includes('/WIP/') && !file.startsWith('Assets/Scripts/WIP/Legacy/MetaProgression/') && !file.startsWith('Assets/Scripts/WIP/Legacy/MonolithicGameRules/')))) continue;
    const quoted = '"'+file+'"'; if(!lines.includes(quoted)) lines.push(quoted);
  }
  const rsp = out+'/'+name+'.rsp'; fs.writeFileSync(rsp,lines.join('\n'));
  const result=cp.spawnSync(unity+'/NetCoreRuntime/dotnet.exe',[unity+'/DotNetSdkRoslyn/csc.dll','@'+rsp],{encoding:'utf8',maxBuffer:30*1024*1024,windowsHide:true});
  const log=(result.stdout||'')+(result.stderr||'')+(result.error||''); fs.writeFileSync(out+'/'+name+'.log',log);
  const errors=log.split(/\r?\n/).filter(l=>/error |Exception|EPERM/.test(l));
  console.log(`${name}: exit=${result.status}, errors=${errors.length}, warnings=${(log.match(/warning CS/g)||[]).length}`);
  console.log(errors.join('\n')); if(result.status!==0) process.exit(result.status||1);
}
compile('Assembly-CSharp'); compile('Assembly-CSharp-Editor');
