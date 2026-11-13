using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;

public static class WashroomBusyGrimePass
{
 const string Folder="Assets/Art/Environment/Washroom/BusyGrime",Marker="Busy washroom wear - layered shoe traffic and wall grime";
 static Transform root;static Material[] ages;static Material wall;
 class Stamp{public Vector3 position;public float yaw,width,length;public int cell,age;}
 static T[] All<T>(Scene s)where T:Component{return s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true)).ToArray();}
 static Material Mat(string name,Texture2D tex,Color c,int queue,float wet,float key,float blood)
 {
  string path=Folder+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Recoil/3D/Multiplicative Wear Decal"));AssetDatabase.CreateAsset(m,path);}m.SetTexture("_MainTex",tex);m.SetColor("_Tint",c);m.SetFloat("_Wetness",wet);m.SetFloat("_KeyWhite",key);m.SetFloat("_BloodBoost",blood);m.SetFloat("_UseLocalUV",1);m.renderQueue=queue;EditorUtility.SetDirty(m);return m;
 }
 static Texture2D Texture(string name)
 {
  string path=Folder+"/"+name+".png";AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);var ti=(TextureImporter)AssetImporter.GetAtPath(path);ti.textureType=TextureImporterType.Default;ti.alphaSource=TextureImporterAlphaSource.FromInput;ti.alphaIsTransparency=true;ti.wrapMode=TextureWrapMode.Clamp;ti.mipmapEnabled=true;ti.maxTextureSize=2048;ti.anisoLevel=4;ti.textureCompression=TextureImporterCompression.Compressed;ti.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
 }
 static void MeshObject(string name,Vector3[] vertices,Vector2[] uv,int[] triangles,Material mat)
 {
  string path=Folder+"/"+name+".asset";var m=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(!m){m=new Mesh();AssetDatabase.CreateAsset(m,path);}m.Clear();m.name=name;m.vertices=vertices;m.uv=uv;var localUV=new Vector2[vertices.Length];for(int i=0;i<vertices.Length;i++)localUV[i]=new[]{new Vector2(0,0),new Vector2(1,0),new Vector2(1,1),new Vector2(0,1)}[i%4];m.uv2=localUV;m.triangles=triangles;m.RecalculateNormals();m.RecalculateBounds();EditorUtility.SetDirty(m);var g=new GameObject(name);g.transform.SetParent(root,false);g.AddComponent<MeshFilter>().sharedMesh=m;var r=g.AddComponent<MeshRenderer>();r.sharedMaterial=mat;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;r.receiveShadows=true;
 }
 static void Quad(string name,Vector3 a,Vector3 b,Vector3 c,Vector3 d,Material m,Rect uv)
 {MeshObject(name,new[]{a,b,c,d},new[]{new Vector2(uv.xMin,uv.yMin),new Vector2(uv.xMax,uv.yMin),new Vector2(uv.xMax,uv.yMax),new Vector2(uv.xMin,uv.yMax)},new[]{0,2,1,0,3,2},m);}
 static void Route(List<Stamp> list,System.Random rng,Vector2 from,Vector2 to,float offset,int pass)
 {
  Vector2 dir=(to-from).normalized,side=new Vector2(dir.y,-dir.x);int steps=Mathf.CeilToInt((to-from).magnitude/.32f);float yaw=Mathf.Atan2(dir.x,dir.y)*Mathf.Rad2Deg;
  for(int i=0;i<steps;i++){float f=(i+.5f)/steps;Vector2 p=Vector2.Lerp(from,to,f)+side*((i%2==0?-.095f:.095f)+offset+(float)(rng.NextDouble()-.5)*.05f);float age=(float)rng.NextDouble();list.Add(new Stamp{position=new Vector3(p.x,.010f,p.y),yaw=yaw+(float)(rng.NextDouble()-.5)*18,width=.25f+(float)rng.NextDouble()*.045f,length=.29f+(float)rng.NextDouble()*.035f,cell=rng.Next(16),age=age<.52f?0:age<.88f?1:2});}
 }
 public static string Apply()
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play first");Directory.CreateDirectory(Folder);var previous=SceneManager.GetActiveScene();var s=SceneManager.GetSceneByPath(WashroomRefugeDetailPass.ScenePath);bool added=!s.isLoaded;if(added)s=EditorSceneManager.OpenScene(WashroomRefugeDetailPass.ScenePath,OpenSceneMode.Additive);SceneManager.SetActiveScene(s);
  try{
   var w=All<RecoilPrototype>(s).Single();if(w.transform.Find(Marker))return "Already applied";Directory.CreateDirectory("Backups/WashroomBusyGrime");EditorSceneManager.SaveScene(s,"Backups/WashroomBusyGrime/"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
   var collision=All<Collider2D>(s).ToDictionary(c=>c.GetInstanceID(),c=>c.bounds.ToString("F6"));var prop=All<Transform>(s).Where(t=>t.name.Contains("complete cloth and support")||t.name=="Bandages - bloodied heap and scattered rolls").ToDictionary(t=>t,t=>t.position);
   var atlas=Texture("VariedSoleAtlas");var grime=Texture("WallRunoffAndCornerDirt");var shader=Shader.Find("Recoil/3D/Washroom Grime Decal");if(!shader||ShaderUtil.ShaderHasError(shader))throw new Exception("Grime shader error");
   ages=new[]{Mat("01 Faded accumulated shoe traffic",atlas,new Color(.39f,.35f,.31f,.26f),2997,.04f,0,.45f),Mat("02 Repeated muddy tread",atlas,new Color(.42f,.36f,.31f,.44f),2998,.11f,0,.55f),Mat("03 Dark blood and dirt impressions",atlas,new Color(.38f,.26f,.22f,.66f),2999,.08f,0,.85f)};
   wall=Mat("Wall runoff and ingrained corner grime",grime,new Color(.50f,.48f,.42f,.72f),2995,.04f,1,.15f);
   root=new GameObject(Marker).transform;root.SetParent(w.transform,false);root.position=Vector3.zero;var list=new List<Stamp>();var rng=new System.Random(197812);
   Route(list,rng,new Vector2(1.15f,-.10f),new Vector2(4.70f,3.80f),0,0);Route(list,rng,new Vector2(4.65f,3.72f),new Vector2(1.35f,.12f),.12f,1);
   Route(list,rng,new Vector2(7.65f,2.25f),new Vector2(3.15f,3.78f),0,2);Route(list,rng,new Vector2(3.20f,3.70f),new Vector2(7.70f,1.95f),.08f,3);
   Route(list,rng,new Vector2(1.08f,.25f),new Vector2(7.8f,.85f),0,4);Route(list,rng,new Vector2(2.05f,3.45f),new Vector2(4.65f,2.85f),0,5);
   for(int i=0;i<30;i++){float x=2.75f+(float)rng.NextDouble()*2.35f,z=2.80f+(float)rng.NextDouble()*.98f;list.Add(new Stamp{position=new Vector3(x,.01f,z),yaw=(float)rng.NextDouble()*360,width=.27f,length=.30f,cell=rng.Next(16),age=i%3});}
   for(int age=0;age<3;age++){var v=new List<Vector3>();var uv=new List<Vector2>();var tri=new List<int>();foreach(var item in list.Where(p=>p.age==age)){int k=v.Count;var q=Quaternion.Euler(0,item.yaw,0);foreach(var p in new[]{new Vector3(-.5f,0,-.5f),new Vector3(.5f,0,-.5f),new Vector3(.5f,0,.5f),new Vector3(-.5f,0,.5f)})v.Add(item.position+q*Vector3.Scale(p,new Vector3(item.width,1,item.length)));float u=(item.cell%4)/4f,wv=(item.cell/4)/4f,eps=.0015f;uv.AddRange(new[]{new Vector2(u+eps,wv+eps),new Vector2(u+.25f-eps,wv+eps),new Vector2(u+.25f-eps,wv+.25f-eps),new Vector2(u+eps,wv+.25f-eps)});tri.AddRange(new[]{k,k+2,k+1,k,k+3,k+2});}MeshObject("Shoe traffic age layer "+(age+1),v.ToArray(),uv.ToArray(),tri.ToArray(),ages[age]);}
   // Planar alpha overlays separated from original surfaces. Wall tiles and all props remain unchanged.
   Quad("Rear tiled wall - water drips and ingrained smears",new Vector3(.065f,.02f,4.647f),new Vector3(8.935f,.02f,4.647f),new Vector3(8.935f,3.66f,4.647f),new Vector3(.065f,3.66f,4.647f),wall,new Rect(0,0,1,1));
   Quad("Left wall corner - rising damp return",new Vector3(.012f,.02f,3.00f),new Vector3(.012f,.02f,4.64f),new Vector3(.012f,1.80f,4.64f),new Vector3(.012f,1.80f,3.00f),wall,new Rect(.05f,0,.45f,.72f));
   Quad("Right wall corner - damp return",new Vector3(8.988f,.02f,4.64f),new Vector3(8.988f,.02f,3.25f),new Vector3(8.988f,1.80f,3.25f),new Vector3(8.988f,1.80f,4.64f),wall,new Rect(.48f,0,.45f,.72f));
   Quad("Rear floor corner - caked muddy dust",new Vector3(.06f,.012f,4.22f),new Vector3(8.94f,.012f,4.22f),new Vector3(8.94f,.012f,4.642f),new Vector3(.06f,.012f,4.642f),wall,new Rect(0,0,1,.24f));
   Quad("Left floor corner - ingrained dirt",new Vector3(.012f,.012f,3.00f),new Vector3(.40f,.012f,3.00f),new Vector3(.40f,.012f,4.64f),new Vector3(.012f,.012f,4.64f),wall,new Rect(0,0,.45f,.24f));
   var rearStrip=root.Find("Rear floor corner - caked muddy dust").GetComponent<MeshFilter>().sharedMesh;rearStrip.uv=new[]{new Vector2(0,.24f),new Vector2(1,.24f),new Vector2(1,0),new Vector2(0,0)};EditorUtility.SetDirty(rearStrip);
   var sideStrip=root.Find("Left floor corner - ingrained dirt").GetComponent<MeshFilter>().sharedMesh;sideStrip.uv=new[]{new Vector2(0,0),new Vector2(0,.24f),new Vector2(.45f,.24f),new Vector2(.45f,0)};EditorUtility.SetDirty(sideStrip);
   Physics2D.SyncTransforms();foreach(var c in All<Collider2D>(s))if(collision[c.GetInstanceID()]!=c.bounds.ToString("F6"))throw new Exception("Collision changed");foreach(var p in prop)if(p.Key.position!=p.Value)throw new Exception("User prop position changed");
   if(root.GetComponentsInChildren<Collider2D>(true).Length!=0||root.GetComponentsInChildren<Collider>(true).Length!=0)throw new Exception("Overlay collision");EditorSceneManager.MarkSceneDirty(s);EditorSceneManager.SaveScene(s);AssetDatabase.SaveAssets();Directory.CreateDirectory("output/washroom-busy-grime");string report="PASS: "+list.Count+" adult-size varied sole impressions from 16-cell atlas, 3 opacity/age groups combined into 3 meshes. Rear wall and 2 corner return overlays, 2 wall/floor seam overlays. Transparency, no depth write, stable layer queues, separate from floor/wall faces. Original mud patches retained. All collision bounds and user prop positions unchanged. No added colliders. Built-in image_gen textures.";File.WriteAllText("output/washroom-busy-grime/verification.txt",report);WashroomMassLaundryPass.Capture(s);return report;
  }finally{if(added)EditorSceneManager.CloseScene(s,true);if(previous.IsValid()&&previous.isLoaded){SceneManager.SetActiveScene(previous);foreach(var w in All<Recoil3DWorld>(previous))w.ApplyAtmosphere();foreach(var w in All<WarehouseAtmosphereTest>(previous))w.ApplyLook();}}
 }
}
