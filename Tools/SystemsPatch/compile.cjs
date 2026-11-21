// Read Unity response files and compile owned runtime/editor code into Library, never ScriptAssemblies.
const fs = require('fs'), cp = require('child_process'), path = require('path');
const walk = d => fs.existsSync(d) ? fs.readdirSync(d,{withFileTypes:true}).flatMap(e=>e.isDirectory()?walk(d+'/'+e.name):[d+'/'+e.name]) : [];
const unity = 'E:/Program Files/Unity/Hub/Editor/6000.0.47f1/Editor/Data';
const output = 'Library/SystemsPatch-20261118';
fs.mkdirSync(output,{recursive:true});
const response = walk('Library/Bee/artifacts').filter(p=>p.endsWith('/Assembly-CSharp.rsp'));
if (!response.length) throw Error('Unity response files missing. Open this project in Unity first.');
const dir = path.dirname(response.sort((a,b)=>fs.statSync(b).mtimeMs-fs.statSync(a).mtimeMs)[0]).replaceAll('\\','/');
for (const name of ['Assembly-CSharp','Assembly-CSharp-Editor']) {
 let rsp=fs.readFileSync(dir+'/'+name+'.rsp','utf8')
  .replace(/^-out:.*$/m, '-out:"'+output+'/'+name+'.dll"')
  .replace(/^-refout:.*\r?\n/m,'')
  .replace(/^[-/]analyzer:.*\r?\n/gm,'').replace(/^[-/]additionalfile:.*\r?\n/gm,'');
 rsp=rsp.split(/\r?\n/).filter(l=>!(l.startsWith('"Assets/')&&l.endsWith('.cs"')&&!fs.existsSync(l.slice(1,-1)))).join('\n');
 const owned=walk('Assets/Scripts').concat(walk('Assets/Editor')).filter(p=>p.endsWith('.cs')&&!p.includes('/Scripts/StateMachine/') /* asmdef + existing response entries */);
 for (const p of owned) {
  const editor=p.includes('/Editor/');
  if ((name.endsWith('-Editor')===editor)&&!rsp.includes('"'+p+'"')) rsp+='\n"'+p+'"';
 }
 if(name.endsWith('-Editor')) rsp=rsp.replace(/-r:"[^"\n]*Assembly-CSharp(?:\.ref)?\.dll"/g,'-r:"'+output+'/Assembly-CSharp.dll"');
 const file=output+'/'+name+'.rsp';fs.writeFileSync(file,rsp);
 if (process.argv.includes('--prepare')) continue;
 const run=cp.spawnSync(unity+'/NetCoreRuntime/dotnet.exe',[unity+'/DotNetSdkRoslyn/csc.dll','@'+file],{encoding:'utf8',windowsHide:true,maxBuffer:20*1024*1024});
 fs.writeFileSync(output+'/'+name+'.log',(run.stdout||'')+(run.stderr||''));
 console.log(name,'exit',run.status);
 console.log(((run.stdout||'')+(run.stderr||'')).split('\n').filter(x=>/error |warning /.test(x)).slice(0,25).join('\n'));
 if(run.error) throw run.error;
 if(run.status!==0) process.exit(run.status||1);
}
