using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class FactoryForecourtRoadPass
{
 static void Curve(List<Vector3> path,Vector3 a,Vector3 b,Vector3 c,Vector3 d,int samples)
 {
  for(int i=path.Count==0?0:1;i<=samples;i++)
  {float t=i/(float)samples,u=1-t;path.Add(a*u*u*u+3*b*u*u*t+3*c*u*t*t+d*t*t*t);}
 }
 public static List<Vector3> PathToFactory(Transform publicRoad,float gateX)
 {
  const float mergeZ=4;
  var start=publicRoad.TransformPoint(new Vector3(-66.8f-.07f*mergeZ,0,mergeZ));start.y=0;
  var piers=publicRoad.gameObject.scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<Transform>(true)).Where(t=>t.name=="Masonry rear gate pier").ToArray();
  float roadZ=piers.Length>0?piers.Average(t=>t.position.z):-3.8f;
  var path=new List<Vector3>();
  // Follow the annotated arrows: public road -> forward -> across forecourt foreground -> back -> store/front gate.
  // Both curves stay outside the canopy instead of taking a diagonal shortcut across the petrol lanes.
  var foregroundStart=new Vector3(-88,0,-23);
  Curve(path,start,start+Vector3.back*20,new Vector3(-104,0,-23),foregroundStart,40);
  for(int i=1;i<=48;i++)path.Add(Vector3.Lerp(foregroundStart,new Vector3(-40,0,-23),i/48f));
  var upright=new Vector3(-24,0,-13);
  Curve(path,path.Last(),new Vector3(-29,0,-23),new Vector3(-24,0,-20),upright,28);
  var topOfUpright=new Vector3(-24,0,roadZ-7);
  for(int i=1;i<=6;i++)path.Add(Vector3.Lerp(upright,topOfUpright,i/6f));
  var straightStart=new Vector3(-17,0,roadZ);
  Curve(path,topOfUpright,new Vector3(-24,0,roadZ-2),new Vector3(-21,0,roadZ),straightStart,20);
  int count=Mathf.CeilToInt(gateX+12-straightStart.x);
  for(int i=1;i<=count;i++)path.Add(new Vector3(Mathf.Lerp(straightStart.x,gateX+12,i/(float)count),0,roadZ));
  return path;
 }
 public static string Apply()
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play first");
  var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
  var all=scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<Transform>(true)).ToArray();
  var root=all.Single(t=>t.name=="Factory rear-gate approach road - economy vehicle revision");
  var road=all.Single(t=>t.name=="Road - foreground to misty horizon");
  var gateX=all.First(t=>t.name=="Masonry rear gate pier").position.x;
  var before=all.SelectMany(t=>t.GetComponents<Collider2D>()).Select(c=>c.GetInstanceID()+"|"+c.bounds).ToArray();
  Directory.CreateDirectory("Backups/FactoryForecourtRoad");var backup="Backups/FactoryForecourtRoad/"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity";EditorSceneManager.SaveScene(scene,backup,true);
  const string folder="Assets/Recoil3D/FactoryForecourtRoad";Directory.CreateDirectory(folder);AssetDatabase.Refresh();
  var path=PathToFactory(road,gateX);
  var filters=root.GetComponentsInChildren<MeshFilter>();
  foreach(var filter in filters)
  {
   bool shoulder=filter.name=="Worn gravel road shoulders";float half=shoulder?3.45f:2.88f;
   var v=new List<Vector3>();var tri=new List<int>();
   for(int i=0;i<path.Count;i++)
   {
    var tangent=(path[Mathf.Min(path.Count-1,i+1)]-path[Mathf.Max(0,i-1)]).normalized;
    var right=new Vector3(tangent.z,0,-tangent.x);
    v.Add(filter.transform.InverseTransformPoint(path[i]-right*half+Vector3.up*(shoulder?.008f:.023f)));
    v.Add(filter.transform.InverseTransformPoint(path[i]+right*half+Vector3.up*(shoulder?.008f:.023f)));
    if(i>0){int n=i*2;tri.AddRange(new[]{n-2,n,n+1,n-2,n+1,n-1});}
   }
   var mesh=new Mesh();mesh.name=shoulder?"Connected forecourt road shoulders":"Factory to station foreground to public road";
   mesh.SetVertices(v);mesh.SetTriangles(tri,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
   AssetDatabase.CreateAsset(mesh,AssetDatabase.GenerateUniqueAssetPath(folder+"/"+mesh.name+".asset"));filter.sharedMesh=mesh;EditorUtility.SetDirty(filter);
  }
  var after=scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<Collider2D>(true)).Select(c=>c.GetInstanceID()+"|"+c.bounds).ToArray();
  if(!before.SequenceEqual(after))throw new Exception("Gameplay collision changed");
  float gateZ=all.Where(t=>t.name=="Masonry rear gate pier").Average(t=>t.position.z);
  if(path.Min(p=>p.z)<-27.01f||path.Where(p=>p.x>=-17).Any(p=>Mathf.Abs(p.z-gateZ)>.001f))throw new Exception("Road connection mismatch");
  AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
  string report="PASS: annotated foreground loop; from gate/store straight to X=-17, forward turn outside canopy at X=-24, horizontal foreground road Z=-23 from X=-40 to-88, then turn back to public road at "+path[0]+". Gate X="+gateX+", Z="+gateZ+". Original public road, all car placements, roadside props and gameplay colliders retained. Backup: "+backup;
  Directory.CreateDirectory("output/factory-forecourt-road");File.WriteAllText("output/factory-forecourt-road/verification.txt",report);return report;
 }
}
