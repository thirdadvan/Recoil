using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

// Authoring only: retain every existing car transform, replace its baked geometry.
public static class OpeningEconomyVehiclePass
{
 const string Folder="Assets/Recoil3D/EconomyVehicles";
 const string Marker="Factory rear-gate approach road - economy vehicle revision";
 static Material steel,rubber,glass,inside,rust,lamp,red;
 static Material Load(string name){return AssetDatabase.LoadAssetAtPath<Material>("Assets/Recoil3D/Opening/ApproachExpansion/"+name+".mat");}
 static Transform Group(Transform parent,string name,Vector3 at){var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=at;return t;}
 static Transform Box(Transform parent,string name,Vector3 at,Vector3 size,Material material,PrimitiveType type=PrimitiveType.Cube)
 {
  var t=GameObject.CreatePrimitive(type).transform;t.name=name;t.SetParent(parent,false);t.localPosition=at;t.localScale=size;
  Object.DestroyImmediate(t.GetComponent<Collider>());t.GetComponent<Renderer>().sharedMaterial=material;return t;
 }
 static void Rod(Transform parent,Vector3 a,Vector3 b,float r,Material material)
 {var t=Box(parent,"Thin frame",(a+b)*.5f,new Vector3(r*2,(a-b).magnitude*.5f,r*2),material,PrimitiveType.Cylinder);t.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);}
 static void Window(Transform parent,string name,Vector3 a,Vector3 b,Vector3 c,Vector3 d)
 {
  var mesh=new Mesh();mesh.vertices=new[]{a,b,c,d,a,b,c,d};mesh.triangles=new[]{0,1,2,0,2,3,6,5,4,7,6,4};mesh.RecalculateNormals();mesh.RecalculateBounds();
  var t=Group(parent,name,Vector3.zero);t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=glass;
 }
 static Mesh Save(string name,Mesh mesh){mesh.name=name;AssetDatabase.CreateAsset(mesh,AssetDatabase.GenerateUniqueAssetPath(Folder+"/"+name+".asset"));return mesh;}
 // Rings: Z, half width, top height, bottom height. Small bevels avoid the previous stretched box silhouette.
 static Transform Hull(Transform parent,string name,Vector4[] rings,Material material)
 {
  var vertices=new List<Vector3>();var triangles=new List<int>();
  foreach(var r in rings){vertices.Add(new Vector3(-r.y,r.w,r.x));vertices.Add(new Vector3(r.y,r.w,r.x));vertices.Add(new Vector3(r.y*.98f,r.z,r.x));vertices.Add(new Vector3(-r.y*.98f,r.z,r.x));}
  for(int i=0;i<rings.Length-1;i++)for(int j=0;j<4;j++){int a=i*4+j,b=i*4+(j+1)%4,c=b+4,d=a+4;triangles.AddRange(new[]{a,b,c,a,c,d});}
  triangles.AddRange(new[]{0,3,2,0,2,1});int last=(rings.Length-1)*4;triangles.AddRange(new[]{last,last+1,last+2,last,last+2,last+3});
  var mesh=new Mesh();mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
  var t=Group(parent,name,Vector3.zero);t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=material;return t;
 }
 static Quaternion OriginalBakedFacing(Mesh mesh,Material[] materials)
 {
  int steelIndex=Array.FindIndex(materials,m=>m&&m.name=="Exposed aged vehicle steel");
  if(steelIndex<0)throw new Exception("Missing chassis reference");
  var v=mesh.vertices;var indices=mesh.GetTriangles(steelIndex).Distinct().Take(24).ToArray();
  var center=indices.Aggregate(Vector3.zero,(sum,i)=>sum+v[i])/indices.Length;
  float xx=0,zz=0,xz=0;foreach(int i in indices){var p=v[i]-center;xx+=p.x*p.x;zz+=p.z*p.z;xz+=p.x*p.z;}
  float angle=.5f*Mathf.Atan2(2*xz,zz-xx);var forward=new Vector3(Mathf.Sin(angle),0,Mathf.Cos(angle));
  int lightIndex=Array.FindIndex(materials,m=>m&&m.name=="Clouded headlight and plate");
  if(lightIndex>=0){var front=mesh.GetTriangles(lightIndex).Distinct().Select(i=>v[i]).Aggregate(Vector3.zero,(sum,p)=>sum+p)/mesh.GetTriangles(lightIndex).Distinct().Count();if(Vector3.Dot(front-center,forward)>0)forward=-forward;}
  var normals=mesh.normals;var up=normals[indices.OrderByDescending(i=>normals[i].y).First()];
  return Quaternion.LookRotation(forward,up);
 }
 static Transform Build(Transform parent,int type,int seed,bool open,Material paint)
 {
  float length=type==0?3.05f:type==1?3.95f:type==2?4.22f:4.25f;
  float width=type==0?1.38f:type==1?1.55f:type==2?1.63f:1.82f;
  float height=type==0?1.40f:type==1?1.50f:type==2?1.44f:1.95f;
  float front=-length*.5f,back=length*.5f;
  float cf=front+(type==0?.40f:type==3?.48f:1.02f),cb=type==0?back-.32f:type==1?back-.78f:type==2?back-.88f:back-.18f;
  var model=Group(parent,"New economy car geometry",Vector3.zero);
  Box(model,"Chassis",new Vector3(0,.42f,0),new Vector3(width*.92f,.14f,length-.12f),steel);
  float wheelCenterZ=length*.5f-(type==0?.49f:.64f),wheelRadius=type==0?.245f:type==3?.31f:.28f;
  float bodyDrop=type==0?.10f:type==1?.16f:type==2?.14f:.12f;
  // Actual wheel openings, rather than complete wheels hanging below a flat high body.
  var bodyRings=new List<Vector4>{new Vector4(front,width*.43f,.76f,.40f)};
  foreach(float centerZ in new[]{-wheelCenterZ,wheelCenterZ})
   for(int k=0;k<=6;k++)
   {
    float angle=Mathf.PI-k*Mathf.PI/6f,z=centerZ+Mathf.Cos(angle)*(wheelRadius+.07f);
    float bottom=Mathf.Max(.39f,wheelRadius+bodyDrop+Mathf.Sin(angle)*(wheelRadius+.07f));
    bodyRings.Add(new Vector4(z,width*.5f,.84f,bottom));
   }
  bodyRings.Add(new Vector4(back,width*.43f,.76f,.42f));
  Hull(model,"Lower body",bodyRings.OrderBy(r=>r.x).ToArray(),paint);
  var bonnet=Hull(model,"Short bonnet",new[]{new Vector4(front+.015f,width*.43f,.80f,.72f),new Vector4(cf-.05f,width*.49f,.94f,.76f)},paint);
  if(type!=3)Hull(model,type==0?"Short rear engine cover":"Small luggage boot",new[]{new Vector4(cb+.02f,width*.48f,.94f,.77f),new Vector4(back-.02f,width*.43f,.81f,.73f)},paint);
  Box(model,"Dark interior",new Vector3(0,1.06f,(cf+cb)*.5f),new Vector3(width*.83f,.35f,cb-cf-.12f),inside);
  var roof=Hull(model,"Cabin roof",new[]{new Vector4(cf+.12f,width*.39f,height-.12f,height-.18f),new Vector4(cf+.33f,width*.405f,height,height-.06f),new Vector4(cb-.26f,width*.405f,height,height-.06f),new Vector4(cb-.08f,width*.39f,height-.10f,height-.15f)},paint);
  float windshieldY=(height+.96f)*.5f;
  var wind=Box(model,"Windscreen",new Vector3(0,windshieldY,cf+.07f),new Vector3(width*.78f,height-.99f,.025f),glass);wind.localRotation=Quaternion.Euler(type==3?12:24,0,0);
  var rear=Box(model,"Rear glass",new Vector3(0,windshieldY,cb-.07f),new Vector3(width*.77f,height-.99f,.025f),glass);rear.localRotation=Quaternion.Euler(-17,0,0);
  float doorLength=type==0?.98f:type==1?1.16f:type==2?.92f:.90f;
  foreach(float side in new[]{-1f,1f})
  {
   Rod(model,new Vector3(side*width*.49f,.91f,cf-.05f),new Vector3(side*width*.39f,height-.08f,cf+.23f),.027f,paint);
   Rod(model,new Vector3(side*width*.49f,.91f,cb+.03f),new Vector3(side*width*.39f,height-.06f,cb-.15f),.032f,paint);
   var hinge=Group(model,"Front door",new Vector3(side*width*.50f,0,cf+.09f));
   Box(hinge,"Door skin",new Vector3(0,.81f,doorLength*.5f),new Vector3(.045f,.36f,doorLength),paint);
   var wa=new Vector3(-side*.015f,1.0f,.025f);
   var wb=new Vector3(-side*width*.105f,height-.105f,.18f);
   var wc=new Vector3(-side*width*.105f,height-.105f,doorLength-.04f);
   var wd=new Vector3(-side*.015f,1.0f,doorLength-.015f);
   Window(hinge,"Tapered door glass",wa,wb,wc,wd);
   Rod(hinge,wa,wb,.014f,paint);Rod(hinge,wb,wc,.014f,paint);Rod(hinge,wc,wd,.014f,paint);Rod(hinge,wd,wa,.014f,paint);
   Box(hinge,"Simple handle",new Vector3(side*.032f,.96f,doorLength*.82f),new Vector3(.04f,.035f,.13f),steel);
   if(open&&side<0)hinge.localRotation=Quaternion.Euler(0,-55-seed%3*9,0);
   float restStart=cf+.12f+doorLength,rest=cb-restStart;
   if(rest>.05f)
   {
    Box(model,"Rear side",new Vector3(side*width*.49f,type==3?1.33f:.80f,(restStart+cb)*.5f),new Vector3(.045f,type==3?1.14f:.32f,rest),paint);
    if(type!=3)
    {
     var qa=new Vector3(side*width*.49f,1.0f,restStart+.025f);
     var qb=new Vector3(side*width*.40f,height-.12f,restStart+.04f);
     var qc=new Vector3(side*width*.40f,height-.12f,cb-.18f);
     var qd=new Vector3(side*width*.49f,1.0f,cb-.04f);
     Window(model,"Tapered quarter glass",qa,qb,qc,qd);Rod(model,qa,qb,.018f,paint);Rod(model,qb,qc,.018f,paint);
    }
    if(type==2)Box(model,"Rear door handle",new Vector3(side*width*.515f,.95f,cb-.14f),new Vector3(.035f,.03f,.13f),steel);
   }
   float wheelZ=length*.5f-(type==0?.49f:.64f);float radius=type==0?.245f:type==3?.31f:.28f;
   foreach(float z in new[]{-wheelZ,wheelZ})
   {
    var tyre=Box(model,"Small steel wheel tyre",new Vector3(side*(width*.5f-.055f),radius,z),new Vector3(radius*2,.07f,radius*2),rubber,PrimitiveType.Cylinder);tyre.localRotation=Quaternion.Euler(0,0,90);
    var hub=Box(model,"Plain pressed steel wheel",new Vector3(side*(width*.5f+.018f),radius,z),new Vector3(radius*1.15f,.01f,radius*1.15f),steel,PrimitiveType.Cylinder);hub.localRotation=Quaternion.Euler(0,0,90);
   }
   for(int k=0;k<3;k++)Box(model,"Old corrosion",new Vector3(side*(width*.5f+.008f),.56f+.045f*(k%2),front+.3f+(length-.6f)*(k+.25f)/3),new Vector3(.012f,.05f,.12f+.04f*((seed+k)%3)),rust);
   var head=Box(model,"Round headlamp",new Vector3(side*width*.32f,.75f,front-.018f),new Vector3(type==0?.17f:.22f,.018f,type==0?.17f:.22f),lamp,PrimitiveType.Cylinder);head.localRotation=Quaternion.Euler(90,0,0);
   Box(model,"Small rear lamp",new Vector3(side*width*.36f,.74f,back+.02f),new Vector3(.13f,.16f,.025f),red);
   if(type==1)Hull(model,"Rounded wing",new[]{new Vector4(front+.02f,width*.46f,.82f,.76f),new Vector4(front+.39f,width*.52f,.97f,.80f),new Vector4(cf,width*.49f,.94f,.80f)},paint);
  }
  foreach(float z in new[]{front-.045f,back+.045f})Box(model,"Thin utilitarian bumper",new Vector3(0,.46f,z),new Vector3(width+.07f,.065f,.09f),steel);
  Box(model,"Number plate without text",new Vector3(0,.61f,front-.035f),new Vector3(.33f,.09f,.018f),inside);
  for(int k=0;k<5;k++)Box(model,type==0?"Rear engine cooling slot":"Simple grille",new Vector3((k-2)*.08f,type==0?.84f:.73f,type==0?back+.022f:front-.026f),new Vector3(.05f,.10f,.012f),inside);
  if(type==3){Box(model,"Rear cargo doors",new Vector3(0,1.19f,back-.035f),new Vector3(width*.86f,1.33f,.045f),paint);Box(model,"Rear door seam",new Vector3(0,1.19f,back-.065f),new Vector3(.022f,1.34f,.012f),inside);}
  // Mark three actual solid parts for rebuilding traversal collision in place.
  foreach(Transform part in model)
   if(part.name!="Small steel wheel tyre"&&part.name!="Plain pressed steel wheel")part.localPosition+=Vector3.down*bodyDrop;
  bonnet.name="Traversal bonnet";roof.name="Traversal roof";return model;
 }
 static Mesh Bake(Transform model,Transform car,out Material[] materials)
 {
  var entries=model.GetComponentsInChildren<MeshFilter>().SelectMany(f=>Enumerable.Range(0,f.sharedMesh.subMeshCount).Select(i=>new KeyValuePair<Material,CombineInstance>(f.GetComponent<Renderer>().sharedMaterials[i],new CombineInstance{mesh=f.sharedMesh,subMeshIndex=i,transform=car.worldToLocalMatrix*f.transform.localToWorldMatrix}))).GroupBy(e=>e.Key).ToArray();
  var temp=new List<Mesh>();foreach(var e in entries){var mesh=new Mesh{indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(e.Select(x=>x.Value).ToArray(),true,true);temp.Add(mesh);}
  var final=new Mesh{indexFormat=IndexFormat.UInt32};final.CombineMeshes(temp.Select(m=>new CombineInstance{mesh=m,transform=Matrix4x4.identity}).ToArray(),false,false);final.RecalculateBounds();
  materials=entries.Select(e=>e.Key).ToArray();foreach(var m in temp)Object.DestroyImmediate(m);return final;
 }
 static void ColliderFor(Transform car,Bounds bounds)
 {
  var c=car.gameObject.AddComponent<BoxCollider2D>();c.offset=car.InverseTransformPoint(bounds.center);c.size=new Vector2(bounds.size.x/Mathf.Abs(car.lossyScale.x),bounds.size.y/Mathf.Abs(car.lossyScale.y));
 }
 static void Road(Transform world,Transform oldRoad,Transform gate)
 {
  // The starting public road remains; the factory gate gets its own approach.
  oldRoad.gameObject.SetActive(true);
  var root=Group(world,Marker,Vector3.zero);
  var asphalt=AssetDatabase.LoadAssetAtPath<Material>("Assets/Recoil3D/RoadCheckpoint/Blended aged entrance asphalt.mat");
  if(!asphalt)asphalt=AssetDatabase.LoadAssetAtPath<Material>("Assets/Recoil3D/Opening/Materials/Worn road asphalt.mat");
  if(!asphalt)asphalt=oldRoad.GetComponentsInChildren<MeshRenderer>(true).First().sharedMaterial;
  var shoulder=AssetDatabase.LoadAssetAtPath<Material>("Assets/Recoil3D/RoadCheckpoint/Dirty gravel road shoulder.mat");if(!shoulder)shoulder=asphalt;
  float gx=gate.position.x;
  var path=FactoryForecourtRoadPass.PathToFactory(oldRoad,gx);
  for(int layer=0;layer<2;layer++)
  {
   var v=new List<Vector3>();var tri=new List<int>();float half=layer==0?3.45f:2.88f;
   for(int i=0;i<path.Count;i++){var tangent=(path[Mathf.Min(path.Count-1,i+1)]-path[Mathf.Max(0,i-1)]).normalized;var right=new Vector3(tangent.z,0,-tangent.x);v.Add(path[i]-right*half+Vector3.up*(layer==0?.008f:.023f));v.Add(path[i]+right*half+Vector3.up*(layer==0?.008f:.023f));if(i>0){int n=i*2;tri.AddRange(new[]{n-2,n,n+1,n-2,n+1,n-1});}}
   var m=new Mesh();m.SetVertices(v);m.SetTriangles(tri,0);m.RecalculateNormals();m.RecalculateBounds();var t=Group(root,layer==0?"Worn gravel road shoulders":"Curved public road enters factory rear gate",Vector3.zero);t.gameObject.AddComponent<MeshFilter>().sharedMesh=Save(t.name,m);t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=layer==0?shoulder:asphalt;
  }
 }
 public static string ReplaceRemainingCivilianCars()
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play first");
  var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
  var all=scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<Transform>(true)).ToArray();
  steel=Load("Exposed aged vehicle steel");rubber=Load("Perished tyres");glass=Load("Dusty dark abandoned glazing");inside=Load("Dark torn interior");rust=Load("Old brown corrosion");lamp=Load("Clouded headlight and plate");red=Load("Broken dark red tail lamps");
  int n=0;
  foreach(var car in all.Where(t=>t.name=="Parked car"||t.name.StartsWith("Wreck A -")||t.name.StartsWith("Wreck B -")))
  {
   if(car.Find("Economy civilian car - single mesh"))continue;
   foreach(var r in car.GetComponentsInChildren<Renderer>(true)){r.enabled=false;EditorUtility.SetDirty(r);}
   foreach(var visual in car.GetComponentsInChildren<Recoil3DSpriteVisual>(true)){visual.suppressed=true;EditorUtility.SetDirty(visual);}
   int type=car.name=="Parked car"?2:car.name.StartsWith("Wreck A")?0:1;
   var model=Build(car,type,50+n,car.name!="Parked car",Load(car.name=="Parked car"?"Faded blue sedan":"Burnt charcoal shell"));
   Material[] mats;var mesh=Save("Additional civilian car "+car.name,Bake(model,car,out mats));Object.DestroyImmediate(model.gameObject);
   var t=Group(car,"Economy civilian car - single mesh",Vector3.zero);t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;t.gameObject.AddComponent<MeshRenderer>().sharedMaterials=mats;n++;
  }
  AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
  File.AppendAllText("output/economy-cars-factory-road/verification.txt","\nUpdated starting car and two older civilian wrecks as well; their original roots and gameplay colliders retained.");return "Updated "+n+" additional civilian cars";
 }
 public static string Apply()
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play first");var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
  var all=scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<Transform>(true)).ToArray();var world=all.Single(t=>t.GetComponent<RecoilPrototype>());
  if(world.Find(Marker))return "Already applied; current authored transforms retained";
  var cars=all.Where(t=>t.name.StartsWith("Car - ")&&t.GetComponent<MeshFilter>()).ToArray();
  var poses=cars.ToDictionary(t=>t,t=>new[]{t.position,t.rotation.eulerAngles,t.localScale});
  var doorState=all.SelectMany(t=>t.GetComponents<RecoilDoor>()).Select(t=>t.name+"|"+t.transform.position+"|"+t.destinationScene+"|"+t.destinationSpawn).ToArray();
  Directory.CreateDirectory("Backups/EconomyCarsFactoryRoad");string backup="Backups/EconomyCarsFactoryRoad/"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity";EditorSceneManager.SaveScene(scene,backup,true);
  Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
  steel=Load("Exposed aged vehicle steel");rubber=Load("Perished tyres");glass=Load("Dusty dark abandoned glazing");inside=Load("Dark torn interior");rust=Load("Old brown corrosion");lamp=Load("Clouded headlight and plate");red=Load("Broken dark red tail lamps");
  int[] counts=new int[4];int index=0;
  foreach(var car in cars)
  {
   var mf=car.GetComponent<MeshFilter>();var renderer=car.GetComponent<MeshRenderer>();var old=mf.sharedMesh;
   var materials=renderer.sharedMaterials;var facing=OriginalBakedFacing(old,materials);
   int type=index%10<5?0:index%10<8?1:index%10==8?2:3;
   if(car.name.Contains("delivery van")||car.name.Contains("shoulder car 3")||car.name.Contains("right 6"))type=3;
   if(car.name.Contains("Estate car blocking"))type=2;
   if(car.name.Contains("Second saloon"))type=1;
   if(car.name.Contains("Third hatchback"))type=0;
   var paint=materials.FirstOrDefault(m=>m&&(m.name.StartsWith("Faded ")||m.name=="Rust red delivery van"||m.name=="Burnt charcoal shell"));if(!paint)paint=Load("Faded cream wagon");
   var model=Build(car,type,index,index%4==1,paint);model.localRotation=facing;
   // Original cars have baked yaw. Keep the user's root rotation, including later manual changes.
   var oldMin=old.bounds.min.y;var newMin=model.GetComponentsInChildren<Renderer>().Min(r=>car.InverseTransformPoint(r.bounds.min).y);
   model.localPosition+=Vector3.up*(oldMin-newMin);
   bool solid=car.GetComponents<BoxCollider2D>().Length>0;
   if(solid)
   {
    foreach(var collider in car.GetComponents<BoxCollider2D>())Object.DestroyImmediate(collider);
    ColliderFor(car,model.Find("Lower body").GetComponent<Renderer>().bounds);
    ColliderFor(car,model.Find("Traversal bonnet").GetComponent<Renderer>().bounds);
    ColliderFor(car,model.Find("Traversal roof").GetComponent<Renderer>().bounds);
   }
   Material[] resultMaterials;mf.sharedMesh=Save("Economy car "+(++index).ToString("00")+" "+new[]{"126p-inspired microcar","Syrena-inspired rounded car","125p-inspired compact saloon","Zuk-inspired work van"}[type],Bake(model,car,out resultMaterials));renderer.sharedMaterials=resultMaterials;
   Object.DestroyImmediate(model.gameObject);counts[type]++;EditorUtility.SetDirty(car.gameObject);
  }
  var gate=all.First(t=>t.name=="Masonry rear gate pier");Road(world,all.Single(t=>t.name=="Road - foreground to misty horizon"),gate);
  if(cars.Any(t=>t.position!=poses[t][0]||t.rotation.eulerAngles!=poses[t][1]||t.localScale!=poses[t][2]||t.childCount!=0))throw new Exception("Car pose changed unexpectedly");
  var after=world.GetComponentsInChildren<RecoilDoor>(true).Select(t=>t.name+"|"+t.transform.position+"|"+t.destinationScene+"|"+t.destinationSpawn).ToArray();if(!doorState.SequenceEqual(after))throw new Exception("Door changed unexpectedly");
  AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
  string report="Rebuilt "+cars.Length+" independent cars; microcar/rounded/saloon/van="+string.Join("/",counts.Select(n=>n.ToString()).ToArray())+"; every original position, root rotation and scale retained; traversal collision rebuilt from new body/bonnet/roof; road curves from foreground into rear gate X="+gate.position.x+". Backup: "+backup;
  Directory.CreateDirectory("output/economy-cars-factory-road");File.WriteAllText("output/economy-cars-factory-road/verification.txt",report);return report;
 }
}
