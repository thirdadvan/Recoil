using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
public static class FactoryConflictEnvironment
{
 const string Folder="Assets/Recoil3D/ConflictAftermath",Marker="1978 conflict aftermath - editable";
 static Material cloth,blood,metal,paper,wood,orange,tile,rubber,glass,ink;static Material textMat;static Mesh blanketMesh,shirtMesh;static Texture2D photo;
 static T[] All<T>(Scene s)where T:Component{return s.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<T>(true)).ToArray();}
 static Material Mat(string name,Color c,string shader="Recoil/3D/Weathered Surface"){
  string path=Folder+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find(shader));AssetDatabase.CreateAsset(m,path);}m.color=c;m.enableInstancing=true;if(m.HasProperty("_Glossiness"))m.SetFloat("_Glossiness",.12f);if(m.HasProperty("_Weathering"))m.SetFloat("_Weathering",.65f);EditorUtility.SetDirty(m);return m;
 }
 static void Materials(){
  cloth=Mat("Dusty bedding and bandage linen",new Color(.36f,.34f,.26f));blood=Mat("Old dried blood on fabric",new Color(.15f,.052f,.035f));metal=Mat("Oxidised steel and sheet coils",new Color(.18f,.22f,.20f));paper=Mat("Yellowed medicine paper",new Color(.53f,.50f,.37f));wood=Mat("Salvaged barricade timber",new Color(.23f,.18f,.12f));orange=Mat("Faded worker overalls",new Color(.42f,.23f,.07f));rubber=Mat("Discarded shield and boot rubber",new Color(.045f,.052f,.048f));ink=Mat("Faded official red ink",new Color(.24f,.045f,.028f));glass=Mat("Clouded bottle glass",new Color(.20f,.28f,.25f));tile=Mat("Stained bathhouse tile",new Color(.45f,.47f,.42f),"Recoil/3D/Aged Bath Tiles");
  textMat=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Depth tested printed lettering.mat");if(!textMat){textMat=new Material(Shader.Find("Recoil/3D/World Text"));AssetDatabase.CreateAsset(textMat,Folder+"/Depth tested printed lettering.mat");}
  blanketMesh=ClothMesh("Crumpled blanket mesh",false);shirtMesh=ClothMesh("Hanging work shirt mesh",true);
 }
 static Transform G(string name,Transform parent,Vector3 position){var t=new GameObject(name).transform;t.SetParent(parent,false);t.position=position;return t;}
 static Transform Shape(string name,Transform parent,Vector3 p,Vector3 size,Material m,PrimitiveType type=PrimitiveType.Cube){var go=GameObject.CreatePrimitive(type);go.name=name;Object.DestroyImmediate(go.GetComponent<Collider>());go.transform.SetParent(parent,false);go.transform.position=p;go.transform.localScale=size;var r=go.GetComponent<Renderer>();r.sharedMaterial=m;return go.transform;}
 static Transform Rod(string name,Transform parent,Vector3 a,Vector3 b,float radius,Material m){var t=Shape(name,parent,(a+b)*.5f,new Vector3(radius*2,Vector3.Distance(a,b)*.5f,radius*2),m,PrimitiveType.Cylinder);t.rotation=Quaternion.FromToRotation(Vector3.up,b-a);return t;}
 static Transform Label(string value,Transform parent,Vector3 p,float size,Color colour){var t=G(value,parent,p);var tm=t.gameObject.AddComponent<TextMesh>();tm.text=value;tm.anchor=TextAnchor.MiddleCenter;tm.alignment=TextAlignment.Center;tm.characterSize=size;tm.fontSize=64;tm.color=colour;tm.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");tm.font.RequestCharactersInTexture(value,64,FontStyle.Normal);tm.GetComponent<Renderer>().sharedMaterial=textMat;t.gameObject.AddComponent<Recoil3DDepthText>();return t;}
 static Mesh ClothMesh(string name,bool shirt){var saved=AssetDatabase.LoadAssetAtPath<Mesh>(Folder+"/"+name+".asset");if(saved)return saved;var v=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();int nx=8,ny=10;
  for(int y=0;y<=ny;y++)for(int x=0;x<=nx;x++){float u=x/(float)nx,w=y/(float)ny;float width=shirt?(w>.65f?.34f:w>.45f?.48f:.23f):.5f;v.Add(shirt?new Vector3((u-.5f)*width*2,w-.5f,Mathf.Sin(u*15+w*3)*.028f+Mathf.Sin(w*8)*.035f):new Vector3(u-.5f,.025f*Mathf.Sin(u*13+w*8)+.04f*Mathf.Sin(w*Mathf.PI),w-.5f));uv.Add(new Vector2(u,w));}
  for(int y=0;y<ny;y++)for(int x=0;x<nx;x++){int i=y*(nx+1)+x;triangles.AddRange(new[]{i,i+nx+1,i+1,i+1,i+nx+1,i+nx+2});}
  var mesh=new Mesh();mesh.name=name;mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,Folder+"/"+name+".asset");return mesh;
 }
 static Transform Drape(Transform p,Vector3 position,Vector3 scale,Material m,bool hanging=false){var t=G(hanging?"Work shirt on hanging hook":"Crumpled linen",p,position);t.localScale=scale;t.gameObject.AddComponent<MeshFilter>().sharedMesh=hanging?shirtMesh:blanketMesh;t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=m;return t;}
 static void Stain(Transform p,Vector3 position,float width,int seed){var r=new System.Random(seed);for(int i=0;i<5;i++){float x=((float)r.NextDouble()-.5f)*width,z=((float)r.NextDouble()-.5f)*width;Shape("Dried blood absorbed in linen",p,position+new Vector3(x,.012f,z),new Vector3(width*.42f,.009f,width*.28f),blood,PrimitiveType.Sphere);}}
 static void Bedding(Transform p,Vector3 at,int seed){var g=G("Abandoned improvised treatment bed "+seed,p,at);Shape("Flattened straw mattress",g,at+Vector3.up*.065f,new Vector3(.85f,.13f,1.65f),cloth,PrimitiveType.Sphere);Drape(g,at+new Vector3(.04f,.14f,.10f),new Vector3(.85f,1,1.5f),cloth);Shape("Rolled blanket pillow",g,at+new Vector3(0,.18f,.60f),new Vector3(.64f,.16f,.22f),cloth,PrimitiveType.Sphere);Stain(g,at+new Vector3(.08f,.17f,.12f),.30f,seed);Medicines(g,at+new Vector3(.6f,.04f,-.4f),seed);}
 static void Medicines(Transform p,Vector3 at,int seed){for(int i=0;i<4;i++){var t=Shape("Opened antibiotic carton - old paper",p,at+new Vector3(i*.10f,.025f,(i%2)*.09f),new Vector3(.16f,.05f,.075f),paper);t.rotation=Quaternion.Euler(0,seed+i*28,0);Shape("Faded printed medicine band",t,t.position+new Vector3(0,.027f,0),new Vector3(.10f,.005f,.025f),ink);}Shape("Empty brown-glass medicine bottle",p,at+new Vector3(.12f,.08f,.25f),new Vector3(.08f,.16f,.08f),glass,PrimitiveType.Cylinder);Drape(p,at+new Vector3(-.18f,.02f,.05f),new Vector3(.10f,1,.6f),cloth);}
 static void Hanging(Transform p,Vector3 at,int count){Rod("Overhead changing rail",p,at+new Vector3(-.6f,0,0),at+new Vector3(count*.65f,0,0),.025f,metal);for(int i=0;i<count;i++){var top=at+Vector3.right*i*.65f;Rod("Rusty hanging chain",p,top,top+Vector3.down*.46f,.009f,metal);Rod("Clothes hook",p,top+Vector3.down*.46f,top+new Vector3(.10f,-.54f,0),.016f,metal);Drape(p,top+new Vector3(.02f,-1.05f,.015f),new Vector3(.68f,1,.45f),i%2==0?orange:cloth,true);foreach(float side in new[]{-.11f,.11f})Drape(p,top+new Vector3(side,-1.65f,.04f),new Vector3(.16f,.8f,.18f),orange,true);}}
 static ReadableProp Note(Transform p,Vector3 at,string title,string text,Texture2D image=null){var t=Shape(title,p,at,new Vector3(.25f,.012f,.34f),paper);var item=t.gameObject.AddComponent<ReadableProp>();item.displayName=title;item.pageText=text;item.pageImage=image;item.interactionPrompt=image?"E / EXAMINE PHOTOGRAPH":"E / READ NOTE";item.interactionRange=1.75f;return item;}
 static void Poster(Transform p,Vector3 at,bool health){var g=G("Official poster - defaced by workers",p,at);Shape("Weathered paper pasted to plaster",g,at,new Vector3(1.0f,1.4f,.025f),paper);Shape("Official red header",g,at+new Vector3(0,.42f,-.018f),new Vector3(.86f,.23f,.009f),ink);Label(health?"HEALTH\nIS OUR DUTY":"PRODUCTION\nIS RECOVERY",g,at+new Vector3(0,.41f,-.03f),.026f,new Color(.57f,.55f,.43f));for(int i=0;i<5;i++)Shape("Printed factory chimney",g,at+new Vector3(-.3f+i*.14f,-.02f,-.019f),new Vector3(.08f,.2f+i%2*.13f,.01f),metal);Label(health?"REPORT TO THE\nHEALTH STATION":"TOGETHER FOR\nTHE NEXT SHIFT",g,at+new Vector3(0,-.3f,-.028f),.022f,new Color(.13f,.16f,.14f));
  var slash=Shape("Workers' crossed-out slogan",g,at+new Vector3(0,.1f,-.043f),new Vector3(.95f,.045f,.012f),blood);slash.rotation=Quaternion.Euler(0,0,-27);Label(health?"WHERE ARE THEY?":"NO MORE CUTS",g,at+new Vector3(0,-.53f,-.046f),.024f,new Color(.28f,.055f,.035f));var r=g.gameObject.AddComponent<ReadableProp>();r.displayName="Defaced official poster";r.interactionPrompt="E / READ POSTER";r.interactionRange=1.65f;r.pageText=health?"HEALTH IS OUR DUTY\nReport to the health station.\n\nSomeone has written across the bottom:\nWHERE ARE THEY?":"PRODUCTION IS RECOVERY\nTogether for the next shift.\n\nThe printed slogan is crossed out:\nNO MORE CUTS";
 }
 static void Showers(Transform p){float f=7.6f;var g=G("Tiled workers wash bay - reference1",p,new Vector3(24.8f,f,5));
  Shape("Tiled rear wall",g,new Vector3(24.85f,f+1.85f,6.55f),new Vector3(5.4f,3.7f,.14f),tile);Shape("Tiled return and thick partition",g,new Vector3(22.17f,f+1.85f,5.0f),new Vector3(.34f,3.7f,3.05f),tile);Shape("Tiled lintel",g,new Vector3(24.8f,f+3.35f,4.2f),new Vector3(5.6f,.5f,1.7f),tile);Shape("Old tiled wash floor",g,new Vector3(24.8f,f+.022f,4.8f),new Vector3(5.4f,.04f,3.4f),tile);
  Rod("Overhead water supply",g,new Vector3(22.35f,f+3.08f,6.35f),new Vector3(27.4f,f+3.08f,6.35f),.035f,metal);
  foreach(float x in new[]{22.8f,23.8f,25f,26.4f}){Rod("Exposed shower riser",g,new Vector3(x,f+1.08f,6.31f),new Vector3(x,f+2.8f,6.31f),.019f,metal);Rod("Bent shower arm",g,new Vector3(x,f+2.8f,6.31f),new Vector3(x,f+2.8f,5.98f),.022f,metal);Shape("Downward metal shower head",g,new Vector3(x,f+2.76f,5.98f),new Vector3(.17f,.045f,.17f),metal,PrimitiveType.Cylinder);Shape("Old mixer tap",g,new Vector3(x,f+1.1f,6.24f),new Vector3(.18f,.04f,.07f),metal);}
  for(int i=0;i<14;i++)Shape("Drain grate bar",g,new Vector3(23.2f+i*.26f,f+.05f,4.9f),new Vector3(.08f,.02f,.4f),metal);
  Shape("Dark doorway to utility recess",g,new Vector3(27.0f,f+1.1f,6.46f),new Vector3(.85f,2.2f,.035f),rubber);Shape("Wall mounted bench",g,new Vector3(24.8f,f+.43f,6.04f),new Vector3(2.2f,.10f,.34f),wood);
  Hanging(p,new Vector3(18.8f,f+3.4f,4.3f),4);
  Shape("Wash bay enamel fluorescent housing",g,new Vector3(25.0f,f+3.05f,4.8f),new Vector3(1.3f,.10f,.34f),metal);
  var diffuser=Mat("Wash bay frosted lamp diffuser",new Color(.55f,.59f,.54f),"Standard");diffuser.EnableKeyword("_EMISSION");diffuser.SetColor("_EmissionColor",new Color(.7f,.76f,.68f));
  Shape("Wash bay luminous diffuser",g,new Vector3(25,f+2.99f,4.8f),new Vector3(1.16f,.025f,.24f),diffuser);
  var lt=G("Wash bay fixture light",g,new Vector3(25,f+2.91f,4.6f));lt.rotation=Quaternion.Euler(90,0,0);var l=lt.gameObject.AddComponent<Light>();l.type=LightType.Spot;l.intensity=1.5f;l.range=4.6f;l.spotAngle=112;l.innerSpotAngle=65;l.color=new Color(.72f,.78f,.71f);l.shadows=LightShadows.None;

 }
 static void FactoryInterior(Transform p){
  Bedding(p,new Vector3(18.5f,7.61f,1.75f),7);Bedding(p,new Vector3(20.2f,7.61f,2.45f),8);Bedding(p,new Vector3(24.9f,11.61f,2.6f),9);
  for(int i=0;i<6;i++){Drape(p,new Vector3(17.9f+i*.38f,7.66f,1.05f+i%2*.38f),new Vector3(.17f,1,.52f),cloth);Stain(p,new Vector3(17.9f+i*.38f,7.68f,1.05f+i%2*.38f),.1f,20+i);}
  Showers(p);Poster(p,new Vector3(16.8f,9.65f,6.54f),false);Poster(p,new Vector3(17.0f,13.65f,6.54f),true);Poster(p,new Vector3(4.5f,2,5.8f),false);
  Note(p,new Vector3(18.1f,8.25f,.85f),"Guard rota — 26 October 1978","26 OCTOBER 1978\n\nTwo at the left entrance. Change at the bell.\nKeep a passage clear for the wounded.\nDo not move the bedding back into the wash bay.\n\n[The bottom of the page is stained and torn.]");
  Note(p,new Vector3(25.6f,12.25f,.85f),"Scrap of a worker's note","27 OCTOBER\n\nThe doors will hold if the cabinet stays across them.\nThere are still people on the upper floor.\nDo not leave anyone behind.");
  for(int i=0;i<2;i++){float x=i==0?3.4f:31.2f;Shape("Heavy cabinet pushed clear of entrance",p,new Vector3(x,.92f,2.9f),new Vector3(1.3f,1.84f,.65f),metal).rotation=Quaternion.Euler(0,i==0?18:-15,0);for(int n=0;n<3;n++){var t=Shape("Abandoned barricade timber",p,new Vector3(x+n*.15f,.24f+n*.13f,1.5f),new Vector3(1.7f,.17f,.18f),wood);t.rotation=Quaternion.Euler(0,27+n*18,8+n*5);}}
 }
 static void Washroom(Transform p){for(int pile=0;pile<3;pile++){Vector3 at=new Vector3(3.1f+pile*1.6f,.05f,2.0f+pile%2*.7f);Shape("Laundry tub waiting to be cleaned",p,at+Vector3.up*.18f,new Vector3(.85f,.36f,.65f),metal);for(int i=0;i<7;i++){var pos=at+new Vector3((i%3-.8f)*.19f,.36f+i*.015f,(i/3-.6f)*.19f);Drape(p,pos,new Vector3(.45f,1,.42f),i%3==0?orange:cloth);Stain(p,pos+Vector3.up*.03f,.17f,pile*11+i);}}
  Drape(p,new Vector3(6.8f,.03f,1),new Vector3(.25f,1,.8f),cloth);Stain(p,new Vector3(6.8f,.05f,1),.25f,90);Rod("Clothes drying rod",p,new Vector3(3,2.7f,4.7f),new Vector3(7.5f,2.7f,4.7f),.025f,metal);Hanging(p,new Vector3(3.4f,3.0f,4.7f),4);
 }
 static void ChangingRoom(Transform p){Hanging(p,new Vector3(2.2f,3.2f,4.3f),5);Bedding(p,new Vector3(8.8f,.02f,2.3f),38);Medicines(p,new Vector3(7,.07f,1.5f),31);
  Shape("W.'s battered personal case",p,new Vector3(5.1f,.43f,1.7f),new Vector3(.95f,.22f,.55f),wood);var n=Note(p,new Vector3(5.0f,.56f,1.45f),"Private photograph — W. / P.","1975. W. / P.",photo);
  var m=Mat("Private photograph printed surface",Color.white,"Standard");m.mainTexture=photo;var q=Shape("Photograph face",p,new Vector3(5,.571f,1.45f),new Vector3(.29f,.014f,.21f),m);q.rotation=Quaternion.Euler(0,-11,0);EditorUtility.SetDirty(m);
 }
 static void Bag(Transform p,Vector3 at,int i){var b=Shape("Old sand-filled flood sack",p,at,new Vector3(.24f,.375f,.42f),cloth,PrimitiveType.Capsule);b.rotation=Quaternion.Euler(0,i*37,90);Shape("Twisted sack tie",p,at+b.up*.38f,new Vector3(.10f,.07f,.13f),cloth);}
 static void Outside(Transform p,Scene s){var world=All<RecoilPrototype>(s).Single();var doors=All<RecoilDoor>(s);var entry=doors.First(d=>d.name=="Factory Entrance - Employee Card");float x=entry.transform.position.x;
  // Barricade remains in front/back depth, leaving a continuous1.8m-wide Z corridor.
  for(int bank=0;bank<2;bank++)for(int row=0;row<3;row++)for(int i=0;i<10;i++)Bag(p,new Vector3(x-3+i*.69f+row%2*.2f,.12f+row*.20f,bank==0?-2.4f:2.7f),bank*30+row*10+i);
  for(int j=0;j<3;j++){float bx=x-2.8f+j*1.0f;Shape("Barricade cabinet cleared beside factory entrance",p,new Vector3(bx,.7f,3.6f),new Vector3(.7f,1.4f,.8f),metal).rotation=Quaternion.Euler(0,20+j*13,0);Rod("Broken gate brace piled aside",p,new Vector3(bx-1,.1f,2.3f),new Vector3(bx+.8f,.6f,2.9f),.07f,metal);}
  foreach(var sill in All<Transform>(s).Where(t=>t.name=="Window sill"&&t.position.x>x-2&&t.position.x<x+33&&t.position.y<20).ToArray()){
   float wx=sill.position.x,wy=sill.position.y+1.05f;float z=sill.position.z-.35f;
   for(int j=0;j<3;j++){var plank=Shape("Salvaged plank across factory glazing",p,new Vector3(wx,wy-.65f+j*.57f,z-j*.025f),new Vector3(2.45f,.23f,.11f),wood);plank.rotation=Quaternion.Euler(0,0,(j%2==0?1:-1)*8);}
   for(int j=0;j<5;j++)Shape("Strip cut from coiled steel plate",p,new Vector3(wx-.75f+j*.32f,wy+.1f,z-.12f),new Vector3(.25f,1.6f,.05f),metal).rotation=Quaternion.Euler(0,0,3);
  }
  foreach(float at in new[]{x-1.8f,x+29}){Drape(p,new Vector3(at,.03f,1.3f),new Vector3(.7f,1,.55f),orange);Shape("Discarded riot shield - cracked plate",p,new Vector3(at+1,.14f,1.6f),new Vector3(.55f,.12f,.95f),rubber).rotation=Quaternion.Euler(0,24,0);}
 }
 static Texture2D MakePhoto(bool force=false){
  string realisticPath=Folder+"/Private workers photograph realistic.png";if(File.Exists(realisticPath))return AssetDatabase.LoadAssetAtPath<Texture2D>(realisticPath);
  string path=Folder+"/Private workers photograph.png";if(File.Exists(path)&&!force)return AssetDatabase.LoadAssetAtPath<Texture2D>(path);var preview=EditorSceneManager.NewPreviewScene();
  try{var root=new GameObject("Photo staging");SceneManager.MoveGameObjectToScene(root,preview);root.transform.position=new Vector3(1000,1000,1000);Vector3 origin=root.transform.position;
   var grey=Mat("Photograph faded coats",new Color(.28f,.26f,.21f),"Standard");var skin=Mat("Photograph faces",new Color(.54f,.49f,.40f),"Standard");Shape("Factory wall",root.transform,origin+new Vector3(0,1.2f,2),new Vector3(7,3.4f,.3f),metal);
   for(int i=0;i<2;i++){float x=i==0?-.65f:.65f;var centre=origin+new Vector3(x,0,0);foreach(float dx in new[]{-.13f,.13f}){Shape("Worker trouser leg",root.transform,centre+new Vector3(dx,.43f,0),new Vector3(.22f,.40f,.25f),grey,PrimitiveType.Capsule);Shape("Work boot",root.transform,centre+new Vector3(dx,.07f,-.05f),new Vector3(.25f,.15f,.35f),rubber);}Shape("Heavy buttoned work coat",root.transform,centre+new Vector3(0,1.15f,0),new Vector3(i==0?.54f:.78f,.95f,i==0?.30f:.50f),grey,PrimitiveType.Sphere);Shape("Coat front seam",root.transform,centre+new Vector3(0,1.15f,i==0?-.154f:-.254f),new Vector3(.018f,.68f,.012f),metal);for(int button=0;button<4;button++)Shape("Coat button",root.transform,centre+new Vector3(.025f,.95f+button*.13f,i==0?-.159f:-.259f),new Vector3(.025f,.025f,.012f),rubber,PrimitiveType.Sphere);Shape("Coat breast pocket",root.transform,centre+new Vector3(-.13f,1.38f,i==0?-.14f:-.23f),new Vector3(.13f,.14f,.024f),grey);Shape("Head",root.transform,centre+new Vector3(0,1.81f,-.02f),new Vector3(.29f,.34f,.28f),skin,PrimitiveType.Sphere);Shape("Flat worker cap",root.transform,centre+new Vector3(0,1.98f,-.02f),new Vector3(.34f,.10f,.33f),grey,PrimitiveType.Capsule);foreach(float side in new[]{-1f,1f})Rod("Coat sleeve",root.transform,centre+new Vector3(side*(i==0?.27f:.46f),1.44f,0),centre+new Vector3(side*(i==0?.32f:.51f),.86f,-.03f),.10f,grey);}
   var light=new GameObject("Photo fill").AddComponent<Light>();SceneManager.MoveGameObjectToScene(light.gameObject,preview);light.type=LightType.Directional;light.intensity=1.3f;light.transform.rotation=Quaternion.Euler(35,-25,0);
   var camera=new GameObject("Photo camera").AddComponent<Camera>();SceneManager.MoveGameObjectToScene(camera.gameObject,preview);camera.scene=preview;camera.transform.position=origin+new Vector3(0,1.2f,-6);camera.transform.LookAt(origin+new Vector3(0,1,0));camera.orthographic=true;camera.orthographicSize=1.32f;camera.farClipPlane=12;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.24f,.25f,.23f);
   var rt=RenderTexture.GetTemporary(768,512,24);camera.targetTexture=rt;var active=RenderTexture.active;Texture2D tex=null;try{camera.Render();RenderTexture.active=rt;tex=new Texture2D(768,512,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,768,512),0,0);tex.Apply();var pixels=tex.GetPixels();var random=new System.Random(1975);for(int i=0;i<pixels.Length;i++){float l=pixels[i].grayscale;float noise=((float)random.NextDouble()-.5f)*.065f;pixels[i]=new Color(l*1.04f+.08f+noise,l*.94f+.055f+noise,l*.76f+.025f+noise);}tex.SetPixels(pixels);tex.Apply();File.WriteAllBytes(path,tex.EncodeToPNG());}finally{RenderTexture.active=active;camera.targetTexture=null;RenderTexture.ReleaseTemporary(rt);if(tex)Object.DestroyImmediate(tex);}
  }finally{EditorSceneManager.ClosePreviewScene(preview);}AssetDatabase.ImportAsset(path);return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
 }
 public static void Apply(){
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play first");Materials();photo=MakePhoto();var original=SceneManager.GetActiveScene();string backup="Backups/FactoryConflictEnvironment/Scenes-"+DateTime.Now.ToString("yyyyMMdd-HHmmss");Directory.CreateDirectory(backup);var report=new List<string>();
  var paths=new[]{"Assets/Scenes/3D/RecoilFactoryInterior.unity","Assets/Scenes/3D/FactoryRooms/FactoryWashroomF2.unity","Assets/Scenes/3D/FactoryRooms/FactoryChangingRoomF2.unity","Assets/Scenes/3D/RecoilPrototype.unity"};
  foreach(var path in paths){var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);SceneManager.SetActiveScene(scene);try{
   EditorSceneManager.SaveScene(scene,backup+"/"+scene.name+".unity",true);var w=All<RecoilPrototype>(scene).Single();var doors=All<RecoilDoor>(scene);var routes=doors.Select(d=>d.destinationScene+"|"+d.destinationSpawn+"|"+d.transform.position).ToArray();var old=w.transform.Find(Marker);if(old)Object.DestroyImmediate(old.gameObject);var group=G(Marker,w.transform,Vector3.zero);
   if(scene.name=="RecoilFactoryInterior")FactoryInterior(group);else if(scene.name=="FactoryWashroomF2")Washroom(group);else if(scene.name=="FactoryChangingRoomF2")ChangingRoom(group);else Outside(group,scene);
   var look=All<WarehouseAtmosphereTest>(scene).FirstOrDefault();if(look){look.decorativeLights=look.decorativeLights.Where(l=>l).Concat(group.GetComponentsInChildren<Light>()).Distinct().ToArray();look.lamps=look.lamps.Where(l=>l).Concat(group.GetComponentsInChildren<Light>()).Distinct().ToArray();EditorUtility.SetDirty(look);}
   foreach(var ai in All<RampagerController>(scene)){ai.coreFoldDuration=1;ai.coreStillDuration=2.5f;ai.coreAttackTransitionDuration=.5f;ai.exposedCoreRecoveryDuration=4;EditorUtility.SetDirty(ai);}
   for(int i=0;i<doors.Length;i++)if(routes[i]!=doors[i].destinationScene+"|"+doors[i].destinationSpawn+"|"+doors[i].transform.position)throw new Exception("Existing route changed");
   if(group.GetComponentsInChildren<Collider>().Length>0||group.GetComponentsInChildren<Collider2D>().Length>0)throw new Exception("Decoration must not add hidden blockers");
   EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);report.Add(scene.name+": "+group.GetComponentsInChildren<MeshRenderer>().Length+" visual pieces; "+group.GetComponentsInChildren<ReadableProp>().Length+" readable objects; routes/spawns and collision unchanged");
  }finally{if(opened)EditorSceneManager.CloseScene(scene,true);}}
  SceneManager.SetActiveScene(original);var originalWorld=All<Recoil3DWorld>(original).FirstOrDefault();if(originalWorld)originalWorld.ApplyAtmosphere();AssetDatabase.SaveAssets();Directory.CreateDirectory("output/factory-conflict");File.WriteAllText("output/factory-conflict/result.txt",string.Join("\n",report)+"\nBackup: "+backup);
  File.WriteAllText("output/factory-conflict/references.txt","Visual references, not a real-world location assignment:\nIPN archival photographs (Wujek factory barricades, occupation, armed cordon): https://edukacja.ipn.gov.pl/edu/materialy-edukacyjne/teki-edukacyjn/stan-wojenny-19811983/155929,Fotografie.html\nEuropean Solidarity Centre, Gate2 and posted demands: https://ecs.gda.pl/historia-pewnej-wolnosci/\nUser image1: tiled communal washroom, exposed risers, benches and deep partitions.\nAll aftermath objects refer to the fictional1978 conflict seen in1982: dry brown stains, dusty linen, empty medicine packaging, cleared barricades, defaced notices. No real people, emblems or photographs copied into fictional evidence.\nPhoto asset is a staged render using the game's geometry style; right worker has a bulky coat/chest silhouette.\n");
 }

 public static void RefinePresentation(){
  Materials();photo=MakePhoto(true);var original=SceneManager.GetActiveScene();
  foreach(var path in new[]{"Assets/Scenes/3D/RecoilFactoryInterior.unity","Assets/Scenes/3D/FactoryRooms/FactoryWashroomF2.unity","Assets/Scenes/3D/FactoryRooms/FactoryChangingRoomF2.unity"}){
   var s=SceneManager.GetSceneByPath(path);bool opened=!s.isLoaded;if(opened)s=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);try{
    var w=All<RecoilPrototype>(s).Single();var group=w.transform.Find(Marker);if(!group)continue;
    if(s.name=="RecoilFactoryInterior"){
     var board=All<Recoil3DSpriteVisual>(s).First(v=>v.name=="Notice board"&&v.transform.parent.name=="Open break and drinking alcove");var bp=board.transform.position;bp.x=20.5f;board.transform.position=bp;board.depthOffset=board.transform.InverseTransformPoint(new Vector3(bp.x,bp.y,6.60f)).z;board.Sync();EditorUtility.SetDirty(board);
     var g=group.Find("Tiled workers wash bay - reference1");
     if(!g.Find("Reflected light from wash bay fluorescent")){var lt=G("Reflected light from wash bay fluorescent",g,new Vector3(25,10.50f,4.58f));lt.rotation=Quaternion.LookRotation(new Vector3(25,9.55f,6.55f)-lt.position);var l=lt.gameObject.AddComponent<Light>();l.type=LightType.Spot;l.color=new Color(.72f,.78f,.71f);l.intensity=1.4f;l.range=3.8f;l.spotAngle=112;l.innerSpotAngle=65;l.shadows=LightShadows.None;
      var look=All<WarehouseAtmosphereTest>(s).Single();look.decorativeLights=look.decorativeLights.Where(x=>x).Concat(new[]{l}).Distinct().ToArray();look.lamps=look.lamps.Where(x=>x).Concat(new[]{l}).Distinct().ToArray();EditorUtility.SetDirty(look);}
    }else{
     float oldZ=s.name=="FactoryWashroomF2"?4.7f:4.3f,target=s.name=="FactoryWashroomF2"?3.6f:2.4f;
     foreach(var t in group.GetComponentsInChildren<Transform>().Where(t=>t.name=="Overhead changing rail"||t.name=="Rusty hanging chain"||t.name=="Clothes hook"||t.name=="Work shirt on hanging hook"))if(t.position.z>oldZ-.3f){var p=t.position;p.z-=oldZ-target;t.position=p;}
     foreach(var item in group.GetComponentsInChildren<ReadableProp>().Where(r=>r.pageImage))item.pageImage=photo;
    }
    EditorSceneManager.MarkSceneDirty(s);EditorSceneManager.SaveScene(s);
   }finally{if(opened)EditorSceneManager.CloseScene(s,true);}
  }
  SceneManager.SetActiveScene(original);var world=All<Recoil3DWorld>(original).FirstOrDefault();if(world)world.ApplyAtmosphere();AssetDatabase.SaveAssets();
 }

 public static void AddMainGateAftermath(){
  Materials();const string path="Assets/Scenes/3D/RecoilPrototype.unity";var original=SceneManager.GetActiveScene();var s=SceneManager.GetSceneByPath(path);bool opened=!s.isLoaded;if(opened)s=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
  try{var w=All<RecoilPrototype>(s).Single();var group=w.transform.Find(Marker);var gate=All<Transform>(s).First(t=>t.name=="Heavy barred factory main gate - chained");var existing=group.Find("Sandbag barricade cleared at compound main gate");if(existing)Object.DestroyImmediate(existing.gameObject);var g=G("Sandbag barricade cleared at compound main gate",group,Vector3.zero);
   float gx=gate.position.x;for(int bank=0;bank<2;bank++)for(int row=0;row<3;row++)for(int i=0;i<12;i++)Bag(g,new Vector3(gx-8+i*.64f+row%2*.18f,.12f+row*.20f,bank==0?-2.8f:3.2f),100+bank*40+row*12+i);
   for(int i=0;i<3;i++)Shape("Riot barricade concrete footing moved aside",g,new Vector3(gx-2+i*.8f,.18f,4.5f),new Vector3(.7f,.36f,.9f),metal);
   foreach(var tie in group.GetComponentsInChildren<Transform>().Where(t=>t.name=="Twisted sack tie")){int index=tie.GetSiblingIndex();if(index>0){var sack=tie.parent.GetChild(index-1);if(sack.name=="Old sand-filled flood sack")tie.position=sack.position+sack.up*.38f;}}
   EditorSceneManager.MarkSceneDirty(s);EditorSceneManager.SaveScene(s);
  }finally{if(opened)EditorSceneManager.CloseScene(s,true);SceneManager.SetActiveScene(original);var world=All<Recoil3DWorld>(original).FirstOrDefault();if(world)world.ApplyAtmosphere();}
 }
 public static void Verify(){
  var original=SceneManager.GetActiveScene();var report=new List<string>();
  foreach(var path in new[]{"Assets/Scenes/3D/RecoilFactoryInterior.unity","Assets/Scenes/3D/FactoryRooms/FactoryWashroomF2.unity","Assets/Scenes/3D/FactoryRooms/FactoryChangingRoomF2.unity","Assets/Scenes/3D/RecoilPrototype.unity"}){
   var s=SceneManager.GetSceneByPath(path);bool opened=!s.isLoaded;if(opened)s=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);try{
    var group=All<RecoilPrototype>(s).Single().transform.Find(Marker);if(!group)throw new Exception("Missing aftermath group");if(group.GetComponentsInChildren<Collider>(true).Length+group.GetComponentsInChildren<Collider2D>(true).Length>0)throw new Exception("New props block gameplay path");
    foreach(var b in group.GetComponentsInChildren<Transform>().Where(t=>t.name=="Old sand-filled flood sack"))if(Mathf.Abs(b.position.z)<1.8f)throw new Exception("Sandbag intrudes into cleared corridor");
    foreach(var ai in All<RampagerController>(s))if(Mathf.Abs(ai.CoreReactionTotal-4)>.001f)throw new Exception("Wrong core reaction duration");
    foreach(var note in group.GetComponentsInChildren<ReadableProp>())if(!note.pageImage&&string.IsNullOrWhiteSpace(note.pageText))throw new Exception("Empty readable");
    report.Add("PASS "+s.name+": aftermath group present; no new physical blockers; bags outside walking planes; four-second core reaction; "+group.GetComponentsInChildren<ReadableProp>().Length+" valid readable objects");
   }finally{if(opened)EditorSceneManager.CloseScene(s,true);}
  }
  var shader=Shader.Find("Recoil/3D/Aged Bath Tiles");if(!shader.isSupported||ShaderUtil.GetShaderMessages(shader).Any(m=>m.severity==UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error))throw new Exception("Tile shader compile failure");report.Add("PASS aged tile shader on current Metal renderer");
  SceneManager.SetActiveScene(original);var world=All<Recoil3DWorld>(original).FirstOrDefault();if(world)world.ApplyAtmosphere();File.WriteAllLines("output/factory-conflict/verification.txt",report);
 }
}
