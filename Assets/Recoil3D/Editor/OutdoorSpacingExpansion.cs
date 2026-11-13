using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
public static class OutdoorSpacingExpansion
{
 static T[] All<T>(Scene s)where T:Component{return s.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<T>(true)).ToArray();}
 public static string Apply(){if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play first");var scene=SceneManager.GetSceneByPath("Assets/Scenes/3D/RecoilPrototype.unity");if(!scene.isLoaded)throw new Exception("Open outdoor first");var w=All<RecoilPrototype>(scene).Single();const string marker="Factory left gap expanded 30m and store offset 8m";if(w.transform.Find(marker))return "Already applied";
  string backup="Backups/OutdoorSpacing/"+DateTime.Now.ToString("yyyyMMdd-HHmmss");Directory.CreateDirectory(backup);EditorSceneManager.SaveScene(scene,backup+"/RecoilPrototype.unity",true);
  var factory=w.transform.Find("Factory Building - four storeys");var tower=w.transform.Find("Broadcast Tower");Vector3 towerPos=tower.position;var pumps=w.GetComponentsInChildren<FuelPumpInteraction>(true);var pumpPositions=pumps.Select(p=>p.transform.position).ToArray();var doors=w.GetComponentsInChildren<RecoilDoor>(true);var routes=doors.Select(d=>d.destinationScene+"|"+d.destinationSpawn).ToArray();
  var move=new List<Transform>();foreach(string name in new[]{"Factory Checkpoint","Truck - soft canvas roof","Factory Building - four storeys","Factory warehouse yard and sewer - editable","3D factory yard - editable perimeter truck and gate","Factory yard - relocated midground industry","Factory right yard - three street lights one working","Residential approach - forest river checkpoint and housing","1978 conflict aftermath - editable","Factory rear gate and old sandbags - editable","1978 outdoor repaired breach and cleared barricades - editable"}){var t=w.transform.Find(name);if(!t)throw new Exception("Missing root "+name);move.Add(t);}
  var depth=w.transform.Find("Depth revision - continuous barriers and selective light");move.Add(depth.Find("Continuous iron gate - locked across yard depth"));move.Add(depth.Find("Distant steelworks - furnace stoves conveyors chimneys"));
  foreach(string name in new[]{"03 Props and Objectives","05 Lighting"})foreach(Transform t in w.transform.Find(name))if(t.position.x>=14)move.Add(t);
  foreach(Transform t in w.transform.Find("04 Characters"))if(t.position.x>=14)move.Add(t);
  var terrain=w.transform.Find("02 Terrain and Cover");var ground=terrain.GetComponentsInChildren<BoxCollider2D>(true).Single(c=>c.bounds.size.x>150&&c.bounds.size.y<2);foreach(Transform t in terrain)if(t!=ground.transform&&t.position.x>=14)move.Add(t);
  var distinct=move.Distinct().Where(t=>!move.Any(p=>p!=t&&t.IsChildOf(p))).ToArray();foreach(var t in distinct)t.position+=Vector3.right*30;
  // Extend terrain and floor to the right without moving the opening or tower.
  var size=ground.size;size.x+=30/Mathf.Abs(ground.transform.lossyScale.x);ground.size=size;var off=ground.offset;off.x+=15/ground.transform.lossyScale.x;ground.offset=off;
  foreach(var v in ground.GetComponentsInChildren<Recoil3DSpriteVisual>(true).Where(v=>v.meshRenderer)){var r=v.meshRenderer;var p=r.transform.position;p.x+=15;r.transform.position=p;var scale=r.transform.localScale;scale.x+=30/Mathf.Abs(r.transform.parent.lossyScale.x);r.transform.localScale=scale;EditorUtility.SetDirty(v);}
  var floor=w.transform.Find("3D continuous foreground floor");floor.position+=Vector3.right*15;var fs=floor.localScale;fs.x+=30/Mathf.Abs(floor.parent.lossyScale.x);floor.localScale=fs;
  var store=w.transform.Find("Service Station - editable exterior");foreach(Transform t in store)if(!t.GetComponent<RecoilFuelStation>())t.position+=Vector3.right*8;
  foreach(string name in new[]{"Store Outside Door Spawn","Service store - three roaming sentries and warning","Tutorial - Shift Stealth Walk"}){var t=w.transform.Find(name);if(t)t.position+=Vector3.right*8;}
  foreach(Transform t in w.transform.Find("03 Props and Objectives"))if(t.name.StartsWith("Street Lamp")&&t.position.x<0&&t.position.x> -32)t.position+=Vector3.right*8;
  foreach(Transform t in w.transform.Find("04 Characters"))if(t.position.x<0&&t.position.x> -32)t.position+=Vector3.right*8;
  // Choose the rightmost middle-lane dispenser; black screens are independent of damage/nozzle state.
  var lit=pumps.Where(p=>!p.startsBroken).OrderByDescending(p=>p.transform.position.x).First();foreach(var p in pumps){p.screenBlack=p!=lit;if(p.serviceDisplay)p.serviceDisplay.SetActive(p==lit&&!p.startsBroken);if(p.outOfOrderDisplay)p.outOfOrderDisplay.SetActive(false);EditorUtility.SetDirty(p);}
  foreach(var d in w.GetComponentsInChildren<Recoil3DFuelPumpDisplay>(true)){d.screenBlack=d.source!=lit||Mathf.Abs(d.transform.position.z-lit.transform.position.z)>2;d.Sync();EditorUtility.SetDirty(d);}
  var mark=new GameObject(marker);mark.transform.SetParent(w.transform,false);Physics2D.SyncTransforms();if(tower.position!=towerPos)throw new Exception("Tower moved");if(!pumpPositions.SequenceEqual(pumps.Select(p=>p.transform.position)))throw new Exception("Pump rows moved");if(!routes.SequenceEqual(doors.Select(d=>d.destinationScene+"|"+d.destinationSpawn)))throw new Exception("Door route changed");
  var gate=w.transform.Find("Depth revision - continuous barriers and selective light/Continuous iron gate - locked across yard depth");if(gate.GetComponentsInChildren<BoxCollider2D>(true).Any(c=>Mathf.Abs(c.bounds.center.x-gate.position.x)>.01f))throw new Exception("Gate collision separated");
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);int interiors=0;
  // Keep indoor broadcast emitters at the tower's actual relative distance after expanding the outdoor gap.
  foreach(var entry in EditorBuildSettings.scenes.Where(e=>e.enabled&&e.path.StartsWith("Assets/Scenes/3D/")&&e.path!=scene.path)){var s=SceneManager.GetSceneByPath(entry.path);bool opened=!s.isLoaded;if(opened)s=EditorSceneManager.OpenScene(entry.path,OpenSceneMode.Additive);try{var waves=All<SoundWaveManager>(s).FirstOrDefault(q=>q.buildingBroadcastsEnabled);if(!waves)continue;EditorSceneManager.SaveScene(s,backup+"/"+s.name+".unity",true);var source=waves.transform.Find("Broadcast Speaker");if(source){source.position-=Vector3.right*30;EditorSceneManager.MarkSceneDirty(s);EditorSceneManager.SaveScene(s);interiors++;}}finally{if(opened)EditorSceneManager.CloseScene(s,true);}}
  SceneManager.SetActiveScene(scene);var world=All<Recoil3DWorld>(scene).FirstOrDefault();if(world)world.ApplyAtmosphere();AssetDatabase.SaveAssets();Directory.CreateDirectory("output/outdoor-spacing");string report="PASS factory and enclosure shifted +30m; tower and pumps retained; store +8m; ground extended +30m; gate collision aligned; all routes unchanged; indoor broadcast sources updated: "+interiors+". Sole lit pump: "+lit.name+" "+lit.transform.position+". Backup: "+backup;File.WriteAllText("output/outdoor-spacing/verification.txt",report);return report;
 }
}
