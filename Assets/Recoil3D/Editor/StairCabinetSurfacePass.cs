using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
[InitializeOnLoad]
public static class StairCabinetSurfacePass
{
 const string Dir="output/stair-cabinet-surfaces";
 static StairCabinetSurfacePass(){EditorApplication.update+=Poll;}
 static void Poll(){if(!File.Exists(Dir+"/request")||EditorApplication.isCompiling||EditorApplication.isUpdating)return;if(EditorApplication.isPlaying){EditorApplication.isPlaying=false;return;}if(EditorApplication.isPlayingOrWillChangePlaymode)return;string cmd=File.ReadAllText(Dir+"/request").Trim();File.Delete(Dir+"/request");try{Inspect();if(cmd=="fix")Fix();}catch(Exception e){File.WriteAllText(Dir+"/error.txt",e.ToString());Debug.LogException(e);}}
 static T[] All<T>(Scene s)where T:Component{return s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true)).ToArray();}

 static bool Live(Recoil3DSpriteVisual v){return v.meshRenderer&&v.meshRenderer.enabled&&!v.suppressed&&v.gameObject.activeInHierarchy;}
 static void Depth(Recoil3DSpriteVisual v,float z,float width){v.depth=width;v.depthOffset=v.transform.InverseTransformPoint(new Vector3(v.transform.position.x,v.transform.position.y,z)).z;v.Sync();EditorUtility.SetDirty(v);EditorUtility.SetDirty(v.meshRenderer.transform);}
 static void Fix(){var report=new StringBuilder();var prev=SceneManager.GetActiveScene();foreach(string path in new[]{"Assets/Scenes/3D/RecoilFactoryInterior.unity","Assets/Scenes/3D/RecoilPrototype.unity"}){var scene=SceneManager.GetSceneByPath(path);bool added=!scene.isLoaded;if(added)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);try{
 var colliders=All<Collider2D>(scene).Select(c=>c.GetInstanceID()+"|"+c.enabled+"|"+c.gameObject.activeInHierarchy+"|"+c.bounds.ToString("F6")).ToArray();
 var stairs=All<WalkableStairway>(scene);var routes=stairs.SelectMany(st=>st.routePoints).Select(t=>t.GetInstanceID()+"|"+t.position.ToString("F6")).ToArray();
 string backup="Backups/StairCabinetSurfaces/"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+"/"+scene.name+".unity";Directory.CreateDirectory(System.IO.Path.GetDirectoryName(backup));if(!EditorSceneManager.SaveScene(scene,backup,true))throw new Exception("Backup failed");int edges=0,duplicates=0,cabinets=0;
 if(scene.name=="RecoilFactoryInterior")foreach(var stair in stairs){var visuals=stair.GetComponentsInChildren<Recoil3DSpriteVisual>(true);var bodies=visuals.Where(v=>Live(v)&&v.name=="Grated steel tread").ToArray();if(bodies.Length==0)continue;
 foreach(var group in All<Transform>(scene).Where(t=>t.name=="3D stairs - "+stair.name&&t.gameObject.activeInHierarchy)){if(group.GetComponentsInChildren<Collider>(true).Length+group.GetComponentsInChildren<Collider2D>(true).Length>0)throw new Exception("Duplicate contains collision");group.gameObject.SetActive(false);EditorUtility.SetDirty(group.gameObject);duplicates++;}
 foreach(var edge in visuals.Where(v=>Live(v)&&v.name=="Tread front edge")){var body=bodies.OrderBy(v=>(v.transform.position-edge.transform.position).sqrMagnitude).First();if(Vector2.Distance(body.transform.position,edge.transform.position)>.12f)throw new Exception("Cannot match tread edge");var b=body.meshRenderer.bounds;Depth(edge,b.min.z-.020f,.024f);var pos=edge.meshRenderer.transform.position;pos.y+=b.max.y-.008f-edge.meshRenderer.bounds.max.y;edge.meshRenderer.transform.position=pos;EditorUtility.SetDirty(edge.meshRenderer.transform);if(edge.meshRenderer.bounds.max.z>=b.min.z-.003f||edge.meshRenderer.bounds.max.y>=b.max.y-.003f)throw new Exception("Tread edge remains coplanar");edges++;}
 }
 if(scene.name=="RecoilPrototype")foreach(var cabinet in All<Transform>(scene).Where(t=>t.name=="Tall filing cabinet"||t.name=="Overturned filing cabinet")){var visuals=cabinet.GetComponentsInChildren<Recoil3DSpriteVisual>(true);var body=visuals.Single(v=>v.name=="Steel cabinet body");float front=body.meshRenderer.bounds.min.z;var drawers=visuals.Where(v=>v.name=="Drawer face").ToArray();foreach(var drawer in drawers)Depth(drawer,front-.022f,.032f);
 foreach(var v in visuals.Where(v=>v.name=="Recessed drawer handle"||v.name=="Old paper label")){var drawer=drawers.OrderBy(d=>(d.transform.position-v.transform.position).sqrMagnitude).First();float face=drawer.meshRenderer.bounds.min.z;bool handle=v.name=="Recessed drawer handle";Depth(v,face-(handle?.020f:.004f),handle?.024f:.004f);}
 foreach(var v in visuals.Where(v=>v.name=="Cabinet top rim"))Depth(v,body.meshRenderer.bounds.center.z,body.meshRenderer.bounds.size.z+.04f);
 foreach(var d in drawers)if(d.meshRenderer.bounds.max.z>=front-.003f)throw new Exception("Drawer remains coplanar");cabinets++;}
 Physics2D.SyncTransforms();if(!colliders.SequenceEqual(All<Collider2D>(scene).Select(c=>c.GetInstanceID()+"|"+c.enabled+"|"+c.gameObject.activeInHierarchy+"|"+c.bounds.ToString("F6"))))throw new Exception("Collider changed");if(!routes.SequenceEqual(stairs.SelectMany(st=>st.routePoints).Select(t=>t.GetInstanceID()+"|"+t.position.ToString("F6"))))throw new Exception("Stair route changed");
 foreach(var stair in stairs.Where(st=>st.Available)){float distance;if(!stair.TryEntry(stair.PositionAt(0),1,out distance))throw new Exception("Stair lower entry failed");if(stair.upperLandingAvailable&&!stair.TryEntry(stair.PositionAt(stair.Length),-1,out distance))throw new Exception("Stair upper entry failed");for(int i=0;i<=100;i++){var a=stair.PositionAt(stair.Length*i/100f);if(float.IsNaN(a.x)||float.IsNaN(a.y))throw new Exception("Stair path invalid");}}
 EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new Exception("Save failed");report.AppendLine("PASS "+scene.name+": tread edges="+edges+", active duplicate groups disabled="+duplicates+", cabinets="+cabinets+"; all collider bounds and routes unchanged; stair entry and 101 route samples verified. Backup: "+backup);
 if(edges>0){var v=stairs.SelectMany(st=>st.GetComponentsInChildren<Recoil3DSpriteVisual>(true)).First(x=>Live(x)&&x.name=="Grated steel tread");Capture(scene,v.meshRenderer.bounds.center+Vector3.up*.8f,2.3f,"Interior-stairs");}
 if(cabinets>0){var t=All<Transform>(scene).First(x=>x.name=="Tall filing cabinet");Capture(scene,t.position,2,"Outdoor-cabinets");}
 }finally{if(added)EditorSceneManager.CloseScene(scene,true);if(prev.IsValid()&&prev.isLoaded)SceneManager.SetActiveScene(prev);}}
 File.WriteAllText(Dir+"/verification.txt",report.ToString());}
 static void Capture(Scene s,Vector3 centre,float size,string name){var go=new GameObject("Temporary surface inspection camera");SceneManager.MoveGameObjectToScene(go,s);var cam=go.AddComponent<Camera>();cam.scene=s;cam.orthographic=true;cam.orthographicSize=size;cam.aspect=1.5f;cam.transform.position=centre+new Vector3(1,1,-14);cam.transform.LookAt(centre);cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=Color.black;
 var light=go.AddComponent<Light>();light.type=LightType.Point;light.range=30;light.intensity=1.4f;
 var rt=new RenderTexture(1200,800,24);var tex=new Texture2D(1200,800,TextureFormat.RGB24,false);var old=RenderTexture.active;try{cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1200,800),0,0);tex.Apply();File.WriteAllBytes(Dir+"/"+name+".png",tex.EncodeToPNG());}finally{cam.targetTexture=null;RenderTexture.active=old;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);UnityEngine.Object.DestroyImmediate(go);}}
 static void Inspect(){var report=new StringBuilder();var prev=SceneManager.GetActiveScene();foreach(string path in new[]{"Assets/Scenes/3D/RecoilFactoryInterior.unity","Assets/Scenes/3D/RecoilPrototype.unity"}){var s=SceneManager.GetSceneByPath(path);bool added=!s.isLoaded;if(added)s=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);try{
 report.AppendLine(path);foreach(var t in All<Transform>(s).Where(t=>t.name.StartsWith("3D stairs -")))report.AppendLine(t.name+" active="+t.gameObject.activeInHierarchy);
 foreach(var stair in All<WalkableStairway>(s)){report.AppendLine("STAIR "+stair.name);foreach(var v in stair.GetComponentsInChildren<Recoil3DSpriteVisual>(true).Where(v=>v.meshRenderer&&v.meshRenderer.enabled&&v.gameObject.activeInHierarchy&&(v.name.Contains("tread")||v.name.Contains("Tread"))).Take(8))report.AppendLine(v.name+" pos="+v.transform.position.ToString("F4")+" bounds="+v.meshRenderer.bounds.ToString("F4"));}
 foreach(var cab in All<Transform>(s).Where(t=>t.name=="Tall filing cabinet"||t.name=="Overturned filing cabinet")){report.AppendLine("CABINET "+cab.name);foreach(var v in cab.GetComponentsInChildren<Recoil3DSpriteVisual>(true).Where(v=>v.meshRenderer))report.AppendLine(v.name+" pos="+v.transform.position.ToString("F4")+" bounds="+v.meshRenderer.bounds.ToString("F4"));}
 }finally{if(added)EditorSceneManager.CloseScene(s,true);if(prev.IsValid()&&prev.isLoaded)SceneManager.SetActiveScene(prev);}}
 File.WriteAllText(Dir+"/inspection.txt",report.ToString());}
}
