// Emits apply_patch input only. Never writes assets directly; references/art remain intact.
const fs=require('fs');
const paths={player:'Assets/Prefabs/UI/GamePlay/PanelHub/Inventory/PlayerInventoryPanel.prefab',enemy:'Assets/Prefabs/UI/GamePlay/PanelHub/Inventory/EnemyInventoryPanel.prefab',canvas:'Assets/Prefabs/UI/GamePlay/Canvas-GamePlay.prefab'};
function parse(text){return text.replace(/\r/g,'').split(/(?=^--- !u!)/m).map(raw=>{const m=raw.match(/^--- !u!(\d+) &(\d+)/);return{raw,id:m?.[2],type:+m?.[1]};});}
const P=parse(fs.readFileSync(paths.player,'utf8')),E=parse(fs.readFileSync(paths.enemy,'utf8'));
let next=930280000000000001n;const id=()=>String(next++);
const find=(ds,k)=>ds.find(d=>d.id===String(k));
const go=(ds,name)=>ds.find(d=>d.type===1&&d.raw.includes('  m_Name: '+name+'\n'));
const rectFor=(ds,g)=>ds.find(d=>d.type===224&&d.raw.includes('m_GameObject: {fileID: '+g+'}'));
const rectName=(ds,name)=>rectFor(ds,go(ds,name).id);
const set=(d,field,value)=>{if(field==='m_Children'){d.raw=d.raw.replace(/^  m_Children:(?: \[\])?\n(?:  -[^\n]*\n)*/m,'  m_Children: '+value+'\n');return;}const re=new RegExp('(^ +'+field+': ?)[^\\n]*','m');if(!re.test(d.raw))throw Error('Missing '+field+' on '+d.id);d.raw=d.raw.replace(re,'$1'+value);};
const child=(d,child)=>{d.raw=d.raw.replace(/  m_Children:\n/,'  m_Children:\n  - {fileID: '+child+'}\n');if(!d.raw.includes('fileID: '+child+'}'))d.raw=d.raw.replace('  m_Children: []','  m_Children:\n  - {fileID: '+child+'}');};
function mod(instance,target,guid,field,value){const line=`    - target: {fileID: ${target}, guid: ${guid}, type: 3}\n      propertyPath: ${field}\n      value: ${value}\n      objectReference: {fileID: 0}\n`;const re=new RegExp('    - target: \\{fileID: '+target+', guid: '+guid+', type: 3\\}\\n      propertyPath: '+field.replace(/[.]/g,'\\.')+'\\n      value:[^\\n]*\\n      objectReference:[^\\n]*\\n');if(re.test(instance.raw))instance.raw=instance.raw.replace(re,line);else instance.raw=instance.raw.replace('    m_Modifications:\n','    m_Modifications:\n'+line);}
function fields(ds,rect,values){if(!rect)throw Error('Missing rect');if(!rect.raw.includes(' stripped\n')){for(const [k,v]of Object.entries(values))set(rect,k,v);return;}
 const m=rect.raw.match(/m_CorrespondingSourceObject: \{fileID: (\d+), guid: (\w+)/),inst=find(ds,rect.raw.match(/m_PrefabInstance: \{fileID: (\d+)/)[1]);
 for(const[k,v]of Object.entries(values)){const vec=v.match(/^\{x: ([^,]+), y: ([^,}]+)(?:, z: ([^}]+))?\}/);if(vec){mod(inst,m[1],m[2],k+'.x',vec[1]);mod(inst,m[1],m[2],k+'.y',vec[2]);if(vec[3]!==undefined)mod(inst,m[1],m[2],k+'.z',vec[3]);}else mod(inst,m[1],m[2],k,v);}
}
function box(ds,r,x,y,w,h,scale=1){fields(ds,r,{m_AnchorMin:'{x: 0, y: 1}',m_AnchorMax:'{x: 0, y: 1}',m_Pivot:'{x: 0, y: 1}',m_AnchoredPosition:`{x: ${x}, y: ${-y}}`,m_SizeDelta:`{x: ${w}, y: ${h}}`,m_LocalScale:`{x: ${scale}, y: ${scale}, z: ${scale}}`});}
function top(ds,r,height){fields(ds,r,{m_AnchorMin:'{x: 0, y: 1}',m_AnchorMax:'{x: 1, y: 1}',m_Pivot:'{x: 0.5, y: 1}',m_AnchoredPosition:'{x: 0, y: 0}',m_SizeDelta:`{x: 0, y: ${height}}`});}
function fill(ds,r,x,y){fields(ds,r,{m_AnchorMin:'{x: 0, y: 0}',m_AnchorMax:'{x: 1, y: 1}',m_Pivot:'{x: 0.5, y: 0.5}',m_AnchoredPosition:`{x: 0, y: ${-y/2}}`,m_SizeDelta:`{x: ${-x*2}, y: ${-y}}`});}
function componentRect(ds,c){const comp=find(ds,c);if(!comp)throw Error('Missing component '+c);const owner=comp.raw.match(/m_GameObject: \{fileID: (\d+)/)?.[1];const direct=owner&&owner!=='0'?rectFor(ds,owner):null;if(direct)return direct;let instance=comp.raw.match(/m_PrefabInstance: \{fileID: (\d+)/)?.[1];if(!instance||instance==='0')instance=find(ds,owner)?.raw.match(/m_PrefabInstance: \{fileID: (\d+)/)?.[1];const result=ds.find(d=>d.type===224&&d.raw.includes('m_PrefabInstance: {fileID: '+instance+'}'));if(!result)throw Error('No component rect '+c+' owner '+owner+' instance '+instance);return result;}
function subtree(ds,root){const selected=new Set([root]);let changed=true;while(changed){changed=false;for(const d of ds){if(!d.id||selected.has(d.id))continue;const parent=d.raw.match(/m_Father: \{fileID: (\d+)/)?.[1],owner=d.raw.match(/m_GameObject: \{fileID: (\d+)/)?.[1],instance=d.raw.match(/m_PrefabInstance: \{fileID: (\d+)/)?.[1],transformParent=d.raw.match(/m_TransformParent: \{fileID: (\d+)/)?.[1];
 if((parent&&selected.has(parent))||(owner&&selected.has(owner))||(instance&&selected.has(instance))||(transformParent&&selected.has(transformParent))){selected.add(d.id);changed=true;}
 if(d.type===1&&selected.has(d.id)){for(const m of d.raw.matchAll(/component: \{fileID: (\d+)/g))selected.add(m[1]);}
 }for(const d of ds.filter(x=>x.type===224&&selected.has(x.id))){const owner=d.raw.match(/m_GameObject: \{fileID: (\d+)/)?.[1];if(owner&&!selected.has(owner)){selected.add(owner);changed=true;}}}return ds.filter(d=>selected.has(d.id));}
function clone(ds,selection){const map=new Map(selection.filter(d=>d.id).map(d=>[d.id,id()]));const copies=selection.map(d=>{let raw=d.raw.replace(/^--- !u!(\d+) &(\d+)/,(_,t,k)=>`--- !u!${t} &${map.get(k)}`);raw=raw.replace(/\{fileID: (\d+)\}/g,(all,k)=>map.has(k)?'{fileID: '+map.get(k)+'}':all);return{...d,id:map.get(d.id),raw};});ds.push(...copies);return{map,copies};}
function reparent(ds,rect,parent){const old=rect.raw.match(/m_Father: \{fileID: (\d+)/)?.[1];if(old&&find(ds,old)){const r=find(ds,old);r.raw=r.raw.replace('  - {fileID: '+rect.id+'}\n','');}set(rect,'m_Father','{fileID: '+parent+'}');child(find(ds,parent),rect.id);}
function grid(ds,rect){const g=ds.find(d=>d.type===114&&d.raw.includes('m_GameObject: {fileID: '+rect.raw.match(/m_GameObject: \{fileID: (\d+)/)[1]+'}')&&d.raw.includes('  m_CellSize:'));if(g){set(g,'m_CellSize','{x: 88, y: 88}');set(g,'m_Spacing','{x: 6, y: 6}');set(g,'m_ConstraintCount','6');}}
function rootSize(ds,w=900,h=760){const r=ds.find(d=>d.type===224&&/m_Father: \{fileID: 0\}/.test(d.raw));set(r,'m_SizeDelta',`{x: ${w}, y: ${h}}`);}
function equipment(ds,refs){const body=rectName(ds,'Body'),g=ds.find(d=>d.type===114&&d.raw.includes('m_GameObject: {fileID: '+body.raw.match(/m_GameObject: \{fileID: (\d+)/)[1]+'}')&&d.raw.includes('m_CellSize'));if(g)set(g,'m_Enabled','0');refs.forEach((ref,i)=>{const r=componentRect(ds,ref);box(ds,r,i<4?9+i*62:18+(i-4)*80,i<4?8:108,100,150,i<4?.56:.68);});}
function passive(ds,root,refs){box(ds,root,24,84,260,272);const title=rectName(ds,'PassiveTitle');top(ds,title,28);const row=ds.find(d=>d.type===224&&d.raw.includes('m_Father: {fileID: '+root.id+'}')&&d.id!==title.id);fill(ds,row,0,36);const g=ds.find(d=>d.type===114&&d.raw.includes('m_GameObject: {fileID: '+row.raw.match(/m_GameObject: \{fileID: (\d+)/)[1]+'}')&&d.raw.includes('m_ChildControlWidth'));if(g)set(g,'m_Enabled','0');refs.forEach((ref,i)=>box(ds,componentRect(ds,ref),i<3?8+i*84:50+(i-3)*84,4+Math.floor(i/3)*112,100,150,.72));}
function common(ds){rootSize(ds);box(ds,rectName(ds,'EquipmentSection'),24,368,260,260);box(ds,rectName(ds,'PocketSection'),300,84,576,132);box(ds,rectName(ds,'InventorySection'),300,228,576,508);top(ds,rectName(ds,'HeaderSection'),54);const h=rectName(ds,'HeaderSection');fields(ds,h,{m_AnchoredPosition:'{x: 0, y: -18}',m_SizeDelta:'{x: -48, y: 54}'});
 for(const name of ['PocketSection','InventorySection','EquipmentSection']){const section=rectName(ds,name),header=ds.find(d=>d.type===224&&d.raw.includes('m_Father: {fileID: '+section.id+'}')&&ds.some(g=>g.type===1&&g.raw.includes('  m_Name: Header\n')&&d.raw.includes('m_GameObject: {fileID: '+g.id+'}')));top(ds,header,28);}
 const body=rectName(ds,'Body');fill(ds,body,0,28);grid(ds,rectName(ds,'InventoryGridContent'));grid(ds,rectName(ds,'PocketGrid'));
 const scroll=rectName(ds,'InventoryScrollView');fields(ds,scroll,{m_AnchorMin:'{x: 0, y: 0}',m_AnchorMax:'{x: 1, y: 1}',m_Pivot:'{x: 0.5, y: 0.5}',m_AnchoredPosition:'{x: 0, y: -14}',m_SizeDelta:'{x: -16, y: -44}'});
}
// Player: add a scrollable authored pocket viewport using the existing inventory scroll visuals.
const pocket=rectName(P,'PocketSection'),pocketGrid=rectName(P,'PocketGrid');
if(!go(P,'PocketScrollView')){
 const scroll=rectName(P,'InventoryScrollView'),viewport=rectName(P,'Viewport');const selected=subtree(P,scroll.id).filter(d=>!subtree(P,rectName(P,'InventoryGridContent').id).includes(d));
 const {map,copies}=clone(P,selected);const sr=find(P,map.get(scroll.id)),vp=find(P,map.get(viewport.id));set(go(copies,'InventoryScrollView'),'m_Name','PocketScrollView');
 reparent(P,sr,pocket.id);set(vp,'m_Children','[]');reparent(P,pocketGrid,vp.id);
 const scrollComp=copies.find(d=>d.type===114&&d.raw.includes('m_Content:'));set(scrollComp,'m_Content','{fileID: '+pocketGrid.id+'}');
 const fit=P.find(d=>d.type===114&&d.raw.includes('m_HorizontalFit:')&&d.raw.includes('m_GameObject: {fileID: '+go(P,'InventoryGridContent').id+'}'));
 const cl=clone(P,[fit]);set(cl.copies[0],'m_GameObject','{fileID: '+go(P,'PocketGrid').id+'}');const pg=go(P,'PocketGrid');pg.raw=pg.raw.replace('  m_Component:\n','  m_Component:\n  - component: {fileID: '+cl.copies[0].id+'}\n');
}
const pg=rectName(P,'PocketGrid');fields(P,pg,{m_AnchorMin:'{x: 0, y: 1}',m_AnchorMax:'{x: 1, y: 1}',m_Pivot:'{x: 0.5, y: 1}',m_AnchoredPosition:'{x: 0, y: 0}',m_SizeDelta:'{x: 0, y: 88}'});
common(P);fields(P,rectName(P,'PocketScrollView'),{m_AnchorMin:'{x: 0, y: 0}',m_AnchorMax:'{x: 1, y: 1}',m_Pivot:'{x: 0.5, y: 0.5}',m_AnchoredPosition:'{x: 0, y: -14}',m_SizeDelta:'{x: -16, y: -44}'});
const pUI=P.find(d=>d.type===114&&d.raw.includes('guid: e3c9490afc0c49f0b7be6cf2d5453e18'));
const refs=(ui,key)=>[...ui.raw.match(new RegExp('  '+key+':\\n((?:  - \\{fileID: \\d+\\}\\n)+)'))[1].matchAll(/fileID: (\d+)/g)].map(m=>m[1]);
const pPass=refs(pUI,'_passiveSlots');passive(P,rectFor(P,go(P,'PassiveSection').id),pPass);
const armor=pUI.raw.match(/_armorSlot: \{fileID: (\d+)/)[1],head=pUI.raw.match(/_headSlot: \{fileID: (\d+)/)[1];equipment(P,[armor,head,...refs(pUI,'_weaponSlots'),...refs(pUI,'_ammoSlots')]);
// Enemy: same authored passive, pocket and head widgets, retaining every existing equipment/inventory binding.
const enemyRoot=E.find(d=>d.type===224&&d.raw.includes('m_Father: {fileID: 0}'));
if(!go(E,'PassiveSection')){
 const pr=rectFor(P,go(P,'PassiveSection').id),cl=clone(E,subtree(P,pr.id));const cr=find(E,cl.map.get(pr.id));set(cr,'m_Father','{fileID: '+enemyRoot.id+'}');child(enemyRoot,cr.id);
 for(const d of cl.copies.filter(d=>d.type===114&&d.raw.includes('guid: '+fs.readFileSync('Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/PassiveInventorySlotUI.cs.meta','utf8').match(/^guid: (\w+)/m)[1])))if(d.raw.includes('  m_Enabled:'))set(d,'m_Enabled','0');
}
if(!go(E,'PocketSection')){
 const pr=rectName(P,'PocketSection'),cl=clone(E,subtree(P,pr.id)),cr=find(E,cl.map.get(pr.id));const area=rectName(E,'InventoryArea');set(cr,'m_Father','{fileID: '+area.id+'}');child(area,cr.id);
 for(const d of cl.copies.filter(d=>d.type===1&&/m_Name: Sort(Compact|Rarity)/.test(d.raw)))set(d,'m_IsActive','0');
 for(const d of cl.copies)d.raw=d.raw.replaceAll('{fileID: '+pUI.id+'}','{fileID: 0}'); // Hidden player-only sort actions have no enemy owner.
}
const eUI=E.find(d=>d.type===114&&d.raw.includes('  _equipmentSectionRoot:'));
if(!eUI.raw.includes('  _headSlot:')){
 const component=find(P,head),r=componentRect(P,head),inst=find(P,r.raw.match(/m_PrefabInstance: \{fileID: (\d+)/)[1]);
 const cl=clone(E,P.filter(d=>d.id===inst.id||d.raw.includes('m_PrefabInstance: {fileID: '+inst.id+'}')||d.id===head));const i=find(E,cl.map.get(inst.id));set(i,'m_TransformParent','{fileID: '+rectName(E,'Body').id+'}');child(rectName(E,'Body'),cl.map.get(r.id));
 const hc=find(E,cl.map.get(head));set(hc,'m_Enabled','0');eUI.raw=eUI.raw.replace('  _armorSlot:', '  _headSlot: {fileID: '+hc.id+'}\n  _armorSlot:');
}
common(E);
const ePass=E.filter(d=>d.type===114&&d.raw.includes('guid: '+fs.readFileSync('Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/PassiveInventorySlotUI.cs.meta','utf8').match(/^guid: (\w+)/m)[1])).map(d=>d.id);
passive(E,rectFor(E,go(E,'PassiveSection').id),ePass);
equipment(E,[eUI.raw.match(/_armorSlot: \{fileID: (\d+)/)[1],eUI.raw.match(/_headSlot: \{fileID: (\d+)/)[1],...refs(eUI,'_weaponSlots'),...refs(eUI,'_ammoSlots')]);
const ePocketScroll=rectName(E,'PocketScrollView');fields(E,ePocketScroll,{m_AnchorMin:'{x: 0, y: 0}',m_AnchorMax:'{x: 1, y: 1}',m_Pivot:'{x: 0.5, y: 0.5}',m_AnchoredPosition:'{x: 0, y: -14}',m_SizeDelta:'{x: -16, y: -44}'});
// Pure patch generation. Full existing document IDs are kept; only modified docs + new authored docs are emitted.
function patch(path,updated){const before=parse(fs.readFileSync(path,'utf8'));const by=new Map(updated.filter(d=>d.id).map(d=>[d.id,d.raw]));const oldIds=new Set(before.map(d=>d.id));const added=updated.filter(d=>d.id&&!oldIds.has(d.id)).map(d=>d.raw).join('');let result='*** Begin Patch\n*** Update File: '+path+'\n';const existing=before.filter(d=>d.id);const last=existing[existing.length-1].id;for(const d of existing){let raw=by.get(d.id)??d.raw;if(d.id===last&&added)raw=raw.trimEnd()+'\n'+added;if(raw===d.raw)continue;result+='@@\n'+d.raw.trimEnd().split('\n').map(l=>'-'+l).join('\n')+'\n'+raw.trimEnd().split('\n').map(l=>'+'+l).join('\n')+'\n';}return result+'*** End Patch';}
if(process.argv[2]==='player')process.stdout.write(patch(paths.player,P));else if(process.argv[2]==='enemy')process.stdout.write(patch(paths.enemy,E));else if(process.argv[2]==='summary')console.log(JSON.stringify({playerDocs:P.length,enemyDocs:E.length,enemyPassives:ePass.length}));
else if(process.argv[2]==='tidy-player'||process.argv[2]==='tidy-enemy'){
 const player=process.argv[2]==='tidy-player',docs=player?P:E;
 for(const d of docs)if(d.id?.startsWith('930280'))d.raw=d.raw.replace(/[ \t]+$/gm,'');
 process.stdout.write(patch(player?paths.player:paths.enemy,docs));
}
else if(process.argv[2]==='canvas'){
 const text=fs.readFileSync(paths.canvas,'utf8').replace(/\r/g,'');let result='*** Begin Patch\n*** Update File: '+paths.canvas+'\n';
 for(const m of text.matchAll(/    - target: \{fileID: (\d+), guid: (e9d4acb5f26701046a8e0d65a1a5cc56|daef46e15391f1144807f6c2b9306945), type: 3\}\n      propertyPath: ([^\n]+)\n      value:([^\n]*)\n      objectReference: [^\n]*\n/g)){
  let updated=m[0];const field=m[3],root=m[1]==='2160544705564579186';
  if(root){const values={'m_SizeDelta.x':900,'m_SizeDelta.y':760,'m_AnchoredPosition.x':m[2].startsWith('e9')?-470:470,'m_AnchoredPosition.y':0};if(field in values)updated=updated.replace(/      value:[^\n]*/,'      value: '+values[field]);}
  else if(/^m_(Anchor|Anchored|SizeDelta|Pivot|LocalPosition|LocalScale|LocalRotation|LocalEuler|CellSize|Spacing|Constraint|ChildControl|ChildForce)/.test(field))updated='';
  if(updated!==m[0])result+='@@\n'+m[0].trimEnd().split('\n').map(l=>'-'+l).join('\n')+'\n'+(updated?updated.trimEnd().split('\n').map(l=>'+'+l).join('\n')+'\n':'');
 }
 process.stdout.write(result+'*** End Patch');
}
