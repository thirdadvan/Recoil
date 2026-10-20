using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class FactoryWarehouseDoorRepair
{
 const string Warehouse="Assets/Scenes/RecoilFactoryWarehouse.unity",Store="Assets/Scenes/RecoilWarehouseStoreroom.unity";
 static Scene Open(string p){var s=SceneManager.GetSceneByPath(p);return s.isLoaded?s:EditorSceneManager.OpenScene(p,OpenSceneMode.Additive);}
 static RecoilPrototype World(Scene s){return s.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<RecoilPrototype>(true)).First();}
 static void Save(Scene s){EditorSceneManager.MarkSceneDirty(s);EditorSceneManager.SaveScene(s);}
 [MenuItem("Recoil/Levels/Repair Warehouse Door and Roof Sight")]
 public static void Repair()
 {
  if(EditorApplication.isPlaying)throw new Exception("Exit Play before repairing scene geometry.");
  var original=SceneManager.GetActiveScene();var loaded=Enumerable.Range(0,SceneManager.sceneCount).Select(i=>SceneManager.GetSceneAt(i).path).ToArray();
  var warehouse=Open(Warehouse);var w=World(warehouse);Directory.CreateDirectory("Backups/FactoryWarehouseDoorRepair");EditorSceneManager.SaveScene(warehouse,"Backups/FactoryWarehouseDoorRepair/Warehouse_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+".unity",true);
  SceneManager.SetActiveScene(warehouse);var serial=new SerializedObject(w);FactoryWarehouseBuilder.UseArt((Sprite)serial.FindProperty("sceneSprite").objectReferenceValue,(Material)serial.FindProperty("sceneMaterial").objectReferenceValue);
  var old=w.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="Right locked storage room");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
  var root=FactoryWarehouseBuilder.G("Locked storeroom entrance - background wall",w.transform,new Vector2(26.5f,0));
  FactoryWarehouseBuilder.B("Storeroom alcove backing",root,new Vector2(26.5f,2),new Vector2(6.4f,4),0x405348,-9);
  var door=FactoryWarehouseBuilder.Door(root,"Locked storeroom",new Vector2(26.5f,1.25f),Store,"Entrance Spawn");door.requiredItemId="warehouse_locked_room_key";door.lockedMessage="The storeroom is locked. A key is needed.";
  // The warehouse corridor passes in front of this door. The actual room is beyond it in depth.
  FactoryWarehouseBuilder.Lamp(root,new Vector2(26.5f,3.35f),.27f,3.2f);FactoryWarehouseBuilder.G("Storeroom Return Spawn",root,new Vector2(25.1f,.845f));Save(warehouse);
  if(!File.Exists(Store))
  {
   var store=FactoryWarehouseBuilder.NewInterior(Store,"WAREHOUSE LOCKED STORE",8,3.8f);var sw=World(store);var props=FactoryWarehouseBuilder.G("Stored parts - editable",sw.transform,Vector2.zero);
   FactoryWarehouseBuilder.Door(props,"Return to warehouse",new Vector2(1.2f,1.25f),Warehouse,"Storeroom Return Spawn");
   for(int i=0;i<3;i++){float x=3.2f+i*1.5f;FactoryWarehouseBuilder.B("Spare parts crate",props,new Vector2(x,.6f),new Vector2(1.1f,1.2f),0x776d50,-4);FactoryWarehouseBuilder.B("Crate strap",props,new Vector2(x,.6f),new Vector2(.08f,1.2f),0x40533e,-3);}
   FactoryWarehouseBuilder.Lamp(props,new Vector2(4.5f,3.55f),.25f,3.5f);Save(store);
  }
  var settings=EditorBuildSettings.scenes.ToList();if(!settings.Any(s=>s.path==Store))settings.Add(new EditorBuildSettingsScene(Store,true));EditorBuildSettings.scenes=settings.ToArray();
  int roofs=0;
  foreach(var entry in settings.Where(s=>s.enabled))
  {
   var scene=Open(entry.path);var context=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<RecoilPrototype>(true)).FirstOrDefault();if(!context)continue;
   foreach(var c in context.GetComponentsInChildren<BoxCollider2D>(true))
   {
    string name=c.name.ToLowerInvariant();if(c.isTrigger || c.GetComponent<SoftRoofPlatform>() || !(name.Contains("roof")||name.Contains("ceiling")||name.Contains("parapet")||name.Contains("屋顶")||name.Contains("顶棚")))continue;
    var visibility=c.GetComponent<RecoilSurfaceVisibility>();if(!visibility)visibility=c.gameObject.AddComponent<RecoilSurfaceVisibility>();visibility.blocksSight=false;roofs++;
   }
   Save(scene);
  }
  AssetDatabase.SaveAssets();foreach(var entry in settings){var s=SceneManager.GetSceneByPath(entry.path);if(s.isLoaded&&!loaded.Contains(entry.path))EditorSceneManager.CloseScene(s,true);}SceneManager.SetActiveScene(original);
  File.WriteAllText("output/level-previews/warehouse-repair-result.txt","WAREHOUSE_DOOR_REPAIR_COMPLETE / transparent hard roofs: "+roofs);Debug.Log("WAREHOUSE_DOOR_REPAIR_COMPLETE / transparent hard roofs: "+roofs);
 }
}
