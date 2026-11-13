using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
public static class OutdoorWallFixturesAlignment
{
 static T[] All<T>(Scene s)where T:Component{return s.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<T>(true)).ToArray();}
 static void SurfaceDepth(Transform t,float centre,float depth){foreach(var v in t.GetComponentsInChildren<Recoil3DSpriteVisual>(true).Where(v=>v.meshRenderer&&!v.suppressed)){v.depth=depth/Mathf.Abs(v.transform.lossyScale.z);v.depthOffset=v.transform.InverseTransformPoint(new Vector3(v.transform.position.x,v.transform.position.y,centre)).z;v.Sync();EditorUtility.SetDirty(v);}}
 static Transform Box(Transform p,string name,Vector3 pos,Vector3 size,Material m){var t=GameObject.CreatePrimitive(PrimitiveType.Cube).transform;t.name=name;t.SetParent(p,false);t.position=pos;t.localScale=size;Object.DestroyImmediate(t.GetComponent<Collider>());t.GetComponent<Renderer>().sharedMaterial=m;return t;}
 public static string Apply(){if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play first");var s=SceneManager.GetSceneByPath("Assets/Scenes/3D/RecoilPrototype.unity");if(!s.isLoaded)throw new Exception("Open outdoor scene first");var all=All<Transform>(s);var world=All<RecoilPrototype>(s).Single().transform;const string marker="Outdoor doors shutter and panel aligned to masonry";if(world.Find(marker))return "Already aligned";
  string backup="Backups/OutdoorWallFixtures/"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity";Directory.CreateDirectory(Path.GetDirectoryName(backup));EditorSceneManager.SaveScene(s,backup,true);
  var route=All<RecoilDoor>(s).Select(d=>d.name+"|"+d.destinationScene+"|"+d.destinationSpawn+"|"+(Vector2)d.transform.position).ToArray();var collisions=All<Collider2D>(s).Select(c=>c.GetInstanceID()+"|"+c.bounds).ToArray();
  var root=new GameObject(marker).transform;root.SetParent(world,false);float wall=2.88f;int doors=0;
  foreach(var d in All<RecoilDoor>(s).Where(d=>d.name=="Factory Entrance - Employee Card"||d.name=="Factory right service door"||d.name=="Enter warehouse"||d.name.Contains("side fire exit"))){
   var reveal=d.GetComponentsInChildren<MeshRenderer>(true).FirstOrDefault(r=>r.name=="Dark inner reveal");var frame=d.GetComponentsInChildren<MeshRenderer>(true).FirstOrDefault(r=>r.name=="3D mesh - Steel door frame");float back=reveal?reveal.bounds.max.z:frame?frame.bounds.max.z:2.07f;var p=d.transform.position;p.z+=wall+.012f-back;d.transform.position=p;EditorUtility.SetDirty(d);doors++;
  }
  // The store's newer glass door is already attached; only move it a few cm so the frame bites into masonry.
  var store=all.Single(t=>t.name=="Service store entrance");var storeFrame=store.GetComponentsInChildren<MeshRenderer>(true).First(r=>r.name=="Recessed aluminium jamb");var sp=store.position;sp.z+=wall+.012f-storeFrame.bounds.max.z;store.position=sp;
  var warehouse=all.Single(t=>t.name=="Factory Warehouse - exterior");var shutter=all.Single(t=>t.name=="Loading shutter");var shutterGroup=new GameObject("Warehouse rolling shutter - mounted to front wall").transform;shutterGroup.SetParent(warehouse,false);shutterGroup.position=new Vector3(shutter.position.x,0,wall);
  shutter.SetParent(shutterGroup,true);SurfaceDepth(shutter,wall-.07f,.10f);foreach(var t in all.Where(t=>t.name=="Shutter slat"&&t.IsChildOf(warehouse))){t.SetParent(shutterGroup,true);SurfaceDepth(t,wall-.137f,.025f);}
  var steel=AssetDatabase.LoadAssetAtPath<Material>("Assets/Recoil3D/ConflictRemnants/Pitted dark iron.mat");
  foreach(float side in new[]{-1f,1f})Box(shutterGroup,"Rolling shutter guide rail fixed to masonry",new Vector3(shutter.position.x+side*2.56f,2.4f,wall-.10f),new Vector3(.13f,4.84f,.24f),steel);
  Box(shutterGroup,"Roller shutter hood",new Vector3(shutter.position.x,4.92f,wall-.18f),new Vector3(5.35f,.25f,.42f),steel);
  var panel=all.Single(t=>t.name=="Warehouse distribution box - interactive");var pp=panel.position;pp.z=wall-.155f;panel.position=pp;
  foreach(Transform part in panel){switch(part.name){case "Metal enclosure":SurfaceDepth(part,wall-.155f,.32f);break;case "Recessed cover":SurfaceDepth(part,wall-.33f,.045f);break;case "Cover latch":SurfaceDepth(part,wall-.37f,.07f);break;case "Power symbol plate":SurfaceDepth(part,wall-.358f,.014f);break;case "Incoming conduit":SurfaceDepth(part,wall-.052f,.085f);break;}}
  // Inspection point remains on the same X/Y, and the cabinet's back penetrates masonry by 5mm.
  foreach(float y in new[]{.67f,1.53f})Box(panel,"Electrical cabinet wall fixing rail",new Vector3(pp.x,y,wall-.018f),new Vector3(.99f,.06f,.055f),steel);
  if(!route.SequenceEqual(All<RecoilDoor>(s).Select(d=>d.name+"|"+d.destinationScene+"|"+d.destinationSpawn+"|"+(Vector2)d.transform.position)))throw new Exception("Door route or XY changed");Physics2D.SyncTransforms();if(!collisions.SequenceEqual(All<Collider2D>(s).Select(c=>c.GetInstanceID()+"|"+c.bounds)))throw new Exception("Collision changed");
  var panelBack=panel.GetComponentsInChildren<MeshRenderer>(true).Single(r=>r.name=="3D mesh - Metal enclosure").bounds.max.z;if(Mathf.Abs(panelBack-wall)>.025f)throw new Exception("Panel not touching wall");
  EditorSceneManager.MarkSceneDirty(s);if(!EditorSceneManager.SaveScene(s))throw new Exception("Save failed");Directory.CreateDirectory("output/outdoor-wall-fixtures");string report="PASS: "+doors+" steel doors and store glass door attached to masonry; shutter exposed with guides and hood; panel body cover latch and conduits layered and wall mounted. All door XY/routes and collision unchanged. Backup: "+backup;File.WriteAllText("output/outdoor-wall-fixtures/verification.txt",report);return report;
 }
}
