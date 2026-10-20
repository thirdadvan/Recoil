using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class WarehouseRackLadderBuilder
{
 [MenuItem("Recoil/Levels/Add Warehouse Rack Ladders and Freight Noise")]
 public static string Build()
 {
  if(EditorApplication.isPlaying)throw new Exception("Exit Play first.");
  const string path="Assets/Scenes/RecoilFactoryWarehouse.unity";var scene=SceneManager.GetSceneByPath(path);if(!scene.isLoaded)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
  Directory.CreateDirectory("Backups/WarehouseRackLadders");EditorSceneManager.SaveScene(scene,"Backups/WarehouseRackLadders/Warehouse_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+".unity",true);
  var w=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<RecoilPrototype>(true)).First();var so=new SerializedObject(w);FactoryWarehouseBuilder.UseArt((Sprite)so.FindProperty("sceneSprite").objectReferenceValue,(Material)so.FindProperty("sceneMaterial").objectReferenceValue);
  var racks=w.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Steel rack - pallets and freight").ToArray();int added=0;
  foreach(var rack in racks)
  {
   if(rack.Find("Left rack ladder - editable"))continue;
   Vector2 basePos=(Vector2)rack.position+Vector2.left*1.8f;var ladder=FactoryWarehouseBuilder.G("Left rack ladder - editable",rack,basePos);
   for(int side=-1;side<=1;side+=2)FactoryWarehouseBuilder.B("Ladder rail",ladder,basePos+new Vector2(side*.23f,2.5f),new Vector2(.055f,5),0x9aa394,2);
   for(int i=0;i<17;i++)FactoryWarehouseBuilder.B("Ladder rung",ladder,basePos+new Vector2(0,.12f+i*.29f),new Vector2(.5f,.05f),0x9aa394,3);
   var area=ladder.gameObject.AddComponent<BoxCollider2D>();area.isTrigger=true;area.size=new Vector2(.6f,5);area.offset=Vector2.up*2.5f;ladder.gameObject.AddComponent<ClimbableLadder>();
   if(added==0)PrefabUtility.SaveAsPrefabAsset(ladder.gameObject,"Assets/RecoilPrototype/Prefabs/WarehouseRackLadder.prefab");added++;
  }
  var crane=w.GetComponentInChildren<KBKStackerCrane>();if(!crane.GetComponent<FreightNoiseEmitter>())crane.gameObject.AddComponent<FreightNoiseEmitter>();
  const string prefabPath="Assets/RecoilPrototype/Prefabs/KBKStackerCrane.prefab";var prefab=PrefabUtility.LoadPrefabContents(prefabPath);
  try{if(!prefab.GetComponent<FreightNoiseEmitter>())prefab.AddComponent<FreightNoiseEmitter>();PrefabUtility.SaveAsPrefabAsset(prefab,prefabPath);}finally{PrefabUtility.UnloadPrefabContents(prefab);}
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();return "Racks="+racks.Length+" / ladders added="+added+" / Freight noise attached";
 }
}
