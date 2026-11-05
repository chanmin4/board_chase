#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

[InitializeOnLoad]
public static class MedicalEquipmentImporter
{
 const string Root = "Assets/MedicalEquipment_URP";
 const string Flag = Root + "/Editor/BUILD_ON_IMPORT.flag";
 [Serializable] public class Mat { public string id, albedo, emission; public float[] color, emissionColor; public float metallic, smooth; public bool transparent, doubleSided; }
 [Serializable] public class Part { public string path, material; }
 [Serializable] public class Device { public string name; public Part[] pieces; }
 [Serializable] public class Manifest { public Mat[] materials; public Device[] devices; }
 static bool queued; static int attempts;
 static MedicalEquipmentImporter() { Queue(); }
 static void Queue() { if (queued) return; queued=true; EditorApplication.delayCall += TryAuto; }
 static void TryAuto() {
   queued=false;
   if (!File.Exists(Flag)) return;
   if (EditorApplication.isCompiling || EditorApplication.isUpdating) { if (++attempts<90) Queue(); return; }
   if (Build()) { File.Delete(Flag); AssetDatabase.Refresh(); }
   else if (++attempts<30) Queue();
 }
 [MenuItem("Tools/Medical Equipment/Rebuild Prefabs")]
 public static void Rebuild() { if (Build()) { if(File.Exists(Flag)) File.Delete(Flag); AssetDatabase.Refresh(); } }
 static bool Build() {
  Shader shader=Shader.Find("Universal Render Pipeline/Lit");
  if(shader==null) {Debug.LogError("[Medical Equipment] URP/Lit shader missing.");return false;}
  TextAsset ja=AssetDatabase.LoadAssetAtPath<TextAsset>(Root+"/Documentation/Manifest.json");
  if(ja==null) return false;
  Manifest manifest=JsonUtility.FromJson<Manifest>(ja.text);
  if(manifest==null || manifest.devices==null) return false;
  foreach(var d in manifest.devices) foreach(var p in d.pieces)
    if (AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/"+p.path)==null) return false;
  var materials=new Dictionary<string,Material>();
  foreach(var d in manifest.materials) {
    string path=Root+"/Materials/"+d.id+".mat";
    Material m=AssetDatabase.LoadAssetAtPath<Material>(path);
    if(m==null){m=new Material(shader);AssetDatabase.CreateAsset(m,path);}
    m.shader=shader;
    m.SetColor("_BaseColor",new Color(d.color[0],d.color[1],d.color[2],d.color[3]));
    m.SetFloat("_Metallic",d.metallic);m.SetFloat("_Smoothness",d.smooth);
    m.SetFloat("_Cull",d.doubleSided?0:2);
    m.SetTexture("_BaseMap",string.IsNullOrEmpty(d.albedo)?null:AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/"+d.albedo));
    m.SetTexture("_EmissionMap",string.IsNullOrEmpty(d.emission)?null:AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/"+d.emission));
    bool emit=(!string.IsNullOrEmpty(d.emission) || Mathf.Max(d.emissionColor[0],Mathf.Max(d.emissionColor[1],d.emissionColor[2]))>0);
    if(emit){m.SetColor("_EmissionColor",new Color(d.emissionColor[0],d.emissionColor[1],d.emissionColor[2],1));m.EnableKeyword("_EMISSION");} else {m.SetColor("_EmissionColor",Color.black);m.DisableKeyword("_EMISSION");}
    m.SetFloat("_Surface",d.transparent?1:0);
    if(d.transparent){m.SetOverrideTag("RenderType","Transparent");m.SetInt("_SrcBlend",(int)BlendMode.SrcAlpha);m.SetInt("_DstBlend",(int)BlendMode.OneMinusSrcAlpha);m.SetInt("_ZWrite",0);m.renderQueue=(int)RenderQueue.Transparent;m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");}
    else{m.SetOverrideTag("RenderType","Opaque");m.SetInt("_SrcBlend",(int)BlendMode.One);m.SetInt("_DstBlend",(int)BlendMode.Zero);m.SetInt("_ZWrite",1);m.renderQueue=-1;m.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");}
    materials[d.id]=m; EditorUtility.SetDirty(m);
  }
  try { foreach(var d in manifest.devices) BuildOne(d,materials);AssetDatabase.SaveAssets();Debug.Log("[Medical Equipment] 15 prefabs generated in Assets/MedicalEquipment_URP/Prefabs");return true; }
  catch(Exception e){ Debug.LogError("[Medical Equipment] Failed: "+e);return false; }
 }
 static void BuildOne(Device d,Dictionary<string,Material> materials) {
   var models=new List<GameObject>();var sections=new List<Mesh>();
   try {
    var order=new List<string>();var grouped=new Dictionary<string,List<CombineInstance>>();
    foreach(var p in d.pieces) {
      if(!grouped.ContainsKey(p.material)) {order.Add(p.material);grouped[p.material]=new List<CombineInstance>();}
      GameObject source=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/"+p.path);
      GameObject go=UnityEngine.Object.Instantiate(source);go.hideFlags=HideFlags.HideAndDontSave;models.Add(go);
      foreach(var filter in go.GetComponentsInChildren<MeshFilter>()){
        if(filter.sharedMesh==null) continue;
        Matrix4x4 T=go.transform.worldToLocalMatrix*filter.transform.localToWorldMatrix;
        for(int si=0;si<filter.sharedMesh.subMeshCount;si++)
          grouped[p.material].Add(new CombineInstance{mesh=filter.sharedMesh,subMeshIndex=si,transform=T});
      }
    }
    var pieces=new List<CombineInstance>();var mats=new List<Material>();
    foreach(var key in order) {
       if(grouped[key].Count==0)continue;
       Mesh mesh=new Mesh{indexFormat=IndexFormat.UInt32,name=d.name+"_"+key};
       mesh.CombineMeshes(grouped[key].ToArray(),true,true,false);sections.Add(mesh);
       pieces.Add(new CombineInstance{mesh=mesh,subMeshIndex=0,transform=Matrix4x4.identity});mats.Add(materials[key]);
    }
    Mesh combined=new Mesh{indexFormat=IndexFormat.UInt32,name="Mesh_"+d.name};
    combined.CombineMeshes(pieces.ToArray(),false,true,false);combined.RecalculateBounds();
    var bb=combined.bounds;var pivot=new Vector3(bb.center.x,bb.min.y,bb.center.z);var v=combined.vertices;
    for(int i=0;i<v.Length;i++)v[i]-=pivot;
    combined.vertices=v;combined.RecalculateBounds();
    string meshPath=Root+"/Meshes/Generated/Mesh_"+d.name+".asset";
    Mesh stored=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
    if(stored==null){AssetDatabase.CreateAsset(combined,meshPath);stored=combined;}
    else{EditorUtility.CopySerialized(combined,stored);UnityEngine.Object.DestroyImmediate(combined);EditorUtility.SetDirty(stored);}
    GameObject o=new GameObject("P_"+d.name);
    try {o.AddComponent<MeshFilter>().sharedMesh=stored;var mr=o.AddComponent<MeshRenderer>();mr.sharedMaterials=mats.ToArray();mr.shadowCastingMode=ShadowCastingMode.On;mr.receiveShadows=true;var bc=o.AddComponent<BoxCollider>();bc.center=stored.bounds.center;bc.size=stored.bounds.size;PrefabUtility.SaveAsPrefabAsset(o,Root+"/Prefabs/P_"+d.name+".prefab");}
    finally {UnityEngine.Object.DestroyImmediate(o);}
   } finally {foreach(var m in sections) UnityEngine.Object.DestroyImmediate(m);foreach(var o in models)UnityEngine.Object.DestroyImmediate(o);}
 }
}
#endif
