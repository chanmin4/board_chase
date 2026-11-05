const fs=require('fs'),assert=require('assert');let checks=0;
const check=(v,m)=>{checks++;assert(v,m);};
const load=p=>fs.readFileSync(p,'utf8').replace(/\r/g,'');
const root='Assets/Prefabs/UI/GamePlay/PanelHub/Inventory/';
const parse=s=>s.split(/(?=^--- !u!)/m).filter(x=>/^--- !u!/.test(x)).map(raw=>{const m=raw.match(/^--- !u!(\d+) &(\d+)/);return{raw,id:m[2],type:+m[1]};});
for(const name of ['PlayerInventoryPanel','EnemyInventoryPanel']){
 const text=load(root+name+'.prefab'),ds=parse(text),ids=new Set(ds.map(d=>d.id));
 check(ids.size===ds.length,name+' duplicate local IDs');
 for(const d of ds)for(const m of d.raw.matchAll(/\{fileID: (\d+)\}/g))check(m[1]==='0'||ids.has(m[1]),name+' dangling local ref '+m[1]+' in '+d.id);
 const go=n=>ds.find(d=>d.type===1&&d.raw.includes('  m_Name: '+n+'\n'));
 const rect=n=>ds.find(d=>d.type===224&&d.raw.includes('m_GameObject: {fileID: '+go(n)?.id+'}'));
 for(const n of ['EquipmentSection','PocketSection','InventorySection','PocketScrollView','PocketGrid','PassiveSection'])check(!!go(n),name+' missing '+n);
 for(const n of ['InventoryGridContent','PocketGrid']){
  const g=ds.find(d=>d.type===114&&d.raw.includes('m_GameObject: {fileID: '+go(n).id+'}')&&d.raw.includes('m_CellSize'));
  check(g.raw.includes('m_CellSize: {x: 88, y: 88}')&&g.raw.includes('m_ConstraintCount: 6'),name+' grid not 6x88 '+n);
 }
 check(rect('EquipmentSection').raw.includes('m_AnchoredPosition: {x: 24, y: -368}'),name+' equipment origin');
 check(rect('EquipmentSection').raw.includes('m_SizeDelta: {x: 260, y: 260}'),name+' compact equipment section');
 check(rect('PassiveSection').raw.includes('m_SizeDelta: {x: 260, y: 272}'),name+' enlarged passive section');
 const ui=ds.find(d=>d.raw.includes('  _armorSlot:')&&d.raw.includes('  _weaponSlots:'));
 const single=k=>ui.raw.match(new RegExp('  '+k+': \\{fileID: (\\d+)'))[1];
 const array=k=>[...ui.raw.match(new RegExp('  '+k+':\\n((?:  - \\{fileID: \\d+\\}\\n)+)'))[1].matchAll(/fileID: (\d+)/g)].map(m=>m[1]);
 const componentRect=id=>{const c=ds.find(d=>d.id===id),owner=c.raw.match(/m_GameObject: \{fileID: (\d+)/)?.[1];const r=ds.find(d=>d.type===224&&owner!=='0'&&d.raw.includes('m_GameObject: {fileID: '+owner+'}'));if(r)return r;let inst=c.raw.match(/m_PrefabInstance: \{fileID: (\d+)/)?.[1];if(!inst||inst==='0')inst=ds.find(d=>d.id===owner)?.raw.match(/m_PrefabInstance: \{fileID: (\d+)/)?.[1];return ds.find(d=>d.type===224&&d.raw.includes('m_PrefabInstance: {fileID: '+inst+'}'));};
 const value=(r,k)=>{if(!r.raw.includes(' stripped\n')){const [field,axis]=k.split('.');return +r.raw.match(new RegExp('  '+field+': \\{[^}]*?'+axis+': ([^,}]+)'))[1];}const inst=ds.find(d=>d.id===r.raw.match(/m_PrefabInstance: \{fileID: (\d+)/)[1]),target=r.raw.match(/m_CorrespondingSourceObject: \{fileID: (\d+)/)[1];const entry=inst.raw.split('    - target: ').find(s=>s.startsWith('{fileID: '+target+',')&&s.includes('propertyPath: '+k+'\n'));check(!!entry,name+' missing explicit geometry '+k);return +entry.match(/      value: ([^\n]+)/)[1];};
 const refs=[single('_armorSlot'),single('_headSlot'),...array('_weaponSlots'),...array('_ammoSlots')];
 check(refs.length===7,name+' equipment bindings count');
 refs.forEach((id,i)=>{const r=componentRect(id),x=i<4?9+i*62:18+(i-4)*80,y=i<4?-8:-108,scale=i<4?.56:.68;check(value(r,'m_AnchoredPosition.x')===x&&value(r,'m_AnchoredPosition.y')===y,name+' 4+3 slot position '+i);check(value(r,'m_LocalScale.x')===scale,name+' compact equipment scale '+i);check(x+100*scale<=260&&-y+150*scale<=232,name+' equipment fits '+i);});
 const passiveGuid=load('Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/PassiveInventorySlotUI.cs.meta').match(/^guid: (\w+)/m)[1];
 const pass=array(name==='PlayerInventoryPanel'?'_passiveSlots':'_passiveInspectSlots').map(id=>ds.find(d=>d.id===id));
 check(pass.length===8,name+' combat/survival slot count');
 pass.forEach((c,i)=>{const r=componentRect(c.id);check(value(r,'m_LocalScale.x')===.58,name+' passive scale '+i);check(value(r,'m_AnchoredPosition.y')===-(8+(i%2)*112),name+' category row '+i);check(value(r,'m_AnchoredPosition.x')===4+Math.floor(i/2)*63,name+' category column '+i);});
 check(rect('PocketSection').raw.includes('m_AnchoredPosition: {x: 300, y: -84}'),name+' pocket not right');
 check(rect('InventorySection').raw.includes('m_AnchoredPosition: {x: 300, y: -228}'),name+' inventory not right');
 check(rect('InventorySection').raw.includes('m_SizeDelta: {x: 576, y: 508}'),name+' five row viewport');
 check(508-28-16===5*88+4*6,'Five rows geometry');
 check(6*88+5*6<=576-16,'Six columns fit');
 const scroll=ds.find(d=>d.type===114&&d.raw.includes('m_GameObject: {fileID: '+go('PocketScrollView').id+'}')&&d.raw.includes('m_Content:'));
 check(scroll&&scroll.raw.includes('m_Content: {fileID: '+rect('PocketGrid').id+'}'),name+' pocket scroll content binding');
 for(const d of ds.filter(d=>d.type===224&&!d.raw.includes(' stripped\n'))){
  const block=d.raw.match(/  m_Children:\n((?:  -[^\n]*\n)*)/);if(!block)continue;
  for(const m of block[1].matchAll(/fileID: (\d+)/g)){
   const c=ds.find(x=>x.id===m[1]);check(!!c,name+' missing child rect');
   if(c.raw.includes(' stripped\n')){
    const instance=c.raw.match(/m_PrefabInstance: \{fileID: (\d+)/)?.[1],inst=ds.find(x=>x.id===instance);
    check(inst?.raw.includes('m_TransformParent: {fileID: '+d.id+'}'),name+' nested prefab parent mismatch '+c.id);
   }else check(c.raw.includes('m_Father: {fileID: '+d.id+'}'),name+' parent/child mismatch '+c.id);
  }
 }
}
const enemy=load('Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/EnemyInventoryUI.cs');
check(enemy.includes('ItemSlotIndexBase + slotIndex'),'Enemy source indices changed');
check(enemy.includes('i<pocketCount?_enemyPocketGrid:_inventorySlotRoot'),'Enemy pocket not reparented by source index');
const passives=load('Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/EnemyInventoryUI.Passives.cs');
check(!passives.includes('Instantiate(')&&!passives.includes('new GameObject'),'Enemy passive visuals still generated at runtime');
check(passives.includes('view.enabled=false'),'Enemy passive click/drag handlers enabled');
console.log('PASS '+checks+' split-layout prefab/reference/geometry/source checks. Not Unity native rendering.');
