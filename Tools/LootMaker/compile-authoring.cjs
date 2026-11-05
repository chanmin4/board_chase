const fs=require('fs');
const dir='Library/LootMakerAuthoring-20260929'; fs.mkdirSync(dir,{recursive:true});
for(const name of ['Assembly-CSharp','Assembly-CSharp-Editor']) {
 let text=fs.readFileSync('Library/Bee/artifacts/1900b0aE.dag/'+name+'.rsp','utf8');
 text=text.replace(/^-out:.*$/m,`-out:"${dir}/${name}.dll"`).replace(/^-refout:.*\r?\n/m,'')
  .replace(/^[-/]analyzer:.*\r?\n/gm,'').replace(/^[-/]additionalfile:.*\r?\n/gm,'')
  .split(/\r?\n/).filter(l=>!(l.startsWith('"Assets/')&&l.endsWith('.cs"')&&!fs.existsSync(l.slice(1,-1)))).join('\n');
 if(name.endsWith('Editor')) {
  text=text.replace(/-r:"[^"]*\/Assembly-CSharp.ref.dll"/g,`-r:"${dir}/Assembly-CSharp.dll"`);
  for(const path of ['Assets/Editor/LootMakerAuthoring.cs','Assets/Editor/LootMakerDeletion.cs','Assets/Editor/LootProgressionEditorWindow.ContainerAuthoring.cs','Assets/Tests/EditMode/Editor/LootMakerAuthoringTests.cs'])
   if(!text.includes(path))text+='\n"'+path+'"\n';
 }
 fs.writeFileSync(dir+'/'+name+'.rsp',text);
}
