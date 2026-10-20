using System;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class FourStoreyFactoryBuilder
{
 const string Outdoors="Assets/Scenes/RecoilPrototype.unity",Indoors="Assets/Scenes/RecoilFactoryInterior.unity",Shortcut="factory_shortcut_floor3";
 const float Storey=7.6f;
 static Sprite sprite;static Material material;
 static Transform G(string n,Transform parent,Vector2 p){var t=new GameObject(n).transform;t.SetParent(parent,false);t.position=p;return t;}
 static GameObject B(string n,Vector2 p,Vector2 size,int color,int order,Transform parent,bool solid=false){var t=G(n,parent,p);t.localScale=new Vector3(size.x,size.y,1);var r=t.gameObject.AddComponent<SpriteRenderer>();r.sprite=sprite;r.sharedMaterial=material;r.color=RecoilPrototype.Hex(color);r.sortingOrder=order;if(solid)t.gameObject.AddComponent<BoxCollider2D>();return t.gameObject;}
 static void Text(string text,Vector2 p,Transform parent){var t=G(text,parent,p);var m=t.gameObject.AddComponent<TextMesh>();m.text=text;m.fontSize=48;m.characterSize=.065f;m.anchor=TextAnchor.MiddleCenter;m.color=new Color(.58f,.65f,.62f);m.GetComponent<MeshRenderer>().sortingOrder=5;}
 static void Set(UnityEngine.Object obj,string key,UnityEngine.Object value){var s=new SerializedObject(obj);s.FindProperty(key).objectReferenceValue=value;s.ApplyModifiedPropertiesWithoutUndo();}
 static void DoorVisual(Transform door){B("Door frame",door.position,new Vector2(1.35f,2.65f),0x697778,0,door);B("Door leaf",door.position,new Vector2(1.12f,2.42f),0x3d5350,1,door);B("Push bar",(Vector2)door.position+new Vector2(0,-.1f),new Vector2(.8f,.07f),0xaba582,2,door);}
 static void Light(Transform parent,Vector2 position){var fixture=G("Weak stairwell light",parent,position);B("Fixture",position,new Vector2(.7f,.1f),0x535f60,-2,fixture);var t=G("Light - fixture child",fixture,position);var light=t.gameObject.AddComponent<RecoilLight2D>();light.radius=3.5f;light.intensity=.26f;}
 static WalkableStairway Stair(Transform parent,string name,Vector2 low,Vector2 turn,Vector2 high){var root=G(name,parent,low);var s=root.gameObject.AddComponent<WalkableStairway>();s.routePoints=new[]{G("01 Lower entrance - feet",root,low),G("02 Return landing - feet",root,turn),G("03 Upper entrance - feet",root,high)};s.travelSpeed=3.6f;Vector2[] points={low,turn,high};for(int i=1;i<3;i++){var a=points[i-1];var b=points[i];int count=Mathf.CeilToInt((b.y-a.y)/.19f);for(int j=0;j<count;j++){var p=Vector2.Lerp(a,b,(j+.5f)/count);B("Steel tread",p-Vector2.up*.06f,new Vector2(Mathf.Abs(b.x-a.x)/count+.08f,.08f),0x596c70,1,root);}var rail=B("Handrail",(a+b)*.5f+Vector2.up*.9f,new Vector2(Vector2.Distance(a,b),.055f),0x96a7a4,2,root);rail.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg);for(int j=0;j<5;j++){var p=Vector2.Lerp(a,b,j/4f);B("Rail post",p+Vector2.up*.45f,new Vector2(.045f,.9f),0x788e8b,2,root);}}B("Half landing",turn-Vector2.up*.09f,new Vector2(1.25f,.18f),0x657675,1,root,true);Text("W / UP   S / DOWN",low+new Vector2(0,1.8f),root);return s;}
 static Scene Scene(string path){var s=SceneManager.GetSceneByPath(path);return s.isLoaded?s:EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);}
 static RecoilPrototype World(Scene s){return s.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<RecoilPrototype>()).First();}
 static void Backup(Scene scene){Directory.CreateDirectory("Backups/FourStoreyFactory");var path="Backups/FourStoreyFactory/"+scene.name+"_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+".unity";EditorSceneManager.SaveScene(scene,path,true);}
 [MenuItem("Recoil/Levels/Build Four Storey Factory")]
 public static void Build(){
  if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play first.");var original=SceneManager.GetActiveScene();var previouslyLoaded=Enumerable.Range(0,SceneManager.sceneCount).Select(i=>SceneManager.GetSceneAt(i).path).ToArray();
  var outside=Scene(Outdoors);var inside=Scene(Indoors);var ow=World(outside);var iw=World(inside);
  if(ow.GetComponentsInChildren<Transform>(true).Any(t=>t.name=="Factory Building - four storeys"))throw new InvalidOperationException("Factory already built. Edit saved objects instead of regenerating.");
  Backup(outside);Backup(inside);var so=new SerializedObject(ow);sprite=(Sprite)so.FindProperty("sceneSprite").objectReferenceValue;material=(Material)so.FindProperty("sceneMaterial").objectReferenceValue;
  SceneManager.SetActiveScene(outside);var entry=ow.GetComponentsInChildren<RecoilDoor>(true).First(d=>d.destinationScene==Indoors);float delta=6;entry.transform.position+=Vector3.right*delta;float left=entry.transform.position.x-1.3f,right=left+32;
  var building=G("Factory Building - four storeys",ow.transform,new Vector2(left,0));entry.transform.SetParent(building,true);
  foreach(var t in ow.GetComponentsInChildren<Transform>(true)){if(t.name=="Factory interior"||t.name=="Factory column"){var sr=t.GetComponent<SpriteRenderer>();if(sr)sr.enabled=false;}if(t.name=="Factory Return Spawn" || t.name=="Truck - soft canvas roof" || (t.name=="Factory High Platform" && t.position.x>30))t.position+=Vector3.right*delta;if(t.name=="Factory Return Spawn")t.SetParent(building,true);if(t.name=="Exit Lamp"&&!t.IsChildOf(entry.transform))t.position+=Vector3.right*delta;}
  var entrance=(Transform)so.FindProperty("factoryEntrance").objectReferenceValue;if(entrance){entrance.position=new Vector2(left,0);entrance.SetParent(building,true);}
  B("Factory main facade",new Vector2((left+right)/2,15.2f),new Vector2(32,30.4f),0x29343a,-9,building);B("Left side elevation",new Vector2(left-.6f,15.2f),new Vector2(1.2f,30.4f),0x1b252c,-8,building);
  for(int floor=0;floor<4;floor++){float y=floor*Storey;B("Floor cornice "+(floor+1),new Vector2((left+right)/2,y+.2f),new Vector2(32,.24f),0x566367,-7,building);for(int bay=0;bay<8;bay++){float x=left+3+bay*3.6f;B("Factory window",new Vector2(x,y+3.8f),new Vector2(2.35f,2.3f),0x121e26,-7,building);B("Window mullion",new Vector2(x,y+3.8f),new Vector2(.06f,2.3f),0x6c7777,-6,building);B("Window crossbar",new Vector2(x,y+3.8f),new Vector2(2.35f,.07f),0x6c7777,-6,building);}Text("FLOOR "+(floor+1),new Vector2(left+1.4f,y+6.3f),building);}
  var roof=B("Factory parapet",new Vector2((left+right)/2,30.6f),new Vector2(32.6f,.4f),0x667171,-5,building,true);roof.AddComponent<SoftRoofPlatform>();roof.GetComponent<BoxCollider2D>().isTrigger=true;
  var ext=G("Third floor side fire exit - locked outside",building,new Vector2(left-.6f,Storey*2+1.25f));DoorVisual(ext);var outer=ext.gameObject.AddComponent<RecoilDoor>();outer.requiresClue=false;outer.completesLevel=false;outer.requiredItemId=Shortcut;outer.lockedMessage="The door is locked from the other side.";outer.interactionPrompt="E / FIRE EXIT";outer.destinationScene=Indoors;outer.destinationSpawn="Third Floor Shortcut Spawn";
  float accessX=left-1.15f;var access=G("Exterior fire escape - W up S down",building,new Vector2(accessX,0));for(int i=0;i<2;i++){float y=i*Storey;Stair(access,"Fire escape flight "+(i+1),new Vector2(accessX,y),new Vector2(accessX-5.8f,y+Storey*.5f),new Vector2(accessX,y+Storey));B("Storey landing",new Vector2(accessX, y+Storey-.1f),new Vector2(2.4f,.2f),0x627772,1,access,true);}G("Third Floor Fire Escape Spawn",building,new Vector2(accessX,Storey*2+.845f));
  var walls=ow.GetComponentsInChildren<BoxCollider2D>(true).Where(c=>!c.isTrigger&&c.bounds.size.x<1&&c.bounds.size.y>10).OrderBy(c=>c.bounds.center.x).ToArray();if(walls.Length>1)walls.Last().transform.position=new Vector2(right+6,walls.Last().transform.position.y);
  var outsideCamera=outside.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<RecoilCameraController>(true)).FirstOrDefault();if(outsideCamera){outsideCamera.maximumY=30;}
  EditorSceneManager.MarkSceneDirty(outside);EditorSceneManager.SaveScene(outside);
  SceneManager.SetActiveScene(inside);var newLevels=G("Four-storey interior additions",iw.transform,Vector2.zero);
  var ceiling=iw.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Low Factory Ceiling");ceiling.position=new Vector2(17.5f,30.7f);ceiling.localScale=new Vector3(36,.6f,1);
  foreach(string n in new[]{"Left Factory Wall","Right Factory Wall"}){var t=iw.GetComponentsInChildren<Transform>(true).First(x=>x.name==n);t.position=new Vector2(n.StartsWith("Left")?-.25f:35.25f,15.2f);t.localScale=new Vector3(.5f,30.4f,1);}
  var ground=iw.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Factory Floor");ground.position=new Vector2(17.5f,-.5f);ground.localScale=new Vector3(36,1,1);
  var backdrop=iw.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Factory interior");backdrop.position=new Vector2(17.5f,15.2f);backdrop.localScale=new Vector3(35,30.4f,1);
  var area=iw.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Factory Dark Area");area.position=new Vector2(17.5f,15.2f);area.localScale=new Vector3(35,30.4f,1);
  for(int i=1;i<4;i++){float y=i*Storey;B("Floor "+(i+1)+" - solid slab",new Vector2(14,y-.15f),new Vector2(28,.3f),0x4b5e60,0,newLevels,true);for(int x=3;x<28;x+=5){B("Upper wall seam",new Vector2(x,y+3.7f),new Vector2(.06f,6.8f),0x29383e,-8,newLevels);B("Upper floor conduit",new Vector2(x,y+6.7f),new Vector2(4.9f,.12f),0x4b5658,-7,newLevels);}Text("FLOOR "+(i+1),new Vector2(25,y+5.8f),newLevels);Light(newLevels,new Vector2(8,y+6.6f));Light(newLevels,new Vector2(27,y+6.6f));}
  var stairs=G("Right Stairwell - continuous switchbacks",newLevels,new Vector2(28,0));for(int i=0;i<3;i++)Stair(stairs,"Floor "+(i+1)+" to "+(i+2),new Vector2(28,i*Storey),new Vector2(34,i*Storey+Storey*.5f),new Vector2(28,(i+1)*Storey));
  var inner=G("Third floor left fire exit - unlock shortcut",newLevels,new Vector2(1.1f,Storey*2+1.25f));DoorVisual(inner);var door=inner.gameObject.AddComponent<RecoilDoor>();door.requiresClue=false;door.completesLevel=false;door.openStateItemId=Shortcut;door.firstOpenMessage="Fire exit unlocked. This route now connects to the exterior stairs.";door.openPrompt="E / UNLOCK FIRE EXIT";door.travelPrompt="E / EXIT TO FIRE ESCAPE";door.destinationScene=Outdoors;door.destinationSpawn="Third Floor Fire Escape Spawn";G("Third Floor Shortcut Spawn",newLevels,new Vector2(2.4f,Storey*2+.845f));
  var camera=inside.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<RecoilCameraController>(true)).FirstOrDefault();if(camera){camera.maximumY=27.2f;camera.minimumY=3;camera.followOffset=new Vector2(camera.followOffset.x,1.8f);camera.preserveFootFraming=true;camera.minimumFootScreenMargin=.1f;}
  var sw=iw.GetComponentsInChildren<SoundWaveManager>(true).FirstOrDefault();if(sw){var serial=new SerializedObject(sw);var prop=serial.FindProperty("controlArea");if(prop!=null)prop.objectReferenceValue=area;serial.ApplyModifiedPropertiesWithoutUndo();}
  EditorSceneManager.MarkSceneDirty(inside);EditorSceneManager.SaveScene(inside);AssetDatabase.SaveAssets();
  foreach(var path in new[]{Outdoors,Indoors}){var s=SceneManager.GetSceneByPath(path);if(s.isLoaded&&!previouslyLoaded.Contains(path))EditorSceneManager.CloseScene(s,true);}SceneManager.SetActiveScene(original);Debug.Log("FOUR_STOREY_FACTORY_COMPLETE: 4 floors, 5 stairways, exterior door shifted +6m, third-floor fire exit shortcut.");
 }
 public static void CaptureLayout(){
  Directory.CreateDirectory("output/level-previews");
  foreach(var path in new[]{Outdoors,Indoors}){
   var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);
   var roots=scene.GetRootGameObjects();var camera=roots.SelectMany(r=>r.GetComponentsInChildren<Camera>()).First();
   foreach(var controller in roots.SelectMany(r=>r.GetComponentsInChildren<RecoilCameraController>()))controller.enabled=false;
   foreach(var light in roots.SelectMany(r=>r.GetComponentsInChildren<RecoilLighting2D>()))light.enabled=false;
   foreach(var shadow in roots.SelectMany(r=>r.GetComponentsInChildren<RecoilSightOcclusion>()))shadow.enabled=false;
   foreach(var r in roots.SelectMany(g=>g.GetComponentsInChildren<SpriteRenderer>()))if(r.name=="Darkness Layer")r.enabled=false;
   camera.orthographic=true;camera.orthographicSize=14;camera.transform.rotation=Quaternion.identity;camera.transform.position=new Vector3(path==Outdoors?51.5f:17.5f,9.8f,-10);
   var rt=new RenderTexture(1600,1000,24);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;var image=new Texture2D(1600,1000,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1600,1000),0,0);image.Apply();File.WriteAllBytes("output/level-previews/"+(path==Outdoors?"factory-exterior":"factory-interior")+"-layout.png",image.EncodeToPNG());RenderTexture.active=null;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(rt);
  }
  Debug.Log("FOUR_STOREY_LAYOUT_CAPTURE_COMPLETE (layout-only, darkness hidden, not saved)");
 }

 public static void FinaliseHierarchy(){
  var outside=EditorSceneManager.OpenScene(Outdoors,OpenSceneMode.Single);var world=World(outside);var building=world.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Factory Building - four storeys");var spawn=world.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Factory Return Spawn");spawn.SetParent(building,true);var entrance=world.GetComponentsInChildren<RecoilDoor>(true).First(d=>d.destinationScene==Indoors && d.requiredItemId=="employee_access_card");foreach(var lamp in world.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Exit Lamp"))if(!lamp.IsChildOf(entrance.transform))lamp.SetParent(entrance.transform,true);EditorSceneManager.MarkSceneDirty(outside);EditorSceneManager.SaveScene(outside);
  var inside=EditorSceneManager.OpenScene(Indoors,OpenSceneMode.Single);var stairs=World(inside).GetComponentsInChildren<WalkableStairway>().OrderBy(s=>s.routePoints[0].position.y).First();PrefabUtility.SaveAsPrefabAsset(stairs.gameObject,"Assets/RecoilPrototype/Prefabs/WalkableSwitchbackStairway.prefab");AssetDatabase.SaveAssets();Debug.Log("FOUR_STOREY_HIERARCHY_COMPLETE");
 }

}
