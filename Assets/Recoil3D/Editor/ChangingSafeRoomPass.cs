using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;

public static class ChangingSafeRoomPass
{
 const string ScenePath="Assets/Scenes/3D/FactoryRooms/FactoryChangingRoomF2.unity",Folder="Assets/Art/Environment/ChangingRoom/SafeRoomProps",Marker="Safe refuge - former strike repair and equipment room",Output="output/changing-safe-room";
 static Mesh cube,cylinder,chain;
 static Material wood,steel,dark,rust,cloth,paper,green,warm;
 static Transform root;
 static T[] All<T>(Scene s)where T:Component{return s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true)).ToArray();}
 static Transform G(string name,Transform parent,Vector3 at){var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=at;return t;}
 static Transform MeshObject(string name,Transform parent,Vector3 at,Vector3 size,Mesh mesh,Material mat){var t=G(name,parent,at);t.localScale=size;t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=mat;return t;}
 static Transform B(string name,Transform parent,Vector3 at,Vector3 size,Material mat){return MeshObject(name,parent,at,size,cube,mat);}
 static Transform C(string name,Transform parent,Vector3 at,float radius,float height,Material mat){return MeshObject(name,parent,at,new Vector3(radius,height,radius),cylinder,mat);}
 static void R(string name,Transform parent,Vector3 a,Vector3 b,float radius,Material mat){var t=C(name,parent,(a+b)*.5f,radius,Vector3.Distance(a,b),mat);t.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);}
 static Material Load(string path){var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m)throw new Exception("Missing material "+path);return m;}
 static void Materials()
 {
  cube=Resources.GetBuiltinResource<Mesh>("Cube.fbx");cylinder=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Art/Environment/ManagerOffice/Cylinder12.asset");
  wood=Load("Assets/Art/Environment/ChangingRoom/Worn bench timber.mat");steel=Load("Assets/Art/Environment/ManagerOffice/Painted warm grey steel.mat");dark=Load("Assets/Art/Environment/ManagerOffice/Bakelite and rubber.mat");rust=Load("Assets/Art/Environment/ChangingRoom/Oxidised exposed steel edges.mat");cloth=Load("Assets/Art/Environment/ManagerOffice/Faded olive vinyl.mat");paper=Load("Assets/Art/Environment/ManagerOffice/Old paper.mat");green=Load("Assets/Art/Environment/ChangingRoom/Faded industrial locker enamel.mat");
  warm=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Warm frosted lamp.mat");if(!warm){warm=new Material(Shader.Find("Standard"));warm.color=new Color(.78f,.61f,.36f);warm.EnableKeyword("_EMISSION");warm.SetColor("_EmissionColor",new Color(.8f,.36f,.10f));AssetDatabase.CreateAsset(warm,Folder+"/Warm frosted lamp.mat");}
  chain=AssetDatabase.LoadAssetAtPath<Mesh>(Folder+"/Shared lowpoly chain link.asset");if(!chain){var verts=new List<Vector3>();var tri=new List<int>();for(int i=0;i<12;i++)for(int j=0;j<4;j++){float a=i*Mathf.PI*2/12,b=j*Mathf.PI*.5f;verts.Add(new Vector3((.035f+.008f*Mathf.Cos(b))*Mathf.Cos(a),(.060f+.008f*Mathf.Cos(b))*Mathf.Sin(a),.008f*Mathf.Sin(b)));}for(int i=0;i<12;i++)for(int j=0;j<4;j++){int a=i*4+j,b=((i+1)%12)*4+j,c=((i+1)%12)*4+(j+1)%4,d=i*4+(j+1)%4;tri.AddRange(new[]{a,b,c,a,c,d});}chain=new Mesh();chain.SetVertices(verts);chain.SetTriangles(tri,0);chain.RecalculateNormals();chain.RecalculateBounds();AssetDatabase.CreateAsset(chain,Folder+"/Shared lowpoly chain link.asset");}
 }
 static void HangingChain(Transform parent,Vector3 at)
 {var g=G("Stored chain - historical defence equipment",parent,at);for(int i=0;i<11;i++){var t=MeshObject("Individual chain link",g,new Vector3(0,-i*.094f,0),Vector3.one,chain,rust);t.localRotation=Quaternion.Euler(0,i%2*90,0);}}
 static void ToolRack(Transform bench)
 {
  var g=G("Old strike equipment rack - complete display props",root,new Vector3(10.6f,0,4.05f));
  B("Reclaimed board backing",g,new Vector3(0,2.18f,0),new Vector3(2.2f,1.48f,.045f),wood);
  for(int i=0;i<9;i++)B("Board seam",g,new Vector3(-1+i*.25f,2.18f,-.027f),new Vector3(.008f,1.44f,.007f),dark);
  var pole=G("Old improvised defence pole - nonfunctional prop",g,new Vector3(-.78f,1.64f,-.085f));pole.localRotation=Quaternion.Euler(0,0,-12);
  R("Worn salvaged wooden shaft",pole,Vector3.zero,new Vector3(0,1.15f,0),.025f,wood);C("Aged metal end collar",pole,new Vector3(0,1.08f,0),.032f,.16f,steel);B("Worn flat metal cap",pole,new Vector3(0,1.22f,0),new Vector3(.045f,.15f,.023f),rust);
  var club=G("Wrapped factory handle - old defence baton",g,new Vector3(-.2f,1.72f,-.10f));club.localRotation=Quaternion.Euler(0,0,8);R("Weathered tool handle",club,Vector3.zero,new Vector3(0,.82f,0),.033f,wood);for(int i=0;i<5;i++)C("Old cloth grip fold",club,new Vector3(0,.08f+i*.044f,0),.037f,.035f,cloth);C("Worn metal cap",club,new Vector3(0,.78f,0),.037f,.11f,rust);
  HangingChain(g,new Vector3(.47f,2.82f,-.12f));
  var crowbar=G("Salvaged bent steel bar",g,new Vector3(.94f,1.68f,-.12f));R("Steel bar",crowbar,Vector3.zero,new Vector3(0,.83f,0),.021f,steel);R("Bent end",crowbar,new Vector3(0,.83f,0),new Vector3(-.13f,.91f,0),.021f,steel);
  foreach(float x in new[]{-.8f,-.18f,.48f,.96f})B("Storage hook",g,new Vector3(x,2.89f,-.08f),new Vector3(.055f,.06f,.09f),steel);
 }
 static void Workbench()
 {
  var b=G("Strike-era repair workbench - complete editable assembly",root,new Vector3(10.6f,0,3.34f));
  for(int i=0;i<4;i++)B("Heavy timber top plank",b,new Vector3(0,.88f,-.39f+i*.26f),new Vector3(2.20f,.09f,.24f),wood);
  foreach(float x in new[]{-.90f,.90f})foreach(float z in new[]{-.40f,.40f})B("Steel bench leg",b,new Vector3(x,.42f,z),new Vector3(.07f,.84f,.07f),steel);
  foreach(float z in new[]{-.42f,.42f})B("Bench stretcher",b,new Vector3(0,.23f,z),new Vector3(1.88f,.055f,.055f),steel);
  B("Lower supply shelf",b,new Vector3(0,.26f,0),new Vector3(1.90f,.045f,.80f),wood);
  var vise=G("Old bench vise - editable tool",b,new Vector3(-.68f,.93f,-.32f));B("Vise mounting base",vise,new Vector3(0,.025f,0),new Vector3(.31f,.05f,.24f),steel);B("Cast vise body",vise,new Vector3(0,.12f,0),new Vector3(.22f,.17f,.14f),green);foreach(float x in new[]{-.12f,.12f})B("Vise jaw",vise,new Vector3(x,.22f,0),new Vector3(.07f,.07f,.22f),steel);R("Vise screw housing",vise,new Vector3(-.22f,.12f,0),new Vector3(.22f,.12f,0),.028f,steel);R("Vise handle",vise,new Vector3(-.25f,.02f,0),new Vector3(-.25f,.23f,0),.012f,dark);
  var tools=G("Loose repair tools - hammer file and pliers",b,new Vector3(-.11f,.94f,-.06f));R("Hammer timber handle",tools,new Vector3(-.16f,.015f,-.23f),new Vector3(-.03f,.015f,.12f),.017f,wood);B("Hammer head",tools,new Vector3(-.03f,.025f,.13f),new Vector3(.18f,.055f,.065f),steel);B("Hand file",tools,new Vector3(.17f,.012f,-.07f),new Vector3(.035f,.020f,.29f),steel);B("File grip",tools,new Vector3(.17f,.018f,-.26f),new Vector3(.045f,.035f,.12f),wood);R("Pliers steel jaw",tools,new Vector3(.37f,.02f,.16f),new Vector3(.36f,.02f,.23f),.018f,steel);R("Pliers handle",tools,new Vector3(.37f,.02f,.16f),new Vector3(.28f,.02f,-.04f),.014f,dark);R("Second pliers handle",tools,new Vector3(.37f,.02f,.16f),new Vector3(.45f,.02f,-.04f),.014f,dark);
  var tray=G("Tray of spare industrial hardware",b,new Vector3(-.13f,.955f,.30f));B("Metal parts tray",tray,Vector3.zero,new Vector3(.36f,.025f,.22f),green);for(int i=0;i<7;i++){var t=C("Old bolt",tray,new Vector3(-.13f+i*.04f,.03f,(i%2)*.065f-.03f),.010f,.075f,steel);t.localRotation=Quaternion.Euler(90,i*23,0);}
  var stock=G("Abandoned repair stock - handles and steel pieces",b,new Vector3(.2f,.31f,0));foreach(float z in new[]{-.20f,0,.20f})R("Stored handle blank",stock,new Vector3(-.65f,0,z),new Vector3(.55f,0,z),.025f,wood);B("Cloth wrapped spare parts",stock,new Vector3(-.58f,.08f,0),new Vector3(.36f,.16f,.34f),cloth);
  G("Toolbox placement anchor",b,new Vector3(.61f,.93f,-.05f));
  var lamp=G("Warm repair light - complete lamp",b,new Vector3(.92f,.95f,.30f));C("Lamp base",lamp,Vector3.zero,.10f,.04f,green);R("Lamp stem",lamp,new Vector3(0,.02f,0),new Vector3(0,.45f,0),.014f,steel);C("Lamp shade",lamp,new Vector3(0,.48f,-.03f),.11f,.12f,green);C("Warm lamp diffuser",lamp,new Vector3(0,.413f,-.03f),.085f,.015f,warm);var l=G("Warm work pool",lamp,new Vector3(-.07f,.31f,-.12f)).gameObject.AddComponent<Light>();l.type=LightType.Point;l.range=3.6f;l.intensity=1.25f;l.color=new Color(1,.78f,.49f);l.shadows=LightShadows.None;
  ToolRack(b);
 }
 static void RestCorner()
 {
  var g=G("Quiet refuge corner - water tea and supplies",root,new Vector3(1.0f,0,2.38f));
  B("Low wooden bedside crate",g,new Vector3(-.35f,.24f,-.70f),new Vector3(.62f,.48f,.58f),wood);
  for(int i=0;i<3;i++)B("Crate board gap",g,new Vector3(-.35f,.10f+i*.12f,-.998f),new Vector3(.60f,.008f,.006f),dark);
  var lantern=G("Warm emergency lantern",g,new Vector3(-.35f,.51f,-.70f));C("Lantern foot",lantern,Vector3.zero,.083f,.05f,green);C("Frosted lantern body",lantern,new Vector3(0,.14f,0),.059f,.23f,warm);C("Lantern cap",lantern,new Vector3(0,.28f,0),.081f,.035f,green);foreach(float side in new[]{-1f,1f})R("Lantern wire guard",lantern,new Vector3(side*.072f,.02f,0),new Vector3(side*.072f,.29f,0),.006f,steel);var light=G("Soft refuge lantern pool",lantern,new Vector3(0,.13f,-.1f)).gameObject.AddComponent<Light>();light.type=LightType.Point;light.range=3.5f;light.intensity=.90f;light.color=new Color(1,.75f,.44f);light.shadows=LightShadows.None;
  var mug=G("Enamel mug and water bottle",g,new Vector3(-.12f,.51f,-.70f));C("Enamel mug",mug,Vector3.zero,.042f,.095f,paper);C("Dark tea",mug,new Vector3(0,.050f,0),.035f,.003f,dark);R("Mug handle",mug,new Vector3(.04f,-.02f,0),new Vector3(.07f,.03f,0),.007f,steel);C("Water bottle",mug,new Vector3(.20f,.09f,.04f),.043f,.27f,green);C("Bottle neck",mug,new Vector3(.20f,.245f,.04f),.021f,.045f,steel);
  var aid=G("Stored bandage tin - closed",g,new Vector3(.30f,.10f,.70f));B("Old first aid tin",aid,Vector3.zero,new Vector3(.38f,.20f,.26f),paper);B("Tin lid seam",aid,new Vector3(0,.09f,0),new Vector3(.39f,.018f,.27f),steel);
 }
 public static string Apply()
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play first");Directory.CreateDirectory(Folder);Directory.CreateDirectory(Output);var previous=SceneManager.GetActiveScene();var scene=SceneManager.GetSceneByPath(ScenePath);bool added=!scene.isLoaded;if(added)scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);SceneManager.SetActiveScene(scene);
  try
  {
   var world=All<RecoilPrototype>(scene).Single();if(world.transform.Find(Marker))return "Already authored";
   string backup="Backups/ChangingSafeRoom/"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity";Directory.CreateDirectory(System.IO.Path.GetDirectoryName(backup));if(!EditorSceneManager.SaveScene(scene,backup,true))throw new Exception("Backup failed");
   var physics=All<Collider2D>(scene).Select(c=>c.GetInstanceID()+"|"+c.bounds.ToString("F6")).ToArray();
   var furniture=All<Transform>(scene).Where(t=>t.name.StartsWith("Bench 1 -")||t.name.StartsWith("Bench 2 -")||t.name.StartsWith("Changing lockers - whole bank")||t.name.StartsWith("Changing fluorescent ")).ToDictionary(t=>t,t=>t.position);
   var room=world.transform.Find("Workers changing room - connected lockers and detailed benches");var wall=room.Find("Static combined room surfaces");float oldBack=wall.GetComponentsInChildren<MeshRenderer>().Max(r=>r.bounds.max.z);wall.position+=Vector3.forward;
   foreach(var t in room.Cast<Transform>().Where(t=>t.name=="Number or sign - SZATNIA"))t.position+=Vector3.forward;
   foreach(var name in new[]{"Room floor","Room ceiling"}){var v=All<Recoil3DSpriteVisual>(scene).Single(x=>x.name==name);var before=v.meshRenderer.bounds;v.depth+=1;v.depthOffset+=.5f;v.Sync();EditorUtility.SetDirty(v);if(Mathf.Abs(v.meshRenderer.bounds.min.z-before.min.z)>.002f||Mathf.Abs(v.meshRenderer.bounds.max.z-before.max.z-1)>.002f)throw new Exception("Room extension did not preserve foreground edge");}
   var bank=room.Cast<Transform>().Where(t=>t.name.StartsWith("Changing lockers - whole bank")).OrderBy(t=>t.position.z).First();
   foreach(var t in room.GetComponentsInChildren<Transform>(true).Where(t=>t.name.Contains("- hinged full height door"))){t.localRotation=Quaternion.identity;EditorUtility.SetDirty(t);}
   int[] numbers={102,105,110};float[] angles={18,42,66};for(int i=0;i<numbers.Length;i++){var t=bank.GetComponentsInChildren<Transform>(true).Single(x=>x.name=="Locker "+numbers[i]+" - hinged full height door");t.localRotation=Quaternion.Euler(0,angles[i],0);EditorUtility.SetDirty(t);}
   root=G(Marker,world.transform,Vector3.zero);root.position=Vector3.zero;Materials();Workbench();RestCorner();
   var bed=All<Transform>(scene).SingleOrDefault(t=>t.name=="Abandoned improvised treatment bed 38");if(bed){bed.position=new Vector3(1.15f,bed.position.y,2.95f);EditorUtility.SetDirty(bed);}
   foreach(var pair in furniture)if(pair.Key.position!=pair.Value)throw new Exception("Existing furniture placement changed");Physics2D.SyncTransforms();if(!physics.SequenceEqual(All<Collider2D>(scene).Select(c=>c.GetInstanceID()+"|"+c.bounds.ToString("F6"))))throw new Exception("Gameplay collision changed");
   if(Mathf.Abs(wall.GetComponentsInChildren<MeshRenderer>().Max(r=>r.bounds.max.z)-oldBack-1)>.002f)throw new Exception("Back wall extension failed");
   int open=room.GetComponentsInChildren<Transform>(true).Count(t=>t.name.Contains("- hinged full height door")&&Quaternion.Angle(t.localRotation,Quaternion.identity)>1);if(open!=3)throw new Exception("Expected exactly three open doors");if(All<RampagerController>(scene).Any(e=>e.isActiveAndEnabled))throw new Exception("Hostile AI remains in refuge");
   EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new Exception("Save failed");AssetDatabase.SaveAssets();ChangingRoomRebuild.Capture(scene,"Safe-room-overview",new Vector3(6,1.8f,1.8f),5.3f);ChangingRoomRebuild.Capture(scene,"Repair-corner",new Vector3(10.6f,1.8f,3.4f),2.0f);
   string report="PASS: back wall and wall fittings +1 m in Z; floor/ceiling depth +1 m with foreground edge fixed. Existing bench/locker/fluorescent parent positions preserved. Three static doors opened: 102=18, 105=42, 110=66 degrees; 109 remains interactive. Historical-inspired tool/defence equipment rack, repair bench/vice/hand tools, warm rest corner and medical bedding. No active hostile AI. Colliders unchanged. Backup: "+backup;File.WriteAllText(Output+"/room-verification.txt",report);return report;
  }
  finally{if(added)EditorSceneManager.CloseScene(scene,true);if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);}
 }
 public static string InstallToolbox(string assetPath)
 {
  var scene=SceneManager.GetSceneByPath(ScenePath);if(!scene.isLoaded)throw new Exception("Open changing room first");var anchor=All<Transform>(scene).Single(t=>t.name=="Toolbox placement anchor");if(anchor.childCount>0)return "Toolbox already installed";var asset=AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);if(!asset)throw new Exception("Generated model is not imported as a GameObject");var model=(GameObject)PrefabUtility.InstantiatePrefab(asset,scene);model.name="Meshy old worker toolbox - whole prop";model.transform.SetParent(anchor,false);model.transform.localRotation=Quaternion.identity;
  var renderers=model.GetComponentsInChildren<Renderer>();if(renderers.Length==0)throw new Exception("Toolbox has no renderers");var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);float scale=.58f/Mathf.Max(bounds.size.x,bounds.size.z);model.transform.localScale*=scale;
  bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);model.transform.position+=new Vector3(anchor.position.x-bounds.center.x,anchor.position.y-bounds.min.y,anchor.position.z-bounds.center.z);
  int triangles=model.GetComponentsInChildren<MeshFilter>().Sum(f=>f.sharedMesh?f.sharedMesh.triangles.Length/3:0);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);File.WriteAllText(Output+"/meshy-toolbox.txt","Meshy text-to-model trial installed: "+assetPath+"; triangles="+triangles+"; width/depth max=0.58 m. Existing meshes/materials preserved; prop has an independent parent.");return "Installed toolbox; triangles="+triangles;
 }
}
