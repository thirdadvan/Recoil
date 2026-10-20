using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class KBKStackerCraneBuilder
{
 [MenuItem("Recoil/Levels/Add Warehouse KBK Stacker Crane")]
 public static void Build()
 {
  if(EditorApplication.isPlaying)throw new Exception("Exit Play first.");const string path="Assets/Scenes/RecoilFactoryWarehouse.unity";
  var original=SceneManager.GetActiveScene();var scene=SceneManager.GetSceneByPath(path);bool loaded=scene.isLoaded;if(!loaded)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
  var w=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<RecoilPrototype>()).First();if(w.GetComponentInChildren<KBKStackerCrane>())throw new Exception("Crane already exists; edit the saved device.");
  Directory.CreateDirectory("Backups/KBKStackerCrane");EditorSceneManager.SaveScene(scene,"Backups/KBKStackerCrane/Warehouse_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+".unity",true);SceneManager.SetActiveScene(scene);
  var serial=new SerializedObject(w);FactoryWarehouseBuilder.UseArt((Sprite)serial.FindProperty("sceneSprite").objectReferenceValue,(Material)serial.FindProperty("sceneMaterial").objectReferenceValue);
  var root=FactoryWarehouseBuilder.G("Demag KBK Stacker Crane - editable",w.transform,Vector2.zero);var crane=root.gameObject.AddComponent<KBKStackerCrane>();
  crane.startPoint=FactoryWarehouseBuilder.G("01 Start - floor level",root,new Vector2(22.5f,0));crane.destinationPoint=FactoryWarehouseBuilder.G("02 Drop point - floor level",root,new Vector2(5,0));
  foreach(float y in new[]{6.45f,6.75f})FactoryWarehouseBuilder.B("KBK suspension rail",root,new Vector2(14.625f,y),new Vector2(21.25f,.15f),0xb39b58,-1);
  for(int i=0;i<6;i++){float x=4.5f+i*4;FactoryWarehouseBuilder.B("Rail ceiling suspension",root,new Vector2(x,6.95f),new Vector2(.08f,.4f),0x737f72,-2);}
  var rig=FactoryWarehouseBuilder.G("03 Travelling trolley and mast",root,new Vector2(22.5f,0));crane.travelRig=rig;
  FactoryWarehouseBuilder.B("Trolley housing",rig,new Vector2(23.6f,6.5f),new Vector2(1.2f,.5f),0xa79251,2);
  foreach(float x in new[]{23.18f,24.02f})FactoryWarehouseBuilder.B("Trolley wheel",rig,new Vector2(x,6.62f),new Vector2(.22f,.22f),0x283d34,3);
  FactoryWarehouseBuilder.B("Motor and chain hoist",rig,new Vector2(23.6f,6.05f),new Vector2(.7f,.55f),0x566b59,2);
  foreach(float x in new[]{23.42f,23.78f})FactoryWarehouseBuilder.B("Guided steel mast",rig,new Vector2(x,3.05f),new Vector2(.12f,5.85f),0x96a28a,1);
  for(int i=0;i<10;i++)FactoryWarehouseBuilder.B("Mast cross brace",rig,new Vector2(23.6f,.4f+i*.58f),new Vector2(.45f,.045f),0x617762,0);
  var lift=FactoryWarehouseBuilder.G("04 Lift carriage and fork platform",rig,new Vector2(22.5f,0));crane.liftCarriage=lift;
  FactoryWarehouseBuilder.B("Fork carriage",lift,new Vector2(23.6f,.55f),new Vector2(.65f,1.1f),0xb49951,4);
  var deck=FactoryWarehouseBuilder.B("Fork deck - soft moving platform",lift,new Vector2(22.5f,-.06f),new Vector2(3,.12f),0xc2ae74,6);var collider=deck.AddComponent<BoxCollider2D>();collider.isTrigger=true;crane.forkPlatform=deck.AddComponent<SoftRoofPlatform>();
  for(int i=0;i<10;i++)FactoryWarehouseBuilder.B("Fork deck ribs",lift,new Vector2(21.12f+i*.3f,-.005f),new Vector2(.12f,.055f),0x5b6653,7);
  var pendant=FactoryWarehouseBuilder.G("05 Fixed ground control - E",root,new Vector2(9.6f,.95f));FactoryWarehouseBuilder.B("Ground switch housing",pendant,pendant.position,new Vector2(.3f,.55f),0xb59c57,7);
  var status=FactoryWarehouseBuilder.B("Status indicator",pendant,(Vector2)pendant.position+Vector2.up*.12f,new Vector2(.14f,.1f),0x73ae97,8);crane.statusLamp=status.GetComponent<SpriteRenderer>();
  FactoryWarehouseBuilder.B("Operate button",pendant,(Vector2)pendant.position-Vector2.up*.12f,new Vector2(.12f,.13f),0x985548,8);
  FactoryWarehouseBuilder.B("Ground switch pedestal",pendant,(Vector2)pendant.position-Vector2.up*.6f,new Vector2(.09f,.7f),0x6a7865,13);
  foreach(var renderer in pendant.GetComponentsInChildren<SpriteRenderer>())if(renderer.name!="Ground switch pedestal")renderer.sortingOrder+=7;
  pendant.position=new Vector3(pendant.position.x,pendant.position.y,-.1f);
  var control=pendant.gameObject.AddComponent<KBKCraneSwitch>();control.crane=crane;control.itemId="kbk_crane_switch";control.displayName="Stacker crane ground switch";control.interactionDuration=0;control.interactionRange=2;control.resultId="";
  var goods=FactoryWarehouseBuilder.G("06 Scattered freight at drop point",root,new Vector2(5,0));
  foreach(var p in new[]{new Vector2(2.6f,.38f),new Vector2(3.4f,.3f),new Vector2(6.8f,.5f),new Vector2(7.5f,.25f)}){var box=FactoryWarehouseBuilder.B("Loose freight crate",goods,p,new Vector2(.8f,.65f),0x8c7855,5);box.transform.rotation=Quaternion.Euler(0,0,p.x<8?-12:16);FactoryWarehouseBuilder.B("Crate strap",box.transform,p,new Vector2(.08f,.65f),0x4c5843,6);}
  FactoryWarehouseBuilder.B("Dropped pallet",goods,new Vector2(5.3f,.08f),new Vector2(1.5f,.14f),0x7a6d48,4);
  crane.ResetMachine();FreightVisualRefit.Style(crane);Directory.CreateDirectory("Assets/RecoilPrototype/Prefabs");PrefabUtility.SaveAsPrefabAsset(root.gameObject,"Assets/RecoilPrototype/Prefabs/KBKStackerCrane.prefab");
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();if(!loaded)EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(original);Debug.Log("KBK_STACKER_CRANE_COMPLETE");
 }
}
