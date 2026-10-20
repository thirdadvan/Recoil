using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class FactoryLeftFireEscapeBuilder
{
 const string Outside="Assets/Scenes/RecoilPrototype.unity",Inside="Assets/Scenes/RecoilFactoryInterior.unity";
 static RecoilPrototype W(Scene s){return s.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<RecoilPrototype>(true)).First();}
 static Scene Open(string path){var s=SceneManager.GetSceneByPath(path);return s.isLoaded?s:EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);}
 static void Art(RecoilPrototype w){var so=new SerializedObject(w);var s=(Sprite)so.FindProperty("sceneSprite").objectReferenceValue;var m=(Material)so.FindProperty("sceneMaterial").objectReferenceValue;FactoryWarehouseBuilder.UseArt(s,m);FactoryOfficeRefitBuilder.UseArt(s,m);}
 static Transform G(string name,Transform parent,Vector2 pos){return FactoryWarehouseBuilder.G(name,parent,pos);}
 static GameObject B(string name,Transform parent,Vector2 pos,Vector2 size,int color,int order=1,bool solid=false){return FactoryWarehouseBuilder.B(name,parent,pos,size,color,order,solid);}
 static WalkableStairway Flight(Transform root,string name,float x,float low,float high,bool exterior)
 {
  var t=G(name,root,new Vector2(x,low));var stair=t.gameObject.AddComponent<WalkableStairway>();stair.routePoints=new[]{G("01 Lower entrance - feet",t,new Vector2(x,low)),G("02 Return landing - feet",t,new Vector2(x+(exterior?-5.8f:5.8f),(low+high)*.5f)),G("03 Upper entrance - feet",t,new Vector2(x,high))};stair.catchRadius=.55f;FactoryOfficeRefitBuilder.RebuildStair(stair,low,high,exterior);return stair;
 }
 static RecoilDoor Door(Transform root,string name,float x,float floor,string scene,string spawn,bool damaged)
 {
  var d=FactoryWarehouseBuilder.Door(root,name,new Vector2(x,floor+1.25f),scene,spawn);
  var art=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/RecoilPrototype/Art/FactoryRefit/SteelFireExit.png");
  if(art)
  {
   foreach(var r in d.GetComponentsInChildren<SpriteRenderer>().ToArray())UnityEngine.Object.DestroyImmediate(r.gameObject);
   var t=G(damaged?"Severely warped steel leaf":"Steel fire door",d.transform,new Vector2(x,floor+1.35f));var sr=t.gameObject.AddComponent<SpriteRenderer>();sr.sprite=art;sr.sortingOrder=3;sr.color=RecoilPrototype.Hex(damaged?0x6b6258:0x9da296);t.localScale=new Vector3(1.55f/art.bounds.size.x,2.7f/art.bounds.size.y,1);if(damaged)t.rotation=Quaternion.Euler(0,0,-9);
  }
  if(damaged){d.permanentlyBlocked=true;d.irreparableDamage=true;d.blockedMessage="The door is severely warped. It cannot be used.";B("Bent push bar",d.transform,new Vector2(x,floor+1.2f),new Vector2(1.05f,.09f),0x827763,5).transform.rotation=Quaternion.Euler(0,0,17);B("Broken hinge",d.transform,new Vector2(x+.6f,floor+2.4f),new Vector2(.22f,.22f),0x3a302b,5);}
  return d;
 }
 static void Landing(Transform root,float x,float doorX,float floor,bool broken)
 {
  float min=Mathf.Min(x,doorX)-.55f,max=Mathf.Max(x,doorX)+.75f;
  var t=G(broken?"Floor 4 landing - destroyed, no support":"Fire exit landing",root,new Vector2((min+max)*.5f,floor));
  if(!broken){var deck=B("Steel landing deck",t,new Vector2((min+max)*.5f,floor-.1f),new Vector2(max-min,.2f),0x68716b,1,true);if(root.GetComponentsInParent<Transform>(true).Any(p=>p.name=="Exterior fire escape - W up S down")){deck.AddComponent<SoftRoofPlatform>();deck.GetComponent<BoxCollider2D>().isTrigger=true;}B("Landing safety rail",t,new Vector2((min+max)*.5f,floor+.92f),new Vector2(max-min,.07f),0x69786e,2);}
  else{for(int i=0;i<5;i++){var fragment=B("Hanging broken grating - visual only",t,new Vector2(min+.25f+i*(max-min-.5f)/4,floor-.25f-(i%2)*.18f),new Vector2(.42f,.08f),0x605747,2);fragment.transform.rotation=Quaternion.Euler(0,0,-35-i*11);}B("Bent railing - visual only",t,new Vector2(doorX-.5f,floor+.55f),new Vector2(.06f,1.3f),0x655e4e,2).transform.rotation=Quaternion.Euler(0,0,32);}
 }
 static void Clutter(Transform root,float x,float floor)
 {
  var t=G("Floor 2 entrance - impassable piled freight",root,new Vector2(x,floor));
  var block=t.gameObject.AddComponent<BoxCollider2D>();block.size=new Vector2(3.2f,3.3f);block.offset=Vector2.up*1.65f;
  if(root.GetComponentsInParent<Transform>(true).Any(p=>p.name=="Exterior fire escape - W up S down")){FurnishExteriorClutter(t,x,floor);return;}
  for(int row=0;row<3;row++)for(int col=0;col<4;col++){Vector2 p=new Vector2(x-1.2f+col*.8f,floor+.53f+row*1.07f);B("Stacked crate",t,p,new Vector2(.78f,1.02f),row%2==0?0x71634e:0x5f6557,6);B("Crate strap",t,p,new Vector2(.07f,1.02f),0x353d35,7);}
  var cabinet=B("Discarded steel cabinet",t,new Vector2(x-.94f,floor+1.25f),new Vector2(.62f,2.5f),0x4a554d,8);cabinet.transform.rotation=Quaternion.Euler(0,0,8);
 }
 static void FurnishExteriorClutter(Transform root,float x,float floor)
 {
  var visibility=root.GetComponent<RecoilSurfaceVisibility>();if(!visibility)visibility=root.gameObject.AddComponent<RecoilSurfaceVisibility>();visibility.blocksSight=false;
  // No opaque backing sheet: each chair/cabinet remains a separate editable silhouette.
  for(int c=0;c<2;c++){
   float cx=x+(c==0?.8f:-.32f),height=c==0?2.25f:1.75f;
   var cabinet=G(c==0?"Tall filing cabinet":"Overturned filing cabinet",root,new Vector2(cx,floor+height*.5f));
   B("Steel cabinet body",cabinet,new Vector2(cx,floor+height*.5f),new Vector2(.74f,height),c==0?0x65716b:0x545f5b,6);
   B("Cabinet top rim",cabinet,new Vector2(cx,floor+height),new Vector2(.8f,.07f),0x8c9281,7);
   for(int row=0;row<4;row++){float y=floor+(row+.5f)*height/4;B("Drawer face",cabinet,new Vector2(cx,y),new Vector2(.63f,height/4-.055f),c==0?0x738076:0x606d62,7);B("Recessed drawer handle",cabinet,new Vector2(cx,y+.05f),new Vector2(.23f,.045f),0x283c37,8);B("Old paper label",cabinet,new Vector2(cx,y-.065f),new Vector2(.15f,.045f),0xaaa489,8);}
   cabinet.rotation=Quaternion.Euler(0,0,c==0?-7:18);
  }
  Chair(root,x-1.03f,floor+.05f,0,"Discarded chair - ground");
  Chair(root,x+.1f,floor+2.13f,-27,"Chair wedged above cabinets");
  Chair(root,x-.9f,floor+1.15f,22,"Tilted chair between furniture");
 }
 static void Chair(Transform root,float x,float y,float angle,string name)
 {
  var t=G(name,root,new Vector2(x,y+.5f));
  B("Wooden seat",t,new Vector2(x,y+.47f),new Vector2(.67f,.09f),0x8b7960,9);
  for(int side=-1;side<=1;side+=2){B("Chair leg",t,new Vector2(x+side*.25f,y+.235f),new Vector2(.055f,.47f),0x5c6256,8);B("Back upright",t,new Vector2(x+side*.25f,y+.8f),new Vector2(.05f,.65f),0x657061,8);}
  B("Chair back - top slat",t,new Vector2(x,y+1.1f),new Vector2(.58f,.12f),0x968467,9);
  B("Chair back - middle slat",t,new Vector2(x,y+.91f),new Vector2(.56f,.09f),0x7e765f,9);
  t.rotation=Quaternion.Euler(0,0,angle);
 }
 public static string RefitExteriorEntranceClutter()
 {
  if(EditorApplication.isPlaying)throw new Exception("Exit Play before editing.");var original=SceneManager.GetActiveScene();bool added=!SceneManager.GetSceneByPath(Outside).isLoaded;var scene=Open(Outside);var w=W(scene);Art(w);
  var root=w.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Floor 2 entrance - impassable piled freight");
  string backup="Backups/ExteriorOfficeClutter/"+DateTime.Now.ToString("yyyyMMdd_HHmmss");Directory.CreateDirectory(backup);EditorSceneManager.SaveScene(scene,backup+"/Outdoor.unity",true);
  foreach(var child in root.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);
  FurnishExteriorClutter(root,root.position.x,root.position.y);
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
  var block=root.GetComponent<BoxCollider2D>();string result="Furniture groups="+root.childCount+"; solid collision="+(!block.isTrigger)+"; blocks sight="+RecoilSightOcclusion.IsBlocker(block);
  SceneManager.SetActiveScene(original);if(added)EditorSceneManager.CloseScene(scene,true);return result;
 }
 [MenuItem("Recoil/Levels/Complete Left Fire Escapes")]
 public static string Build()
 {
  if(EditorApplication.isPlaying)throw new Exception("Exit Play first.");var original=SceneManager.GetActiveScene();var outside=Open(Outside);var inside=Open(Inside);var ow=W(outside);var iw=W(inside);
  if(iw.transform.Find("Left fire escape - four floors editable"))return "Already built; edit existing objects.";
  string backup="Backups/FactoryLeftFireEscape/"+DateTime.Now.ToString("yyyyMMdd_HHmmss");Directory.CreateDirectory(backup);EditorSceneManager.SaveScene(outside,backup+"/Outdoor.unity",true);EditorSceneManager.SaveScene(inside,backup+"/Interior.unity",true);
  float[] floors={0,0,0,0};for(int f=1;f<4;f++)floors[f]=iw.GetComponentsInChildren<BoxCollider2D>().First(c=>c.name=="Floor "+(f+1)+" - solid slab").bounds.max.y;
  var ext3=ow.GetComponentsInChildren<RecoilDoor>().First(d=>d.requiredItemId=="factory_shortcut_floor3");float ox=ext3.transform.position.x-2.5f;float doorX=ext3.transform.position.x;
  var exterior=ow.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Exterior fire escape - W up S down");Art(ow);SceneManager.SetActiveScene(outside);
  var existing=exterior.GetComponentsInChildren<WalkableStairway>().OrderBy(s=>s.routePoints[0].position.y).ToArray();
  for(int i=0;i<existing.Length;i++){existing[i].transform.position=new Vector2(ox,floors[i]);existing[i].routePoints[0].position=new Vector2(ox,floors[i]);FactoryOfficeRefitBuilder.RebuildStair(existing[i],floors[i],floors[i+1],true);}
  var upper=Flight(exterior,"Fire escape flight 3 - damaged upper landing",ox,floors[2],floors[3],true);upper.upperLandingAvailable=false;
  for(int f=0;f<4;f++)
  {
   var level=G("Floor "+(f+1)+" exit and landing",exterior,new Vector2(ox,floors[f]));Landing(level,ox,doorX,floors[f],f==3);
   if(f==2)ext3.transform.SetParent(level,true);
   else if(f>0){var d=Door(level,"Floor "+(f+1)+" side fire exit",doorX,floors[f],Inside,"Left Fire Escape Floor "+(f+1)+" Spawn",f==3);if(f==1){d.requiredItemId="factory_shortcut_floor2";Clutter(level,doorX,floors[f]);}}
   if(f>0)G("Left Fire Escape Floor "+(f+1)+" Outside Spawn",level,new Vector2(ox+.45f,floors[f]+.845f));
  }
  Art(iw);SceneManager.SetActiveScene(inside);var innerRoot=G("Left fire escape - four floors editable",iw.transform,Vector2.zero);float ix=4,innerDoorX=1.1f;
  // Interior uses the existing right stairwell; left fire exits do not add another staircase.
  var inner3=iw.GetComponentsInChildren<RecoilDoor>().First(d=>d.openStateItemId=="factory_shortcut_floor3");
  for(int f=0;f<4;f++)
  {
   var level=G("Floor "+(f+1)+" fire exit and landing",innerRoot,new Vector2(innerDoorX,floors[f]));Landing(level,ix,innerDoorX,floors[f],f==3);
   if(f==2)inner3.transform.SetParent(level,true);
   else if(f>0){var d=Door(level,"Floor "+(f+1)+" left fire exit",innerDoorX,floors[f],Outside,"Left Fire Escape Floor "+(f+1)+" Outside Spawn",f==3);if(f==1){d.openStateItemId="factory_shortcut_floor2";d.firstOpenMessage="Fire exit unlocked.";Clutter(level,innerDoorX,floors[f]);}}
   if(f>0)G("Left Fire Escape Floor "+(f+1)+" Spawn",level,new Vector2(f==3?6:3.2f,floors[f]+.845f));
  }
  var slab=iw.GetComponentsInChildren<BoxCollider2D>().First(c=>c.name=="Floor 4 - solid slab");float right=slab.bounds.max.x,min=ix+1.3f;slab.transform.position=new Vector3((min+right)*.5f,slab.transform.position.y,slab.transform.position.z);slab.transform.localScale=new Vector3((right-min)/slab.size.x,slab.transform.localScale.y,slab.transform.localScale.z);
  foreach(var trim in iw.GetComponentsInChildren<SpriteRenderer>(true).Where(r=>r.name=="Corridor skirting"&&Mathf.Abs(r.bounds.center.y-floors[3])<.25f))
  {float edge=trim.bounds.max.x;trim.transform.position=new Vector3((min+edge)*.5f,trim.transform.position.y,trim.transform.position.z);trim.transform.localScale=new Vector3((edge-min)/trim.sprite.bounds.size.x,trim.transform.localScale.y,trim.transform.localScale.z);}
  Physics2D.SyncTransforms();
  B("Torn floor edge - visual only",innerRoot,new Vector2(min, floors[3]-.12f),new Vector2(.12f,.35f),0x806d51,2);
  EditorSceneManager.MarkSceneDirty(outside);EditorSceneManager.SaveScene(outside);EditorSceneManager.MarkSceneDirty(inside);EditorSceneManager.SaveScene(inside);AssetDatabase.SaveAssets();SceneManager.SetActiveScene(original);
  return "Left escapes: 3 upper-floor fire exits per scene, 3 exterior flights; floor 2 blocked, floor 4 landing absent and door warped; floor 3 shortcut retained.";
 }
}
