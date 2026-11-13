using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

public static class FactoryOfficeWindowAlignment
{
 const string Marker="Office windows aligned to 3D facade";
 static T[] All<T>(Scene s)where T:Component{return s.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<T>(true)).ToArray();}
 static Recoil3DSpriteVisual Surface(Transform t){return t.GetComponentsInChildren<Recoil3DSpriteVisual>(true).First(v=>!v.suppressed&&v.meshRenderer);}
 static void Depth(Transform t,float centre,float thickness){var v=Surface(t);v.depth=thickness/Mathf.Abs(t.lossyScale.z);v.depthOffset=v.transform.InverseTransformPoint(new Vector3(v.transform.position.x,v.transform.position.y,centre)).z;v.Sync();EditorUtility.SetDirty(v);}
 public static string Apply()
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play first");var original=SceneManager.GetActiveScene();const string path="Assets/Scenes/3D/RecoilPrototype.unity";var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
  try{var all=All<Transform>(scene);var building=all.Single(t=>t.name=="Factory Building - four storeys");var detail=building.Find("Facade - editable office windows and structural details");if(detail.Find(Marker))return "Already aligned";
   string backup="Backups/FactoryOfficeWindows/"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity";Directory.CreateDirectory(Path.GetDirectoryName(backup));EditorSceneManager.SaveScene(scene,backup,true);
   var routes=All<RecoilDoor>(scene).Select(d=>d.name+"|"+d.destinationScene+"|"+d.destinationSpawn+"|"+d.transform.position).ToArray();var collisions=All<Collider2D>(scene).Select(c=>c.GetInstanceID()+"|"+c.bounds).ToArray();
   var facade=building.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Factory main facade");float wall=Surface(facade).meshRenderer.bounds.min.z;
   var windowParts=detail.Cast<Transform>().Where(t=>new[]{"Window recessed lintel","Dirty office glazing","Steel window jamb","Window centre mullion","Window sill","Transom"}.Contains(t.name)).ToArray();
   var glass=windowParts.Where(t=>t.name=="Dirty office glazing").OrderBy(t=>t.position.y).ThenBy(t=>t.position.x).ToArray();
   var root=new GameObject(Marker).transform;root.SetParent(detail,false);int boards=0,index=0;
   foreach(var pane in glass){var centre=pane.position;float height=Surface(pane).meshRenderer.bounds.size.y;var group=new GameObject("Office window "+(++index)+" - glass frame and fixed boarding").transform;group.SetParent(root,false);group.position=new Vector3(centre.x,centre.y,wall-.08f);
    var parts=windowParts.Where(t=>Mathf.Abs(t.position.x-centre.x)<1.35f&&Mathf.Abs(t.position.y-centre.y)<height*.5f+.3f).ToArray();foreach(var part in parts){part.SetParent(group,true);switch(part.name){case "Window recessed lintel":Depth(part,wall-.035f,.05f);break;case "Dirty office glazing":Depth(part,wall-.085f,.035f);break;case "Window sill":Depth(part,wall-.18f,.34f);break;default:Depth(part,wall-.15f,.12f);break;}}
    var planks=all.Where(t=>t.name=="Salvaged plank across factory glazing"&&Mathf.Abs(t.position.x-centre.x)<.1f&&Mathf.Abs(t.position.y-centre.y)<height*.5f+.5f).OrderBy(t=>t.position.y).ToArray();
    var sheets=all.Where(t=>t.name=="Strip cut from coiled steel plate"&&Mathf.Abs(t.position.x-centre.x)<1.05f&&Mathf.Abs(t.position.y-centre.y)<height*.5f+.5f).OrderBy(t=>t.position.x).ToArray();
    for(int i=0;i<planks.Length;i++){var t=planks[i];t.SetParent(group,true);t.position=new Vector3(centre.x,centre.y+(i-(planks.Length-1)*.5f)*height*.32f,wall-.265f);boards++;}
    foreach(var t in sheets){t.SetParent(group,true);var p=t.position;p.y=centre.y-.05f;p.z=wall-.35f;t.position=p;var scale=t.localScale;scale.y=Mathf.Min(1.6f,height*.82f);t.localScale=scale;boards++;}
    var glazing=Surface(pane).meshRenderer.bounds;if(glazing.max.z>=wall-.02f)throw new Exception("Glazing remains buried");foreach(var t in planks){var bounds=t.GetComponent<Renderer>().bounds;float gap=Surface(parts.First(x=>x.name=="Window centre mullion")).meshRenderer.bounds.min.z-bounds.max.z;if(gap<-.01f||gap>.03f)throw new Exception("Boarding not touching window frame: "+gap);}
   }
   if(boards!=128||index!=16)throw new Exception("Unexpected window / boarding count "+index+"/"+boards);
   if(!routes.SequenceEqual(All<RecoilDoor>(scene).Select(d=>d.name+"|"+d.destinationScene+"|"+d.destinationSpawn+"|"+d.transform.position)))throw new Exception("Door routes changed");if(!collisions.SequenceEqual(All<Collider2D>(scene).Select(c=>c.GetInstanceID()+"|"+c.bounds)))throw new Exception("Collision changed");
   EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new Exception("Save failed");Directory.CreateDirectory("output/factory-office-windows");string report="PASS "+index+" office windows exposed in front of facade Z="+wall+"; "+boards+" boards and steel strips fitted to frames, glass/frame/boarding have separate depths; original collision and doors unchanged. Backup: "+backup;File.WriteAllText("output/factory-office-windows/verification.txt",report);return report;
  }finally{if(opened)EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(original);}
 }
}
