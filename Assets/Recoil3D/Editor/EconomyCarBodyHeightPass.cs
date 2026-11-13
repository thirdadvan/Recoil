using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;

public static class EconomyCarBodyHeightPass
{
 const BindingFlags Flags=BindingFlags.NonPublic|BindingFlags.Static;
 public static string Apply()
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play first");
  var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
  var filters=scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<MeshFilter>(true)).Where(f=>AssetDatabase.GetAssetPath(f.sharedMesh).StartsWith("Assets/Recoil3D/EconomyVehicles/")&&f.GetComponent<Renderer>()&&f.GetComponent<Renderer>().enabled&&f.GetComponent<Renderer>().sharedMaterials.Any(m=>m&&m.name=="Exposed aged vehicle steel")).ToArray();
  if(filters.Any(f=>AssetDatabase.GetAssetPath(f.sharedMesh).Contains("LowBodyRevision")))return "Revision already applied; authored car positions preserved";
  Directory.CreateDirectory("Backups/CarBodyHeight");var backup="Backups/CarBodyHeight/"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity";EditorSceneManager.SaveScene(scene,backup,true);
  const string folder="Assets/Recoil3D/EconomyVehicles/LowBodyRevision";Directory.CreateDirectory(folder);AssetDatabase.Refresh();
  var builder=typeof(OpeningEconomyVehiclePass);var collisions=typeof(VehicleTraversalCollisionsPass);
  string[] fields={"steel","rubber","glass","inside","rust","lamp","red"};
  string[] matNames={"Exposed aged vehicle steel","Perished tyres","Dusty dark abandoned glazing","Dark torn interior","Old brown corrosion","Clouded headlight and plate","Broken dark red tail lamps"};
  for(int i=0;i<fields.Length;i++)builder.GetField(fields[i],Flags).SetValue(null,AssetDatabase.LoadAssetAtPath<Material>("Assets/Recoil3D/Opening/ApproachExpansion/"+matNames[i]+".mat"));
  int index=0,solid=0;var report=new System.Text.StringBuilder();
  foreach(var filter in filters)
  {
   var car=filter.transform;var mesh=filter.sharedMesh;var path=AssetDatabase.GetAssetPath(mesh);var materials=car.GetComponent<Renderer>().sharedMaterials;
   int modelType=path.Contains("microcar")?0:path.Contains("rounded car")?1:path.Contains("compact saloon")?2:path.Contains("work van")?3:car.parent.name=="Parked car"?2:car.parent.name.StartsWith("Wreck A")?0:1;
   int seed;if(path.Contains("Economy car "))seed=int.Parse(Path.GetFileName(path).Substring(12,2))-1;else seed=car.parent.name=="Parked car"?50:car.parent.name.StartsWith("Wreck A")?51:52;
   var facing=(Quaternion)builder.GetMethod("OriginalBakedFacing",Flags).Invoke(null,new object[]{mesh,materials});
   int steel=Array.FindIndex(materials,m=>m.name=="Exposed aged vehicle steel");var vertices=mesh.vertices;var used=mesh.GetTriangles(steel).Distinct().Take(24).ToArray();
   var center=used.Aggregate(Vector3.zero,(sum,i)=>sum+vertices[i])/used.Length;
   var paint=materials.First(m=>m.name.StartsWith("Faded ")||m.name=="Rust red delivery van"||m.name=="Burnt charcoal shell");
   var model=(Transform)builder.GetMethod("Build",Flags).Invoke(null,new object[]{car,modelType,seed,seed%4==1,paint});
   model.localRotation=facing;model.localPosition=center-facing*new Vector3(0,.42f,0);
   try
   {
    var existing=car.GetComponents<BoxCollider2D>();
    if(existing.Length>0)
    {
     var plans=new[]{"Lower body","Traversal bonnet","Traversal roof"}.Select(n=>(Bounds)collisions.GetMethod("PlaneSection",Flags).Invoke(null,new object[]{model.Find(n),0f})).ToArray();
     foreach(var c in existing)Object.DestroyImmediate(c);
     foreach(var b in plans)collisions.GetMethod("Add",Flags).Invoke(null,new object[]{car,b});solid++;
    }
    var args=new object[]{model,car,null};var result=(Mesh)builder.GetMethod("Bake",Flags).Invoke(null,args);
    var asset=AssetDatabase.GenerateUniqueAssetPath(folder+"/Low body car "+(++index).ToString("00")+".asset");result.name="Lowered body and recessed wheels - "+car.name;AssetDatabase.CreateAsset(result,asset);
    filter.sharedMesh=result;car.GetComponent<Renderer>().sharedMaterials=(Material[])args[2];EditorUtility.SetDirty(filter);EditorUtility.SetDirty(car.GetComponent<Renderer>());
    report.AppendLine(car.name+" | "+car.position+" | type="+modelType+" | original="+path);
   }
   finally{Object.DestroyImmediate(model.gameObject);}
  }
  Physics2D.SyncTransforms();AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
  string summary="Lowered "+index+" civilian vehicle bodies by model type (microcar10cm, rounded16cm, saloon14cm, van12cm); real wheel openings and recessed narrower tyres; all root transforms retained; refreshed "+solid+" vehicles' gameplay collision. Backup: "+backup;
  Directory.CreateDirectory("output/car-body-height");File.WriteAllText("output/car-body-height/verification.txt",summary+"\n"+report);return summary;
 }
}
