// Emits an apply_patch document only. Never writes or moves Unity assets directly.
const fs = require('fs'), crypto = require('crypto');
const guid = () => crypto.randomBytes(16).toString('hex');
const script = 'Assets/Scripts/Gameplay/World/Sectors/Portal/SectorPortalActivator.cs';
const faction = 'Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/ShooterFactionUtility.cs';
const prefab = 'Assets/Prefabs/GameScene/Sector/SectorObject/InteractableObject/PortalActivationDevice.prefab';
if (fs.existsSync(prefab)) throw Error('Already authored; refusing to replace existing assets.');
const sg = guid(), pg = guid();
const source = fs.readFileSync('Assets/Prefabs/GameScene/Sector/SectorObject/SM_Props_12.prefab', 'utf8');
const common = '  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n';
const go = (id, name, components) => `--- !u!1 &${id}\nGameObject:\n${common}  serializedVersion: 6\n  m_Component:\n` + components.map(n => `  - component: {fileID: ${n}}\n`).join('') + `  m_Layer: 8\n  m_Name: ${name}\n  m_TagString: Untagged\n  m_Icon: {fileID: 0}\n  m_NavMeshLayer: 0\n  m_StaticEditorFlags: 0\n  m_IsActive: 1\n`;
const tf = (id, owner, parent, children, scale) => `--- !u!4 &${id}\nTransform:\n${common}  m_GameObject: {fileID: ${owner}}\n  serializedVersion: 2\n  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}\n  m_LocalPosition: {x: 0, y: 0, z: 0}\n  m_LocalScale: {x: ${scale}, y: ${scale}, z: ${scale}}\n  m_ConstrainProportionsScale: 0\n  m_Children:` + (children.length ? '\n' + children.map(n => `  - {fileID: ${n}}\n`).join('') : ' []\n') + `  m_Father: {fileID: ${parent}}\n  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}\n`;
let content = '%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n' + go(100000, 'PortalActivationDevice', [400000, 11400000, 13500000]) + tf(400000, 100000, 0, [400001], 1);
content += `--- !u!114 &11400000\nMonoBehaviour:\n${common}  m_GameObject: {fileID: 100000}\n  m_Enabled: 1\n  m_EditorHideFlags: 0\n  m_Script: {fileID: 11500000, guid: ${sg}, type: 3}\n  m_Name: \n  m_EditorClassIdentifier: \n  _visual: {fileID: 400001}\n`;
content += `--- !u!135 &13500000\nSphereCollider:\n${common}  m_GameObject: {fileID: 100000}\n  m_Material: {fileID: 0}\n  m_IncludeLayers:\n    serializedVersion: 2\n    m_Bits: 0\n  m_ExcludeLayers:\n    serializedVersion: 2\n    m_Bits: 0\n  m_LayerOverridePriority: 0\n  m_IsTrigger: 1\n  m_ProvidesContacts: 0\n  m_Enabled: 1\n  serializedVersion: 3\n  m_Radius: 2.5\n  m_Center: {x: 0, y: 1, z: 0}\n`;
content += go(100001, 'Visual', [400001]) + tf(400001, 100001, 400000, [400002], 1);
content += go(100002, 'TemporaryPanel_ReplaceThis', [400002, 3300000, 2300000]) + tf(400002, 100002, 400001, [], 3);
for (const [oldId, newId] of [['6681501263971065806', '3300000'], ['8889404958621920004', '2300000']]) {
    let section = source.split(/(?=--- !u!)/).find(x => x.includes('&' + oldId + '\r\n') || x.includes('&' + oldId + '\n'));
    if (!section) throw Error(oldId);
    content += section.replaceAll(oldId, newId).replaceAll('6943845589376223953', '100002').replace(/\r/g, '');
}
const scriptMeta = g => `fileFormatVersion: 2\nguid: ${g}\nMonoImporter:\n  externalObjects: {}\n  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n  icon: {instanceID: 0}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n`;
const prefabMeta = `fileFormatVersion: 2\nguid: ${pg}\nPrefabImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n`;
let patch = '*** Begin Patch\n';
for (const [f, c] of [[script + '.meta', scriptMeta(sg)], [faction + '.meta', scriptMeta(guid())], [prefab, content], [prefab + '.meta', prefabMeta]]) {
    if (fs.existsSync(f)) throw Error('Existing: ' + f);
    patch += `*** Add File: ${f}\n` + c.trimEnd().split('\n').map(l => '+' + l).join('\n') + '\n';
}
const nested = `--- !u!1001 &9110400000000000001\nPrefabInstance:\n  m_ObjectHideFlags: 0\n  serializedVersion: 2\n  m_Modification:\n    serializedVersion: 3\n    m_TransformParent: {fileID: 7518838868959679074}\n    m_Modifications:\n` + ['x', 'y', 'z'].map((axis, i) => `    - target: {fileID: 400000, guid: ${pg}, type: 3}\n      propertyPath: m_LocalPosition.${axis}\n      value: ${[96, 0, 88][i]}\n      objectReference: {fileID: 0}\n`).join('') + `    m_RemovedComponents: []\n    m_RemovedGameObjects: []\n    m_AddedGameObjects: []\n    m_AddedComponents: []\n  m_SourcePrefab: {fileID: 100100000, guid: ${pg}, type: 3}\n--- !u!4 &9110400000000000002 stripped\nTransform:\n  m_CorrespondingSourceObject: {fileID: 400000, guid: ${pg}, type: 3}\n  m_PrefabInstance: {fileID: 9110400000000000001}\n  m_PrefabAsset: {fileID: 0}\n`;
patch += '*** Update File: Assets/Prefabs/GameScene/Sector/Bases/Sector_Base.prefab\n@@\n %TAG !u! tag:unity3d.com,2011:\n' + nested.trimEnd().split('\n').map(l => '+' + l).join('\n') + '\n';
patch += '@@\n   - {fileID: 7837968522783440049}\n+  - {fileID: 9110400000000000002}\n   m_Father: {fileID: 0}\n*** End Patch';
process.stdout.write(patch);
