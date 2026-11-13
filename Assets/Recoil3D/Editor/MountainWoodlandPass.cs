using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

public static class MountainWoodlandPass
{
 const string Marker="Mountain woodland - individually editable spruces";
 static float Height(MeshFilter terrain,float x,float z)
 {
  var mesh=terrain.sharedMesh;var v=mesh.vertices;var indices=mesh.triangles;
  for(int i=0;i<indices.Length;i+=3)
  {
   var a=terrain.transform.TransformPoint(v[indices[i]]);var b=terrain.transform.TransformPoint(v[indices[i+1]]);var c=terrain.transform.TransformPoint(v[indices[i+2]]);
   float det=(b.z-c.z)*(a.x-c.x)+(c.x-b.x)*(a.z-c.z);if(Mathf.Abs(det)<.000001f)continue;
   float u=((b.z-c.z)*(x-c.x)+(c.x-b.x)*(z-c.z))/det,w=((c.z-a.z)*(x-c.x)+(a.x-c.x)*(z-c.z))/det,q=1-u-w;
   if(u>=-.0001f&&w>=-.0001f&&q>=-.0001f)return u*a.y+w*b.y+q*c.y;
  }
  throw new Exception("No mountain surface under tree at "+x+","+z);
 }
 static Transform Group(Transform parent,string name){var t=new GameObject(name).transform;t.SetParent(parent,false);return t;}
 public static string Apply()
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play first");
  var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();var all=scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<Transform>(true)).ToArray();
  var mountains=all.Single(t=>t.name=="Continuous distant mountains - right of factory");if(mountains.Find(Marker))return "Woodland already placed; authored tree positions retained";
  var templates=Enumerable.Range(100,4).Select(i=>all.Single(t=>t.name=="Tree - spruce "+i)).ToArray();
  var near=mountains.Find("Near ridge continuation behind industrial backdrop").GetComponent<MeshFilter>();var far=mountains.Find("Far ridge continuation").GetComponent<MeshFilter>();
  var before=all.SelectMany(t=>t.GetComponents<Collider2D>()).Select(c=>c.GetInstanceID()+"|"+c.bounds).ToArray();
  Directory.CreateDirectory("Backups/MountainWoodland");var backup="Backups/MountainWoodland/"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity";EditorSceneManager.SaveScene(scene,backup,true);
  var root=Group(mountains,Marker);var slopes=Group(root,"Near slopes - uneven woodland clusters");var crests=Group(root,"Small groups along upper slopes");var distant=Group(root,"Far ridge - sparse silhouettes");
  var rng=new System.Random(197306);int count=0;
  for(int row=0;row<3;row++)
  {
   int slots=row==0?50:row==1?26:23;
   for(int i=0;i<slots;i++)
   {
    if(row==0&&(i%9==4||i%9==5)||row==1&&i%4==2||row==2&&i%6==2)continue;
    float x=(row==0?4+i*6.7f:row==1?8+i*12.9f:10+i*14.8f)+(float)(rng.NextDouble()-.5)*2.4f;
    float retreat=Mathf.SmoothStep(0,1,Mathf.Clamp01(x/30));
    float depth=row==0?.29f+(i%4)*.10f:row==1?.69f+(i%3)*.065f:.34f+(i%4)*.10f;
    float z=(row==2?82:36)+75*retreat+38*depth;
    var terrain=row==2?far:near;float y=Height(terrain,x,z);
    var template=templates[(i+row)%templates.Length];var tree=Object.Instantiate(template.gameObject,row==0?slopes:row==1?crests:distant).transform;
    tree.name="Mountain spruce "+(++count).ToString("000");tree.gameObject.SetActive(true);
    float size=(row==2?.95f:1.05f)+(float)rng.NextDouble()*.28f;
    // A modest scale lift compensates for the new hills receding behind the factory.
    size*=1+.22f*retreat;
    tree.localScale=new Vector3(size*(.90f+(float)rng.NextDouble()*.16f),size,size*(.90f+(float)rng.NextDouble()*.16f));
    tree.rotation=Quaternion.Euler(0,(float)rng.NextDouble()*360,0);
    tree.position=new Vector3(x,y+.01f-template.GetComponent<MeshFilter>().sharedMesh.bounds.min.y*tree.lossyScale.y,z);
    var renderer=tree.GetComponent<MeshRenderer>();renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
    foreach(var mat in renderer.sharedMaterials){mat.enableInstancing=true;EditorUtility.SetDirty(mat);}
    if(tree.childCount!=0||tree.GetComponentsInChildren<MeshFilter>().Length!=1)throw new Exception("Tree not independently editable");
    if(Mathf.Abs(renderer.bounds.min.y-y-.01f)>.002f)throw new Exception("Floating tree");
   }
  }
  if(root.GetComponentsInChildren<Collider>(true).Length+root.GetComponentsInChildren<Collider2D>(true).Length>0)throw new Exception("Unexpected collision");
  var after=scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<Collider2D>(true)).Select(c=>c.GetInstanceID()+"|"+c.bounds).ToArray();if(!before.SequenceEqual(after))throw new Exception("Gameplay collision changed");
  AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
  string report="PASS "+count+" individually editable spruces across near slopes, upper slopes and far ridge, reusing left woodland meshes/materials; irregular spacing, gaps, varied sizes/yaw; all bases sampled from actual hill triangles; instancing enabled, distant shadows off, no new collision. Backup: "+backup;
  Directory.CreateDirectory("output/mountain-woodland");File.WriteAllText("output/mountain-woodland/verification.txt",report);return report;
 }
}
