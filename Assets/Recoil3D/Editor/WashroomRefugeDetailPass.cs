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

public static class WashroomRefugeDetailPass
{
 public const string ScenePath="Assets/Scenes/3D/FactoryRooms/FactoryWashroomF2.unity";
 const string Folder="Assets/Art/Environment/Washroom",Output="output/washroom-refit",Marker="Washroom - rebuilt lighting and disordered treatment laundry";
 static Mesh cube,cylinder;
 static Material steel,dark,ceramic,linen,blood,rust,tube,deadTube,tile;
 static Transform root;
 static T[] All<T>(Scene s)where T:Component{return s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true)).ToArray();}
 static Transform G(string n,Transform p,Vector3 at){var t=new GameObject(n).transform;t.SetParent(p,false);t.localPosition=at;return t;}
 static Transform B(string n,Transform p,Vector3 at,Vector3 size,Material mat){var t=G(n,p,at);t.localScale=size;t.gameObject.AddComponent<MeshFilter>().sharedMesh=cube;t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=mat;return t;}
 static Transform C(string n,Transform p,Vector3 at,float radius,float height,Material mat){var t=G(n,p,at);t.localScale=new Vector3(radius,height,radius);t.gameObject.AddComponent<MeshFilter>().sharedMesh=cylinder;t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=mat;return t;}
 static void R(string n,Transform p,Vector3 a,Vector3 b,float radius,Material mat){var t=C(n,p,(a+b)*.5f,radius,(a-b).magnitude,mat);t.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);}
 static Material Load(string path){var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m)throw new Exception("Missing material "+path);return m;}
 static Material Mat(string n,int colour){string path=Folder+"/"+n+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Recoil/3D/Weathered Surface"));AssetDatabase.CreateAsset(m,path);}m.color=RecoilPrototype.Hex(colour);m.SetFloat("_Glossiness",.14f);m.enableInstancing=true;EditorUtility.SetDirty(m);return m;}
 static void Materials(){cube=Resources.GetBuiltinResource<Mesh>("Cube.fbx");cylinder=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Art/Environment/ManagerOffice/Cylinder12.asset");steel=Load("Assets/Art/Environment/ManagerOffice/Painted warm grey steel.mat");dark=Load("Assets/Art/Environment/ManagerOffice/Bakelite and rubber.mat");ceramic=Load("Assets/Recoil3D/ConflictAftermath/Worn ivory ceramic.mat");linen=Load("Assets/Recoil3D/ConflictAftermath/Dusty bedding and bandage linen.mat");blood=Load("Assets/Recoil3D/ConflictAftermath/Old dried blood on fabric.mat");rust=Load("Assets/Art/Environment/ChangingRoom/Oxidised exposed steel edges.mat");tube=Load("Assets/Art/Environment/ManagerOffice/Fluorescent glass.mat");deadTube=Mat("Dead fluorescent glass - no emission",0x343c38);tile=Load("Assets/Recoil3D/ConflictAftermath/Stained bathhouse tile.mat");}
 static Mesh Ribbon(int seed)
 {
  string path=Folder+"/Shared loose gauze strip "+seed+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(saved)return saved;
  var v=new List<Vector3>();var uv=new List<Vector2>();var plain=new List<int>();var stained=new List<int>();int n=28;
  for(int i=0;i<=n;i++){float t=i/(float)n;for(int j=0;j<3;j++){float edge=(j-1)*.033f;float x=t-.5f;float z=Mathf.Sin(t*7+seed)*.105f+edge;float y=.017f+Mathf.Pow(Mathf.Sin(t*6+seed),2)*.018f+Mathf.Sin(t*19+seed)*.008f;v.Add(new Vector3(x,y,z));uv.Add(new Vector2(t,j*.5f));}}
  for(int i=0;i<n;i++)for(int j=0;j<2;j++){int k=i*3+j;var list=((i+seed*3)%19<5)?stained:plain;list.AddRange(new[]{k,k+3,k+1,k+1,k+3,k+4});}
  int count=v.Count;v.AddRange(v.ToArray());uv.AddRange(uv.ToArray());foreach(var list in new[]{plain,stained}){var ts=list.ToArray();for(int i=0;i<ts.Length;i+=3)list.AddRange(new[]{ts[i]+count,ts[i+2]+count,ts[i+1]+count});}
  var mesh=new Mesh();mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.subMeshCount=2;mesh.SetTriangles(plain,0);mesh.SetTriangles(stained,1);mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);return mesh;
 }
 static void Strip(string n,Transform p,Vector3 at,float angle,float scale,int seed)
 {var g=G(n,p,at);g.localRotation=Quaternion.Euler(0,angle,0);g.localScale=Vector3.one*scale;g.gameObject.AddComponent<MeshFilter>().sharedMesh=Ribbon(seed);g.gameObject.AddComponent<MeshRenderer>().sharedMaterials=new[]{linen,blood};}
 static Mesh StainMesh()
 {
  string path=Folder+"/Shared dry cloth stain footprint.asset";var m=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(m)return m;
  var v=new List<Vector3>{Vector3.zero};for(int i=0;i<12;i++){float a=i*Mathf.PI*2/12;float r=.42f+(i%3)*.035f;v.Add(new Vector3(Mathf.Cos(a)*r,0,Mathf.Sin(a)*r));}var triangles=new List<int>();for(int i=0;i<12;i++)triangles.AddRange(new[]{0,1+(i+1)%12,1+i});m=new Mesh();m.SetVertices(v);m.SetTriangles(triangles,0);m.RecalculateNormals();AssetDatabase.CreateAsset(m,path);return m;
 }
 static void DryStain(Vector3 at,Vector3 size,float yaw)
 {var g=G("Old dried stain beneath discarded linen",root,at);g.localScale=size;g.localRotation=Quaternion.Euler(0,yaw,0);g.gameObject.AddComponent<MeshFilter>().sharedMesh=StainMesh();var r=g.gameObject.AddComponent<MeshRenderer>();r.sharedMaterial=blood;r.shadowCastingMode=ShadowCastingMode.Off;}
 static void Fixture(float x,int index,bool broken,List<Light> working)
 {
  var f=G(broken?"Washroom fluorescent 2 - BROKEN":"Washroom fluorescent 1 - working",root,new Vector3(x,3.66f,2.20f));
  B("Enamel reflector and backplate",f,new Vector3(0,.04f,0),new Vector3(1.40f,.08f,.33f),steel);
  foreach(float z in new[]{-.09f,.09f})
  {
   foreach(float end in new[]{-1f,1f})B("Bakelite lampholder",f,new Vector3(end*.63f,-.014f,z),new Vector3(.06f,.065f,.06f),dark);
   if(broken&&z>0){var t=C("Dead loosened tube",f,new Vector3(.10f,-.08f,z),.017f,.91f,deadTube);t.localRotation=Quaternion.Euler(0,0,80);B("Burnt ballast end",f,new Vector3(-.46f,-.02f,z),new Vector3(.19f,.025f,.045f),dark);}
   else{var t=C(broken?"Unlit tube":"Fluorescent tube",f,new Vector3(0,-.03f,z),.018f,1.23f,broken?deadTube:tube);t.localRotation=Quaternion.Euler(0,0,90);}
  }
  var source=f.gameObject.AddComponent<RecoilLight2D>();source.intensity=broken?0:.34f;source.radius=4.5f;source.enabled=!broken;
  var key=G("Downward diffuse source",f,new Vector3(0,-.13f,0)).gameObject.AddComponent<Light>();key.type=LightType.Spot;key.transform.localRotation=Quaternion.Euler(90,0,0);key.spotAngle=133;key.innerSpotAngle=88;key.range=7;key.color=new Color(.80f,.88f,.82f);key.intensity=broken?0:2.4f;key.shadows=broken?LightShadows.None:LightShadows.Soft;key.shadowResolution=LightShadowResolution.Low;key.shadowBias=.025f;key.shadowNormalBias=.06f;key.enabled=!broken;
  var fill=G("Reflected fluorescent source",f,new Vector3(-.15f,-.28f,-.12f)).gameObject.AddComponent<Light>();fill.type=LightType.Point;fill.range=5.4f;fill.intensity=broken?0:.45f;fill.color=key.color;fill.shadows=LightShadows.None;fill.enabled=!broken;if(!broken){working.Add(key);working.Add(fill);}
 }
 static void Details()
 {
  var g=G("Aged washroom fittings and service details",root,Vector3.zero);
  foreach(float x in new[]{3.4f,4.9f})
  {
   var b=G("Washbasin accessories - "+x,g,new Vector3(x,0,4.25f));
   B("Ceramic soap shelf",b,new Vector3(.37f,1.08f,.24f),new Vector3(.19f,.035f,.16f),ceramic);B("Worn bar of soap",b,new Vector3(.37f,1.115f,.24f),new Vector3(.11f,.035f,.075f),linen);
   foreach(float side in new[]{-1f,1f}){R("Towel rail wall bracket",b,new Vector3(side*.31f,.73f,.16f),new Vector3(side*.31f,.73f,-.03f),.012f,steel);}R("Front towel rail",b,new Vector3(-.31f,.73f,-.03f),new Vector3(.31f,.73f,-.03f),.012f,steel);
   for(int i=0;i<4;i++){var p=B("Worn mirror edge",b,new Vector3(-.35f+i*.20f,2.30f,.314f),new Vector3(.09f,.012f,.003f),rust);p.localRotation=Quaternion.Euler(0,0,(i%2==0?11:-6));}
  }
  R("Lower plumbing manifold",g,new Vector3(2.78f,.40f,4.58f),new Vector3(5.48f,.40f,4.58f),.028f,steel);
  foreach(float x in new[]{2.9f,4.15f,5.4f})B("Pipe fixing strap",g,new Vector3(x,.40f,4.56f),new Vector3(.05f,.10f,.05f),rust);
  var drain=G("Floor drain - complete grille",g,new Vector3(4.2f,.013f,3.05f));B("Drain recess",drain,Vector3.zero,new Vector3(.65f,.018f,.25f),dark);for(int i=0;i<9;i++)B("Drain grille bar",drain,new Vector3(-.28f+i*.07f,.014f,0),new Vector3(.024f,.018f,.23f),steel);
  var bucket=G("Abandoned enamel bucket - whole prop",g,new Vector3(5.7f,.03f,4.20f));C("Bucket body",bucket,new Vector3(0,.14f,0),.13f,.26f,steel);C("Bucket dark opening",bucket,new Vector3(0,.275f,0),.112f,.004f,dark);R("Bucket bail left",bucket,new Vector3(-.12f,.22f,0),new Vector3(-.12f,.38f,0),.007f,steel);R("Bucket bail top",bucket,new Vector3(-.12f,.38f,0),new Vector3(.12f,.38f,0),.007f,steel);R("Bucket bail right",bucket,new Vector3(.12f,.38f,0),new Vector3(.12f,.22f,0),.007f,steel);
  for(int i=0;i<6;i++){float x=.45f+i*1.36f;B("Stained grout at wall base",g,new Vector3(x,.10f,4.654f),new Vector3(.24f,.08f,.004f),rust);}
 }
 public static string Apply()
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play first");Directory.CreateDirectory(Folder);Directory.CreateDirectory(Output);
  var prev=SceneManager.GetActiveScene();var s=SceneManager.GetSceneByPath(ScenePath);bool added=!s.isLoaded;if(added)s=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);SceneManager.SetActiveScene(s);
  try
  {
   var world=All<RecoilPrototype>(s).Single();if(world.transform.Find(Marker))return "Already refitted";
   string backup="Backups/WashroomRefit/"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity";Directory.CreateDirectory(System.IO.Path.GetDirectoryName(backup));if(!EditorSceneManager.SaveScene(s,backup,true))throw new Exception("Backup failed");
   var physics=All<Collider2D>(s).Select(c=>c.GetInstanceID()+"|"+c.bounds.ToString("F6")).ToArray();var doors=All<RecoilDoor>(s).Select(d=>EditorJsonUtility.ToJson(d)).ToArray();var cubicles=All<RecoilCubicleDoor>(s).ToArray();var cubicleState=cubicles.Select(c=>c.GetInstanceID()+"|"+EditorJsonUtility.ToJson(c)+"|"+c.hinge.position).ToArray();
   var obsolete=new[]{"Crumpled stained work fabric","Twisted loose bloodied bandage","Work shirt on hanging hook","Overhead changing rail","Rusty hanging chain","Clothes hook","Clothes drying rod"};int oldCloth=0;
   foreach(var t in All<Transform>(s).Where(t=>obsolete.Contains(t.name)).ToArray()){if(t.GetComponentsInChildren<Collider2D>(true).Length>0)throw new Exception("Old cloth unexpectedly has collision");Object.DestroyImmediate(t.gameObject);oldCloth++;}
   foreach(var text in All<TextMesh>(s).Where(t=>t.text.Trim()=="WASH BASINS").ToArray())Object.DestroyImmediate(text.gameObject);
   var oldFixtures=All<Transform>(s).Where(t=>t.name=="Ceiling fixture").ToArray();foreach(var f in oldFixtures){f.gameObject.SetActive(false);EditorUtility.SetDirty(f.gameObject);}
   var world3=world.GetComponent<Recoil3DWorld>();foreach(var l in All<Light>(s))if(!world3||l!=world3.playerVisualLight){l.enabled=false;EditorUtility.SetDirty(l);}
   root=G(Marker,world.transform,Vector3.zero);root.position=Vector3.zero;Materials();var working=new List<Light>();Fixture(3.2f,1,false,working);Fixture(7.45f,2,true,working);Details();
   var tubs=All<Transform>(s).Where(t=>t.name.StartsWith("Overflowing laundry trough ")).OrderBy(t=>t.name).ToArray();if(tubs.Length!=3)throw new Exception("Expected three existing laundry troughs");
   var at=new[]{new Vector3(2.25f,-.07f,3.10f),new Vector3(4.50f,-.07f,3.48f),new Vector3(5.55f,-.07f,2.65f)};float[] turns={-17,9,27};
   for(int i=0;i<tubs.Length;i++){tubs[i].position=at[i];tubs[i].rotation=Quaternion.Euler(0,turns[i],0);tubs[i].SetParent(root,true);EditorUtility.SetDirty(tubs[i]);foreach(float side in new[]{-1f,1f})R("Laundry trough handle",tubs[i],new Vector3(side*.45f,.30f,-.12f),new Vector3(side*.45f,.30f,.12f),.014f,steel);}
   G("Clothing heap anchor - wall",root,new Vector3(.95f,.012f,3.76f)).localRotation=Quaternion.Euler(0,13,0);
   G("Clothing heap anchor - trough 1",tubs[0],new Vector3(.02f,.27f,-.015f)).localRotation=Quaternion.Euler(0,-27,0);
   G("Clothing heap anchor - trough 2",tubs[1],new Vector3(-.04f,.27f,.02f)).localRotation=Quaternion.Euler(0,47,0);
   G("Bandage heap anchor - trough 3",tubs[2],new Vector3(.02f,.27f,0)).localRotation=Quaternion.Euler(0,16,0);
   G("Bandage heap anchor - floor",root,new Vector3(3.34f,.012f,2.40f)).localRotation=Quaternion.Euler(0,-31,0);
   G("Bandage heap anchor - basin",root,new Vector3(3.54f,.935f,4.22f)).localRotation=Quaternion.Euler(0,21,0);
   Strip("Bandage trailing from trough",tubs[0],new Vector3(.27f,.30f,-.05f),84,.65f,0);
   Strip("Loose gauze beside trough",root,new Vector3(2.90f,.014f,2.36f),37,.91f,1);
   Strip("Discarded gauze near drain",root,new Vector3(4.47f,.017f,2.65f),-21,.58f,2);
   Strip("Narrow bloodied strip",root,new Vector3(5.70f,.015f,2.05f),112,.75f,3);
   Strip("Dry dressing beneath wall pile",root,new Vector3(1.26f,.013f,3.08f),14,.90f,4);
   DryStain(new Vector3(.90f,.006f,3.74f),new Vector3(1.35f,1,.75f),14);DryStain(new Vector3(3.40f,.006f,2.35f),new Vector3(.32f,1,.20f),53);
   var look=All<WarehouseAtmosphereTest>(s).FirstOrDefault();if(look){look.originalLights=new Light[0];look.originalIntensities=new float[0];look.testIntensities=new float[0];look.decorativeLights=working.ToArray();look.lamps=working.Where(l=>l.type==LightType.Spot).ToArray();look.testAmbient=new Color(.055f,.068f,.062f);look.originalAmbient=look.testAmbient;look.softGlow=.023f;look.scattering=.025f;look.testStart=32;look.testEnd=68;look.ApplyLook();EditorUtility.SetDirty(look);}
   Physics2D.SyncTransforms();if(!physics.SequenceEqual(All<Collider2D>(s).Select(c=>c.GetInstanceID()+"|"+c.bounds.ToString("F6")))||!doors.SequenceEqual(All<RecoilDoor>(s).Select(d=>EditorJsonUtility.ToJson(d))))throw new Exception("Gameplay collision/exit changed");if(!cubicleState.SequenceEqual(cubicles.Select(c=>c.GetInstanceID()+"|"+EditorJsonUtility.ToJson(c)+"|"+c.hinge.position)))throw new Exception("Cubicle interaction changed");
   var broken=root.Find("Washroom fluorescent 2 - BROKEN");if(broken.GetComponentsInChildren<Light>().Any(l=>l.enabled||l.intensity!=0)||broken.GetComponent<RecoilLight2D>().enabled)throw new Exception("Broken fitting emits light");
   EditorSceneManager.MarkSceneDirty(s);EditorSceneManager.SaveScene(s);AssetDatabase.SaveAssets();ChangingRoomRebuild.Capture(s,"Washroom-overview",new Vector3(4.5f,1.65f,2.75f),4.3f);
   string report="PASS: two separate complete fluorescent parents, one permanently dark/broken (no enabled light/emission). Retired "+oldCloth+" repeated cloth/rail pieces; relocated three grounded laundry troughs with varied angles; floor gauze and dry staining; model anchors ready for generated garment/bandage heaps. Basins, cubicle interaction refs, exits and collision bounds preserved. Backup: "+backup;File.WriteAllText(Output+"/verification.txt",report);return report;
  }
  finally{if(added)EditorSceneManager.CloseScene(s,true);if(prev.IsValid()&&prev.isLoaded)SceneManager.SetActiveScene(prev);}
 }
 public static string Install(string kind,string assetPath)
 {
  var s=SceneManager.GetSceneByPath(ScenePath);if(!s.isLoaded)s=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
  var anchorName=kind=="clothes"?"Clothing heap anchor -":"Bandage heap anchor -";var anchors=All<Transform>(s).Where(t=>t.name.StartsWith(anchorName)).ToArray();var asset=AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);if(!asset)throw new Exception("Generated model not imported");int added=0;
  foreach(var anchor in anchors){if(anchor.childCount>0)continue;var model=(GameObject)PrefabUtility.InstantiatePrefab(asset,s);model.name=kind=="clothes"?"Bloodied work garments - whole heap":"Discarded rolled and loose bandages - whole prop";model.transform.SetParent(anchor,false);var renderers=model.GetComponentsInChildren<Renderer>();if(renderers.Length==0)throw new Exception("Model has no renderers");var b=renderers[0].bounds;foreach(var r in renderers)b.Encapsulate(r.bounds);
   bool wall=anchor.name.EndsWith("wall");bool floor=anchor.name.EndsWith("floor");bool basin=anchor.name.EndsWith("basin");Vector3 size=wall?new Vector3(1.52f,.60f,.95f):kind=="clothes"?new Vector3(.69f,.37f,.48f):basin?new Vector3(.28f,.15f,.21f):floor?new Vector3(.51f,.17f,.35f):new Vector3(.64f,.32f,.47f);
   float scale=Mathf.Min(size.x/b.size.x,size.y/b.size.y,size.z/b.size.z);model.transform.localScale*=scale;b=renderers[0].bounds;foreach(var r in renderers)b.Encapsulate(r.bounds);model.transform.position+=new Vector3(anchor.position.x-b.center.x,anchor.position.y-b.min.y,anchor.position.z-b.center.z);added++;
  }
  EditorSceneManager.MarkSceneDirty(s);EditorSceneManager.SaveScene(s);Directory.CreateDirectory(Output);File.WriteAllText(Output+"/"+kind+"-model.txt","Installed "+added+" independent prop instances from shared asset "+assetPath);return "Installed "+added+" "+kind+" model instances";
 }
 public static string CaptureFinal()
 {
  var previous=SceneManager.GetActiveScene();var s=SceneManager.GetSceneByPath(ScenePath);if(!s.isLoaded)s=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);SceneManager.SetActiveScene(s);
  try{var world=All<Recoil3DWorld>(s).Single();world.ApplyAtmosphere();foreach(var look in All<WarehouseAtmosphereTest>(s))look.ApplyLook();ChangingRoomRebuild.Capture(s,"Washroom-overview",new Vector3(4.5f,1.65f,2.75f),4.3f);ChangingRoomRebuild.Capture(s,"Washroom-laundry-detail",new Vector3(1.8f,.57f,3.50f),1.9f);return "Captured final washroom";}finally{if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);}
 }
}
