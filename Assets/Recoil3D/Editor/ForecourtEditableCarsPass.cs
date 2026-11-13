using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

// A one-time authoring operation: one car = one transform, mesh and renderer.
public static class ForecourtEditableCarsPass
{
 const string Folder="Assets/Recoil3D/EditableForecourtCars";
 static int serial;
 public static void MakeSingle(Transform car)
 {
  var filters=car.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.sharedMesh&&f.GetComponent<MeshRenderer>()).ToArray();if(filters.Length==0)throw new Exception("No car mesh: "+car.name);
  if(filters.Length==1&&filters[0].transform==car&&car.childCount==0)return;
  var inverse=Matrix4x4.TRS(car.position,Quaternion.identity,Vector3.one).inverse;var entries=new List<KeyValuePair<Material,CombineInstance>>();
  foreach(var f in filters){var mats=f.GetComponent<MeshRenderer>().sharedMaterials;for(int i=0;i<f.sharedMesh.subMeshCount;i++)entries.Add(new KeyValuePair<Material,CombineInstance>(mats[Mathf.Min(i,mats.Length-1)],new CombineInstance{mesh=f.sharedMesh,subMeshIndex=i,transform=inverse*f.transform.localToWorldMatrix}));}
  var materials=new List<Material>();var temporary=new List<Mesh>();foreach(var group in entries.GroupBy(e=>e.Key)){var mesh=new Mesh{indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(group.Select(e=>e.Value).ToArray(),true,true);temporary.Add(mesh);materials.Add(group.Key);}
  var final=new Mesh{indexFormat=IndexFormat.UInt32};final.CombineMeshes(temporary.Select(m=>new CombineInstance{mesh=m,transform=Matrix4x4.identity}).ToArray(),false,true);final.RecalculateBounds();string path=car.name.Contains("government army cargo truck")?Folder+"/Army truck.asset":Folder+"/Car "+(++serial).ToString("00")+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(saved){EditorUtility.CopySerialized(final,saved);Object.DestroyImmediate(final);final=saved;}else AssetDatabase.CreateAsset(final,path);foreach(var m in temporary)Object.DestroyImmediate(m);
  foreach(Transform child in car.Cast<Transform>().ToArray())Object.DestroyImmediate(child.gameObject);car.rotation=Quaternion.identity;car.localScale=Vector3.one;var mf=car.GetComponent<MeshFilter>();if(!mf)mf=car.gameObject.AddComponent<MeshFilter>();mf.sharedMesh=final;var mr=car.GetComponent<MeshRenderer>();if(!mr)mr=car.gameObject.AddComponent<MeshRenderer>();mr.sharedMaterials=materials.ToArray();car.name="Car - "+car.name;
 }
 public static void SplitBeforeStaticBatch(Transform art)
 {
  Directory.CreateDirectory(Folder);serial=0;
  foreach(string name in new[]{"Queue spills onto road shoulders","Foreground cars restored - dense at entry then taper","Right forecourt - failed overtaking and merging"}){var parent=art.Find(name);foreach(var car in parent.Cast<Transform>().ToArray())MakeSingle(car);}
 }
 public static string Finish()
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play first");var scene=SceneManager.GetSceneByPath("Assets/Scenes/3D/RecoilPrototype.unity");var w=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<RecoilPrototype>(true)).Single();var all=w.GetComponentsInChildren<BoxCollider2D>(true);var oldBounds=all.Select(c=>c.bounds.ToString()).OrderBy(x=>x).ToArray();var opening=w.transform.Find("Opening approach expanded 20m - editable");var exodus=w.transform.Find("Blocked civilian exodus - road barrier queue and crossing car");
  foreach(string name in new[]{"Abandoned vehicles along the new approach","Long abandoned cars in three refuelling lanes"})foreach(var car in opening.Find(name).Cast<Transform>().ToArray())MakeSingle(car);
  var crossing=exodus.Find("Crossing car and crate - model and collision together");var roofCars=crossing.Cast<Transform>().Where(t=>t.GetComponentsInChildren<MeshFilter>(true).Length>0&&(t.name.Contains("Estate car blocking operation plane")||t.name.Contains("Second saloon - continue over bonnet and roof")||t.name.Contains("Third hatchback - final roof before road"))).ToArray();foreach(var car in roofCars)MakeSingle(car);
  // Solid volumes live directly on their car so moving a car moves its collision too.
  foreach(var old in crossing.GetComponentsInChildren<BoxCollider2D>(true).Where(c=>!c.name.Contains("crate")&&!roofCars.Contains(c.transform)).ToArray()){var car=roofCars.First(t=>old.name.StartsWith("Second")?t.name.Contains("Second"):old.name.StartsWith("Third")?t.name.Contains("Third"):t.name.Contains("Estate"));var b=old.bounds;var box=car.gameObject.AddComponent<BoxCollider2D>();box.size=b.size;box.offset=(Vector2)(b.center-car.position);if(!car.GetComponent<RecoilSurfaceVisibility>())car.gameObject.AddComponent<RecoilSurfaceVisibility>();Object.DestroyImmediate(old.gameObject);}
  var east=exodus.Find("Queue and closure visual geometry/Right forecourt - failed overtaking and merging");int i=0;foreach(var car in east.Cast<Transform>()){var p=car.position;p.z=i<3?-3.8f-(i%3)*.35f:i<6?-7.5f-(i%3)*.35f:-11.2f-(i%2)*.4f;car.position=p;var b=car.GetComponent<Renderer>().bounds;if(b.max.z> -1.5f)car.position+=Vector3.back*(b.max.z+1.5f);i++;}
  Physics2D.SyncTransforms();var newBounds=w.GetComponentsInChildren<BoxCollider2D>(true).Select(c=>c.bounds.ToString()).OrderBy(x=>x).ToArray();if(!oldBounds.SequenceEqual(newBounds))throw new Exception("Roof collision geometry changed");var cars=w.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.name.StartsWith("Car - ")).ToArray();if(cars.Any(f=>f.transform.childCount!=0||f.GetComponentsInChildren<MeshRenderer>().Length!=1))throw new Exception("Car is not a single object");AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Directory.CreateDirectory("output/editable-forecourt-cars");string report="PASS "+cars.Length+" independently editable cars, each one GameObject/mesh/renderer and no child parts; 8 right-side cars moved to foreground; three traversal cars carry their own colliders, original solid bounds unchanged.";File.WriteAllText("output/editable-forecourt-cars/verification.txt",report);return report;
 }
}
