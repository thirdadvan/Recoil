using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;

public static class VehicleTraversalCollisionsPass
{
 static readonly BindingFlags PrivateStatic=BindingFlags.NonPublic|BindingFlags.Static;
 static Bounds PlaneSection(Transform part,float z)
 {
  var filter=part.GetComponent<MeshFilter>();var v=filter.sharedMesh.vertices;var indices=filter.sharedMesh.triangles;
  var points=new List<Vector3>();
  for(int i=0;i<indices.Length;i+=3)
  {
   var triangle=new[]{part.TransformPoint(v[indices[i]]),part.TransformPoint(v[indices[i+1]]),part.TransformPoint(v[indices[i+2]])};
   for(int j=0;j<3;j++)
   {
    var a=triangle[j];var b=triangle[(j+1)%3];
    if(Mathf.Abs(a.z-z)<.0001f)points.Add(a);
    if((a.z-z)*(b.z-z)<0)points.Add(Vector3.Lerp(a,b,(z-a.z)/(b.z-a.z)));
   }
  }
  if(points.Count==0)throw new Exception("Part does not intersect gameplay plane: "+part.name);
  var bounds=new Bounds(points[0],Vector3.zero);foreach(var p in points)bounds.Encapsulate(p);return bounds;
 }
 static BoxCollider2D Add(Transform car,Bounds world)
 {
  // BoxCollider2D uses the parent's projected axes. Compensate for the user's 3D yaw/tilt.
  var m=car.localToWorldMatrix;float determinant=m.m00*m.m11-m.m01*m.m10;
  if(Mathf.Abs(determinant)<.05f)throw new Exception("Car faces too close to camera for this collider projection");
  Vector2 d=(Vector2)(world.center-car.position);
  var col=car.gameObject.AddComponent<BoxCollider2D>();
  col.offset=new Vector2((d.x*m.m11-m.m01*d.y)/determinant,(m.m00*d.y-d.x*m.m10)/determinant);
  float a=Mathf.Abs(m.m00),b=Mathf.Abs(m.m01),c=Mathf.Abs(m.m10),e=Mathf.Abs(m.m11),det=a*e-b*c;
  col.size=new Vector2((world.size.x*e-b*world.size.y)/det,(a*world.size.y-c*world.size.x)/det);
  return col;
 }
 public static string Apply()
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play first");
  var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
  var all=scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<Transform>(true)).ToArray();
  string[] names={"Car - Foreground congested exit car 0","Car - Refuelling lane 2 abandoned car 3"};
  var cars=names.Select(n=>all.Single(t=>t.name==n)).ToArray();
  if(cars.Any(t=>t.GetComponents<Collider2D>().Length>0))throw new Exception("Target already has collision; no changes made");
  Directory.CreateDirectory("Backups/ExtraTraversalCars");var backup="Backups/ExtraTraversalCars/"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity";EditorSceneManager.SaveScene(scene,backup,true);
  var type=typeof(OpeningEconomyVehiclePass);
  string[] fields={"steel","rubber","glass","inside","rust","lamp","red"};
  string[] mats={"Exposed aged vehicle steel","Perished tyres","Dusty dark abandoned glazing","Dark torn interior","Old brown corrosion","Clouded headlight and plate","Broken dark red tail lamps"};
  for(int i=0;i<fields.Length;i++)type.GetField(fields[i],PrivateStatic).SetValue(null,AssetDatabase.LoadAssetAtPath<Material>("Assets/Recoil3D/Opening/ApproachExpansion/"+mats[i]+".mat"));
  var plans=new Dictionary<Transform,Bounds[]>();
  foreach(var car in cars)
  {
   var mesh=car.GetComponent<MeshFilter>().sharedMesh;var materials=car.GetComponent<Renderer>().sharedMaterials;
   var facing=(Quaternion)type.GetMethod("OriginalBakedFacing",PrivateStatic).Invoke(null,new object[]{mesh,materials});
   int steel=Array.FindIndex(materials,m=>m.name=="Exposed aged vehicle steel");var v=mesh.vertices;
   var used=mesh.GetTriangles(steel).Distinct().Take(24).ToArray();var center=used.Aggregate(Vector3.zero,(sum,i)=>sum+v[i])/used.Length;
   int index=car==cars[0]?29:18,modelType=car==cars[0]?3:2;
   var paint=materials.First(m=>m.name.StartsWith("Faded ")||m.name=="Rust red delivery van"||m.name=="Burnt charcoal shell");
   var temporary=(Transform)type.GetMethod("Build",PrivateStatic).Invoke(null,new object[]{car,modelType,index,index%4==1,paint});
   try
   {
    temporary.localRotation=facing;temporary.localPosition=center-facing*new Vector3(0,.42f,0);
    plans.Add(car,new[]{"Lower body","Traversal bonnet","Traversal roof"}.Select(n=>PlaneSection(temporary.Find(n),0)).ToArray());
   }
   finally{Object.DestroyImmediate(temporary.gameObject);}
  }
  foreach(var plan in plans)
  {
   foreach(var b in plan.Value)Add(plan.Key,b);
   var sight=plan.Key.GetComponent<RecoilSurfaceVisibility>();if(!sight)sight=plan.Key.gameObject.AddComponent<RecoilSurfaceVisibility>();sight.blocksSight=false;
   EditorUtility.SetDirty(plan.Key.gameObject);
  }
  Physics2D.SyncTransforms();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
  var report="Added body, bonnet and roof collision to both moved cars, using model sections at gameplay Z=0; all authored transforms retained. Backup: "+backup+"\n"+string.Join("\n",cars.Select(t=>t.name+" "+t.position+"\n"+string.Join("\n",t.GetComponents<BoxCollider2D>().Select(c=>c.bounds.ToString()).ToArray())).ToArray());
  Directory.CreateDirectory("output/extra-traversal-cars");File.WriteAllText("output/extra-traversal-cars/verification.txt",report);return report;
 }
}
