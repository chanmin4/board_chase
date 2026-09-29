// Read-only project audit. Reports are written only beneath Tools/Audit20260922/output.
const fs = require('fs'), path = require('path'), crypto = require('crypto');
const root = process.cwd(), out = path.join(root, 'Tools/Audit20260922/output');
fs.mkdirSync(out, {recursive:true});
const walk = p => fs.readdirSync(p, {withFileTypes:true}).flatMap(e => e.isDirectory() ? walk(path.join(p,e.name)) : [path.join(p,e.name)]);
const rel = p => path.relative(root,p).replaceAll('\\','/');
const assets = walk(path.join(root,'Assets'));
const metas = assets.filter(p=>p.endsWith('.meta'));
const guidMap = new Map(), invalidMetas=[], duplicates=[];
for(const p of metas){const s=fs.readFileSync(p,'utf8'), m=s.match(/^guid: (\S+)/m);if(!m||!(/^[a-f0-9]{32}$/i.test(m[1])))invalidMetas.push(rel(p));else { if(guidMap.has(m[1]))duplicates.push([m[1],guidMap.get(m[1]),rel(p.slice(0,-5))]);guidMap.set(m[1],rel(p.slice(0,-5)));}}
const packageRoots = ['Packages','Library/PackageCache'].filter(p=>fs.existsSync(p));
for(const base of packageRoots)for(const p of walk(base).filter(p=>p.endsWith('.meta'))){const s=fs.readFileSync(p,'utf8'),m=s.match(/^guid: (\S+)/m);if(m&&!guidMap.has(m[1]))guidMap.set(m[1],rel(p.slice(0,-5)));}
const serialized = assets.filter(p=>/\.(prefab|unity|asset|mat|controller|overrideController|playable|renderTexture|lighting|shadervariants)$/.test(p));
const refs = new Map(), scriptRefs = new Map(), missing = [], yamlFiles=[];
for(const p of serialized){const s=fs.readFileSync(p,'utf8'); if(!s.startsWith('%YAML'))continue;const r=rel(p);yamlFiles.push(r);const unique = [...new Set([...s.matchAll(/\bguid: ([a-f\d]{32})\b/gi)].map(x=>x[1]))];refs.set(r,unique.map(g=>guidMap.get(g)).filter(Boolean));for(const g of unique)if(!guidMap.has(g)&&!/^0{16}/.test(g))missing.push({path:r,guid:g});for(const m of s.matchAll(/m_Script: \{fileID: [^,]+, guid: ([a-f\d]{32})/gi)){if(!scriptRefs.has(m[1]))scriptRefs.set(m[1],new Set());scriptRefs.get(m[1]).add(r);}}
const scenes=assets.filter(p=>p.endsWith('.unity')).map(rel);
const firstParty=assets.filter(p=>p.endsWith('.cs')&&/^Assets\/(Scripts|Editor|Tests)\//.test(rel(p)));
const scriptTexts=firstParty.map(p=>[rel(p),fs.readFileSync(p,'utf8')]);
const scriptInfo = scriptTexts.map(([p,s])=>{
 const meta=p+'.meta';const guid=fs.existsSync(meta)?fs.readFileSync(meta,'utf8').match(/^guid: (\S+)/m)?.[1]:null;
 const types=[...s.matchAll(/\b(class|struct|interface|enum)\s+(\w+)(?:\s*:\s*([^\r\n{]+))?/g)].map(m=>({kind:m[1],name:m[2],base:m[3]?.trim()||''}));
 const methods=[...s.matchAll(/^\s*(?:public|protected|private|internal)\s+(?:(?:static|virtual|override|abstract|async|sealed|new)\s+)*(?:[\w<>,?.\[\]]+\s+)(\w+)\s*\([^;{}]*\)/gm)].map(m=>m[1]);
 const typesUsed=[...new Set(scriptTexts.filter(([q,t])=>q!==p&&types.some(y=>new RegExp('\\b'+y.name+'\\b').test(t))).map(([q])=>q))];
 return {path:p,lines:s.split('\n').length,sha256:crypto.createHash('sha256').update(s).digest('hex'),guid,types,methods:[...new Set(methods)],serializedAssets:[...(scriptRefs.get(guid)||[])],referencingScripts:typesUsed,wip:/\/WIP\/|\/Legacy/.test(p),editor:/\/Editor\//.test(p),tests:/\/Tests\//.test(p),todoLines:s.split('\n').flatMap((l,i)=>/\bTODO\b|\bFIXME\b|NotImplementedException/.test(l)?[`${i+1}: ${l.trim()}`]:[])};
});
const activeRoots=['Assets/Scenes/Manager/Initialization.unity','Assets/Scenes/Manager/PersistenceManager.unity','Assets/Scenes/Manager/GamePlay.unity','Assets/Scenes/MainMenu.unity','Assets/Scenes/HomeScene.unity','Assets/Scenes/RUNScene.unity','Assets/Scenes/GameOver.unity'];
const reached=new Set(), queue=[...activeRoots];while(queue.length){const x=queue.pop();if(reached.has(x))continue;reached.add(x);for(const y of refs.get(x)||[])queue.push(y);}
for(const s of scriptInfo)s.sceneReachableAssets=s.serializedAssets.filter(p=>reached.has(p));
const summary={date:'2026-09-22',root,totalAssetFiles:assets.length,metaCount:metas.length,csAll:assets.filter(p=>p.endsWith('.cs')).length,firstPartyScripts:scriptInfo.length,firstPartyLines:scriptInfo.reduce((n,s)=>n+s.lines,0),scenes,serializedYamlCount:yamlFiles.length,invalidMetas,duplicateGuids:duplicates,missingGuidReferenceCount:missing.length,activeSceneReachableMissing:missing.filter(x=>reached.has(x.path)),modules:Object.fromEntries([...new Set(scriptInfo.map(x=>x.path.split('/').slice(0,4).join('/')))].map(m=>[m,scriptInfo.filter(x=>x.path.startsWith(m+'/')).length]))};
for(const [name,data] of Object.entries({summary,scripts:scriptInfo,missing,refs:Object.fromEntries(refs),guids:Object.fromEntries(guidMap)}))fs.writeFileSync(path.join(out,name+'.json'),JSON.stringify(data,null,2));
for(const module of ['Core','Gameplay','Presentation','Authoring','StateMachine','Other']){
 const list=scriptInfo.filter(s=>module==='Other'?!['Core','Gameplay','Presentation','Authoring','StateMachine'].includes(s.path.split('/')[2]):s.path.startsWith('Assets/Scripts/'+module+'/'));
 const md=['# '+module+' 스크립트 전수 정적 인덱스','검사일: 2026-09-22. 함수/타입/직렬화 연결은 정적 추출이며 실행 검증을 뜻하지 않습니다. 동적 생성, 리플렉션 및 Addressables 간접 참조는 별도 시스템 문서와 함께 확인하세요.'];
 for(const s of list)md.push('\n## '+s.path+'\n- 분류: '+(s.tests?'테스트':s.wip?'WIP/Legacy 경로':s.editor?'에디터':'일반 소스')+'; '+s.lines+'행\n- 타입: '+s.types.map(t=>t.name+(t.base?' : '+t.base:'')).join(', ')+'\n- 메서드: '+s.methods.join(', ')+'\n- 직렬화 참조 '+s.serializedAssets.length+'개 / 지정 씬 참조 그래프 내 '+s.sceneReachableAssets.length+'개: '+s.serializedAssets.slice(0,8).join('; ')+'\n- 타입 이름을 참조하는 소스 '+s.referencingScripts.length+'개: '+s.referencingScripts.slice(0,8).join('; '));
 fs.writeFileSync(path.join(out,'scripts-'+module+'.md'),md.join('\n'));
}
console.log(JSON.stringify({...summary,activeSceneReachableMissing:summary.activeSceneReachableMissing.slice(0,50)},null,2));
