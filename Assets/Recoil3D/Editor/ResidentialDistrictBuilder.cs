using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

// Authored scene extension: existing campaign geometry and the original memory are preserved.
public static class ResidentialDistrictBuilder
{
 const string Outside="Assets/Scenes/3D/RecoilPrototype.unity";
 public const string Stairs="Assets/Scenes/3D/RecoilResidentialStairwell.unity";
 public const string Apartment="Assets/Scenes/3D/RecoilResidentialApartment.unity";
 const string Memory="Assets/Scenes/3D/RecoilKitchenMemory.unity";
 const string Template="Assets/Scenes/3D/FactoryRooms/FactoryWashroomF2.unity";
 const string Folder="Assets/Recoil3D/Residential";
 const string Marker="Residential approach - forest river checkpoint and housing";
 static Material concrete,plaster,lowerPlaster,steel,rust,asphalt,paint,black,glass,wood,soil,foliage,farFoliage,water,ivory,warm,red;
 static Mesh cone;static int meshIndex;static string backup;
 static T[] All<T>(Scene s) where T:Component{return s.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<T>(true)).ToArray();}
 static RecoilPrototype World(Scene s){return All<RecoilPrototype>(s).Single();}
 static void Require(bool value,string message){if(!value)throw new Exception(message);}
 static Transform G(string name,Transform parent,Vector3 p){var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=p;return t;}
 static Material Mat(string name,Color color,string shader="Standard"){
  string path=Folder+"/Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
  if(!m){m=new Material(Shader.Find(shader));AssetDatabase.CreateAsset(m,path);}
  string templateName=name.Contains("plaster")?"Architecture - aged room plaster":name.Contains("render")?"Architecture - worn lower wall paint":name.Contains("concrete")||name.Contains("asphalt")?"Architecture - pitted concrete":name.Contains("timber")?"Architecture - old timber":null;
  var template=templateName==null?null:AssetDatabase.LoadAssetAtPath<Material>("Assets/Recoil3D/Materials/"+templateName+".mat");
  if(template&&shader=="Standard"){m.shader=template.shader;m.CopyPropertiesFromMaterial(template);m.SetColor("_BaseTint",color);m.color=Color.white;}else m.color=color;
  if(m.HasProperty("_Glossiness"))m.SetFloat("_Glossiness",.17f);EditorUtility.SetDirty(m);return m;
 }
 static void Setup(){
  Directory.CreateDirectory(Folder+"/Materials");Directory.CreateDirectory(Folder+"/Meshes");AssetDatabase.Refresh();meshIndex=0;
  concrete=Mat("Weathered bridge concrete",new Color(.25f,.28f,.27f));
  plaster=Mat("Workers housing faded ochre plaster",new Color(.39f,.37f,.30f));
  lowerPlaster=Mat("Ground floor soot stained render",new Color(.20f,.25f,.23f));
  steel=Mat("Dull painted grey green steel",new Color(.20f,.25f,.24f));rust=Mat("Old iron rust",new Color(.24f,.13f,.075f));
  asphalt=Mat("Road worn asphalt",new Color(.10f,.12f,.12f));paint=Mat("Faded road markings",new Color(.47f,.45f,.34f));
  black=Mat("Unlit recesses",new Color(.025f,.034f,.035f));glass=Mat("Dusty unlit glass",new Color(.075f,.11f,.115f));
  wood=Mat("Weathered timber",new Color(.21f,.19f,.14f));soil=Mat("Damp earth",new Color(.11f,.13f,.10f));
  foliage=Mat("Nearby spruce and birch leaves",new Color(.075f,.105f,.085f));
  farFoliage=Mat("Distant tree silhouettes",new Color(.025f,.045f,.042f),"Recoil/3D/Distant Atmosphere");farFoliage.SetFloat("_Contrast",.6f);
  water=Mat("River flowing from foreground to distance",new Color(.045f,.075f,.08f),"Recoil/3D/Residential River");
  ivory=Mat("Old signs and window frames",new Color(.48f,.49f,.41f));
  warm=Mat("Warm occupied window",new Color(.40f,.28f,.12f));warm.EnableKeyword("_EMISSION");warm.SetColor("_EmissionColor",new Color(.24f,.14f,.05f));
  red=Mat("Checkpoint faded red paint",new Color(.36f,.095f,.06f));
  cone=SaveMesh("Spruce cone",Cone());
 }
 static Mesh Cone(){var m=new Mesh();var v=new List<Vector3>();var t=new List<int>();int sides=9;
  v.Add(Vector3.up*.5f);v.Add(Vector3.down*.5f);for(int i=0;i<sides;i++){float a=i*Mathf.PI*2/sides;v.Add(new Vector3(Mathf.Cos(a)*.5f,-.5f,Mathf.Sin(a)*.5f));}
  for(int i=0;i<sides;i++){int a=2+i,b=2+(i+1)%sides;t.AddRange(new[]{0,b,a,1,a,b});}m.SetVertices(v);m.SetTriangles(t,0);m.RecalculateNormals();return m;
 }
 static Mesh SaveMesh(string name,Mesh mesh){string p=Folder+"/Meshes/"+name+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(p);if(old){EditorUtility.CopySerialized(mesh,old);Object.DestroyImmediate(mesh);return old;}mesh.name=name;AssetDatabase.CreateAsset(mesh,p);return mesh;}
 static GameObject B(string name,Transform parent,Vector3 p,Vector3 scale,Material mat,PrimitiveType type=PrimitiveType.Cube){
  var g=GameObject.CreatePrimitive(type);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=p;g.transform.localScale=scale;Object.DestroyImmediate(g.GetComponent<Collider>());
  g.GetComponent<Renderer>().sharedMaterial=mat;return g;
 }
 static void Rod(string name,Transform parent,Vector3 a,Vector3 b,float radius,Material mat){var g=B(name,parent,(a+b)*.5f,new Vector3(radius*2,Vector3.Distance(a,b)*.5f,radius*2),mat,PrimitiveType.Cylinder);g.transform.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);}
 static BoxCollider2D Collision(Transform parent,string name,Vector2 centre,Vector2 size,bool sight=true){
  var g=G(name,parent,new Vector3(centre.x,centre.y,0));var c=g.gameObject.AddComponent<BoxCollider2D>();c.size=size;
  g.gameObject.AddComponent<RecoilSurfaceVisibility>().blocksSight=sight;return c;
 }
 static void Batch(Transform root,string id){
  var fs=root.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.sharedMesh&&f.GetComponent<MeshRenderer>()&&f.GetComponent<MeshRenderer>().sharedMaterials.Length==1).ToArray();
  foreach(var group in fs.GroupBy(f=>f.GetComponent<MeshRenderer>().sharedMaterial)){
   var list=group.ToArray();if(list.Length<3)continue;
   var mesh=new Mesh{indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(list.Select(f=>new CombineInstance{mesh=f.sharedMesh,transform=root.worldToLocalMatrix*f.transform.localToWorldMatrix}).ToArray(),true,true);
   mesh=SaveMesh(id+"_"+(meshIndex++).ToString("000"),mesh);var t=G("Combined static geometry - "+group.Key.name,root,Vector3.zero);t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=group.Key;
   foreach(var f in list){Object.DestroyImmediate(f.GetComponent<MeshRenderer>());Object.DestroyImmediate(f);}
  }
 }
 static Light Spot(string name,Transform parent,Vector3 p,Vector3 target,float intensity,float range,float angle,Color color,bool shadows=true){
  var t=G(name,parent,p);t.rotation=Quaternion.LookRotation(parent.TransformPoint(target)-t.position);var l=t.gameObject.AddComponent<Light>();l.type=LightType.Spot;l.intensity=intensity;l.color=color;l.range=range;l.spotAngle=angle;l.innerSpotAngle=angle*.65f;l.shadows=shadows?LightShadows.Soft:LightShadows.None;l.shadowResolution=LightShadowResolution.Medium;l.shadowBias=.025f;l.shadowNormalBias=.10f;l.renderMode=LightRenderMode.ForcePixel;return l;
 }
 static void Label(string value,Transform parent,Vector3 p,float size,Color color){
  var t=G(value,parent,p);var m=t.gameObject.AddComponent<TextMesh>();m.text=value;m.fontSize=64;m.characterSize=size;m.anchor=TextAnchor.MiddleCenter;m.alignment=TextAlignment.Center;m.color=color;
  // Share the project's depth-tested font material instead of the default overlay shader.
  var depth=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Materials/Depth tested residential lettering.mat");
  if(!depth){depth=new Material(Shader.Find("Recoil/3D/World Text"));AssetDatabase.CreateAsset(depth,Folder+"/Materials/Depth tested residential lettering.mat");}
  m.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");m.font.RequestCharactersInTexture(value,64,FontStyle.Normal);m.GetComponent<Renderer>().sharedMaterial=depth;t.gameObject.AddComponent<Recoil3DDepthText>();
 }
 static void Tree(Transform parent,Vector3 p,float height,int seed,bool distant){
  var t=G("Tree "+seed,parent,p);var mat=distant?farFoliage:foliage;
  B("Trunk",t,new Vector3(0,height*.38f,0),new Vector3(.18f,height*.38f,.18f),wood,PrimitiveType.Cylinder);
  for(int i=0;i<3;i++){var c=G("Uneven spruce crown",t,new Vector3((seed%3-1)*.08f,height*(.38f+i*.19f),0));c.localScale=new Vector3(height*(.58f-i*.10f),height*.47f,height*(.53f-i*.09f));c.gameObject.AddComponent<MeshFilter>().sharedMesh=cone;c.gameObject.AddComponent<MeshRenderer>().sharedMaterial=mat;}
 }
 static void Forest(Transform root){var t=G("Forest framing both road edges and both river banks",root,Vector3.zero);var random=new System.Random(1708);
  for(int row=0;row<4;row++)for(int i=0;i<35;i++){
   float x=123+i*3.4f+(float)random.NextDouble()*2;float z=row==0?-11-(float)random.NextDouble()*7:row==1?11+(float)random.NextDouble()*5:row==2?27+(float)random.NextDouble()*8:47+(float)random.NextDouble()*12;
   if(x>147&&x<169)continue;if(x>189&&z<18)continue;if(row==0&&(i%3!=0||(x>134&&x<184)))continue;
   Tree(t,new Vector3(x,0,z),(row==0?4:5)+(float)random.NextDouble()*(row==0?2:5),i+row*35,row>=2);
  }
  // Vegetation continues well below the foreground camera, framing an actual river corridor.
  for(int bank=0;bank<2;bank++)for(int i=0;i<16;i++){float treeZ=-34+i*7;if(treeZ>-24&&treeZ<8)continue;Tree(t,new Vector3(bank==0?144-i%3*1.4f:173+i%3*1.4f,-.3f,treeZ),5+i%4,i+200,true);}
  Batch(t,"forest");foreach(var renderer in t.GetComponentsInChildren<MeshRenderer>())renderer.shadowCastingMode=ShadowCastingMode.Off;
 }
 static Collider Booth(Transform root,float x,string name){
  var t=G(name,root,new Vector3(x,0,2.5f));
  var body=B("Solid shelter - occludes searchlight",t,new Vector3(0,1.25f,.1f),new Vector3(3.1f,2.5f,2.6f),lowerPlaster);var c=body.AddComponent<BoxCollider>();
  var details=G("Booth painted roof window and door",t,Vector3.zero);
  B("Concrete plinth",details,new Vector3(0,.10f,0),new Vector3(3.5f,.2f,3),concrete);
  B("Low overhanging roof",details,new Vector3(0,2.65f,0),new Vector3(3.7f,.25f,3.1f),steel);
  B("Window recess",details,new Vector3(-.55f,1.65f,-1.225f),new Vector3(1.45f,.9f,.06f),black);
  for(int i=0;i<3;i++)B("Window vertical bars",details,new Vector3(-1.2f+i*.65f,1.65f,-1.27f),new Vector3(.045f,1,.05f),ivory);
  B("Window horizontal frame",details,new Vector3(-.55f,1.65f,-1.28f),new Vector3(1.45f,.04f,.05f),ivory);
  B("Closed guard door",details,new Vector3(.95f,1.05f,-1.23f),new Vector3(.7f,2,.05f),steel);
  B("Service hatch sill",details,new Vector3(-.55f,1.13f,-1.34f),new Vector3(1.6f,.09f,.25f),concrete);
  B("Number plate",details,new Vector3(.95f,2.24f,-1.28f),new Vector3(.5f,.18f,.03f),ivory);
  for(int side=-1;side<=1;side+=2)B("Lower guardhouse paint stripe",details,new Vector3(side*1.56f,.7f,0),new Vector3(.03f,.13f,2.6f),red);
  // The raised road barriers leave the walking route open, with bases outside its plane.
  var gate=G("Raised checkpoint barrier",details,new Vector3(-2.4f,0,-1.7f));B("Barrier motor pedestal",gate,new Vector3(0,.65f,0),new Vector3(.45f,1.3f,.4f),steel);
  var arm=B("Raised striped boom",gate,new Vector3(.65f,2.9f,0),new Vector3(.13f,4.1f,.13f),ivory);arm.transform.localRotation=Quaternion.Euler(0,0,-20);
  for(int i=0;i<5;i++){var stripe=B("Barrier red stripe",gate,new Vector3(.1f+i*.25f,1.4f+i*.7f,-.075f),new Vector3(.15f,.23f,.025f),red);stripe.transform.localRotation=arm.transform.localRotation;}
  Spot("Weak guardhouse eave light",t,new Vector3(0,2.55f,-1.65f),new Vector3(0,0,-1.5f),1.2f,5,90,new Color(.65f,.56f,.35f),false);
  Batch(details,"booth"+name.Replace(" ",""));return c;
 }
 static void BridgeAndRoad(Transform root,RecoilPrototype world){
  var t=G("Road bridge and river - crossing from factory to estate",root,Vector3.zero);
  // Keep the pre-existing terrain intact, extend only past its original eastern edge.
  Collision(t,"West bank walkable road",new Vector2(139.5f,-.5f),new Vector2(19,1),false);
  Collision(t,"Bridge deck gameplay collision",new Vector2(158,-.25f),new Vector2(18,.5f),false);
  Collision(t,"East bank walkable road",new Vector2(207,-.5f),new Vector2(80,1),false);
  foreach(var section in new[]{new Vector2(124,149),new Vector2(167,247)}){
   float mid=(section.x+section.y)*.5f,width=section.y-section.x;
   B("River bank solid land",t,new Vector3(mid,-2,20),new Vector3(width,4,112),soil);
   B("Asphalt road with shoulders",t,new Vector3(mid,-.035f,1.1f),new Vector3(width,.06f,8.2f),asphalt);
  }
  B("Bridge concrete deck",t,new Vector3(158,-.28f,1.1f),new Vector3(18.4f,.56f,8.5f),concrete);
  B("Bridge asphalt surface",t,new Vector3(158,.015f,1.1f),new Vector3(18.1f,.025f,8),asphalt);
  for(int i=0;i<14;i++){float x=131+i*7.8f;B("Worn centre dash",t,new Vector3(x,.035f,1.6f),new Vector3(2.6f,.015f,.10f),paint);}
  foreach(float z in new[]{-3.1f,5.3f}){
   B("Bridge curb",t,new Vector3(158,.11f,z),new Vector3(18.5f,.22f,.28f),concrete);
   foreach(float y in new[]{.65f,1.15f})B("Continuous iron bridge rail",t,new Vector3(158,y,z),new Vector3(18.8f,.065f,.065f),steel);
   for(int i=0;i<14;i++)B("Bridge railing upright",t,new Vector3(149+i*1.4f,.61f,z),new Vector3(.07f,1.22f,.07f),steel);
  }
  foreach(float x in new[]{149.5f,166.5f}){B("Bridge abutment",t,new Vector3(x,-2.3f,1.1f),new Vector3(1.4f,4.1f,9.3f),concrete);B("Bridge end coping",t,new Vector3(x,.42f,5.4f),new Vector3(1.1f,.85f,.85f),concrete);}
  for(int i=0;i<2;i++)B("Bridge underside longitudinal girder",t,new Vector3(158,-.85f,i==0?-2.4f:4.6f),new Vector3(17.6f,.7f,.32f),steel);
  B("Riverbed below crossing",t,new Vector3(158,-3.7f,20),new Vector3(18,.4f,115),soil);
  var surface=G("Flowing river toward background",t,new Vector3(158,-2.8f,20));surface.localScale=new Vector3(18,1,114);
  var m=new Mesh();m.vertices=new[]{new Vector3(-.5f,0,-.5f),new Vector3(-.5f,0,.5f),new Vector3(.5f,0,.5f),new Vector3(.5f,0,-.5f)};m.triangles=new[]{0,1,2,0,2,3};m.uv=new[]{Vector2.zero,Vector2.up,Vector2.one,Vector2.right};m.RecalculateNormals();surface.gameObject.AddComponent<MeshFilter>().sharedMesh=SaveMesh("River surface",m);surface.gameObject.AddComponent<MeshRenderer>().sharedMaterial=water;
  for(int side=-1;side<=1;side+=2)for(int i=0;i<19;i++){var rock=B("Uneven river bank stone",t,new Vector3(158+side*(8.6f+i%2*.45f),-1.8f,-31+i*6),new Vector3(1.8f,1.7f,3.3f),concrete);rock.transform.localRotation=Quaternion.Euler(8,i*31,side*15);}
  Batch(t,"bridge-road");
  var shelters=new[]{Booth(root,142,"West bridge checkpoint booth"),Booth(root,174,"East bridge checkpoint booth")};
  Watchtower(root,world,shelters);
 }
 static void Watchtower(Transform root,RecoilPrototype world,Collider[] shelters){
  var t=G("Government watchtower - distant bank",root,new Vector3(158,0,21));var frame=G("Tower lattice and cabin",t,Vector3.zero);
  foreach(float x in new[]{-1.5f,1.5f})foreach(float z in new[]{-1.5f,1.5f}){Rod("Tower leg",frame,new Vector3(x*1.4f,0,z*1.4f),new Vector3(x,11,z),.12f,steel);B("Foundation block",frame,new Vector3(x*1.4f,.15f,z*1.4f),new Vector3(.8f,.3f,.8f),concrete);}
  for(int level=0;level<4;level++){float y=level*2.7f;foreach(float z in new[]{-1.6f,1.6f}){Rod("Cross bracing",frame,new Vector3(-1.6f,y,z),new Vector3(1.6f,y+2.7f,z),.045f,steel);Rod("Cross bracing",frame,new Vector3(1.6f,y,z),new Vector3(-1.6f,y+2.7f,z),.045f,steel);}}
  B("Watch cabin",frame,new Vector3(0,12,0),new Vector3(3.9f,2,3.7f),lowerPlaster);B("Observation ribbon window",frame,new Vector3(0,12.4f,-1.88f),new Vector3(3.6f,.65f,.04f),black);
  for(int i=0;i<4;i++)B("Cabin window mullion",frame,new Vector3(-1.6f+i*1.06f,12.4f,-1.92f),new Vector3(.06f,.7f,.05f),steel);
  B("Cabin roof",frame,new Vector3(0,13.15f,0),new Vector3(4.5f,.25f,4.3f),steel);B("Observation balcony",frame,new Vector3(0,10.8f,-.25f),new Vector3(4.8f,.2f,4.7f),steel);
  Rod("Aerial",frame,new Vector3(1.3f,13.2f,0),new Vector3(1.3f,16,0),.04f,steel);
  for(int i=0;i<25;i++)B("Access ladder rung",frame,new Vector3(1.8f,.4f+i*.43f,1.9f),new Vector3(.6f,.05f,.06f),steel);
  Batch(frame,"watchtower");
  var l=Spot("Sweeping government searchlight",t,new Vector3(0,12.1f,-2.4f),new Vector3(0,.8f,-21),8.5f,48,19,new Color(.76f,.82f,.75f));
  var head=G("Searchlight movable housing",t,new Vector3(0,12.1f,-2.4f));
  B("Searchlight cylindrical shell",head,new Vector3(0,0,.12f),new Vector3(.8f,.36f,.8f),steel,PrimitiveType.Cylinder).transform.localRotation=Quaternion.Euler(90,0,0);
  B("Recessed searchlight lens",head,new Vector3(0,0,.49f),new Vector3(.61f,.035f,.61f),warm,PrimitiveType.Cylinder).transform.localRotation=Quaternion.Euler(90,0,0);
  var sweep=t.gameObject.AddComponent<CheckpointSearchlight>();sweep.world=world;sweep.searchlight=l;sweep.lampHead=head;sweep.leftTarget=new Vector3(134,1,0);sweep.rightTarget=new Vector3(182,1,0);sweep.shelters=shelters;sweep.sweepPeriod=30;sweep.Pose(.25f);
  CheckpointSearchlightReadabilityPass.Configure(sweep);
  var camera=All<Camera>(world.gameObject.scene).First(c=>c.CompareTag("MainCamera"));var look=camera.GetComponent<WarehouseAtmosphereTest>();if(look){look.lamps=(look.lamps??new Light[0]).Concat(new[]{l}).ToArray();look.scattering=.17f;}
 }
 static void Window(Transform parent,float x,float y,float z,bool lit){
  B("Recessed apartment window",parent,new Vector3(x,y,z),new Vector3(1.2f,1.5f,.10f),black);
  B("Window glass",parent,new Vector3(x,y,z-.065f),new Vector3(1.03f,1.30f,.035f),lit?warm:glass);
  foreach(float dx in new[]{-.61f,0,.61f})B("Timber sash upright",parent,new Vector3(x+dx,y,z-.10f),new Vector3(.055f,1.56f,.07f),ivory);
  B("Window sill",parent,new Vector3(x,y-.8f,z-.15f),new Vector3(1.42f,.10f,.30f),concrete);
  B("Window top lintel",parent,new Vector3(x,y+.79f,z-.08f),new Vector3(1.36f,.10f,.15f),plaster);
 }
 static void Housing(Transform root,float centre,float z,bool enterable,bool distant=false){
  var t=G(enterable?"Workers housing Block 3 - enterable stairwell":"Workers housing - closed shops",root,new Vector3(centre,0,z));float width=23,depth=9;
  var art=G("Masonry block - four levels over street shops",t,Vector3.zero);
  B("Full depth residential masonry",art,new Vector3(0,6.6f,depth*.5f),new Vector3(width,13.2f,depth),plaster);
  B("Shop plinth",art,new Vector3(0,1.55f,-.035f),new Vector3(width,3.1f,.14f),lowerPlaster);
  B("Projecting roof cornice",art,new Vector3(0,13.2f,depth*.5f),new Vector3(width+.55f,.25f,depth+.55f),concrete);
  foreach(float y in new[]{3.1f,6.4f,9.6f})B("Floor belt course",art,new Vector3(0,y,-.13f),new Vector3(width,.12f,.27f),concrete);
  for(int f=0;f<3;f++)for(int bay=0;bay<7;bay++)Window(art,-9.4f+bay*3.1f,4.75f+f*3.2f,-.13f,enterable&&f==1&&bay==6);
  foreach(float side in new[]{-1f,1f})for(int f=0;f<3;f++)for(int bay=0;bay<2;bay++){
   var sideGroup=G("Side elevation window",art,new Vector3(side*(width*.5f+.015f),0,2.5f+bay*3.8f));sideGroup.localRotation=Quaternion.Euler(0,side>0?-90:90,0);Window(sideGroup,0,4.75f+f*3.2f,0,false);
  }
  // Street shops have broad display windows, shutters, their own door and a separate housing entrance.
  foreach(float x in new[]{-7.4f,4.7f}){
   B("Shop display recess",art,new Vector3(x,1.35f,-.15f),new Vector3(5.4f,2.2f,.10f),black);
   B("Closed corrugated shop shutter",art,new Vector3(x,1.35f,-.23f),new Vector3(5.2f,2.05f,.06f),steel);
   for(int i=0;i<14;i++)B("Shutter horizontal slat",art,new Vector3(x,.4f+i*.145f,-.28f),new Vector3(5.2f,.025f,.025f),black);
   B("Faded shop fascia",art,new Vector3(x,2.76f,-.22f),new Vector3(5.8f,.35f,.15f),ivory);
   B("Shop transom",art,new Vector3(x,2.48f,-.22f),new Vector3(5.2f,.1f,.08f),black);
   var sign=G("Closed shop lettering",t,new Vector3(x,2.76f,-.32f));Label(x<0?"GROCERIES":"HOUSEHOLD GOODS",sign,Vector3.zero,.025f,new Color(.12f,.15f,.14f));
  }
  B("Residential entrance recess",art,new Vector3(-.8f,1.27f,-.16f),new Vector3(1.75f,2.55f,.15f),black);
  B("Timber entrance door",art,new Vector3(-.8f,1.20f,-.28f),new Vector3(1.27f,2.35f,.12f),wood);
  B("Entrance glass",art,new Vector3(-.8f,1.65f,-.36f),new Vector3(.88f,.9f,.035f),glass);
  B("Entrance canopy",art,new Vector3(-.8f,2.75f,-.65f),new Vector3(2.5f,.15f,1.4f),concrete);
  B("Entry handle",art,new Vector3(-.33f,1,-.39f),new Vector3(.055f,.34f,.08f),ivory);
  B("Mail slot",art,new Vector3(-.8f,.54f,-.36f),new Vector3(.45f,.06f,.04f),black);
  for(int i=0;i<7;i++){float x=-10+i*3.2f;B("Rising damp and patched plaster",art,new Vector3(x,.20f+(i%2)*.13f,-.12f),new Vector3(1.1f+(i%3)*.4f,.4f,.025f),concrete);}
  var light=Spot("Weak residential doorway bulb",t,new Vector3(-.8f,2.65f,-.8f),new Vector3(-.8f,.1f,-1.7f),1.5f,5,75,new Color(.7f,.59f,.38f),false);
  B("Entrance bulkhead shade",art,new Vector3(-.8f,2.62f,-.4f),new Vector3(.4f,.14f,.22f),steel);
  Batch(art,"housing"+centre.ToString("0")+"_"+z.ToString("0"));
  if(enterable){var d=Door(t,"Enter Block 3",new Vector3(-.8f,1.2f,-.4f),Stairs,"Residential Entrance Spawn","E / ENTER APARTMENT BUILDING");G("Residential Outside Spawn",root,new Vector3(centre-.8f,.845f,0));}
  else light.enabled=!distant;
 }
 static RecoilDoor Door(Transform parent,string name,Vector3 p,string scene,string spawn,string prompt){var t=G(name,parent,p);var d=t.gameObject.AddComponent<RecoilDoor>();d.requiresClue=false;d.completesLevel=false;d.destinationScene=scene;d.destinationSpawn=spawn;d.interactionPrompt=prompt;d.interactionOffset=new Vector2(0,-.35f);d.interactionRange=1.55f;return d;}
 static void BuildOutside(){
  var scene=EditorSceneManager.OpenScene(Outside);EditorSceneManager.SaveScene(scene,backup+"/RecoilPrototype.unity",true);var world=World(scene);
  var old=world.transform.Find(Marker);if(old)Object.DestroyImmediate(old.gameObject);
  var root=G(Marker,world.transform,Vector3.zero);
  var boundary=world.GetComponentsInChildren<BoxCollider2D>(true).Where(c=>c.name=="Solid terrain"&&c.bounds.size.x<1&&c.bounds.center.x>125).OrderByDescending(c=>c.bounds.center.x).FirstOrDefault();Require(boundary,"Eastern campaign boundary was not found");var p=boundary.transform.position;p.x=247;boundary.transform.position=p;
  // Its native visual belongs to this collider and moves together with it.
  BridgeAndRoad(root,world);Forest(root);
  var village=G("Workers estate - separate housing and shop entrances",root,Vector3.zero);
  Housing(village,204,3.8f,true);Housing(village,232,4.6f,false);Housing(village,209,25,false,true);Housing(village,238,37,false,true);
  var street=G("Residential pavement and street furniture",root,Vector3.zero);
  B("Raised estate pavement",street,new Vector3(219,-.025f,.6f),new Vector3(55,.05f,6),concrete);
  for(int i=0;i<15;i++)B("Pavement joints",street,new Vector3(191+i*3.8f,.012f,.6f),new Vector3(.025f,.015f,5.7f),asphalt);
  for(int i=0;i<2;i++){float x=190+i*29;B("Bench seat",street,new Vector3(x,.5f,2.5f),new Vector3(2,.14f,.5f),wood);B("Bench back",street,new Vector3(x,.9f,2.7f),new Vector3(2,.65f,.12f),wood);foreach(float dx in new[]{-.75f,.75f})B("Bench support",street,new Vector3(x+dx,.25f,2.5f),new Vector3(.12f,.5f,.5f),steel);}
  Batch(street,"estate-street");
  Spot("Estate storefront reflected fill",root,new Vector3(204,4,-2),new Vector3(204,2,5),1.1f,18,105,new Color(.43f,.50f,.54f),false);
  // Low fixed sky bounce reveals silhouettes without brightening the full forest.
  Spot("Road reflected sky fill",root,new Vector3(157,5,-5),new Vector3(158,-1,2),.75f,32,115,new Color(.38f,.48f,.54f),false);
  Spot("Estate reflected sky fill",root,new Vector3(213,7,-5),new Vector3(213,4,5),.8f,33,110,new Color(.4f,.48f,.53f),false);
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
 }
 static void Set(Object o,string key,Object value){var s=new SerializedObject(o);var p=s.FindProperty(key);Require(p!=null,"Missing field "+key);p.objectReferenceValue=value;s.ApplyModifiedPropertiesWithoutUndo();}
 static Object Ref(Object o,string key){return new SerializedObject(o).FindProperty(key).objectReferenceValue;}
 static RecoilPrototype CleanStairScene(){
  var s=EditorSceneManager.OpenScene(Template);var world=World(s);var player=world.Player;var spawn=(Transform)Ref(world,"spawnPoint");
  var cam=All<Camera>(s).First(c=>c.CompareTag("MainCamera"));
  foreach(Transform child in world.transform.Cast<Transform>().ToArray())if(!player.transform.IsChildOf(child)&&child!=spawn)Object.DestroyImmediate(child.gameObject);
  foreach(var root in s.GetRootGameObjects())if(root!=world.gameObject&&root!=cam.gameObject)Object.DestroyImmediate(root);
  world.name="RECOIL - Workers Residential Stairwell";world.locationLabel="WORKERS HOUSING / BLOCK 3";world.objectiveWithoutCard=world.objectiveWithCard="";world.enableCheckpoint=false;world.startWithWaterGun=false;world.legacyEvidenceEnabled=false;
  Set(world,"clue",null);Set(world,"door",null);Set(world,"checkpointPoint",spawn);Set(world,"factoryEntrance",spawn);spawn.name="Residential Entrance Spawn";spawn.position=new Vector3(3,.845f,0);player.Position=spawn.position;
  var rw=world.GetComponent<Recoil3DWorld>();rw.playerVisualLight=null;rw.emergencyLights=new Light[0];rw.ambientColour=new Color(.017f,.021f,.024f);rw.fogStartDistance=24;rw.fogEndDistance=62;
  var identity=world.GetComponent<Recoil3DScene>();identity.originalScene="";
  var look=cam.GetComponent<WarehouseAtmosphereTest>();look.originalLights=new Light[0];look.originalIntensities=look.testIntensities=new float[0];look.decorativeLights=new Light[0];look.lamps=new Light[0];look.skylightOpening=null;look.contactShadows=null;look.testAmbient=rw.ambientColour;look.manageSceneLighting=true;look.testStart=24;look.testEnd=62;look.showTestControls=false;
  var fog=cam.GetComponent<IndoorVisionFog>();if(fog)fog.effectEnabled=true;
  return world;
 }
 static void StairFlight(Transform parent,float y,int floor){
  var t=G("Residential stairs "+floor+" to "+(floor+1),parent,Vector3.zero);var stair=t.gameObject.AddComponent<WalkableStairway>();
  stair.routePoints=new[]{G("Lower entrance - feet",t,new Vector3(5.7f,y,.7f)),G("Half landing - feet",t,new Vector3(10.3f,y+1.6f,.7f)),G("Upper entrance - feet",t,new Vector3(5.7f,y+3.2f,.7f))};stair.travelSpeed=3.3f;
  var art=G("Narrow concrete stairs on the rear operation plane",t,Vector3.zero);
  for(int f=0;f<2;f++){
   var a=stair.routePoints[f].position;var b=stair.routePoints[f+1].position;
   for(int j=0;j<10;j++){var p=Vector3.Lerp(a,b,(j+.5f)/10);B("Concrete tread",art,p-new Vector3(0,.055f,-.62f),new Vector3(.53f,.11f,1.24f),concrete);}
   Rod("Worn timber handrail",art,a+new Vector3(0,.85f,-.66f),b+new Vector3(0,.85f,-.66f),.04f,wood);
   for(int j=0;j<6;j++){var p=Vector3.Lerp(a,b,j/5f);Rod("Stair baluster",art,p+new Vector3(0,.07f,-.66f),p+new Vector3(0,.85f,-.66f),.022f,steel);}
  }
  B("Half landing",art,new Vector3(10.3f,y+1.52f,1.35f),new Vector3(1.2f,.16f,1.3f),concrete);
  Batch(art,"stairs"+floor);
 }
 static void BuildStairwell(){
  var world=CleanStairScene();var s=world.gameObject.scene;var root=G("Residential stairwell - editable architecture",world.transform,Vector3.zero);
  var left=Collision(root,"Left stairwell wall",new Vector2(-.2f,5),new Vector2(.4f,10));var right=Collision(root,"Right stairwell wall",new Vector2(12.2f,5),new Vector2(.4f,10));
  Collision(root,"Ground floor",new Vector2(6,-.25f),new Vector2(12,.5f),false);Collision(root,"Stairwell ceiling",new Vector2(6,10.1f),new Vector2(12,.2f),false);
  var art=G("Stairwell plaster tiles railings and landings",root,Vector3.zero);
  B("Ground floor slab",art,new Vector3(6,-.25f,2),new Vector3(12,.5f,8),concrete);
  B("Full depth left wall",art,new Vector3(-.25f,5,1.5f),new Vector3(.5f,10,9),plaster);B("Full depth right wall",art,new Vector3(12.25f,5,1.5f),new Vector3(.5f,10,9),plaster);
  B("Stairwell back wall",art,new Vector3(6,5,5.8f),new Vector3(12,10,.4f),plaster);
  var lamps=new List<Light>();
  for(int level=0;level<3;level++){
   float y=level*3.2f;
   B("Green lower wall paint",art,new Vector3(6,y+.65f,5.56f),new Vector3(12,1.3f,.03f),lowerPlaster);
   B("Wall paint boundary",art,new Vector3(6,y+1.32f,5.53f),new Vector3(12,.055f,.04f),steel);
   if(level>0){Collision(root,"Floor "+(level+1)+" landing",new Vector2(2.85f,y-.12f),new Vector2(5.7f,.24f),false);B("Floor "+(level+1)+" landing slab",art,new Vector3(2.85f,y-.12f,2.3f),new Vector3(5.7f,.24f,5.2f),concrete);}
   B("Floor number enamel plaque",art,new Vector3(1.3f,y+1.9f,5.48f),new Vector3(.55f,.65f,.07f),ivory);Label(level.ToString(),root,new Vector3(1.3f,y+1.9f,5.42f),.09f,new Color(.08f,.11f,.10f));
   Window(art,8.7f,y+2.0f,5.45f,false);
   var l=Spot("Stairwell wall lamp floor "+level,root,new Vector3(3.5f,y+2.8f,3.5f),new Vector3(4,y+.2f,.7f),2.1f,6.2f,95,new Color(.63f,.67f,.58f));lamps.Add(l);
   var bounce=G("Bulkhead wall reflected fill floor "+level,root,new Vector3(3.5f,y+2.4f,3.7f)).gameObject.AddComponent<Light>();bounce.type=LightType.Point;bounce.range=5.8f;bounce.intensity=.65f;bounce.color=new Color(.52f,.57f,.49f);bounce.shadows=LightShadows.None;lamps.Add(bounce);
   B("Frosted bulkhead lamp",art,new Vector3(3.5f,y+2.78f,5.4f),new Vector3(.32f,.20f,.14f),warm);
   // Closed neighbour door beside the accessible circulation, clear of the stair route.
   B("Neighbour apartment door frame",art,new Vector3(.9f,y+1.15f,3.0f),new Vector3(1.5f,2.4f,.18f),concrete);B("Closed neighbour apartment door",art,new Vector3(.9f,y+1.12f,2.88f),new Vector3(1.25f,2.2f,.09f),wood);
   if(level<2){StairFlight(root,y,level+1);
    var flightLamp=Spot("Stair flight ceiling light "+level,root,new Vector3(8,y+3.0f,1.7f),new Vector3(8,y+1.1f,1.2f),3.2f,5.4f,95,new Color(.61f,.66f,.61f),false);lamps.Add(flightLamp);
    B("Stair flight bulkhead fixture",art,new Vector3(8,y+3.03f,1.7f),new Vector3(.42f,.10f,.28f),warm);
   }
  }
  // Top landing extends to the right-hand apartment, with no solid floor across either stair flight.
  Collision(root,"Third floor right apartment landing",new Vector2(8.85f,6.28f),new Vector2(6.3f,.24f),false);
  B("Third floor right landing slab",art,new Vector3(8.85f,6.28f,2.3f),new Vector3(6.3f,.24f,5.2f),concrete);
  B("Third floor right apartment frame",art,new Vector3(10.6f,7.6f,3),new Vector3(1.55f,2.5f,.2f),concrete);B("Third floor right apartment door",art,new Vector3(10.6f,7.55f,2.85f),new Vector3(1.27f,2.35f,.10f),wood);B("Apartment brass handle",art,new Vector3(11.05f,7.47f,2.77f),new Vector3(.18f,.04f,.08f),ivory);
  Door(root,"Third floor right apartment",new Vector3(10.6f,7.6f,2.7f),Apartment,"Apartment Entrance Spawn","E / ENTER APARTMENT");G("Third Floor Apartment Return Spawn",world.transform,new Vector3(9.9f,7.245f,0));
  // Foreground entrance is represented by incoming light, matching the rest of the campaign.
  Door(root,"Return to residential street",new Vector3(3,1.2f,-2.2f),Outside,"Residential Outside Spawn","E / LEAVE BUILDING");
  lamps.Add(Spot("Light through foreground street entrance",root,new Vector3(3,2,-4.4f),new Vector3(3,.1f,1.2f),2.1f,8,38,new Color(.45f,.56f,.61f)));
  for(int i=0;i<5;i++){B("Mailboxes",art,new Vector3(.7f+i*.4f,1.15f,5.45f),new Vector3(.34f,.45f,.10f),steel);B("Mailbox slot",art,new Vector3(.7f+i*.4f,1.25f,5.38f),new Vector3(.23f,.025f,.02f),black);}
  lamps.Add(Spot("Third floor apartment doorway light",root,new Vector3(10.6f,9.1f,.4f),new Vector3(10.6f,7.4f,3),2.8f,5.5f,78,new Color(.70f,.63f,.47f),false));
  B("Third floor door wall sconce",art,new Vector3(10.6f,9.08f,2.7f),new Vector3(.38f,.14f,.18f),warm);
  Batch(art,"stairwell");
  var cam=All<Camera>(s).First();var follow=cam.GetComponent<RecoilCameraController>();follow.target=world.Player.transform;follow.viewSize=3.25f;follow.followOffset=new Vector2(.4f,1.65f);follow.panY3D=-.55f;follow.pitch3D=3;follow.minimumY=2.3f;follow.maximumY=8.3f;follow.limitHorizontal=true;follow.leftBoundary=left;follow.rightBoundary=right;follow.maximumMouseOffset=.65f;follow.maximumDownwardMouseOffset=.25f;follow.preserveFootFraming=true;
  var look=cam.GetComponent<WarehouseAtmosphereTest>();look.lamps=lamps.Where(l=>l.type==LightType.Spot).ToArray();look.decorativeLights=lamps.ToArray();look.scattering=.06f;look.softGlow=.035f;
  world.GetComponent<Recoil3DWorld>().ApplyAtmosphere();follow.Initialize(world,world.Player.transform);follow.SnapToTarget();EditorSceneManager.SaveScene(s,Stairs);
 }
 static void BuildApartment(){
  // Copy the room geometry, not the death-return flow or childhood player.
  var s=EditorSceneManager.OpenScene(Memory);var world=World(s);var oldPlayer=world.Player;
  var source=EditorSceneManager.OpenScene(Template,OpenSceneMode.Additive);var player=Object.Instantiate(World(source).Player,world.transform);player.name="Dawn - shared Player Controller";
  var story=world.GetComponent<KitchenMemoryController>();
  if(story){if(story.bedroomOcclusion)story.bedroomOcclusion.SetActive(false);if(story.closedBedroomDoor)story.closedBedroomDoor.SetActive(false);if(story.openBedroomDoor)story.openBedroomDoor.SetActive(true);if(story.mother)Object.DestroyImmediate(story.mother.gameObject);Object.DestroyImmediate(story);}
  foreach(var c in world.GetComponentsInChildren<KitchenMotherConversation>(true))Object.DestroyImmediate(c);
  Set(world,"player",player);Set(world,"actor",player.transform);
  foreach(string field in new[]{"coat","head","hat","gun"})Set(world,field,Ref(player,field));
  Object.DestroyImmediate(oldPlayer.gameObject);
  var cam=All<Camera>(s).Single();Set(player,"world",world);Set(player,"aimCamera",cam);player.Configure(world,cam,(Transform)Ref(player,"coat"),(Transform)Ref(player,"head"),(Transform)Ref(player,"hat"),(Transform)Ref(player,"gun"));
  var spawn=(Transform)Ref(world,"spawnPoint");spawn.name="Apartment Entrance Spawn";spawn.position=new Vector3(12.1f,.845f,0);player.Position=spawn.position;Set(world,"checkpointPoint",spawn);Set(world,"factoryEntrance",spawn);
  world.name="RECOIL - Workers Apartment - memory room duplicate";world.locationLabel="WORKERS HOUSING / THIRD FLOOR APARTMENT";world.objectiveWithoutCard=world.objectiveWithCard="";world.startWithWaterGun=false;world.enableCheckpoint=false;world.GetComponent<Recoil3DScene>().originalScene="";
  var door=world.GetComponentsInChildren<RecoilDoor>(true).Single();door.name="Return to residential stairwell";door.returnToDeathMemoryCheckpoint=false;door.requiredItemId="";door.requiresClue=false;door.completesLevel=false;door.destinationScene=Stairs;door.destinationSpawn="Third Floor Apartment Return Spawn";door.interactionPrompt="E / RETURN TO STAIRWELL";
  var follow=cam.GetComponent<RecoilCameraController>();follow.target=player.transform;follow.Initialize(world,player.transform);follow.SnapToTarget();
  EditorSceneManager.CloseScene(source,true);SceneManager.SetActiveScene(s);EditorSceneManager.SaveScene(s,Apartment);
 }
 public static void Build(){
  Require(!EditorApplication.isPlayingOrWillChangePlaymode,"Exit Play before building");Setup();
  backup="Backups/ResidentialDistrict/"+DateTime.Now.ToString("yyyyMMdd-HHmmss");Directory.CreateDirectory(backup);
  File.Copy("ProjectSettings/EditorBuildSettings.asset",backup+"/EditorBuildSettings.asset",true);
  foreach(string p in new[]{Stairs,Apartment})if(File.Exists(p))File.Copy(p,backup+"/"+Path.GetFileName(p),true);
  BuildOutside();BuildStairwell();BuildApartment();
  var entries=EditorBuildSettings.scenes.ToList();foreach(string p in new[]{Stairs,Apartment})if(!entries.Any(e=>e.path==p))entries.Add(new EditorBuildSettingsScene(p,true));EditorBuildSettings.scenes=entries.ToArray();
  AssetDatabase.SaveAssets();Directory.CreateDirectory("output/residential");
  File.WriteAllText("output/residential/references.txt","Reference: Nowa Huta workers housing, Krakow official heritage documentation.\nhttps://zabytek.pl/pl/obiekty/dzielnica-nowa-huta-\nhttps://www.krakow.pl/turystyka/297737,artykul,nowa-huta-na-europejskim-szlaku-kulturowym-atrium.html\nStreet-facing ground-floor shops have separate housing entrances; repeated upstairs windows, weathered plaster, substantial side walls. No national location imposed on the fictional town.\nLayout: road x130..247; river x149..167 runs +Z; booths x142/174; watchtower (158,0,21); Block 3 entrance x203.2. Ground floor labelled 0, upper landings 1 and 2 (third physical level).\nOriginal kitchen memory and legacy 2D scenes preserved. Apartment is a separate scene with adult Dawn and a return route, with no death-memory story controller.\nSearchlight inspector exposes sweep timing, detection delay and onPlayerDetected event. Default detection gives a warning, without inventing a new death condition.\nBackup: "+backup);
  Verify();Capture();Debug.Log("RESIDENTIAL_DISTRICT_READY: "+backup);
 }
 public static void Verify(){
  var report=new List<string>();var ext=EditorSceneManager.OpenScene(Outside);var w=World(ext);var root=w.transform.Find(Marker);Require(root,"Exterior extension missing");
  var sweep=root.GetComponentInChildren<CheckpointSearchlight>();Require(sweep&&sweep.shelters.Length==2,"Missing searchlight shelters");Physics.SyncTransforms();int hidden=0,exposed=0;
  for(int phase=0;phase<16;phase++){sweep.Pose(phase/16f);for(float x=131;x<185;x+=.3f){var point=new Vector3(x,1,0);if(sweep.Illuminates(point,false)){if(sweep.Illuminates(point))exposed++;else hidden++;}}}
  Require(hidden>0&&exposed>0,"Searchlight must illuminate the bridge and be blocked by booths");sweep.Pose(sweep.previewPhase);report.Add("Searchlight: "+exposed+" exposed samples; "+hidden+" sheltered samples. Actual colliders occlude detection.");
  foreach(string path in new[]{Outside,Stairs,Apartment}){
   var s=EditorSceneManager.OpenScene(path);var world=World(s);foreach(var t in All<Transform>(s))Require(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)==0,"Missing script "+path+" / "+t.name);
   foreach(var d in world.GetComponentsInChildren<RecoilDoor>(true).Where(d=>!string.IsNullOrEmpty(d.destinationScene))){Require(File.Exists(d.destinationScene),"Missing destination "+d.destinationScene);if(new[]{Outside,Stairs,Apartment}.Contains(d.destinationScene)&&!string.IsNullOrEmpty(d.destinationSpawn)){
    var target=EditorSceneManager.OpenScene(d.destinationScene,OpenSceneMode.Additive);Require(All<Transform>(target).Any(t=>t.name==d.destinationSpawn),"Missing spawn "+d.destinationSpawn);EditorSceneManager.CloseScene(target,true);SceneManager.SetActiveScene(s);
   }}
   if(path==Stairs){var stairs=world.GetComponentsInChildren<WalkableStairway>();Require(stairs.Length==2,"Need two stair flights for three levels");foreach(var stair in stairs){Require(stair.Available,"Stair unavailable");float d;Require(stair.TryEntry(stair.PositionAt(0),1,out d),"Stair bottom entry failed");Require(stair.TryEntry(stair.PositionAt(stair.Length),-1,out d),"Stair top entry failed");}report.Add("Stairwell: three levels, two shared-controller switchback stairs; third floor right apartment.");}
   if(path==Apartment){Require(!world.GetComponent<KitchenMemoryController>(),"Duplicate must not run death memory story");Require(!world.Player.GetComponent<Recoil3DActorVisual>().child,"Apartment needs adult Dawn");Require(!world.GetComponentsInChildren<RecoilDoor>().Any(d=>d.returnToDeathMemoryCheckpoint),"Apartment exit cannot use death checkpoint");report.Add("Apartment: independent geometry duplicate, adult player, reversible stairwell route.");}
   report.Add(path+": valid scripts and destination routes");
  }
  Require(File.Exists(Memory),"Original memory missing");File.WriteAllLines("output/residential/verification.txt",report);Debug.Log("RESIDENTIAL_VERIFICATION_PASS: "+string.Join("; ",report));
 }
 public static void RefreshCheckpointForest(){
  Setup();var s=SceneManager.GetSceneByPath(Outside);bool added=!s.isLoaded;var previous=SceneManager.GetActiveScene();if(added)s=EditorSceneManager.OpenScene(Outside,OpenSceneMode.Additive);
  var root=World(s).transform.Find(Marker);var old=root.Find("Forest framing both road edges and both river banks");if(old)Object.DestroyImmediate(old.gameObject);Forest(root);
  EditorSceneManager.MarkSceneDirty(s);EditorSceneManager.SaveScene(s);AssetDatabase.SaveAssets();if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);if(added)EditorSceneManager.CloseScene(s,true);
 }
 public static void PolishLighting(){
  Setup();var s=EditorSceneManager.OpenScene(Stairs);var world=World(s);
  var root=world.transform.Find("Residential stairwell - editable architecture");
  if(!root.Find("Third floor apartment doorway light")){
   var lamp=Spot("Third floor apartment doorway light",root,new Vector3(10.6f,9.1f,.4f),new Vector3(10.6f,7.4f,3),2.8f,5.5f,78,new Color(.70f,.63f,.47f),false);
   B("Third floor door wall sconce",root,new Vector3(10.6f,9.08f,2.7f),new Vector3(.38f,.14f,.18f),warm);
   var look=All<WarehouseAtmosphereTest>(s).Single();look.lamps=look.lamps.Concat(new[]{lamp}).ToArray();look.decorativeLights=look.decorativeLights.Concat(new[]{lamp}).ToArray();
  }
  EditorSceneManager.MarkSceneDirty(s);EditorSceneManager.SaveScene(s);AssetDatabase.SaveAssets();Capture();
 }
 public static void Capture(){
  Directory.CreateDirectory("Captures/ResidentialDistrict");
  foreach(string path in new[]{Outside,Stairs,Apartment}){
   var s=EditorSceneManager.OpenScene(path);var world=World(s);world.GetComponent<Recoil3DWorld>().ApplyAtmosphere();var cam=All<Camera>(s).First(c=>c.CompareTag("MainCamera"));var follow=cam.GetComponent<RecoilCameraController>();follow.enabled=false;cam.scene=s;cam.allowHDR=true;
   if(path==Outside){var sweep=world.GetComponentInChildren<CheckpointSearchlight>();sweep.Pose(.25f);world.Player.Position=new Vector2(154,.845f);world.Player.SyncVisuals(0);CaptureCamera(cam,"BridgeCheckpoint",new Vector3(158,6,-25),new Vector3(158,2,6),56);CaptureCamera(cam,"WorkersHousing",new Vector3(215,6,-25),new Vector3(215,5,5),53);CaptureCamera(cam,"FactoryToEstateOverview",new Vector3(166,26,-65),new Vector3(171,0,18),70);}
   else if(path==Stairs){var fog=cam.GetComponent<IndoorVisionFog>();if(fog)fog.effectEnabled=false;CaptureCamera(cam,"ResidentialStairwell",new Vector3(6,5,-22),new Vector3(6,4.5f,2),36);world.Player.Position=new Vector2(9.2f,7.245f);world.Player.SyncVisuals(0);CaptureCamera(cam,"ThirdFloorRightDoor",new Vector3(9,8,-16),new Vector3(8.5f,7.7f,3),36);}
   else CaptureCamera(cam,"ApartmentMemoryDuplicate",new Vector3(12,3.5f,-25),new Vector3(12,2,3),52);
  }
 }
 static void CaptureCamera(Camera cam,string name,Vector3 pos,Vector3 target,float fov){
  cam.transform.position=pos;cam.transform.LookAt(target);cam.fieldOfView=fov;var rt=RenderTexture.GetTemporary(1440,810,24,RenderTextureFormat.ARGBHalf);var prev=cam.targetTexture;var active=RenderTexture.active;Texture2D raw=null,output=null;
  try{cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;raw=new Texture2D(1440,810,TextureFormat.RGBAFloat,false,true);raw.ReadPixels(new Rect(0,0,1440,810),0,0);raw.Apply();var colors=raw.GetPixels();if(QualitySettings.activeColorSpace==ColorSpace.Linear)for(int i=0;i<colors.Length;i++)colors[i]=colors[i].gamma;output=new Texture2D(1440,810,TextureFormat.RGB24,false,true);output.SetPixels(colors);output.Apply();File.WriteAllBytes("Captures/ResidentialDistrict/"+name+".png",output.EncodeToPNG());}
  finally{cam.targetTexture=prev;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);if(raw)Object.DestroyImmediate(raw);if(output)Object.DestroyImmediate(output);}
 }
}
