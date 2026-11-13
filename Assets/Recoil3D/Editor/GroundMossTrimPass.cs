using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

public static class GroundMossTrimPass
{
 const string PathName="Assets/Scenes/3D/RecoilPrototype.unity",Folder="Assets/Recoil3D/GroundMossTrim",Marker="Narrow ground moss with clear door approaches";
 static T[] All<T>(Scene s)where T:Component{return s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true)).ToArray();}
 struct Zone{public string name;public float minX,maxX,minZ,maxZ;}
 static bool Interval(float a,float d,float lo,float hi,ref float start,ref float end){if(Mathf.Abs(d)<.00001f)return a>=lo&&a<=hi;float p=(lo-a)/d,q=(hi-a)/d;if(p>q){float t=p;p=q;q=t;}start=Mathf.Max(start,p);end=Mathf.Min(end,q);return start<end;}
 static List<Zone> DoorZones(Scene s)
 {
  var result=new List<Zone>();foreach(var door in All<RecoilDoor>(s).Where(d=>d.transform.position.y>.55f&&d.transform.position.y<2.1f&&!d.name.ToLowerInvariant().Contains("drain")&&!(d.destinationScene??"").ToLowerInvariant().Contains("sewer")))
  {
   var renderers=door.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.bounds.size.y>1&&r.bounds.size.y<4&&r.bounds.size.x<4&&r.bounds.size.z<4).ToArray();Bounds bounds;
   if(renderers.Length>0){var active=renderers.Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray();if(active.Length>0)renderers=active;bounds=renderers[0].bounds;foreach(var r in renderers.Skip(1))bounds.Encapsulate(r.bounds);}
   else{var p=door.transform.position;if(Mathf.Abs(p.z)<.1f)p.z=2.8f;bounds=new Bounds(p,new Vector3(1.4f,2.6f,.2f));}
   bool side=bounds.size.z>bounds.size.x;result.Add(new Zone{name=door.name,minX=bounds.min.x-(side ? .75f : .22f),maxX=bounds.max.x+(side ? .75f : .22f),minZ=bounds.min.z-(side ? .22f : .75f),maxZ=bounds.max.z+(side ? .22f : .75f)});
  }
  var shutter=All<Transform>(s).FirstOrDefault(t=>t.name=="Warehouse closed rolling slat door - masonry repair");if(shutter){var p=shutter.position;result.Add(new Zone{name="Warehouse loading shutter",minX=p.x-2.8f,maxX=p.x+2.8f,minZ=p.z-1.2f,maxZ=p.z+.25f});}return result;
 }
 public static string Apply()
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode)return "Playing; deferred";Directory.CreateDirectory(Folder+"/Models");Directory.CreateDirectory("Backups/GroundMossTrim");Directory.CreateDirectory("output/ground-moss-trim");var previous=SceneManager.GetActiveScene();var s=SceneManager.GetSceneByPath(PathName);bool opened=!s.isLoaded;if(opened)s=EditorSceneManager.OpenScene(PathName,OpenSceneMode.Additive);
  try{
   if(All<Transform>(s).Any(t=>t.name==Marker))return "Already trimmed";var backup="Backups/GroundMossTrim/"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")+"-RecoilPrototype.unity";EditorSceneManager.SaveScene(s,backup,true);
   var collisions=All<Collider2D>(s).Select(c=>c.GetInstanceID()+"|"+c.bounds+"|"+c.enabled).ToArray();var doors=All<RecoilDoor>(s).Select(d=>EditorJsonUtility.ToJson(d)+"|"+d.transform.position).ToArray();var zones=DoorZones(s);if(zones.Count==0)throw new Exception("No ground-level door approaches found");
   var renderers=All<MeshRenderer>(s).Where(r=>r.sharedMaterial&&r.sharedMaterial.shader.name=="Recoil/3D/Building Footing Moss And Silt"&&r.sharedMaterial.GetFloat("_Ground")>.5f).ToArray();int index=0,visibleCuts=0,allCuts=0;float widest=0;
   foreach(var renderer in renderers){var filter=renderer.GetComponent<MeshFilter>();var old=filter.sharedMesh;var v=old.vertices;var uv=old.uv;if(v.Length%4!=0)throw new Exception("Unexpected moss topology");var vertices=new List<Vector3>();var coords=new List<Vector2>();var tris=new List<int>();
    for(int i=0;i<v.Length;i+=4){var a=renderer.transform.TransformPoint(v[i]);var b=renderer.transform.TransformPoint(v[i+1]);var c=renderer.transform.TransformPoint(v[i+2]);var d=renderer.transform.TransformPoint(v[i+3]);c=a+(c-a)*.5f;d=b+(d-b)*.5f;widest=Mathf.Max(widest,Vector3.Distance(a,c));var spans=new List<Vector2>{new Vector2(0,1)};var dir=b-a;
     foreach(var zone in zones){float cutA=0,cutB=1;if(!Interval(a.x,dir.x,zone.minX,zone.maxX,ref cutA,ref cutB)||!Interval(a.z,dir.z,zone.minZ,zone.maxZ,ref cutA,ref cutB))continue;var next=new List<Vector2>();bool cut=false;foreach(var span in spans){float left=Mathf.Max(span.x,cutA),right=Mathf.Min(span.y,cutB);if(left>=right){next.Add(span);continue;}cut=true;if(span.x<left-.00001f)next.Add(new Vector2(span.x,left));if(right<span.y-.00001f)next.Add(new Vector2(right,span.y));}if(cut){allCuts++;if(renderer.enabled&&renderer.gameObject.activeInHierarchy)visibleCuts++;}spans=next;}
     foreach(var span in spans){if((span.y-span.x)*dir.magnitude<.015f)continue;int k=vertices.Count;var wa=Vector3.Lerp(a,b,span.x);var wb=Vector3.Lerp(a,b,span.y);var wc=Vector3.Lerp(c,d,span.x);var wd=Vector3.Lerp(c,d,span.y);vertices.Add(renderer.transform.InverseTransformPoint(wa));vertices.Add(renderer.transform.InverseTransformPoint(wb));vertices.Add(renderer.transform.InverseTransformPoint(wc));vertices.Add(renderer.transform.InverseTransformPoint(wd));coords.Add(Vector2.Lerp(uv[i],uv[i+1],span.x));coords.Add(Vector2.Lerp(uv[i],uv[i+1],span.y));coords.Add(Vector2.Lerp(uv[i+2],uv[i+3],span.x));coords.Add(Vector2.Lerp(uv[i+2],uv[i+3],span.y));bool upward=Vector3.Cross(wb-wa,wc-wa).y>0;tris.AddRange(upward?new[]{k,k+1,k+2,k+1,k+3,k+2}:new[]{k,k+2,k+1,k+1,k+2,k+3});}
    }
    var mesh=new Mesh{name="Narrow moss avoiding door approaches "+index,indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};mesh.SetVertices(vertices);mesh.SetUVs(0,coords);mesh.SetTriangles(tris,0);mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,Folder+"/Models/Narrow moss avoiding door approaches "+index+".asset");filter.sharedMesh=mesh;EditorUtility.SetDirty(filter);index++;
   }
   if(visibleCuts==0)throw new Exception("No visible doorway moss was cleared");var marker=new GameObject(Marker);SceneManager.MoveGameObjectToScene(marker,s);marker.transform.SetParent(All<RecoilPrototype>(s).Single().transform,false);
   if(!collisions.SequenceEqual(All<Collider2D>(s).Select(c=>c.GetInstanceID()+"|"+c.bounds+"|"+c.enabled)))throw new Exception("Collision changed");if(!doors.SequenceEqual(All<RecoilDoor>(s).Select(d=>EditorJsonUtility.ToJson(d)+"|"+d.transform.position)))throw new Exception("Door configuration changed");EditorSceneManager.MarkSceneDirty(s);EditorSceneManager.SaveScene(s);AssetDatabase.SaveAssets();
   string result="PASS halved ground moss width on "+index+" source/combined meshes (typical 0.42m, factory 0.543m). Removed moss across "+zones.Count+" ground-level doorway/loading approach zones, including "+visibleCuts+" visible strip intersections. Wall moss and ground colour unchanged. Collision and doors unchanged. Backup="+backup;File.WriteAllText("output/ground-moss-trim/verification.txt",result+"\n"+string.Join("\n",zones.Select(z=>z.name+" X="+z.minX+".."+z.maxX+" Z="+z.minZ+".."+z.maxZ).ToArray()));return result;
  }finally{if(opened)EditorSceneManager.CloseScene(s,true);SceneManager.SetActiveScene(previous);var w=All<Recoil3DWorld>(previous).FirstOrDefault();if(w)w.ApplyAtmosphere();var look=All<WarehouseAtmosphereTest>(previous).FirstOrDefault();if(look)look.ApplyLook();}
 }
}
