using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

public static class KitchenMemorySharedSystems
{
 static void Set(UnityEngine.Object o,string name,UnityEngine.Object value){var s=new SerializedObject(o);s.FindProperty(name).objectReferenceValue=value;s.ApplyModifiedPropertiesWithoutUndo();}
 static Transform G(string name,Transform parent,Vector2 p){var t=new GameObject(name).transform;t.SetParent(parent,false);t.position=p;return t;}
 public static void ApplyChild(RecoilPrototype world){
  var player=world.Player;var profile=player.GetComponent<RecoilPlayerBodyProfile>();if(!profile)profile=player.gameObject.AddComponent<RecoilPlayerBodyProfile>();
  profile.standingHeight=1.12f;profile.crouchingHeight=.66f;profile.halfWidth=.22f;
  var ps=new SerializedObject(player);var body=(Transform)ps.FindProperty("coat").objectReferenceValue;var head=(Transform)ps.FindProperty("head").objectReferenceValue;var hair=(Transform)ps.FindProperty("hat").objectReferenceValue;
  body.name="Boy sweater";body.GetComponent<SpriteRenderer>().color=RecoilPrototype.Hex(0x647b7e);head.name="Boy head";hair.name="Boy hair - no detective hat";
  var old=player.transform.Find("Boy legs and shoes");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);var group=new GameObject("Boy legs and shoes").transform;group.SetParent(player.transform,false);
  var sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/RecoilPrototype/Art/PrototypeSquare.png");var material=body.GetComponent<SpriteRenderer>().sharedMaterial;
  profile.legs=new Transform[2];profile.shoes=new Transform[2];
  for(int i=0;i<2;i++)for(int k=0;k<2;k++){var t=new GameObject(k==0?"Trouser leg":"Shoe").transform;t.SetParent(group,false);var r=t.gameObject.AddComponent<SpriteRenderer>();r.sprite=sprite;r.sharedMaterial=material;r.color=RecoilPrototype.Hex(k==0?0x5c604c:0x293632);r.sortingOrder=k==0?10:11;if(k==0)profile.legs[i]=t;else profile.shoes[i]=t;}
  var camera=(Camera)new SerializedObject(world).FindProperty("cam").objectReferenceValue;var weapon=(Transform)ps.FindProperty("gun").objectReferenceValue;player.Configure(world,camera,body,head,hair,weapon);
  var spawn=(Transform)new SerializedObject(world).FindProperty("spawnPoint").objectReferenceValue;spawn.position=new Vector3(spawn.position.x,profile.standingHeight*.5f+.015f,spawn.position.z);player.Position=spawn.position;player.SyncVisuals(0);
 }
 public static void Attach(Scene scene,KitchenMemoryController story,Transform exit,Transform bedroomDoor){
  var sourceScene=SceneManager.GetSceneByPath("Assets/Scenes/RecoilFactoryWarehouse.unity");bool added=!sourceScene.isLoaded;if(added)sourceScene=EditorSceneManager.OpenScene("Assets/Scenes/RecoilFactoryWarehouse.unity",OpenSceneMode.Additive);
  var source=sourceScene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<RecoilPrototype>(true)).First();var root=story.transform;
  var world=root.GetComponent<RecoilPrototype>();if(!world)world=root.gameObject.AddComponent<RecoilPrototype>();story.world=world;
  var ser=new SerializedObject(world);ser.FindProperty("sceneAuthored").boolValue=true;ser.FindProperty("sceneSprite").objectReferenceValue=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/RecoilPrototype/Art/PrototypeSquare.png");ser.FindProperty("sceneMaterial").objectReferenceValue=AssetDatabase.LoadAssetAtPath<Material>("Assets/RecoilPrototype/Art/PrototypeSprite.mat");ser.ApplyModifiedPropertiesWithoutUndo();
  world.locationLabel="KITCHEN MEMORY";world.enableCheckpoint=false;world.legacyEvidenceEnabled=false;world.startWithWaterGun=false;world.objectiveWithoutCard="Speak to your mother, then leave through the middle door.";world.objectiveWithCard=world.objectiveWithoutCard;
  var player=UnityEngine.Object.Instantiate(source.Player,root);player.name="Boy - shared Player Controller";var ps=new SerializedObject(player);
  var coat=(Transform)ps.FindProperty("coat").objectReferenceValue;var head=(Transform)ps.FindProperty("head").objectReferenceValue;var hat=(Transform)ps.FindProperty("hat").objectReferenceValue;var gun=(Transform)ps.FindProperty("gun").objectReferenceValue;
  if(gun && !gun.IsChildOf(player.transform))gun=UnityEngine.Object.Instantiate(gun.gameObject,player.transform).transform;
  coat.GetComponent<SpriteRenderer>().color=RecoilPrototype.Hex(0x647b7e);head.GetComponent<SpriteRenderer>().color=RecoilPrototype.Hex(0xb4a58e);hat.localScale=new Vector3(.35f,.12f,1);hat.GetComponent<SpriteRenderer>().color=RecoilPrototype.Hex(0x34342e);
  var spawn=G("Player Spawn - kitchen doorway",root,new Vector2(15.55f,.845f));player.Position=spawn.position;
  var cam=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Camera>(true)).First();var cameraControl=cam.GetComponent<RecoilCameraController>();if(!cameraControl)cameraControl=cam.gameObject.AddComponent<RecoilCameraController>();
  var sourceCamera=sourceScene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<RecoilCameraController>(true)).First();EditorUtility.CopySerialized(sourceCamera,cameraControl);cameraControl.target=player.transform;
  player.Configure(world,cam,coat,head,hat,gun);Set(world,"player",player);Set(world,"actor",player.transform);Set(world,"cam",cam);Set(world,"coat",coat);Set(world,"head",head);Set(world,"hat",hat);Set(world,"gun",gun);Set(world,"spawnPoint",spawn);Set(world,"checkpointPoint",spawn);Set(world,"factoryEntrance",spawn);Set(world,"door",exit);Set(world,"clue",null);
  var floor=root.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Continuous floor");if(!floor.GetComponent<BoxCollider2D>())floor.gameObject.AddComponent<BoxCollider2D>();
  var ceiling=root.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Ceiling");if(!ceiling.GetComponent<BoxCollider2D>())ceiling.gameObject.AddComponent<BoxCollider2D>();
  var right=G("Right solid room boundary",root,new Vector2(24.2f,2.2f));right.gameObject.AddComponent<BoxCollider2D>().size=new Vector2(.4f,4.4f);
  var left=G("Left solid room boundary",root,new Vector2(-.2f,2.2f));left.gameObject.AddComponent<BoxCollider2D>().size=new Vector2(.4f,4.4f);
  story.bedroomDoor=bedroomDoor;
  story.closedBedroomDoor.GetComponentsInChildren<SpriteRenderer>(true).First(r=>r.name=="Closed door - side view").gameObject.AddComponent<BoxCollider2D>();
  bedroomDoor.GetComponentsInChildren<SpriteRenderer>(true).First(r=>r.name=="Bedroom door frame").gameObject.AddComponent<BoxCollider2D>();
  var conversation=story.mother.gameObject.AddComponent<KitchenMotherConversation>();conversation.story=story;conversation.itemId="kitchen_mother_conversation";conversation.displayName="Talk to mother";conversation.interactionDuration=0;conversation.interactionRange=1.9f;
  // Target the mother's torso for the existing distance/line-of-sight item interaction.
  story.mother.position+=Vector3.up*.8f;foreach(Transform visual in story.mother)visual.position-=Vector3.up*.8f;
  story.motherCookingPoint.position=story.mother.position;story.bedroomRestPoint.position+=Vector3.up*.8f;
  var door=exit.gameObject.GetComponent<RecoilDoor>();if(!door)door=exit.gameObject.AddComponent<RecoilDoor>();door.requiresClue=false;door.completesLevel=false;door.requiredItemId=KitchenMemoryController.ExitPermission;door.returnToDeathMemoryCheckpoint=true;door.interactionOffset=Vector2.up*.85f;door.interactionPrompt="E / LEAVE";door.lockedMessage="The door won't open yet.";
  var lightingRoot=G("05 Lighting - shared game lighting",root,Vector2.zero);var overlay=lightingRoot.gameObject.AddComponent<RecoilLighting2D>();var sourceLighting=source.GetComponentInChildren<RecoilLighting2D>();if(sourceLighting)EditorUtility.CopySerialized(sourceLighting,overlay);
  var darkness=G("Darkness Layer",lightingRoot,new Vector2(12,2));darkness.localScale=new Vector3(100,60,1);var dr=darkness.gameObject.AddComponent<SpriteRenderer>();dr.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/RecoilPrototype/Art/PrototypeSquare.png");dr.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/RecoilPrototype/Art/FactoryDarkness.mat");dr.sortingOrder=150;
  var fixture=root.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Pendant - kitchen");var lamp=G("Light - pendant child",fixture,new Vector2(19.5f,3.3f)).gameObject.AddComponent<RecoilLight2D>();lamp.radius=4;lamp.intensity=.45f;
  var bedside=root.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Bedside shade");lamp=G("Light - bedside child",bedside,new Vector2(.75f,1.28f)).gameObject.AddComponent<RecoilLight2D>();lamp.radius=2.4f;lamp.intensity=.25f;
  ApplyChild(world);
  overlay.Configure(player.transform,null,dr);cameraControl.Initialize(world,player.transform);cameraControl.SnapToTarget();player.SyncVisuals(0);
  if(added)EditorSceneManager.CloseScene(sourceScene,true);
 }
}
