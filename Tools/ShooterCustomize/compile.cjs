// Compile runtime + editor into Library only, using this checkout's current Unity references.
const fs = require('fs'), cp = require('child_process');
const out = 'Library/ShooterCustomizeVerification';
const unity = 'E:/Program Files/Unity/Hub/Editor/6000.0.47f1/Editor/Data';
const walk = d => fs.readdirSync(d, {withFileTypes:true}).flatMap(e => e.isDirectory() ? walk(d+'/'+e.name) : [d+'/'+e.name]);
fs.mkdirSync(out,{recursive:true});
const files = walk('Library/Bee/artifacts');
for (const name of ['Assembly-CSharp','Assembly-CSharp-Editor']) {
  const source = files.filter(p=>p.endsWith('/'+name+'.rsp')).sort((a,b)=>fs.statSync(b).mtimeMs-fs.statSync(a).mtimeMs)[0];
  if (!source) throw Error('Missing Unity response file: '+name);
  let rsp = fs.readFileSync(source,'utf8').replace(/^-out:.*$/m,`-out:"${out}/${name}.dll"`)
    .replace(/^-refout:.*\r?\n/m,'').replace(/^[-/]analyzer:.*\r?\n/gm,'').replace(/^[-/]additionalfile:.*\r?\n/gm,'')
    .split(/\r?\n/).filter(l=>!(l.startsWith('"Assets/')&&l.endsWith('.cs"')&&!fs.existsSync(l.slice(1,-1)))).join('\n');
  const extra = name === 'Assembly-CSharp' ? [...walk('Assets/Scripts/Gameplay/Entities/Shooter/Common/Visuals'), 'Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/ShooterEquipmentController.Backpack.cs'] :
    [...walk('Assets/Editor'),...walk('Assets/Tests/EditMode/Editor')].filter(p=>/\/(Shooter(Customize|SpawnLook)|MakerSelectionUiState).*\.cs$/.test(p));
  for (const p of extra.filter(p=>p.endsWith('.cs'))) if(!rsp.includes('"'+p+'"')) rsp+='\n"'+p+'"';
  if(name.endsWith('-Editor')) rsp=rsp.replace(/-r:"[^"\n]*[/\\]Assembly-CSharp(?:\.ref)?\.dll"/g,`-r:"${out}/Assembly-CSharp.dll"`);
  fs.writeFileSync(`${out}/${name}.rsp`,rsp);
  const result=cp.spawnSync(unity+'/NetCoreRuntime/dotnet.exe',[unity+'/DotNetSdkRoslyn/csc.dll','@'+out+'/'+name+'.rsp'],{encoding:'utf8',maxBuffer:20*1024*1024,windowsHide:true});
  fs.writeFileSync(`${out}/${name}.compile.log`,(result.stdout||'')+(result.stderr||'')+(result.error||''));
  console.log(name, 'exit='+result.status, result.stdout||'', result.stderr||'', result.error||'');
  if(result.error||result.status!==0)process.exit(1);
}
