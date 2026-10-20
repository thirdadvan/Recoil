using System;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class FreightVisualRefit
{
 static Sprite sprite;static Material material;
 static Transform G(string name,Transform parent,Vector2 position){var t=new GameObject(name).transform;t.SetParent(parent,false);t.position=position;return t;}
 static SpriteRenderer B(string name,Transform parent,Vector2 pos,Vector2 size,int color,int order){var t=G(name,parent,pos);t.localScale=new Vector3(size.x,size.y,1);var r=t.gameObject.AddComponent<SpriteRenderer>();r.sprite=sprite;r.sharedMaterial=material;r.color=RecoilPrototype.Hex(color);r.sortingOrder=order;return r;}
 static void Label(string text,Transform parent,Vector2 p,float size,int order){var t=G(text,parent,p);var mesh=t.gameObject.AddComponent<TextMesh>();mesh.text=text;mesh.fontSize=48;mesh.characterSize=size;mesh.anchor=TextAnchor.MiddleCenter;mesh.color=RecoilPrototype.Hex(0x263a36);mesh.GetComponent<MeshRenderer>().sortingOrder=order;}
 public static void Style(KBKStackerCrane crane){
  var baseRenderer=crane.GetComponentsInChildren<SpriteRenderer>(true).First(r=>r.name=="Trolley housing");sprite=baseRenderer.sprite;material=baseRenderer.sharedMaterial;
  foreach(var t in crane.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Freight details - fixed"||t.name=="Freight details - trolley"||t.name=="Freight details - moving fork").ToArray())UnityEngine.Object.DestroyImmediate(t.gameObject);
  foreach(var r in crane.GetComponentsInChildren<SpriteRenderer>(true)){
   switch(r.name){
    case "KBK suspension rail":case "Trolley housing":case "Fork carriage":case "Ground switch housing":r.color=RecoilPrototype.Hex(0xe4ba59);break;
    case "Guided steel mast":case "Rail ceiling suspension":case "Ground switch pedestal":r.color=RecoilPrototype.Hex(0xaec1b6);break;
    case "Mast cross brace":case "Motor and chain hoist":r.color=RecoilPrototype.Hex(0x658b87);break;
    case "Fork deck - soft moving platform":r.color=RecoilPrototype.Hex(0xf0ce77);break;
    case "Fork deck ribs":r.color=RecoilPrototype.Hex(0xd6d9bc);r.sortingOrder=11;break;
   }
  }
  var fixedRoot=G("Freight details - fixed",crane.transform,crane.transform.position);
  foreach(var rail in crane.GetComponentsInChildren<SpriteRenderer>(true).Where(r=>r.name=="KBK suspension rail").ToArray()){
   var bounds=rail.bounds;B("Bright rail flange",fixedRoot,new Vector2(bounds.center.x,bounds.max.y),new Vector2(bounds.size.x,.035f),0xf2d08a,1);
   for(float x=bounds.min.x+.25f;x<bounds.max.x;x+=1.5f){B("Rail fixing plate",fixedRoot,new Vector2(x,bounds.center.y),new Vector2(.12f,.22f),0x8faaa2,2);B("Rail bolt",fixedRoot,new Vector2(x,bounds.center.y),new Vector2(.035f,.035f),0xe3e1bc,3);}
  }
  var rig=G("Freight details - trolley",crane.travelRig,crane.travelRig.position);var housing=baseRenderer.bounds;
  B("Trolley face inset",rig,housing.center,new Vector2(.84f,.26f),0x6a8279,3);Label("KBK",rig,housing.center,.045f,4);
  var motor=crane.GetComponentsInChildren<SpriteRenderer>(true).First(r=>r.name=="Motor and chain hoist").bounds;
  for(int i=0;i<5;i++)B("Motor cooling vent",rig,new Vector2(motor.center.x-.22f+i*.11f,motor.center.y),new Vector2(.035f,.3f),0x2e504c,4);
  var masts=crane.GetComponentsInChildren<SpriteRenderer>(true).Where(r=>r.name=="Guided steel mast").ToArray();
  foreach(var mast in masts){var bounds=mast.bounds;B("Polished mast guide",rig,new Vector2(bounds.center.x+.025f,bounds.center.y),new Vector2(.035f,bounds.size.y),0xd6dbc4,3);}
  var mastCenter=masts.Select(r=>r.bounds.center.x).Average();
  B("Lift chain dark channel",rig,new Vector2(mastCenter,3.05f+crane.travelRig.position.y),new Vector2(.085f,5.85f),0x2d4945,2);
  for(int i=0;i<20;i++)B("Chain link",rig,new Vector2(mastCenter,.26f+i*.29f+crane.travelRig.position.y),new Vector2(.12f,.04f),0x809c8d,3);
  var lift=G("Freight details - moving fork",crane.liftCarriage,crane.liftCarriage.position);var deck=crane.forkPlatform.Area;float y=deck.yMax;
  // Graphic front face projects in front of the floor, while the existing bearing surface stays at y=0.
  B("Fork front face - visible while parked",lift,new Vector2(deck.center.x,y-.12f),new Vector2(deck.width,.24f),0xecc35d,9);
  B("Fork bright upper lip",lift,new Vector2(deck.center.x,y+.012f),new Vector2(deck.width+.06f,.055f),0xf8e6a0,12);
  B("Fork underside outline",lift,new Vector2(deck.center.x,y-.25f),new Vector2(deck.width+.04f,.035f),0x42625d,10);
  for(int i=0;i<10;i++){var stripe=B("Black hazard stripe",lift,new Vector2(deck.xMin+.15f+i*.3f,y-.12f),new Vector2(.115f,.2f),0x354b43,10);stripe.transform.rotation=Quaternion.Euler(0,0,-23);}
  foreach(float x in new[]{deck.xMin+.055f,deck.xMax-.055f}){B("Fork corner plate",lift,new Vector2(x,y-.11f),new Vector2(.11f,.25f),0xc5d2b9,11);B("Fork corner rivet",lift,new Vector2(x,y-.11f),new Vector2(.04f,.04f),0x48665c,12);}
  var carriage=crane.GetComponentsInChildren<SpriteRenderer>(true).First(r=>r.name=="Fork carriage").bounds;
  B("Carriage service cover",lift,carriage.center,new Vector2(.43f,.63f),0x6a8b7e,6);
  for(int row=-1;row<=1;row+=2)for(int col=-1;col<=1;col+=2)B("Carriage roller bolt",lift,new Vector2(carriage.center.x+col*.25f,carriage.center.y+row*.4f),new Vector2(.085f,.085f),0xe7deb3,7);
  Label("FREIGHT",lift,new Vector2(carriage.center.x,carriage.center.y),.035f,7);
 }
 [MenuItem("Recoil/Art/Highlight Warehouse Freight")]
 public static void Apply(){
  if(EditorApplication.isPlaying)throw new Exception("Exit Play first.");var active=SceneManager.GetActiveScene();const string path="Assets/Scenes/RecoilFactoryWarehouse.unity";var s=SceneManager.GetSceneByPath(path);bool added=!s.isLoaded;if(added)s=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
  var crane=s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<KBKStackerCrane>(true)).First();string backup="Backups/FreightVisualRefit/"+DateTime.Now.ToString("yyyyMMdd_HHmmss");Directory.CreateDirectory(backup);EditorSceneManager.SaveScene(s,backup+"/Warehouse.unity",true);
  var bounds=crane.forkPlatform.Area;Style(crane);Physics2D.SyncTransforms();if(crane.forkPlatform.Area!=bounds)throw new Exception("Art pass changed the bearing surface.");
  var prefab="Assets/RecoilPrototype/Prefabs/KBKStackerCrane.prefab";var prefabRoot=PrefabUtility.LoadPrefabContents(prefab);try{Style(prefabRoot.GetComponent<KBKStackerCrane>());PrefabUtility.SaveAsPrefabAsset(prefabRoot,prefab);}finally{PrefabUtility.UnloadPrefabContents(prefabRoot);}
  EditorSceneManager.MarkSceneDirty(s);EditorSceneManager.SaveScene(s);AssetDatabase.SaveAssets();SceneManager.SetActiveScene(active);if(added)EditorSceneManager.CloseScene(s,true);Debug.Log("FREIGHT_VISUAL_REFIT_COMPLETE");
 }
}
