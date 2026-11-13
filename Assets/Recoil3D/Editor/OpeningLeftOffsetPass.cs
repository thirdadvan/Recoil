using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
public static class OpeningLeftOffsetPass
{
 static T[] All<T>(Scene s)where T:Component{return s.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<T>(true)).ToArray();}
 public static string Apply(){if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play first");var s=SceneManager.GetSceneByPath("Assets/Scenes/3D/RecoilPrototype.unity");if(!s.isLoaded)throw new Exception("Open outdoor first");var w=All<RecoilPrototype>(s).Single();const string marker="Opening left-side props offset 20m - extra pre-encounter space";if(w.transform.Find(marker))return "Already applied";
  string backup="Backups/OpeningLeftOffset/"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity";Directory.CreateDirectory(Path.GetDirectoryName(backup));EditorSceneManager.SaveScene(s,backup,true);
  var pumps=All<FuelPumpInteraction>(s).Select(t=>t.transform.position).ToArray();var enemies=All<RampagerController>(s).Select(t=>t.transform.position).ToArray();var doors=All<RecoilDoor>(s).Select(d=>d.name+"|"+d.destinationScene+"|"+d.destinationSpawn+"|"+d.transform.position).ToArray();var cars=All<MeshFilter>(s).Where(f=>f.name.StartsWith("Car - ")).ToDictionary(f=>f.transform,f=>f.transform.position);var shift=Vector3.left*20;
  foreach(string name in new[]{"Player Spawn","03 Props and Objectives/Parked car","Opening - roadside service station/Road - foreground to misty horizon","Roadside cutting - natural left boundary","Opening approach expanded 20m - editable","Curved forecourt approach and abandoned military checkpoint","Blocked civilian exodus - road barrier queue and crossing car"}){var t=w.transform.Find(name);if(!t)throw new Exception("Missing "+name);t.position+=shift;}
  // Keep the separately arranged right-side foreground cars beside the station.
  var east=w.transform.Find("Blocked civilian exodus - road barrier queue and crossing car/Queue and closure visual geometry/Right forecourt - failed overtaking and merging");east.position-=shift;
  var terrain=w.transform.Find("02 Terrain and Cover");var ground=terrain.GetComponentsInChildren<BoxCollider2D>(true).Single(c=>c.bounds.size.x>200&&c.bounds.size.y<2);var left=terrain.GetComponentsInChildren<BoxCollider2D>(true).Single(c=>c.bounds.size.x<1&&c.bounds.size.y>10&&c.bounds.center.x< -90);left.transform.position+=shift;
  var size=ground.size;size.x+=20/Mathf.Abs(ground.transform.lossyScale.x);ground.size=size;var offset=ground.offset;offset.x-=10/ground.transform.lossyScale.x;ground.offset=offset;
  foreach(var visual in ground.GetComponentsInChildren<Recoil3DSpriteVisual>(true).Where(v=>v.meshRenderer)){var t=visual.meshRenderer.transform;t.position+=Vector3.left*10;var scale=t.localScale;scale.x+=20/Mathf.Abs(t.parent.lossyScale.x);t.localScale=scale;EditorUtility.SetDirty(visual);}
  var floor=w.transform.Find("3D continuous foreground floor");floor.position+=Vector3.left*10;var fs=floor.localScale;fs.x+=20/Mathf.Abs(floor.parent.lossyScale.x);floor.localScale=fs;
  // Carry nearby roadside trees with the opening, then refit them to the existing hill surface.
  var trees=w.transform.Find("Station rear equipment yard and matching woodland - editable/Left forest - same spruce family as factory right");if(trees){foreach(Transform t in trees)if(t.name.StartsWith("Spruce matching")&&t.position.x< -55)t.position+=shift;ForecourtServiceYardPass.GroundExistingForest();}
  var root=new GameObject(marker).transform;root.SetParent(w.transform,false);var road=GameObject.CreatePrimitive(PrimitiveType.Cube);road.name="Extended clear road between left queue and forecourt";road.transform.SetParent(root,false);road.transform.position=new Vector3(-63.0f,-.009f,0);road.transform.localScale=new Vector3(20,.04f,22);Object.DestroyImmediate(road.GetComponent<Collider>());road.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Recoil3D/RoadCheckpoint/Blended aged entrance asphalt.mat");
  Physics2D.SyncTransforms();if(!pumps.SequenceEqual(All<FuelPumpInteraction>(s).Select(t=>t.transform.position)))throw new Exception("Pumps moved");if(!enemies.SequenceEqual(All<RampagerController>(s).Select(t=>t.transform.position)))throw new Exception("Enemies moved");if(!doors.SequenceEqual(All<RecoilDoor>(s).Select(d=>d.name+"|"+d.destinationScene+"|"+d.destinationSpawn+"|"+d.transform.position)))throw new Exception("Doors changed");
  int moved=0;foreach(var entry in cars){bool right=entry.Key.IsChildOf(east);var expected=entry.Value+(right?Vector3.zero:shift);if((entry.Key.position-expected).sqrMagnitude>.0001f)throw new Exception("Unexpected car offset "+entry.Key.name);if(!right)moved++;}
  EditorSceneManager.MarkSceneDirty(s);AssetDatabase.SaveAssets();if(!EditorSceneManager.SaveScene(s))throw new Exception("Save failed");Directory.CreateDirectory("output/opening-left-offset");string report="PASS opening spawn/start car/road/roadblock/army checkpoint/left queue and "+moved+" civilian cars shifted -20m;8 right station cars, pumps, store doors and enemies unchanged; ground expanded to left,20m linking road added. Spawn="+w.transform.Find("Player Spawn").position+". Backup: "+backup;File.WriteAllText("output/opening-left-offset/verification.txt",report);return report;
 }
}
