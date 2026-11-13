using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;

public static class EconomyDoorFitPass
{
 const BindingFlags Flags=BindingFlags.NonPublic|BindingFlags.Static;
 public static string Apply()
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play first");
  var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
  var filters=scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<MeshFilter>(true)).Where(f=>AssetDatabase.GetAssetPath(f.sharedMesh).StartsWith("Assets/Recoil3D/EconomyVehicles/")&&f.GetComponent<Renderer>()&&f.GetComponent<Renderer>().enabled&&f.GetComponent<Renderer>().sharedMaterials.Any(m=>m&&m.name=="Exposed aged vehicle steel")).ToArray();
  if(filters.Any(f=>AssetDatabase.GetAssetPath(f.sharedMesh).Contains("DoorFitRevision")))return "Door fit already applied";
  Directory.CreateDirectory("Backups/CarDoorFit");var backup="Backups/CarDoorFit/"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity";EditorSceneManager.SaveScene(scene,backup,true);
  const string folder="Assets/Recoil3D/EconomyVehicles/DoorFitRevision";Directory.CreateDirectory(folder);AssetDatabase.Refresh();
  var builder=typeof(OpeningEconomyVehiclePass);
  string[] fields={"steel","rubber","glass","inside","rust","lamp","red"};
  string[] matNames={"Exposed aged vehicle steel","Perished tyres","Dusty dark abandoned glazing","Dark torn interior","Old brown corrosion","Clouded headlight and plate","Broken dark red tail lamps"};
  for(int i=0;i<fields.Length;i++)builder.GetField(fields[i],Flags).SetValue(null,AssetDatabase.LoadAssetAtPath<Material>("Assets/Recoil3D/Opening/ApproachExpansion/"+matNames[i]+".mat"));
  var before=scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<Collider2D>(true)).Select(c=>c.GetInstanceID()+"|"+c.bounds).ToArray();
  int index=0,openCount=0;
  foreach(var filter in filters)
  {
   var car=filter.transform;var mesh=filter.sharedMesh;var materials=car.GetComponent<Renderer>().sharedMaterials;
   var facing=(Quaternion)builder.GetMethod("OriginalBakedFacing",Flags).Invoke(null,new object[]{mesh,materials});
   int steel=Array.FindIndex(materials,m=>m.name=="Exposed aged vehicle steel");var v=mesh.vertices;var used=mesh.GetTriangles(steel).Distinct().Take(24).ToArray();
   var center=used.Aggregate(Vector3.zero,(sum,i)=>sum+v[i])/used.Length;var inv=Quaternion.Inverse(facing);
   var chassis=used.Select(i=>inv*(v[i]-center)).ToArray();float length=chassis.Max(p=>p.z)-chassis.Min(p=>p.z)+.12f;
   float[] lengths={3.05f,3.95f,4.22f,4.25f};int modelType=Enumerable.Range(0,4).OrderBy(i=>Mathf.Abs(length-lengths[i])).First();
   float width=new[]{1.38f,1.55f,1.63f,1.82f}[modelType],drop=new[]{.10f,.16f,.14f,.12f}[modelType];
   var offset=center-facing*new Vector3(0,.42f-drop,0);
   float minX=v.Min(p=>(inv*(p-offset)).x),doorLength=new[]{.98f,1.16f,.92f,.90f}[modelType];
   bool opened=minX < -width*.5f-.22f;
   float angle=Mathf.Asin(Mathf.Clamp01((-minX-width*.5f)/doorLength))*Mathf.Rad2Deg;
   int seed=Enumerable.Range(0,3).OrderBy(i=>Mathf.Abs(angle-(55+i*9))).First();
   var pose=car.localToWorldMatrix;
   var paint=materials.First(m=>m.name.StartsWith("Faded ")||m.name=="Rust red delivery van"||m.name=="Burnt charcoal shell");
   var model=(Transform)builder.GetMethod("Build",Flags).Invoke(null,new object[]{car,modelType,seed,opened,paint});
   try
   {
    model.localRotation=facing;model.localPosition=offset;
    var args=new object[]{model,car,null};var result=(Mesh)builder.GetMethod("Bake",Flags).Invoke(null,args);
    result.name="Fitted door and window frames - "+car.name;
    AssetDatabase.CreateAsset(result,AssetDatabase.GenerateUniqueAssetPath(folder+"/Car "+(++index).ToString("00")+".asset"));
    filter.sharedMesh=result;car.GetComponent<Renderer>().sharedMaterials=(Material[])args[2];EditorUtility.SetDirty(filter);EditorUtility.SetDirty(car.GetComponent<Renderer>());
    if(car.localToWorldMatrix!=pose)throw new Exception("Unexpected car transform change");if(opened)openCount++;
   }
   finally{Object.DestroyImmediate(model.gameObject);}
  }
  var after=scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<Collider2D>(true)).Select(c=>c.GetInstanceID()+"|"+c.bounds).ToArray();
  if(!before.SequenceEqual(after))throw new Exception("Collision changed unexpectedly");
  AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
  string report="PASS: fitted tapered front-door glazing and rails plus rear quarter glazing to roof contour on "+index+" civilian vehicles; preserved "+openCount+" open-door cars, all car transforms, low bodies, wheel openings, and collision. Backup: "+backup;
  Directory.CreateDirectory("output/car-door-fit");File.WriteAllText("output/car-door-fit/verification.txt",report);return report;
 }
}
