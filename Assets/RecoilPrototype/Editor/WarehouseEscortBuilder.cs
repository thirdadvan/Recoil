using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class WarehouseEscortBuilder
{
 const string Warehouse="Assets/Scenes/RecoilFactoryWarehouse.unity",Store="Assets/Scenes/RecoilWarehouseStoreroom.unity";
 static RecoilPrototype W(Scene s){return s.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<RecoilPrototype>(true)).First();}
 static Scene Open(string path){var s=SceneManager.GetSceneByPath(path);return s.isLoaded?s:EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);}
 static void Art(RecoilPrototype w){var so=new SerializedObject(w);FactoryWarehouseBuilder.UseArt((Sprite)so.FindProperty("sceneSprite").objectReferenceValue,(Material)so.FindProperty("sceneMaterial").objectReferenceValue);}
 [MenuItem("Recoil/Levels/Add Warehouse Escort Test")]
 public static void Build()
 {
  if(EditorApplication.isPlaying)throw new Exception("Exit Play first.");
  var original=SceneManager.GetActiveScene();var warehouse=Open(Warehouse);var store=Open(Store);
  Directory.CreateDirectory("Backups/WarehouseEscort");string stamp=DateTime.Now.ToString("yyyyMMdd_HHmmss");
  EditorSceneManager.SaveScene(warehouse,"Backups/WarehouseEscort/Warehouse_"+stamp+".unity",true);EditorSceneManager.SaveScene(store,"Backups/WarehouseEscort/Storage_"+stamp+".unity",true);
  var sw=W(store);Art(sw);SceneManager.SetActiveScene(store);
  var child=sw.GetComponentInChildren<EscortChild>(true);
  if(!child)
  {
   Vector2 pos=new Vector2(5.2f,.525f);var t=FactoryWarehouseBuilder.G("Child - interact to hold hands",sw.transform,pos);
   FactoryWarehouseBuilder.B("Body",t,pos+Vector2.down*.06f,new Vector2(.38f,.68f),0x899779,10);
   FactoryWarehouseBuilder.B("Head",t,pos+Vector2.up*.37f,new Vector2(.27f,.28f),0xcabda3,11);
   FactoryWarehouseBuilder.B("Hair",t,pos+Vector2.up*.49f,new Vector2(.3f,.1f),0x3e3530,12);
   foreach(float x in new[]{-.11f,.11f})FactoryWarehouseBuilder.B("Leg",t,pos+new Vector2(x,-.43f),new Vector2(.11f,.19f),0x424b40,10);
   child=t.gameObject.AddComponent<EscortChild>();var line=t.gameObject.AddComponent<LineRenderer>();
   var renderer=t.GetComponentInChildren<SpriteRenderer>();line.sharedMaterial=renderer.sharedMaterial;line.useWorldSpace=true;line.positionCount=2;line.startWidth=line.endWidth=.045f;line.startColor=line.endColor=RecoilPrototype.Hex(0xcabda3);line.sortingOrder=12;line.enabled=false;
   Directory.CreateDirectory("Assets/RecoilPrototype/Resources");PrefabUtility.SaveAsPrefabAsset(t.gameObject,"Assets/RecoilPrototype/Resources/RecoilEscortChild.prefab");
  }
  EditorSceneManager.MarkSceneDirty(store);EditorSceneManager.SaveScene(store);
  var ww=W(warehouse);Art(ww);SceneManager.SetActiveScene(warehouse);
  var group=ww.transform.Find("Warehouse escort encounter - editable");if(!group)group=FactoryWarehouseBuilder.G("Warehouse escort encounter - editable",ww.transform,Vector2.zero);
  for(int i=0;i<3;i++)
  {
   string name="Rampager warehouse "+(i+1);var t=group.Find(name);
   if(!t)
   {
    Vector2 pos=new Vector2(7.3f+i*4.6f,.8f);t=FactoryWarehouseBuilder.G(name,group,pos);
    var sr=t.gameObject.AddComponent<SpriteRenderer>();var art=ww.GetComponentInChildren<RecoilPlayerController>().GetComponentInChildren<SpriteRenderer>();sr.sprite=art.sprite;sr.sharedMaterial=art.sharedMaterial;sr.color=RecoilPrototype.Hex(0xb87570);sr.sortingOrder=9;
    // Sized renderer keeps the gameplay actor transform unscaled.
    sr.drawMode=SpriteDrawMode.Sliced;sr.size=new Vector2(.66f,1.6f);
    var c=t.gameObject.AddComponent<RampagerController>();c.initialState=i<2?RampagerController.Behaviour.Sleeping:RampagerController.Behaviour.Roaming;c.spawnFacing=RampagerController.SpawnDirection.Left;c.Configure(ww,-.55f,.55f);
    FactoryWarehouseBuilder.B("Eye",t,pos+new Vector2(-.15f,.4f),new Vector2(.12f,.08f),0xf5ca97,10);
   }
  }
  foreach(var enemy in group.GetComponentsInChildren<RampagerController>(true))enemy.EnsureStandardComponents();
  var serialized=new SerializedObject(ww);var actors=ww.GetComponentsInChildren<RampagerController>();var list=serialized.FindProperty("enemies");list.arraySize=actors.Length;for(int i=0;i<actors.Length;i++)list.GetArrayElementAtIndex(i).objectReferenceValue=actors[i];serialized.ApplyModifiedPropertiesWithoutUndo();
  EditorSceneManager.MarkSceneDirty(warehouse);EditorSceneManager.SaveScene(warehouse);AssetDatabase.SaveAssets();SceneManager.SetActiveScene(original);
  Debug.Log("WAREHOUSE_ESCORT_READY / child in Storage, three Rampagers in shelf gaps");
 }
}
