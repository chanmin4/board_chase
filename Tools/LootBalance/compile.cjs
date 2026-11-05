// Compile into Library only; never replace the running Editor's assemblies.
const fs = require('fs'), cp = require('child_process');
const out = 'Library/LootBalanceVerification';
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
  const extra = name === 'Assembly-CSharp' ? ['Assets/Scripts/Gameplay/Entities/Common/Equipment/EquipmentRepairPolicy.cs','Assets/Scripts/Gameplay/Items/Loot/StageEquipmentLootDistribution.cs'] :
    [...walk('Assets/Editor'),...walk('Assets/Tests/EditMode/Editor')].filter(p=>/\/LootBalance.*\.cs$/.test(p));
  for (const p of extra) if(!rsp.includes('"'+p+'"')) rsp+='\n"'+p+'"';
  if(name.endsWith('-Editor')) rsp=rsp.replace(/-r:"[^"\n]*[/\\]Assembly-CSharp(?:\.ref)?\.dll"/g,`-r:"${out}/Assembly-CSharp.dll"`);
  fs.writeFileSync(`${out}/${name}.rsp`,rsp);
  const r=cp.spawnSync(unity+'/NetCoreRuntime/dotnet.exe',[unity+'/DotNetSdkRoslyn/csc.dll','@'+out+'/'+name+'.rsp'],{encoding:'utf8',maxBuffer:20*1024*1024,windowsHide:true});
  fs.writeFileSync(`${out}/${name}.compile.log`,(r.stdout||'')+(r.stderr||'')+(r.error||''));
  console.log(name,'exit='+r.status,r.stdout||'',r.stderr||'',r.error||'');
  if(r.error||r.status!==0)process.exit(1);
}
