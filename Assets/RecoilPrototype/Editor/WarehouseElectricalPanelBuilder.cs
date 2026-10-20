using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class WarehouseElectricalPanelBuilder
{
 [MenuItem("Recoil/Levels/Add Warehouse Distribution Box")]
 public static void Build()
 {
  if(EditorApplication.isPlaying)throw new Exception("Exit Play first.");var original=SceneManager.GetActiveScene();var loaded=Enumerable.Range(0,SceneManager.sceneCount).Select(i=>SceneManager.GetSceneAt(i).path).ToArray();
  const string outside="Assets/Scenes/RecoilPrototype.unity",inside="Assets/Scenes/RecoilFactoryWarehouse.unity";
  var os=SceneManager.GetSceneByPath(outside);if(!os.isLoaded)os=EditorSceneManager.OpenScene(outside,OpenSceneMode.Additive);var ow=os.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<RecoilPrototype>()).First();
  if(ow.GetComponentInChildren<ElectricalPanelInteraction>())throw new Exception("Box already exists. Edit its saved objects.");Directory.CreateDirectory("Backups/WarehouseElectricalPanel");EditorSceneManager.SaveScene(os,"Backups/WarehouseElectricalPanel/Outdoor_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+".unity",true);SceneManager.SetActiveScene(os);
  var serial=new SerializedObject(ow);FactoryWarehouseBuilder.UseArt((Sprite)serial.FindProperty("sceneSprite").objectReferenceValue,(Material)serial.FindProperty("sceneMaterial").objectReferenceValue);
  var warehouse=ow.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Factory Warehouse - exterior");var door=warehouse.GetComponentInChildren<RecoilDoor>();Vector2 pos=(Vector2)door.transform.position+new Vector2(2.4f,-.15f);
  var box=FactoryWarehouseBuilder.G("Warehouse distribution box - interactive",warehouse,pos);FactoryWarehouseBuilder.B("Metal enclosure",box,pos,new Vector2(.9f,1.4f),0x77816d,5);FactoryWarehouseBuilder.B("Recessed cover",box,pos,new Vector2(.74f,1.2f),0x475749,6);FactoryWarehouseBuilder.B("Cover latch",box,pos+new Vector2(.24f,0),new Vector2(.08f,.2f),0xb5ac8c,7);FactoryWarehouseBuilder.B("Power symbol plate",box,pos+Vector2.up*.27f,new Vector2(.35f,.25f),0xa69454,7);
  for(int i=0;i<3;i++)FactoryWarehouseBuilder.B("Incoming conduit",box,pos+new Vector2(-.28f+i*.27f,.95f),new Vector2(.06f,.6f),0x667660,4);
  box.gameObject.AddComponent<ElectricalPanelInteraction>();PrefabUtility.SaveAsPrefabAsset(box.gameObject,"Assets/RecoilPrototype/Prefabs/WarehouseDistributionBox.prefab");EditorSceneManager.MarkSceneDirty(os);EditorSceneManager.SaveScene(os);
  var ins=SceneManager.GetSceneByPath(inside);if(!ins.isLoaded)ins=EditorSceneManager.OpenScene(inside,OpenSceneMode.Additive);var iw=ins.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<RecoilPrototype>()).First();EditorSceneManager.SaveScene(ins,"Backups/WarehouseElectricalPanel/Warehouse_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+".unity",true);
  var crane=iw.GetComponentInChildren<KBKStackerCrane>();crane.requiredPowerItem=WarehousePowerState.FreightPower;crane.ResetMachine();if(!iw.GetComponent<WarehouseLightingPower>())iw.gameObject.AddComponent<WarehouseLightingPower>();PrefabUtility.SaveAsPrefabAsset(crane.gameObject,"Assets/RecoilPrototype/Prefabs/KBKStackerCrane.prefab");EditorSceneManager.MarkSceneDirty(ins);EditorSceneManager.SaveScene(ins);AssetDatabase.SaveAssets();
  foreach(var path in new[]{outside,inside})if(!loaded.Contains(path)){var s=SceneManager.GetSceneByPath(path);if(s.isLoaded)EditorSceneManager.CloseScene(s,true);}SceneManager.SetActiveScene(original);Debug.Log("WAREHOUSE_ELECTRICAL_PANEL_COMPLETE");
 }
}
