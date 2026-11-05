// Read-only source-model audit. Writes diagnostics to Library, never changes model/importer assets.
const fs = require('fs'), zlib = require('zlib');
const dir = 'Assets/Prefabs/GameScene/Shooter/Accesory';
const modelGuids = new Map(fs.readdirSync('Assets/Art/Mesh').filter(x=>x.endsWith('.fbx.meta')).map(x=>
  [fs.readFileSync('Assets/Art/Mesh/'+x,'utf8').match(/^guid: (\w+)/m)?.[1],x]));
function parse(file) {
  const b = fs.readFileSync(file), version = b.readUInt32LE(23);
  if (version >= 7500) throw Error('Unsupported FBX version ' + version);
  let p = 27;
  function property() {
    const type = String.fromCharCode(b[p++]);
    const scalar = {Y:[2,'readInt16LE'],C:[1,'readUInt8'],I:[4,'readInt32LE'],F:[4,'readFloatLE'],D:[8,'readDoubleLE'],L:[8,'readBigInt64LE']};
    if (scalar[type]) { const [len, fn] = scalar[type], v = b[fn](p); p += len; return typeof v === 'bigint' ? v.toString() : v; }
    if (type === 'S' || type === 'R') { const len = b.readUInt32LE(p); p += 4; const v = b.subarray(p,p+len); p += len; return type === 'S' ? v.toString('utf8') : null; }
    const length = b.readUInt32LE(p), encoding = b.readUInt32LE(p+4), bytes = b.readUInt32LE(p+8); p += 12;
    let data = b.subarray(p,p+bytes); p += bytes; if (encoding) data = zlib.inflateSync(data);
    const sizes = {f:[4,'readFloatLE'],d:[8,'readDoubleLE'],i:[4,'readInt32LE'],l:[8,'readBigInt64LE'],b:[1,'readUInt8'],c:[1,'readUInt8']};
    if (!sizes[type]) throw Error('Unknown property ' + type);
    const [size, fn] = sizes[type]; return Array.from({length},(_,i)=>{const v=data[fn](i*size); return typeof v==='bigint'?v.toString():v;});
  }
  function node() {
    const end = b.readUInt32LE(p), count = b.readUInt32LE(p+4), nameLen = b[p+12]; p += 13;
    if (!end) return null;
    const name = b.toString('utf8',p,p+nameLen); p += nameLen;
    const props = Array.from({length:count},property), children=[];
    while (p < end) { const n = node(); if (n) children.push(n); else break; }
    p = end; return {name,props,children};
  }
  const roots=[]; while (p+13<b.length) { const n=node(); if(!n)break; roots.push(n); }
  return roots;
}
const round = v => Math.round(v*100000)/100000;
const results = fs.readdirSync(dir).filter(f=>f.endsWith('.prefab')).sort().map(f=>{
  const path = dir+'/'+f, text = fs.readFileSync(path,'utf8');
  const ids = [...text.matchAll(/guid: ([a-f0-9]{32})/g)].map(m=>m[1]);
  const model = ids.map(id=>modelGuids.get(id)).find(Boolean);
  if(!model)return {path,warning:'No source FBX found'};
  const file='Assets/Art/Mesh/'+model.slice(0,-5), nodes=parse(file), objects=nodes.find(n=>n.name==='Objects');
  const geometry=objects.children.filter(n=>n.name==='Geometry').map(n=>{
    const vertices=n.children.find(c=>c.name==='Vertices')?.props[0]||[];
    const min=[Infinity,Infinity,Infinity],max=[-Infinity,-Infinity,-Infinity];
    vertices.forEach((v,i)=>{min[i%3]=Math.min(min[i%3],v);max[i%3]=Math.max(max[i%3],v);});
    return {name:n.props[1],min:min.map(round),max:max.map(round),center:min.map((v,i)=>round((v+max[i])/2))};
  });
  const models=objects.children.filter(n=>n.name==='Model').map(n=>({name:n.props[1],poses:n.children.find(c=>c.name==='Properties70')?.children.filter(c=>/Lcl |Geometric/.test(c.props[0])).map(c=>[c.props[0],c.props.slice(4)])}));
  const globals=nodes.find(n=>n.name==='GlobalSettings')?.children.find(n=>n.name==='Properties70')?.children.map(n=>n.props);
  return {path,file,geometry,models,globals};
});
fs.mkdirSync('Library/ShooterCustomizeVerification',{recursive:true});
fs.writeFileSync('Library/ShooterCustomizeVerification/accessory-fbx-audit.json',JSON.stringify(results,null,2));
for(const r of results) console.log(r.path.split('/').pop(),JSON.stringify(r.geometry),JSON.stringify(r.models));
console.log('Audited',results.length,'prefabs. Source geometry coordinates, before Unity FBX axis/unit conversion.');
