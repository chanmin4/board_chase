// Read-only Notion snapshot preparation. No Unity, Notion or local file writes.
const fs=require('fs'),path=require('path');
const source=require('./read-item-maker.cjs');
const read=p=>fs.readFileSync(p,'utf8').replace(/\r/g,'');
const walk=p=>fs.readdirSync(p,{withFileTypes:true}).flatMap(e=>e.isDirectory()?walk(p+'/'+e.name):[p+'/'+e.name]);
const material=new Map([...read('Assets/Resources/ValuableCategories.asset').matchAll(/  - id: (.+)\n    label: (.+)/g)].map(m=>[m[1],m[2]]));
const rarity={0:'Normal',1:'Rare',3:'Unique',2:'Legendary',4:'Mythic'};
const refs=new Map();
const questGuid=read('Assets/Scripts/Gameplay/Progression/Quests/QuestDefinitionSO.cs.meta').match(/^guid: (.+)/m)[1];
let questCount=0;
for(const file of walk('Assets/ScriptableObject/GameScene').filter(p=>p.endsWith('.asset'))){
 const text=read(file);if(!text.includes('guid: '+questGuid+','))continue;questCount++;
 const label=text.match(/^  _displayName: (.*)/m)?.[1]?.trim()||path.basename(file,'.asset');
 const template=file.includes('/ContentTemplates/')?' [템플릿]':'';
 const objectives=text.split('  _objectives:\n')[1]?.split(/^  _\w+:/m)[0]||'';
 for(const block of objectives.split(/(?=^  - _objectiveType:)/m)){
  const type=Number(block.match(/_objectiveType: (\d+)/)?.[1]);
  for(const [field,role] of [['_targetItem',type===3?'제출':type===2?'수집':'목표 아이템'],['_requiredWeapon','요구 무기']]){
   const guid=block.match(new RegExp(field+': \\{fileID: \\d+, guid: (\\w+)'))?.[1];
   if(!guid)continue;
   const quantity=block.match(/_requiredCount: (\d+)/)?.[1];
   const line=`${role}${quantity?' ×'+quantity:''}: ${label}${template} (${path.basename(file,'.asset')})`;
   if(!refs.has(guid))refs.set(guid,[]);refs.get(guid).push(line);
  }
 }
 const rewards=text.split('  _rewards:\n')[1]?.split(/^  _\w+:/m)[0]||'';
 for(const block of rewards.split(/(?=^  - _rewardType:)/m)){
  if(Number(block.match(/_rewardType: (\d+)/)?.[1])!==0)continue;
  const guid=block.match(/_item: \{fileID: \d+, guid: (\w+)/)?.[1];if(!guid)continue;
  const quantity=block.match(/_itemAmount: (\d+)/)?.[1]||1;
  if(!refs.has(guid))refs.set(guid,[]);refs.get(guid).push(`보상 ×${quantity}: ${label}${template} (${path.basename(file,'.asset')})`);
 }
}
const names=new Map();for(const i of source.items){const n=i.name.trim();names.set(n,(names.get(n)||0)+1);}
function summary(i){const a=i.data;
 switch(i.group){
 case '귀중품·재료':return `물품 분류: ${material.get(a._valuableCategoryId)||'미분류'} · 거래/교환/퀘스트 재료용. 실제 공급·거래 조건은 별도 설정.`;
 case '무기':{const c=a._combat||{};return `${i.subgroup} · 탄창 ${a._magazineSize??6} · 초당 ${c.shotsPerSecond??7.8}발 · 재장전 ${c.reloadDurationSeconds??2}초 · 무기 피해 계수 ×${a._damageMultiplier??1}`;}
 case '탄약':return `${i.subgroup} · 탄 등급 ${a._tier??a.tier??1} · 관통 ${a.penetrationClass??1} · HP 피해 ${a._healthDamage??30}${i.type==='ShotgunShellSO'?' · 펠릿 '+(a._pelletCount??8)+'개':''}`;
 case '방어구':return `방탄 ${a._armorClass??1}클래스 · 최대 내구도 ${a._maxDurability??1000}`;
 case '감염방호 장비':return `가스 방어 ${a._gasDefenseLevel??1} · 필터 내구도 ${a._maxFilterDurability??100}`;
 case '가방':return `인벤토리 +${a._inventorySlotCapacity??8}칸 · 이동 배율 ${a._moveSpeedMultiplier??.9}`;
 case '의료품':case '소모품':
  if(i.type==='InfectionCureItemSO')return Number(a._medicineKind??0)===1
   ?`사용 ${a._useDurationSeconds??4}초 · 최대 HP 감염 잠식 ${a._infectionRecoveryAmount??45} 복구 (현재 HP 치료 아님) · 상점 판매 제외`
   :`사용 ${a._useDurationSeconds??4}초 · ${a._infectionRecoveryDurationSeconds??15}초간 감염 유입 ×${a._exposureGainMultiplier??.5}, 회복 대기 후 부분 게이지 회복 총량 ${a._infectionRecoveryAmount??45} · 최대 HP 잠식 복구 불가`;
  return `사용 ${a._useDurationSeconds??4}초 · HP 회복 ${a._healAmount??30} · 출혈 제거 ${a._bleedingStacksRemoved??0}${a._maxDurability>0?' · 사용 내구도 '+a._maxDurability+' / 1회 소모 '+(a._durabilityCostPerUse??1):''}`;
 case '패시브':{
  const category=Number(a._category??0)===1?'생존':'전투';
  if(a._randomStatsEnabled)return `${category} · 개체별 ${[...new Set((a._rarityRolls||[]).map(r=>rarity[r.rarity]))].join('/')} 수치 추첨 · 원정 한정 효과 · 사용 후 해제하면 비활성`;
  const values=(a._statModifiers||[]).map(s=>`${source.statNames[s.stat]||s.stat}: ${s.value} (보정 방식 ${s.type})`).join(' / ');
  return `${category} · ${values||'수치 효과 없음/기능 플래그 별도 확인'} · 공용 효과 배율은 실제 적용 시 추가`;
 }
 case '열쇠·키툴':return i.type==='KeyToolItemSO'?`열쇠 보관 용량 ${a._slotCapacity??4}칸`:`잠금 티어 ${a._tier??0} · 최대 사용 ${a._maxUses??3}회`;
 case '화폐':return '화폐 · 상점별 거래/교환 조건은 별도';
 case '투척물':return '투척형 소모품 · 피해/범위는 연결된 투사체 정의에서 설정';
 default:return i.path.includes('PassiveParts_')?'패시브 모듈 재료 · 공용 분해/추첨 규칙에서 사용 여부 확인':'기타 ItemSO 원본 · 사용처와 콘텐츠 채택 여부 확인';
 }}
const items=source.items.map(i=>{
 const a=i.data,name=i.name.trim(),flags=[];
 if(names.get(name)>1)flags.push('표시명 중복');if(!i.localizedName)flags.push('번역명 없음');if(i.scope==='테스트·WIP')flags.push('테스트·WIP');if(!a._previewImage?.guid)flags.push('아이콘 없음');
 if(i.group==='패시브'&&!a._randomStatsEnabled&&!(a._statModifiers||[]).some(s=>s.value!==0))flags.push('효과 미설정');
 const result={guid:i.guid,runtimeGuid:i.runtimeGuid,path:i.path,name,asset:i.assetName,file:path.basename(i.path,'.asset'),type:i.type,scope:i.scope,group:i.group,
  subgroup:i.group==='귀중품·재료'?material.get(a._valuableCategoryId)||'미분류':i.group==='패시브'?Number(a._category??0)===1?'생존':'전투':i.subgroup,
  material:i.group==='귀중품·재료'?material.get(a._valuableCategoryId)||'미분류':null,rarity:i.rarity,price:i.price,stack:i.maxStack,owned:i.maxOwned,
  usage:i.group==='패시브'?1:i.usageLevel,description:i.description||'',summary:summary(i),flags,
  quest:[...new Set(refs.get(i.guid)||[])].join('\n'),title:names.get(name)>1?name+' · '+path.basename(i.path,'.asset'):name};
 return result;
});
if(new Set(items.map(i=>i.guid)).size!==items.length||items.some(i=>!/^[a-f0-9]{32}$/.test(i.guid)))throw Error('Item GUID coverage failed');
const result={date:'2026-10-29',count:items.length,counts:source.counts,excluded:source.excluded.length,unknown:source.unknown,questDefinitionsScanned:questCount,items};
if(require.main===module)console.log(JSON.stringify(process.argv.includes('--summary')?{...result,items:undefined}:result));
module.exports=result;
