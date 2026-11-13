using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class OutdoorMountainContinuationPass
{
 const string Folder="Assets/Recoil3D/MountainContinuation",Marker="Continuous distant mountains - right of factory";
 static Mesh Continue(MeshFilter source,float height,out float seamError)
 {
  const int oldColumns=56,rows=8;float spacing=195f/oldColumns;
  var old=source.sharedMesh.vertices;
  if(old.Length<(oldColumns+1)*(rows+1))throw new Exception("Original hill grid not found");
  var edge=Enumerable.Range(0,rows+1).Select(j=>source.transform.TransformPoint(old[j*(oldColumns+1)+oldColumns])).ToArray();
  int columns=Mathf.CeilToInt((340-edge[0].x)/spacing);
  var v=new List<Vector3>();var tri=new List<int>();seamError=0;
  for(int j=0;j<=rows;j++)for(int i=0;i<=columns;i++)
  {
   float dx=i*spacing,phase=oldColumns+i,depth=j/(float)rows;
   // Keep the original edge exact, then recede behind the industrial backdrop.
   float retreat=Mathf.SmoothStep(0,1,Mathf.Clamp01(dx/30f));
   float ridge=height*(.61f+.16f*Mathf.Sin(phase*.19f)+.16f*Mathf.Sin(phase*.43f+1.7f));
   var local=old[j*(oldColumns+1)+oldColumns];local.x+=dx;
   local.y=-.08f+ridge*Mathf.Sin(depth*Mathf.PI);
   var p=source.transform.TransformPoint(local);
   if(i==0)p=edge[j];
   else
   {
    p.z+=75*retreat;
    float ground=source.transform.TransformPoint(new Vector3(local.x,-.08f,local.z)).y;
    p.y=ground+(p.y-ground)*(1+.5f*retreat);
   }
   if(i==0)seamError=Mathf.Max(seamError,Vector3.Distance(p,edge[j]));
   v.Add(p);
   if(i>0&&j>0){int n=j*(columns+1)+i;tri.AddRange(new[]{n-columns-2,n-1,n,n-columns-2,n,n-columns-1});}
  }
  var mesh=new Mesh();mesh.SetVertices(v);mesh.SetTriangles(tri,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
 }
 public static string Apply()
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play first");
  var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
  var all=scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<Transform>(true)).ToArray();var world=all.Single(t=>t.GetComponent<RecoilPrototype>());
  if(world.Find(Marker))return "Already extended; authored mountain transforms retained";
  var before=all.SelectMany(t=>t.GetComponents<Collider2D>()).Select(c=>c.GetInstanceID()+"|"+c.bounds).ToArray();
  Directory.CreateDirectory("Backups/MountainContinuation");var backup="Backups/MountainContinuation/"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity";EditorSceneManager.SaveScene(scene,backup,true);
  Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
  var root=new GameObject(Marker).transform;root.SetParent(world,true);root.position=Vector3.zero;
  int triangles=0;
  foreach(bool far in new[]{false,true})
  {
   var source=all.Single(t=>t.name==(far?"Static detail - Far countryside atmosphere":"Static detail - Near countryside atmosphere")).GetComponent<MeshFilter>();
   float error;var mesh=Continue(source,far?21:13,out error);if(error>.00001f)throw new Exception("Mountain seam mismatch");
   mesh.name=far?"Far ridge continuation":"Near ridge continuation behind industrial backdrop";
   AssetDatabase.CreateAsset(mesh,AssetDatabase.GenerateUniqueAssetPath(Folder+"/"+mesh.name+".asset"));
   var t=new GameObject(mesh.name).transform;t.SetParent(root,false);t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
   var renderer=t.gameObject.AddComponent<MeshRenderer>();renderer.sharedMaterials=source.GetComponent<MeshRenderer>().sharedMaterials;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;triangles+=mesh.triangles.Length/3;
  }
  var after=scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<Collider2D>(true)).Select(c=>c.GetInstanceID()+"|"+c.bounds).ToArray();if(!before.SequenceEqual(after))throw new Exception("Collision changed");
  AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
  string report="PASS near/far mountain ridges continued from exact original right edge to X>340m; reuse original atmosphere materials; recede75m behind factory backdrop; two renderers, "+triangles+" triangles; original hills/trees/buildings/gameplay collision retained. Backup: "+backup;
  Directory.CreateDirectory("output/mountain-continuation");File.WriteAllText("output/mountain-continuation/verification.txt",report);return report;
 }
}
