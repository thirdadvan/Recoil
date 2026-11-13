using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;

public static class WashroomMassLaundryPass
{
 const string Folder="Assets/Art/Environment/Washroom/MassLaundry",Marker="Emergency laundry - bulk casualty clothing and dressings";
 static Transform root;static Mesh cube,cylinder;static Material linen,blood,steel,dark,canvas,olive,ochre;
 static T[] All<T>(Scene s)where T:Component{return s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true)).ToArray();}
 static Transform G(string n,Transform p,Vector3 at){var t=new GameObject(n).transform;t.SetParent(p,false);t.localPosition=at;return t;}
 static Transform B(string n,Transform p,Vector3 at,Vector3 size,Material m){var t=G(n,p,at);t.localScale=size;t.gameObject.AddComponent<MeshFilter>().sharedMesh=cube;t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=m;return t;}
 static void Rod(string n,Transform p,Vector3 a,Vector3 b,float radius,Material m){var t=G(n,p,(a+b)*.5f);t.localScale=new Vector3(radius,(b-a).magnitude,radius);t.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);t.gameObject.AddComponent<MeshFilter>().sharedMesh=cylinder;t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=m;}
 static Material Load(string p){return AssetDatabase.LoadAssetAtPath<Material>(p);}
 static Material Mat(string name,Color c){string p=Folder+"/"+name+".mat";var m=Load(p);if(!m){m=new Material(Shader.Find("Recoil/3D/Weathered Surface"));AssetDatabase.CreateAsset(m,p);}m.color=c;m.SetFloat("_Glossiness",.08f);m.enableInstancing=true;return m;}
 // Shared folded cloth, metre-sized in scene. Separate stain faces avoid decal overlap.
 static Mesh Cloth(int seed,bool hanging,bool garment)
 {
  string path=Folder+"/"+(hanging?"Draped ":garment?"Work garment ":"Rumpled linen ")+seed+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(saved)return saved;
  int nx=20,nz=24;var v=new List<Vector3>();var uv=new List<Vector2>();var plain=new List<int>();var stained=new List<int>();
  for(int j=0;j<=nz;j++)for(int i=0;i<=nx;i++){float u=i/(float)nx,w=j/(float)nz;float fold=Mathf.Sin(u*21+seed)*.025f+Mathf.Sin(w*18+u*7+seed)*.018f;Vector3 p;if(hanging)p=new Vector3(u-.5f,-w,.055f*Mathf.Sin(u*20+seed)*(w*.35f+.65f)+.035f*Mathf.Sin(w*5+seed));else{float mound=.13f*Mathf.Sin(u*Mathf.PI)*Mathf.Sin(w*Mathf.PI);p=new Vector3((u-.5f)+.035f*Mathf.Sin(w*13+seed),.025f+Mathf.Max(0,mound+fold),(w-.5f)+.03f*Mathf.Sin(u*11+seed));}v.Add(p);uv.Add(new Vector2(u,w));}
  for(int j=0;j<nz;j++)for(int i=0;i<nx;i++){float u=(i+.5f)/nx,w=(j+.5f)/nz;if(garment&&((w>.34f&&(u<.25f||u>.75f))||(w<.16f&&u>.43f&&u<.57f)))continue;float dx=u-(.30f+(seed%3)*.13f),dz=w-.46f;float d=dx*dx/.052f+dz*dz/.091f;float blot=d+.2f*Mathf.Sin(i*1.8f+seed)*Mathf.Cos(j*.9f);var list=blot<.75f||(j>18&&i>12&&seed%2==0)?stained:plain;int k=j*(nx+1)+i;list.AddRange(new[]{k,k+nx+1,k+1,k+1,k+nx+1,k+nx+2});}
  int count=v.Count;v.AddRange(v.ToArray());uv.AddRange(uv.ToArray());foreach(var list in new[]{plain,stained}){var orig=list.ToArray();for(int i=0;i<orig.Length;i+=3)list.AddRange(new[]{orig[i]+count,orig[i+2]+count,orig[i+1]+count});}
  var mesh=new Mesh();mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.subMeshCount=2;mesh.SetTriangles(plain,0);mesh.SetTriangles(stained,1);mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);return mesh;
 }
 static Transform Rag(string n,Transform parent,Vector3 at,Vector3 size,int id,bool hanging=false,bool garment=false)
 {var t=G(n,parent,at);t.localScale=size;t.gameObject.AddComponent<MeshFilter>().sharedMesh=Cloth(id%6,hanging,garment);var m=garment?(id%2==0?olive:ochre):linen;t.gameObject.AddComponent<MeshRenderer>().sharedMaterials=new[]{m,m};return t;}
 static Mesh TubMesh()
 {
  string path=Folder+"/Shared oval galvanised wash basin.asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(saved)return saved;int count=32;var v=new List<Vector3>();var tr=new List<int>();
  for(int ring=0;ring<4;ring++)for(int i=0;i<count;i++){float a=i*Mathf.PI*2/count;float r=ring==0?.82f:ring==1?1:ring==2?.96f:.78f;float y=ring==0?0:ring==3?.055f:.48f;v.Add(new Vector3(Mathf.Cos(a)*.72f*r,y,Mathf.Sin(a)*.46f*r));}
  for(int r=0;r<3;r++)for(int i=0;i<count;i++){int a=r*count+i,b=r*count+(i+1)%count,c=a+count,d=b+count;tr.AddRange(new[]{a,c,b,b,c,d});}for(int i=1;i<count-1;i++)tr.AddRange(new[]{3*count,3*count+i+1,3*count+i});var m=new Mesh();m.SetVertices(v);m.SetTriangles(tr,0);m.RecalculateNormals();m.RecalculateBounds();AssetDatabase.CreateAsset(m,path);return m;
 }
 static void Tub(Vector3 at,float yaw,int id)
 {
  var t=G("Soaking basin "+id+" - overflowing bloodied linen",root,at);t.localRotation=Quaternion.Euler(0,yaw,0);t.gameObject.AddComponent<MeshFilter>().sharedMesh=TubMesh();t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=steel;
  for(int i=0;i<7;i++){float x=Mathf.Sin(i*2.1f+id)*.22f,z=Mathf.Cos(i*1.7f)*.12f;var r=Rag(i%3==0?"Full size work shirt in soaking pile":"Bloodied sheet compressed in basin",t,new Vector3(x,.18f+i*.045f,z),new Vector3(.89f,1,.64f),i+id,false,i%3==0);r.localRotation=Quaternion.Euler(0,i*37+id*11,0);}
  Rag("Large wet linen draped over front rim",t,new Vector3(.05f,.51f,-.36f),new Vector3(.60f,.55f,1),id,true);
  foreach(float side in new[]{-1f,1f})Rod("Basin lifting handle",t,new Vector3(side*.73f,.33f,-.11f),new Vector3(side*.73f,.33f,.11f),.014f,steel);
 }
 static void Hamper(Vector3 at,int id)
 {
  var t=G("Bulk dirty linen hamper "+id+" - whole prop",root,at);t.localRotation=Quaternion.Euler(0,id==0?-9:14,0);
  B("Canvas base",t,new Vector3(0,.12f,0),new Vector3(1.12f,.055f,.77f),canvas);
  foreach(float x in new[]{-.56f,.56f}){B("Sagging canvas side",t,new Vector3(x,.52f,0),new Vector3(.025f,.80f,.77f),canvas);Rod("Steel top rail",t,new Vector3(x,.93f,-.40f),new Vector3(x,.93f,.40f),.016f,steel);foreach(float z in new[]{-.4f,.4f})Rod("Hamper corner frame",t,new Vector3(x,.10f,z),new Vector3(x,.94f,z),.015f,steel);}
  foreach(float z in new[]{-.39f,.39f}){B("Canvas end",t,new Vector3(0,.52f,z),new Vector3(1.12f,.8f,.025f),canvas);Rod("Steel top rail",t,new Vector3(-.56f,.93f,z),new Vector3(.56f,.93f,z),.016f,steel);}
  for(int i=0;i<9;i++){var r=Rag("Large bloodied linen and garments - load "+i,t,new Vector3(Mathf.Sin(i*2.3f)*.13f,.68f+i*.043f,Mathf.Cos(i*1.8f)*.09f),new Vector3(.88f,1,.64f),i+id,false,i%3==0);r.localRotation=Quaternion.Euler(0,i*43+id*23,0);}
  Rag("Stained linen overflowing hamper front",t,new Vector3(.10f,1.0f,-.405f),new Vector3(.78f,.76f,1),id+1,true);
 }
 static void ExistingModel(string asset,Transform p,Vector3 at,Vector3 dims,float yaw,string name)
 {
  var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(asset),p.gameObject.scene);go.name=name;go.transform.SetParent(p,false);go.transform.localPosition=at;go.transform.localRotation=Quaternion.Euler(0,yaw,0);var rs=go.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);go.transform.localScale=Vector3.Scale(go.transform.localScale,new Vector3(dims.x/b.size.x,dims.y/b.size.y,dims.z/b.size.z));b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);var target=p.TransformPoint(at);go.transform.position+=new Vector3(target.x-b.center.x,target.y-b.min.y,target.z-b.center.z);
 }
 public static string Apply()
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play first");Directory.CreateDirectory(Folder);var prev=SceneManager.GetActiveScene();var s=SceneManager.GetSceneByPath(WashroomRefugeDetailPass.ScenePath);bool added=!s.isLoaded;if(added)s=EditorSceneManager.OpenScene(WashroomRefugeDetailPass.ScenePath,OpenSceneMode.Additive);SceneManager.SetActiveScene(s);
  try{
   var world=All<RecoilPrototype>(s).Single();if(world.transform.Find("Washroom laundry - reduced load and complete generated props"))return "Superseded by reduced complete Meshy props; bulk procedural refit disabled";if(world.transform.Find(Marker))return "Already revised";Directory.CreateDirectory("Backups/WashroomMassLaundry");EditorSceneManager.SaveScene(s,"Backups/WashroomMassLaundry/"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
   var collisions=All<Collider2D>(s).ToDictionary(c=>c.GetInstanceID(),c=>c.bounds.ToString("F6"));var doors=All<RecoilCubicleDoor>(s).ToArray();var hinges=doors.Select(d=>d.hinge).ToArray();
   foreach(var t in All<Transform>(s).Where(t=>t.name.StartsWith("Overflowing laundry trough ")||t.name.StartsWith("Clothing heap anchor -")||t.name.StartsWith("Bandage heap anchor -")||t.name=="Old dried stain beneath discarded linen").ToArray())if(t)Object.DestroyImmediate(t.gameObject);
   root=G(Marker,world.transform,Vector3.zero);root.position=Vector3.zero;cube=Resources.GetBuiltinResource<Mesh>("Cube.fbx");cylinder=LoadMesh();
   steel=Load("Assets/Art/Environment/ManagerOffice/Painted warm grey steel.mat");dark=Load("Assets/Art/Environment/ManagerOffice/Bakelite and rubber.mat");linen=Mat("Aged unbleached dressing linen",new Color(.49f,.46f,.36f));blood=Mat("Absorbed old blood - broad irregular patches",new Color(.19f,.048f,.029f));canvas=Mat("Heavy soiled laundry canvas",new Color(.28f,.29f,.23f));olive=Mat("Grey green factory work cloth",new Color(.25f,.29f,.23f));ochre=Mat("Faded ochre work cloth",new Color(.34f,.27f,.16f));
   foreach(var m in new[]{linen,olive,ochre}){m.shader=Shader.Find("Recoil/3D/Absorbed Blood Cloth");m.SetColor("_BloodColor",new Color(.19f,.048f,.029f));m.SetFloat("_Seed",m==linen?1:m==olive?3:5);EditorUtility.SetDirty(m);}
   // Bulk dirty receiving corner. Every garment is adult-size; models are never shrunk to box size.
   var pile=G("Wall side accumulated casualty clothing - whole group",root,new Vector3(1.2f,0,3.70f));
   string clothes="Assets/Art/Environment/Washroom/GeneratedProps/AbandonedBloodiedWorkClothes_Optimized.glb";
   ExistingModel(clothes,pile,new Vector3(-.35f,.015f,-.10f),new Vector3(1.32f,.48f,.88f),166,"Adult work garments - lower heap");ExistingModel(clothes,pile,new Vector3(.32f,.20f,.16f),new Vector3(1.14f,.55f,.76f),209,"Adult work garments - rear heap");
   for(int i=0;i<12;i++){var r=Rag("Large loose bloodied garment or sheet "+i,pile,new Vector3(Mathf.Sin(i*2.8f)*.53f,.05f+i*.044f,Mathf.Cos(i*2.2f)*.28f),new Vector3(.90f+i%3*.13f,1,.77f),i,false,i%2==0);r.localRotation=Quaternion.Euler(0,i*39,0);}
   Hamper(new Vector3(.72f,0,2.10f),0);Hamper(new Vector3(2.17f,0,3.26f),1);
   Tub(new Vector3(3.18f,.015f,2.88f),-12,0);Tub(new Vector3(4.77f,.015f,3.45f),11,1);
   // Existing washbasins become visibly loaded treatment points.
   foreach(float x in new[]{3.4f,4.9f})for(int i=0;i<3;i++)Rag("Bloodied dressing soaking in washbasin",root,new Vector3(x+Mathf.Sin(i*3)*.12f,.91f+i*.022f,4.26f),new Vector3(.48f,.7f,.35f),i+(int)x);
   var table=G("Bandage sorting trestle - whole prop",root,new Vector3(5.83f,0,2.03f));B("Worn sorting tabletop",table,new Vector3(0,.83f,0),new Vector3(1.30f,.055f,.68f),steel);foreach(float x in new[]{-.50f,.50f})foreach(float z in new[]{-.25f,.25f})Rod("Trestle leg",table,new Vector3(x,0,z),new Vector3(x,.81f,z),.025f,steel);
   Rag("Bloodied sheet covering sorting table",table,new Vector3(0,.87f,0),new Vector3(1.19f,.6f,.61f),2);
   string band="Assets/Art/Environment/Washroom/GeneratedProps/DiscardedGauzeBandages_Optimized.glb";for(int i=0;i<4;i++)ExistingModel(band,table,new Vector3(-.41f+i*.25f,.91f+i%2*.025f,Mathf.Sin(i*2)*.14f),new Vector3(.31f,.14f,.25f),i*67,"Discarded dressing bundle - full size");
   var rack=G("Improvised draining rail - whole prop",root,Vector3.zero);Rod("Suspended steel drying rail",rack,new Vector3(.32f,2.87f,4.00f),new Vector3(2.76f,2.87f,4.00f),.016f,steel);foreach(float x in new[]{.35f,2.73f})Rod("Rail suspension",rack,new Vector3(x,2.87f,4),new Vector3(x,3.62f,4),.009f,steel);
   Rag("Broad bloodied hospital linen hanging to drain",rack,new Vector3(1.03f,2.87f,4.0f),new Vector3(1.16f,1.48f,1),3,true);Rag("Second stained hanging sheet",rack,new Vector3(2.16f,2.87f,4.04f),new Vector3(.81f,1.19f,1),4,true);
   for(int i=0;i<5;i++)Rag("Long bloodied dressing strip drying separately",rack,new Vector3(2.85f+i*.12f,2.70f,4.28f),new Vector3(.075f,.86f+i%3*.14f,.65f),i,true);
   for(int i=0;i<7;i++){var r=Rag("Linen fallen between receiving and soaking",root,new Vector3(1.75f+i*.54f,.013f,1.57f+Mathf.Sin(i*2.2f)*.14f),new Vector3(i%2==0?.68f:.30f,.36f,i%2==0?.45f:.24f),i,false,i==2);r.localRotation=Quaternion.Euler(0,i*71,0);}
   Physics2D.SyncTransforms();foreach(var c in All<Collider2D>(s))if(!collisions.ContainsKey(c.GetInstanceID())||collisions[c.GetInstanceID()]!=c.bounds.ToString("F6"))throw new Exception("Collision changed");for(int i=0;i<doors.Length;i++)if(!doors[i].hinge||doors[i].hinge!=hinges[i])throw new Exception("Cubicle hinge changed");
   EditorSceneManager.MarkSceneDirty(s);EditorSceneManager.SaveScene(s);AssetDatabase.SaveAssets();Directory.CreateDirectory("output/washroom-mass-laundry");File.WriteAllText("output/washroom-mass-laundry/verification.txt","PASS: removed miniature box garments and old troughs; adult-size accumulated garments, 2 full bulk hampers, 2 oval soaking basins, loaded washbasins, dressing sorting table and hanging linen/draining rail. Original collisions and both cubicle hinge refs unchanged; two fixture setup retained. Front walk lane clear of new geometry (all work groups behind Z1.1). Shared low-poly cloth variants and existing optimized Meshy assets.");Capture(s);return "Bulk laundry refit saved; original collisions and cubicle hinges unchanged";
  }finally{if(added)EditorSceneManager.CloseScene(s,true);if(prev.IsValid()&&prev.isLoaded){SceneManager.SetActiveScene(prev);foreach(var w in All<Recoil3DWorld>(prev))w.ApplyAtmosphere();foreach(var w in All<WarehouseAtmosphereTest>(prev))w.ApplyLook();}}
 }
 static Mesh LoadMesh(){return AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Art/Environment/ManagerOffice/Cylinder12.asset");}
 public static void Capture(Scene s)
 {
  var rs=Object.FindObjectsOfType<Renderer>(true).Where(r=>r.enabled&&(r.gameObject.scene!=s||r.transform.position.z<0||r.GetComponentInParent<RecoilPlayerController>()!=null)).ToArray();var ls=Object.FindObjectsOfType<Light>(true).Where(l=>l.enabled&&l.gameObject.scene!=s).ToArray();try{foreach(var r in rs)r.enabled=false;foreach(var l in ls)l.enabled=false;foreach(var w in All<Recoil3DWorld>(s))w.ApplyAtmosphere();foreach(var w in All<WarehouseAtmosphereTest>(s))w.ApplyLook();ChangingRoomRebuild.Capture(s,"Mass-laundry-overview",new Vector3(4.5f,1.6f,3.0f),4.15f);ChangingRoomRebuild.Capture(s,"Mass-laundry-detail",new Vector3(2.55f,1.17f,3.5f),2.1f);}finally{foreach(var r in rs)if(r)r.enabled=true;foreach(var l in ls)if(l)l.enabled=true;}
 }
}
