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

public static class ChangingRoomRebuild
{
 const string PathScene="Assets/Scenes/3D/FactoryRooms/FactoryChangingRoomF2.unity",Folder="Assets/Art/Environment/ChangingRoom",Marker="Workers changing room - connected lockers and detailed benches",Output="output/changing-room";
 static Material paint,alternate,dark,steel,wood,paper,cloth,plaster,dado,lens,rust;
 static Transform room,geometry;
 static Mesh cube;
 static T[] All<T>(Scene s)where T:Component{return s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true)).ToArray();}
 static Material Mat(string name,int colour,float metallic=0)
 {string path=Folder+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Recoil/3D/Weathered Surface"));AssetDatabase.CreateAsset(m,path);}m.color=RecoilPrototype.Hex(colour);m.SetFloat("_Metallic",metallic);m.SetFloat("_Glossiness",.16f);m.enableInstancing=true;EditorUtility.SetDirty(m);return m;}
 static Material Existing(string name){var m=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Environment/ManagerOffice/"+name+".mat");if(!m)throw new Exception("Missing shared material "+name);return m;}
 static Transform G(string name,Transform parent,Vector3 p){var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=p;return t;}
 static Transform Box(string n,Transform p,Vector3 at,Vector3 size,Material m){var t=G(n,p,at);t.localScale=size;t.gameObject.AddComponent<MeshFilter>().sharedMesh=cube;t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=m;return t;}
 static void Bar(string n,Transform p,Vector3 a,Vector3 b,float width,Material m){var t=Box(n,p,(a+b)*.5f,new Vector3(width,Vector3.Distance(a,b),width),m);t.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);}
 static void Text(string value,Transform p,Vector3 at,float size)
 {var t=G("Number or sign - "+value,p,at);var text=t.gameObject.AddComponent<TextMesh>();text.text=value;text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=48;text.characterSize=size;text.anchor=TextAnchor.MiddleCenter;t.GetComponent<MeshRenderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Recoil3D/ConflictAftermath/Depth tested printed lettering.mat");text.color=new Color(.72f,.73f,.62f);t.gameObject.AddComponent<Recoil3DDepthText>();}
 static void Bake(Transform target,string assetName)
 {
  var renderers=target.GetComponentsInChildren<MeshRenderer>(true).Where(r=>!r.GetComponent<TextMesh>()&&r.GetComponent<MeshFilter>()&&r.GetComponent<MeshFilter>().sharedMesh).ToArray();
  foreach(var group in renderers.GroupBy(r=>r.sharedMaterial))
  {
   var mesh=new Mesh();mesh.name=assetName+" - "+group.Key.name;mesh.indexFormat=IndexFormat.UInt32;
   mesh.CombineMeshes(group.Select(r=>new CombineInstance{mesh=r.GetComponent<MeshFilter>().sharedMesh,transform=target.worldToLocalMatrix*r.transform.localToWorldMatrix}).ToArray(),true,true);
   string file=Folder+"/"+mesh.name.Replace('/','-')+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(file);if(old){EditorUtility.CopySerialized(mesh,old);Object.DestroyImmediate(mesh);mesh=old;}else AssetDatabase.CreateAsset(mesh,file);
   var g=G("Combined - "+group.Key.name,target,Vector3.zero);g.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;g.gameObject.AddComponent<MeshRenderer>().sharedMaterial=group.Key;
  }
  foreach(var r in renderers)Object.DestroyImmediate(r.gameObject);
 }
 static Transform LockerDoor(Transform parent,Vector3 at,int number)
 {
  var hinge=G("Locker "+number+" - hinged full height door",parent,at);
  Box("Pressed sheet steel door",hinge,new Vector3(.325f,1.17f,0),new Vector3(.64f,1.96f,.035f),number%3==0?alternate:paint);
  // The dark vent openings and metal louvres occupy distinct depth layers.
  foreach(float y in new[]{.40f,.47f,.54f,1.77f,1.84f,1.91f}){Box("Vent opening",hinge,new Vector3(.325f,y,-.021f),new Vector3(.32f,.027f,.006f),dark);var lip=Box("Folded vent louvre",hinge,new Vector3(.325f,y+.017f,-.030f),new Vector3(.34f,.015f,.022f),steel);lip.localRotation=Quaternion.Euler(-18,0,0);}
  Box("Number plate frame",hinge,new Vector3(.325f,1.58f,-.027f),new Vector3(.24f,.095f,.014f),steel);
  Box("Aged number insert",hinge,new Vector3(.325f,1.58f,-.038f),new Vector3(.205f,.068f,.006f),dark);Text(number.ToString(),hinge,new Vector3(.325f,1.582f,-.044f),.01955f);
  Box("Recessed latch backing",hinge,new Vector3(.54f,1.12f,-.026f),new Vector3(.073f,.19f,.012f),dark);
  Box("Raised lever latch",hinge,new Vector3(.54f,1.13f,-.055f),new Vector3(.029f,.13f,.035f),steel);
  Box("Padlock hasp",hinge,new Vector3(.54f,1.02f,-.055f),new Vector3(.058f,.025f,.035f),steel);
  foreach(float y in new[]{.59f,1.70f})Box("Barrel hinge",hinge,new Vector3(.008f,y,-.010f),new Vector3(.036f,.09f,.047f),steel);
  for(int j=0;j<4;j++){float x=.03f+((number*17+j*23)%57)*.01f;float y=.22f+((number*13+j*41)%170)*.01f;Box("Small chipped enamel",hinge,new Vector3(x,y,-.020f),new Vector3(.014f+(j%2)*.018f,.007f,.003f),rust);}
  Bake(hinge,"Door "+number);return hinge;
 }
 static Transform Lockers()
 {
  var bank=G("Changing lockers - whole bank",room,Vector3.zero);
  float first=2.35f,pitch=.70f,front=2.08f,back=2.75f;
  Box("Continuous raised plinth",bank,new Vector3(5.50f,.10f,2.42f),new Vector3(7.04f,.20f,.65f),dark);
  Box("Continuous top cap",bank,new Vector3(5.50f,2.24f,2.42f),new Vector3(7.06f,.065f,.70f),steel);
  Box("Continuous locker back",bank,new Vector3(5.50f,1.21f,back),new Vector3(7.04f,2.01f,.035f),paint);
  Transform interactiveHinge=null;
  for(int i=0;i<10;i++)
  {
   float x=first+i*pitch;
   Box("Locker vertical partition",bank,new Vector3(x-.35f,1.20f,2.42f),new Vector3(.028f,2.02f,.67f),steel);
   Box("Locker bottom shelf",bank,new Vector3(x,.22f,2.42f),new Vector3(.67f,.035f,.64f),steel);
   Box("Locker hat shelf",bank,new Vector3(x,1.84f,2.42f),new Vector3(.67f,.025f,.64f),paint);
   Bar("Clothes hanging rail",bank,new Vector3(x-.26f,1.69f,2.43f),new Vector3(x+.26f,1.69f,2.43f),.018f,steel);
   var door=LockerDoor(bank,new Vector3(x-.325f,0,front),101+i);if(i==8)interactiveHinge=door;
  }
  Box("Final bank side",bank,new Vector3(first+9*pitch+.35f,1.20f,2.42f),new Vector3(.028f,2.02f,.67f),steel);
  var interact=G("Locker 109 - interaction",bank,new Vector3(first+8*pitch,1.0f,.7f));var locker=interact.gameObject.AddComponent<WorkerLocker>();locker.doorHinge=interactiveHinge;locker.interactionRange=1.4f;
  return bank;
 }
 static void Bench(float x,float length,int index)
 {
  var b=G("Bench "+index+" - timber slats and steel shoe rack",room,new Vector3(x,0,1.45f));
  for(int i=0;i<3;i++){Box("Worn timber seat slat",b,new Vector3(0,.49f,(i-1)*.145f),new Vector3(length,.055f,.125f),wood);for(int s=-1;s<=1;s+=2)Box("Seat screw head",b,new Vector3(s*(length*.5f-.18f),.520f,(i-1)*.145f),new Vector3(.017f,.004f,.017f),steel);}
  foreach(float side in new[]{-1f,1f}){float xx=side*(length*.5f-.20f);foreach(float z in new[]{-.16f,.16f}){Bar("Tubular steel leg",b,new Vector3(xx,.47f,z),new Vector3(xx,.035f,z*1.14f),.034f,steel);Box("Rubber foot",b,new Vector3(xx,.024f,z*1.14f),new Vector3(.058f,.045f,.058f),dark);}Bar("Leg frame crosspiece",b,new Vector3(xx,.34f,-.18f),new Vector3(xx,.34f,.18f),.028f,steel);}
  foreach(float z in new[]{-.14f,0,.14f})Box("Under-seat shoe shelf",b,new Vector3(0,.17f,z),new Vector3(length-.20f,.020f,.035f),steel);
  // A few creases and scuffs, rather than repeating identical clutter on every bench.
  if(index==1){var towel=Box("Folded faded towel",b,new Vector3(-.58f,.563f,.015f),new Vector3(.48f,.075f,.28f),cloth);towel.localRotation=Quaternion.Euler(0,-9,0);for(int i=0;i<3;i++)Box("Towel folded edge",b,new Vector3(-.73f+i*.14f,.604f,.016f),new Vector3(.018f,.006f,.23f),dado);}
  for(int foot=0;foot<2;foot++){float xx=.12f+foot*.18f;Box("Old work boot sole",b,new Vector3(xx,.206f,-.015f),new Vector3(.145f,.032f,.29f),dark);Box("Work boot upper",b,new Vector3(xx,.277f,.055f),new Vector3(.13f,.12f,.14f),rust);Box("Boot toe",b,new Vector3(xx,.238f,-.065f),new Vector3(.13f,.06f,.16f),dark);}
 }
 static void Walls()
 {
  Box("Near solid changing-room back wall",geometry,new Vector3(6,1.90f,3.32f),new Vector3(12.6f,3.8f,.18f),plaster);
  Box("Oil-painted lower wall",geometry,new Vector3(6,.64f,3.19f),new Vector3(12.5f,1.28f,.035f),dado);
  Box("Dado top trim",geometry,new Vector3(6,1.30f,3.15f),new Vector3(12.5f,.035f,.045f),steel);
  Box("Skirting",geometry,new Vector3(6,.065f,3.12f),new Vector3(12.5f,.13f,.07f),dark);
  foreach(float x in new[]{3.2f,9.2f}){Box("Wire glass window",geometry,new Vector3(x,3.05f,3.10f),new Vector3(1.62f,.78f,.035f),dark);foreach(float yy in new[]{2.64f,3.46f})Box("Window lintel and sill",geometry,new Vector3(x,yy,3.06f),new Vector3(1.76f,.055f,.11f),steel);foreach(float xx in new[]{x-.84f,x+.84f,x})Box("Metal window frame",geometry,new Vector3(xx,3.05f,3.055f),new Vector3(.035f,.85f,.07f),steel);for(int i=0;i<5;i++)Box("Wire glass grille",geometry,new Vector3(x-.64f+i*.32f,3.05f,3.065f),new Vector3(.008f,.78f,.008f),steel);}
  Box("Mirror steel backing",geometry,new Vector3(1.15f,1.84f,3.09f),new Vector3(.66f,1.04f,.06f),steel);
  Box("Dulled mirror glass",geometry,new Vector3(1.15f,1.84f,3.052f),new Vector3(.58f,.96f,.012f),alternate);
  Box("Mirror shelf",geometry,new Vector3(1.15f,1.28f,2.99f),new Vector3(.72f,.045f,.22f),steel);
  Box("Wall hooks backing",geometry,new Vector3(10.55f,1.73f,3.10f),new Vector3(1.48f,.13f,.05f),wood);
  for(int i=0;i<4;i++)Bar("Bent coat hook",geometry,new Vector3(10+i*.36f,1.74f,3.06f),new Vector3(10+i*.36f,1.82f,2.96f),.023f,steel);
  Box("Exhaust grille surround",geometry,new Vector3(10.75f,3.13f,3.08f),new Vector3(.67f,.45f,.08f),steel);Box("Exhaust grille dark opening",geometry,new Vector3(10.75f,3.13f,3.027f),new Vector3(.58f,.37f,.012f),dark);
  for(int i=0;i<6;i++)Box("Ventilation louvre",geometry,new Vector3(10.75f,2.99f+i*.055f,3.012f),new Vector3(.58f,.019f,.014f),steel);
  // Fixed variation keeps wear intentional and cheap: no extra textures or decals.
  for(int i=0;i<18;i++){float x=.25f+(i*37%117)*.10f;float y=.19f+(i*13%21)*.10f;Box("Small worn wall paint",geometry,new Vector3(x,y,3.163f),new Vector3(.045f+(i%3)*.04f,.018f,.005f),plaster);}
 }
 static void Fixture(float x,int index,List<Light> lights)
 {
  var f=G("Changing fluorescent "+index+" - twin tube",room,new Vector3(x,3.62f,1.90f));
  Box("Steel fluorescent trough",f,new Vector3(0,.04f,0),new Vector3(1.45f,.08f,.32f),steel);
  foreach(float z in new[]{-.09f,.09f}){Box("Ivory fluorescent tube",f,new Vector3(0,-.03f,z),new Vector3(1.26f,.035f,.035f),lens);foreach(float side in new[]{-1f,1f})Box("Bakelite tube socket",f,new Vector3(side*.65f,-.02f,z),new Vector3(.065f,.075f,.065f),dark);}
  var key=G("Diffuse fluorescent downlight",f,new Vector3(0,-.14f,0)).gameObject.AddComponent<Light>();key.transform.localRotation=Quaternion.Euler(90,0,0);key.type=LightType.Spot;key.range=6;key.spotAngle=135;key.innerSpotAngle=88;key.color=new Color(.79f,.86f,.78f);key.intensity=2.0f;key.shadows=LightShadows.Soft;key.shadowResolution=LightShadowResolution.Low;key.shadowBias=.025f;key.shadowNormalBias=.05f;lights.Add(key);
  var fill=G("Reflected fluorescent fill",f,new Vector3(0,-.30f,-.10f)).gameObject.AddComponent<Light>();fill.type=LightType.Point;fill.range=4.6f;fill.intensity=.55f;fill.color=key.color;fill.shadows=LightShadows.None;lights.Add(fill);
  var gameplay=f.gameObject.AddComponent<RecoilLight2D>();gameplay.intensity=.36f;gameplay.radius=4.6f;
 }
 static void Retire(Transform t){foreach(var v in t.GetComponentsInChildren<Recoil3DSpriteVisual>(true)){v.suppressed=true;v.Sync();EditorUtility.SetDirty(v);}foreach(var r in t.GetComponentsInChildren<Renderer>(true))r.enabled=false;}
 [MenuItem("Recoil/Art/Rebuild Workers Changing Room")]
 public static string Apply()
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play first");Directory.CreateDirectory(Folder);Directory.CreateDirectory(Output);
  var previous=SceneManager.GetActiveScene();var scene=SceneManager.GetSceneByPath(PathScene);bool added=!scene.isLoaded;if(added)scene=EditorSceneManager.OpenScene(PathScene,OpenSceneMode.Additive);SceneManager.SetActiveScene(scene);
  try
  {
   var world=All<RecoilPrototype>(scene).Single();if(world.transform.Find(Marker))return "Already rebuilt";
   string backup="Backups/ChangingRoomRebuild/"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity";Directory.CreateDirectory(System.IO.Path.GetDirectoryName(backup));if(!EditorSceneManager.SaveScene(scene,backup,true))throw new Exception("Backup failed");
   var physics=All<Collider2D>(scene).Select(c=>c.GetInstanceID()+"|"+c.bounds.ToString("F6")).ToArray();var doors=All<RecoilDoor>(scene).Select(d=>EditorJsonUtility.ToJson(d)).ToArray();
   var old=All<Transform>(scene).Where(t=>t.name=="Cabinet"&&t.parent.name=="Room architecture - editable").ToArray();if(old.Length!=7&&old.Length!=0)throw new Exception("Unexpected retired cabinet count");foreach(var t in old){if(t.GetComponentsInChildren<Collider2D>(true).Length>0)throw new Exception("Cabinet owns gameplay collider");Object.DestroyImmediate(t.gameObject);}
   foreach(var t in All<Transform>(scene).Where(t=>t.name=="Changing bench"||t.name=="Bench leg"||t.name=="Dado").ToArray())Retire(t);
   room=G(Marker,world.transform,Vector3.zero);room.position=Vector3.zero;geometry=G("Static combined room surfaces",room,Vector3.zero);cube=Resources.GetBuiltinResource<Mesh>("Cube.fbx");
   paint=Mat("Faded industrial locker enamel",0x53645c,.35f);alternate=Mat("Mismatched repaired locker enamel",0x68746a,.35f);plaster=Mat("Damp worn changing room plaster",0x787c70);dado=Mat("Dark green oil painted dado",0x344c42);
   steel=Existing("Painted warm grey steel");wood=Mat("Worn bench timber",0x77664a);dark=Existing("Bakelite and rubber");paper=Existing("Old paper");cloth=Existing("Faded olive vinyl");
   rust=Mat("Oxidised exposed steel edges",0x49392b,.35f);lens=Existing("Fluorescent glass");
   Walls();Lockers();Bench(3.775f,2.75f,1);Bench(7.65f,3.0f,2);
   var aftermath=world.transform.Find("1978 conflict aftermath - editable");if(aftermath){var bed=aftermath.Find("Abandoned improvised treatment bed 38");if(bed){var delta=new Vector3(10.7f,bed.position.y,2.35f)-bed.position;bed.position+=delta;EditorUtility.SetDirty(bed);}}
   var lights=new List<Light>();Fixture(3.6f,1,lights);Fixture(8.6f,2,lights);
   foreach(var l in All<Light>(scene).Where(l=>!l.transform.IsChildOf(room)&&l.name!="Main Light")){l.enabled=false;EditorUtility.SetDirty(l);}
   var look=All<WarehouseAtmosphereTest>(scene).FirstOrDefault();if(look){look.originalLights=new Light[0];look.originalIntensities=new float[0];look.testIntensities=new float[0];look.decorativeLights=lights.ToArray();look.lamps=lights.Where(l=>l.type==LightType.Spot).ToArray();look.testAmbient=new Color(.075f,.085f,.075f);look.originalAmbient=look.testAmbient;look.scattering=.025f;look.softGlow=.025f;look.testStart=30;look.testEnd=65;look.ApplyLook();EditorUtility.SetDirty(look);}
   Bake(geometry,"Static changing-room details");Physics2D.SyncTransforms();
   if(!physics.SequenceEqual(All<Collider2D>(scene).Select(c=>c.GetInstanceID()+"|"+c.bounds.ToString("F6")))||!doors.SequenceEqual(All<RecoilDoor>(scene).Select(d=>EditorJsonUtility.ToJson(d))))throw new Exception("Gameplay geometry/doors changed");
   var interactive=All<WorkerLocker>(scene).Single();interactive.BeginOpen();interactive.AdvanceOpening(1);if(!interactive.PageReady||Quaternion.Angle(interactive.doorHinge.localRotation,Quaternion.Euler(0,105,0))>.01f)throw new Exception("Door animation failed");interactive.CloseDoor();
   if(room.GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled).Any(r=>!r.GetComponent<TextMesh>()&&r.bounds.min.z<1.05f))throw new Exception("New furniture intersects walking lane");
   EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new Exception("Save failed");AssetDatabase.SaveAssets();Capture(scene,"Overview",new Vector3(6,1.6f,2),5.1f);Capture(scene,"Locker-detail",new Vector3(5.85f,1.25f,2.15f),1.7f);
   string report="PASS: removed seven filing cabinets; ten connected steel lockers, full hollow shells, louvred doors, latch/number plates; two slatted benches and shoe racks; layered wall/window/mirror/ventilation details; 2 twin fluorescent fixtures. Locker 109 opens to reserved empty page. Original collider bounds and door routes preserved. Door opens 105 degrees and resets. Furniture clear of walking lane. Static parts combined by material. Backup: "+backup;File.WriteAllText(Output+"/verification.txt",report);return report;
  }
  finally{if(added)EditorSceneManager.CloseScene(scene,true);if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);}
 }


 static bool SameTriangles(IList<string> a,IList<string> b)
 {
  if(a.Count!=b.Count)return false;
  Func<string,Tuple<string,Vector3Int[]>> parse=s=>{var halves=s.Split(':');var points=halves[1].Split('/').Select(p=>{var v=p.Split(',');return new Vector3Int(int.Parse(v[0]),int.Parse(v[1]),int.Parse(v[2]));}).ToArray();return Tuple.Create(halves[0],points);};
  var left=a.Select(parse).ToArray();var right=b.Select(parse).ToArray();var used=new bool[b.Count];
  foreach(var triangle in left){int match=-1;for(int i=0;i<right.Length;i++){if(used[i]||triangle.Item1!=right[i].Item1)continue;bool same=triangle.Item2.All(p=>right[i].Item2.Any(q=>Mathf.Abs(p.x-q.x)<=1&&Mathf.Abs(p.y-q.y)<=1&&Mathf.Abs(p.z-q.z)<=1));if(same){match=i;break;}}if(match<0)return false;used[match]=true;}return true;
 }
 static string TriangleKey(Material m,Vector3 a,Vector3 b,Vector3 c){Func<Vector3,string> key=v=>Mathf.RoundToInt(v.x*1000)+","+Mathf.RoundToInt(v.y*1000)+","+Mathf.RoundToInt(v.z*1000);var points=new[]{key(a),key(b),key(c)};Array.Sort(points,StringComparer.Ordinal);return m.name+":"+string.Join("/",points);}
 [MenuItem("Recoil/Art/Make Changing Benches Individually Editable")]
 public static string SeparateBenches()
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play first");
  var previous=SceneManager.GetActiveScene();var scene=SceneManager.GetSceneByPath(PathScene);bool added=!scene.isLoaded;if(added)scene=EditorSceneManager.OpenScene(PathScene,OpenSceneMode.Additive);
  try
  {
   var world=All<RecoilPrototype>(scene).Single();room=world.transform.Find(Marker);geometry=room.Find("Static combined room surfaces");
   if(room.Cast<Transform>().Count(t=>t.name.StartsWith("Bench "))==2)return "Benches already individually editable";
   var oldParents=geometry.Cast<Transform>().Where(t=>t.name.StartsWith("Bench ")).ToArray();if(oldParents.Length!=2||oldParents.Any(t=>t.childCount!=0))throw new Exception("Unexpected original bench hierarchy");
   string backup="Backups/SeparateChangingBenches/"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity";Directory.CreateDirectory(System.IO.Path.GetDirectoryName(backup));if(!EditorSceneManager.SaveScene(scene,backup,true))throw new Exception("Backup failed");
   var physics=All<Collider2D>(scene).Select(c=>c.GetInstanceID()+"|"+c.bounds.ToString("F6")).ToArray();
   var originalTriangles=new List<string>();int removed=0;
   foreach(var filter in geometry.GetComponentsInChildren<MeshFilter>(true))
   {
    var mesh=filter.sharedMesh;var vertices=mesh.vertices;var triangles=mesh.triangles;var keep=new List<int>();var renderer=filter.GetComponent<MeshRenderer>();
    for(int i=0;i<triangles.Length;i+=3){var a=filter.transform.TransformPoint(vertices[triangles[i]]);var b=filter.transform.TransformPoint(vertices[triangles[i+1]]);var c=filter.transform.TransformPoint(vertices[triangles[i+2]]);var centre=(a+b+c)/3;
     bool bench=centre.x>2.2f&&centre.x<9.3f&&centre.y>-.05f&&centre.y<.7f&&centre.z>1.1f&&centre.z<1.85f;
     if(bench){originalTriangles.Add(TriangleKey(renderer.sharedMaterial,a,b,c));removed++;}else keep.AddRange(new[]{triangles[i],triangles[i+1],triangles[i+2]});}
    if(keep.Count==triangles.Length)continue;
    if(keep.Count==0){Object.DestroyImmediate(filter.gameObject);continue;}
    var used=keep.Distinct().ToArray();var map=new Dictionary<int,int>();for(int i=0;i<used.Length;i++)map[used[i]]=i;
    var clean=new Mesh();clean.indexFormat=IndexFormat.UInt32;clean.vertices=used.Select(i=>vertices[i]).ToArray();var uv=mesh.uv;if(uv.Length==vertices.Length)clean.uv=used.Select(i=>uv[i]).ToArray();var normals=mesh.normals;if(normals.Length==vertices.Length)clean.normals=used.Select(i=>normals[i]).ToArray();clean.triangles=keep.Select(i=>map[i]).ToArray();clean.RecalculateBounds();
    string path=Folder+"/"+mesh.name+" - without benches.asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(saved){EditorUtility.CopySerialized(clean,saved);Object.DestroyImmediate(clean);clean=saved;}else AssetDatabase.CreateAsset(clean,path);filter.sharedMesh=clean;EditorUtility.SetDirty(filter);
   }
   if(removed!=720)throw new Exception("Expected exactly 720 bench triangles, found "+removed);
   foreach(var t in oldParents)Object.DestroyImmediate(t.gameObject);
   cube=Resources.GetBuiltinResource<Mesh>("Cube.fbx");wood=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Worn bench timber.mat");dado=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Dark green oil painted dado.mat");rust=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Oxidised exposed steel edges.mat");steel=Existing("Painted warm grey steel");dark=Existing("Bakelite and rubber");cloth=Existing("Faded olive vinyl");
   Bench(3.775f,2.75f,1);Bench(7.65f,3.0f,2);
   var parents=room.Cast<Transform>().Where(t=>t.name.StartsWith("Bench ")).ToArray();var rebuiltTriangles=new List<string>();
   foreach(var parent in parents)foreach(var filter in parent.GetComponentsInChildren<MeshFilter>()){var vertices=filter.sharedMesh.vertices;var triangles=filter.sharedMesh.triangles;var m=filter.GetComponent<MeshRenderer>().sharedMaterial;for(int i=0;i<triangles.Length;i+=3)rebuiltTriangles.Add(TriangleKey(m,filter.transform.TransformPoint(vertices[triangles[i]]),filter.transform.TransformPoint(vertices[triangles[i+1]]),filter.transform.TransformPoint(vertices[triangles[i+2]])));}
   if(!SameTriangles(originalTriangles,rebuiltTriangles))throw new Exception("Rebuilt bench appearance or placement changed");
   Physics2D.SyncTransforms();if(!physics.SequenceEqual(All<Collider2D>(scene).Select(c=>c.GetInstanceID()+"|"+c.bounds.ToString("F6"))))throw new Exception("Gameplay collision changed");
   EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
   string result="PASS: two independent bench parent objects; "+parents[0].childCount+" and "+parents[1].childCount+" separate component/prop children. Timber, screws, steel frames, feet, shoe racks, boots and towel remain editable. 720 triangles match original placement/materials at 1 mm precision. No bench geometry remains in static room meshes. Collisions unchanged. Backup: "+backup;
   File.WriteAllText(Output+"/separate-benches.txt",result);return result;
  }
  finally{if(added)EditorSceneManager.CloseScene(scene,true);if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);}
 }
 public static void Capture(Scene scene,string name,Vector3 centre,float size)
 {
  var go=new GameObject("Temporary changing room review camera");SceneManager.MoveGameObjectToScene(go,scene);var cam=go.AddComponent<Camera>();cam.scene=scene;cam.orthographic=false;cam.fieldOfView=32;cam.aspect=16f/9;cam.transform.position=centre+new Vector3(-.25f,.65f,-size*3.1f);cam.transform.LookAt(centre);cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=Color.black;
  var rt=new RenderTexture(1440,810,24);var tex=new Texture2D(1440,810,TextureFormat.RGB24,false);var before=RenderTexture.active;try{cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1440,810),0,0);tex.Apply();File.WriteAllBytes(Output+"/"+name+".png",tex.EncodeToPNG());}finally{cam.targetTexture=null;RenderTexture.active=before;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(tex);Object.DestroyImmediate(go);}
 }
}
