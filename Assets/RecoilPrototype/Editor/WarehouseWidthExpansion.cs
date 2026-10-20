using System;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

// One-time authored layout operation. Atomic props translate; only structural spans stretch.
public static class WarehouseWidthExpansion
{
 const float Factor=1.5f;
 const string Marker="Warehouse width x1.5 - applied 2026-10-16";
 static Scene Open(string path){var s=SceneManager.GetSceneByPath(path);return s.isLoaded?s:EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);}
 static RecoilPrototype World(Scene s){return s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<RecoilPrototype>(true)).First();}
 static Transform Find(Transform root,string name){return root.GetComponentsInChildren<Transform>(true).First(t=>t.name==name);}
 static void Map(Transform t,float anchor=0){var p=t.position;p.x=anchor+(p.x-anchor)*Factor;t.position=p;}
 static void Shift(Transform t,float amount){t.position+=Vector3.right*amount;}
 static void ResizeSpan(Transform t,float center,float width){
  var renderer=t.GetComponent<SpriteRenderer>();float oldWidth=renderer?renderer.sprite.bounds.size.x*Mathf.Abs(t.lossyScale.x):Mathf.Abs(t.lossyScale.x);float ratio=width/oldWidth;
  t.position=new Vector3(center,t.position.y,t.position.z);var scale=t.localScale;scale.x*=ratio;t.localScale=scale;
  var surface=t.Find("Surface - unified industrial material");if(surface){scale=surface.localScale;scale.x/=ratio;surface.localScale=scale;var sr=surface.GetComponent<SpriteRenderer>();if(sr && sr.drawMode==SpriteDrawMode.Tiled){var size=sr.size;size.x*=ratio;sr.size=size;}}
 }
 static void SpreadChildren(Transform parent){foreach(var child in parent.Cast<Transform>().ToArray())Map(child);}
 static void ExpandInterior(RecoilPrototype w){
  var root=w.transform;var architecture=Find(root,"Architecture - editable");var right=Find(architecture,"Right hard wall").GetComponent<BoxCollider2D>();float rightInner=right.bounds.min.x,newRight=rightInner*Factor;
  foreach(var t in architecture.Cast<Transform>().ToArray()){
   var sr=t.GetComponent<SpriteRenderer>();if(t.name=="Floor"||t.name=="Back wall")ResizeSpan(t,t.position.x*Factor,sr.bounds.size.x*Factor);
   else if(t.name=="Right hard wall")t.position=new Vector3(newRight+right.bounds.size.x*.5f,t.position.y,t.position.z);
   else if(t.name=="Ceiling"){float left=Mathf.Min(sr.bounds.min.x*Factor,-.4f),end=Mathf.Max(sr.bounds.max.x*Factor,newRight+.4f);ResizeSpan(t,(left+end)*.5f,end-left);}
  }
  var props=Find(root,"Warehouse freight - editable");var body=Find(props,"Pallet truck body");var handle=Find(props,"Pallet truck handle");
  var truck=new GameObject("Pallet truck - grouped for layout").transform;truck.SetParent(props,false);truck.position=new Vector2(body.position.x,0);body.SetParent(truck,true);handle.SetParent(truck,true);
  SpreadChildren(props);
  var lightRoot=Find(root,"05 Lighting");foreach(var t in lightRoot.Cast<Transform>().ToArray())if(t.name!="Darkness Layer" && !t.name.Contains("shadow mesh"))Map(t);
  Map(Find(root,"Locked storeroom entrance - background wall"));
  var encounter=Find(root,"Warehouse escort encounter - editable");foreach(var enemy in encounter.GetComponentsInChildren<RampagerController>(true)){
   var points=enemy.GetComponentsInChildren<RampagerPatrolPoint>(true);var positions=points.Select(p=>p.transform.position).ToArray();Map(enemy.transform);
   for(int i=0;i<points.Length;i++){var p=positions[i];p.x*=Factor;points[i].transform.position=p;}enemy.leftOffset*=Factor;enemy.rightOffset*=Factor;
  }
  Map(Find(root,"Entrance Spawn"));Map(w.Player.transform);w.Player.Position=w.Player.transform.position;
  var area=Find(root,"Interior Dark Area");ResizeSpan(area,newRight*.5f,newRight);
  var crane=w.GetComponentInChildren<KBKStackerCrane>();
  foreach(var t in crane.transform.Cast<Transform>().ToArray()){
   if(t.name=="Freight details - fixed"){UnityEngine.Object.DestroyImmediate(t.gameObject);continue;}
   if(t.name=="KBK suspension rail"){var sr=t.GetComponent<SpriteRenderer>();ResizeSpan(t,t.position.x*Factor,sr.bounds.size.x*Factor);}
   else Map(t);
  }
  var serial=new SerializedObject(w);FactoryWarehouseBuilder.UseArt((Sprite)serial.FindProperty("sceneSprite").objectReferenceValue,(Material)serial.FindProperty("sceneMaterial").objectReferenceValue);
  var detail=FactoryWarehouseBuilder.G("Freight details - fixed",crane.transform,Vector2.zero);
  foreach(var rail in crane.GetComponentsInChildren<SpriteRenderer>(true).Where(r=>r.name=="KBK suspension rail").ToArray()){
   var bounds=rail.bounds;FactoryWarehouseBuilder.B("Bright rail flange",detail,new Vector2(bounds.center.x,bounds.max.y),new Vector2(bounds.size.x,.035f),0xf2d08a,1);
   for(float x=bounds.min.x+.25f;x<bounds.max.x;x+=1.5f){FactoryWarehouseBuilder.B("Rail fixing plate",detail,new Vector2(x,bounds.center.y),new Vector2(.12f,.22f),0x8faaa2,2);FactoryWarehouseBuilder.B("Rail bolt",detail,new Vector2(x,bounds.center.y),new Vector2(.035f,.035f),0xe3e1bc,3);}
  }
 }
 static void ExpandExterior(RecoilPrototype w){
  var root=w.transform;var warehouse=Find(root,"Factory Warehouse - exterior");var wall=Find(warehouse,"Warehouse wall").GetComponent<SpriteRenderer>();float anchor=wall.bounds.min.x,oldRight=wall.bounds.max.x,extra=wall.bounds.size.x*(Factor-1);
  foreach(var t in warehouse.Cast<Transform>().ToArray()){
   if(t.name=="Warehouse wall" || t.name=="Roof parapet"){var sr=t.GetComponent<SpriteRenderer>();ResizeSpan(t,anchor+(t.position.x-anchor)*Factor,sr.bounds.size.x+extra);}
   else Map(t,anchor);
  }
  // Move yard assemblies as intact groups, so drains and return markers stay aligned.
  var yard=warehouse.parent;
  foreach(var t in yard.Cast<Transform>().ToArray())if(t!=warehouse && t.position.x>=oldRight-.001f)Shift(t,extra);
  var ground=w.GetComponentsInChildren<BoxCollider2D>(true).Where(c=>c.name=="Solid terrain"&&!c.isTrigger).OrderByDescending(c=>c.bounds.size.x).First();var bounds=ground.bounds;ResizeSpan(ground.transform,bounds.center.x+extra*.5f,bounds.size.x+extra);
  foreach(var c in w.GetComponentsInChildren<BoxCollider2D>(true).Where(c=>c.name=="Solid terrain"&&c.bounds.size.x<1&&c.bounds.center.x>oldRight).ToArray())Shift(c.transform,extra);
 }
 [MenuItem("Recoil/Levels/Expand Warehouse Width 1.5x")]
 public static void Apply(){
  if(EditorApplication.isPlaying)throw new Exception("Exit Play first.");var active=SceneManager.GetActiveScene();var loaded=Enumerable.Range(0,SceneManager.sceneCount).Select(i=>SceneManager.GetSceneAt(i).path).ToArray();var inside=Open("Assets/Scenes/RecoilFactoryWarehouse.unity");var outside=Open("Assets/Scenes/RecoilPrototype.unity");var iw=World(inside);var ow=World(outside);
  if(iw.transform.Find(Marker)||ow.transform.Find(Marker))throw new Exception("Width expansion already applied; edit the authored scene instead.");
  string backup="Backups/WarehouseWidthExpansion/"+DateTime.Now.ToString("yyyyMMdd_HHmmss");Directory.CreateDirectory(backup);EditorSceneManager.SaveScene(inside,backup+"/Warehouse.unity",true);EditorSceneManager.SaveScene(outside,backup+"/Outdoor.unity",true);
  var racks=iw.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Steel rack - pallets and freight").ToArray();var rackSizes=racks.Select(t=>t.lossyScale).ToArray();var fork=iw.GetComponentInChildren<KBKStackerCrane>();float forkWidth=fork.forkPlatform.Area.width;
  ExpandInterior(iw);ExpandExterior(ow);Physics2D.SyncTransforms();
  for(int i=0;i<racks.Length;i++)if(racks[i].lossyScale!=rackSizes[i])throw new Exception("Rack scale changed.");if(Mathf.Abs(fork.forkPlatform.Area.width-forkWidth)>.001f)throw new Exception("Fork width changed.");
  foreach(var world in new[]{iw,ow}){var marker=new GameObject(Marker);marker.transform.SetParent(world.transform,false);}
  EditorSceneManager.MarkSceneDirty(inside);EditorSceneManager.MarkSceneDirty(outside);EditorSceneManager.SaveScene(inside);EditorSceneManager.SaveScene(outside);AssetDatabase.SaveAssets();SceneManager.SetActiveScene(active);foreach(var s in new[]{inside,outside})if(!loaded.Contains(s.path))EditorSceneManager.CloseScene(s,true);
  Debug.Log("WAREHOUSE_WIDTH_EXPANSION_COMPLETE / rack and fork dimensions retained");
 }
}
