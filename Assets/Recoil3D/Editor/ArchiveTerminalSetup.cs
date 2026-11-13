using System;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;
public static class ArchiveTerminalSetup
{
 const string Archive="Assets/Scenes/3D/FactoryRooms/FactoryArchiveF3.unity";
 const string Prefab="Assets/Art/Environment/ArchiveRoom/Archive terminal Coaler.prefab";
 static Transform G(string name,Transform parent,Vector3 local){var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=local;return t;}
 static Material Mat(string name,Color color){var path="Assets/Art/Environment/ArchiveRoom/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Unlit/Color"));AssetDatabase.CreateAsset(m,path);}m.color=color;return m;}
 static ArchivePersonRecord Record(string id,string title,string[] fields,string summary)
 {
  var path="Assets/Resources/RecoilInformation/"+id+".asset";var r=AssetDatabase.LoadAssetAtPath<ArchivePersonRecord>(path);if(!r){r=ScriptableObject.CreateInstance<ArchivePersonRecord>();AssetDatabase.CreateAsset(r,path);}r.recordId=id;r.title=title;r.fields=fields;r.dialogueSummary=summary;EditorUtility.SetDirty(r);return r;
 }
 public static string Apply()
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit mode required");var previous=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(previous.isDirty)EditorSceneManager.SaveScene(previous);
  Directory.CreateDirectory("Assets/Resources/RecoilInformation");AssetDatabase.Refresh();
  if(!AssetDatabase.LoadAssetAtPath<GameObject>(Prefab)){
   EditorSceneManager.OpenScene("Assets/Scenes/3D/RecoilFactoryInterior.unity",OpenSceneMode.Single);
   var template=Object.FindObjectsOfType<RampagerController>().First(e=>e.name=="Rampager");var clone=Object.Instantiate(template.gameObject);clone.name="Coaler - archive terminal guard";
   foreach(var point in clone.GetComponentsInChildren<RampagerPatrolPoint>(true))Object.DestroyImmediate(point.gameObject);
   var route=clone.GetComponent<RampagerPatrolRoute>();if(route)Object.DestroyImmediate(route);
   var controller=clone.GetComponent<RampagerController>();controller.initialState=RampagerController.Behaviour.Roaming;controller.sleepPoint=null;
   var so=new SerializedObject(controller);so.FindProperty("world").objectReferenceValue=null;so.ApplyModifiedPropertiesWithoutUndo();var arrival=clone.GetComponent<RampagerDepthArrival>();if(arrival)arrival.navigation=null;
   PrefabUtility.SaveAsPrefabAsset(clone,Prefab);Object.DestroyImmediate(clone);
  }
  var asset=AssetDatabase.LoadAssetAtPath<NpcDialogueSequence>(CoalerCosplayerDialogueSetup.IntroductionPath);if(!asset){CoalerCosplayerDialogueSetup.CreateContent();asset=AssetDatabase.LoadAssetAtPath<NpcDialogueSequence>(CoalerCosplayerDialogueSetup.IntroductionPath);}var girlfriend=asset.lines[3];girlfriend.progressFlag=CoalerCosplayerConversation.GirlfriendNameFlag;asset.lines[3]=girlfriend;EditorUtility.SetDirty(asset);
  var scene=EditorSceneManager.OpenScene(Archive,OpenSceneMode.Single);var world=Object.FindObjectOfType<RecoilPrototype>();
  var aneta=Record("archive_aneta","ANETA",new[]{"NAME: ANETA","EMPLOYER: THIS FACTORY","EMPLOYMENT RECORD: CONFIRMED","CROSS-REFERENCE: FACTORY MANAGEMENT / FOURTH FLOOR","CURRENT LOCATION: NOT RECORDED","FILE STATUS: RESTRICTED"},"ANETA. Factory employment record confirmed. Further enquiries are referred to factory management on the fourth floor. Her current location is not recorded.");
  var ewa=Record("archive_ewa","EWA",new[]{"NAME QUERY: EWA","RESULT: NO VERIFIED MATCH","CURRENT LOCATION: NOT RECORDED"},"EWA. No verified match was found in this index. No current location is recorded.");
  var all=world.GetComponentsInChildren<Transform>(true);var station=all.First(t=>t.name=="Right computer station - desk terminal keyboard and cables");var screen=all.First(t=>t.name=="Green curved screen");var keyboard=all.First(t=>t.name=="Separate keyboard - keys and spacebar");
  if(!station.Find("Archive terminal - powered screen and dialogue")){
   var root=G("Archive terminal - powered screen and dialogue",station,Vector3.zero);var interaction=G("Terminal interaction point",root,Vector3.zero);interaction.position=new Vector3(screen.position.x,1.0f,0);
   var anchor=G("Terminal dialogue anchor",root,Vector3.zero);anchor.position=screen.position;
   var face=GameObject.CreatePrimitive(PrimitiveType.Quad);face.name="Powered green phosphor screen";Object.DestroyImmediate(face.GetComponent<Collider>());face.transform.SetParent(root,false);face.transform.SetPositionAndRotation(screen.position-screen.forward*.019f,screen.rotation);face.transform.localScale=new Vector3(.414f,.273f,1);face.GetComponent<MeshRenderer>().sharedMaterial=Mat("Archive terminal dark green phosphor",new Color(.018f,.075f,.035f));
   var label=G("Terminal green character display",root,Vector3.zero);label.SetPositionAndRotation(face.transform.position-face.transform.forward*.003f,face.transform.rotation);var text=label.gameObject.AddComponent<TextMesh>();text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=64;text.characterSize=.0022f;text.anchor=TextAnchor.UpperLeft;text.color=new Color(.40f,.96f,.53f);text.text="FACTORY RECORDS\nINDEX / 1978\n\nNAME QUERY >\nREADY_";label.position+=face.transform.right*-.18f+face.transform.up*.11f;label.GetComponent<MeshRenderer>().sharedMaterial=ArchiveRoomPresentationFix.TerminalTextMaterial(text.font);
   var light=G("Monitor green light spill",root,Vector3.zero).gameObject.AddComponent<Light>();light.transform.position=screen.position-screen.forward*.16f;light.transform.rotation=Quaternion.LookRotation(-screen.forward);light.type=LightType.Spot;light.spotAngle=115;light.innerSpotAngle=65;light.range=4.0f;light.intensity=1.3f;light.color=new Color(.38f,.85f,.53f);light.shadows=LightShadows.None;
   var terminal=root.gameObject.AddComponent<ArchiveTerminalConversation>();terminal.world=world;terminal.interactionPoint=interaction;terminal.screenAnchor=anchor;terminal.keyboard=keyboard;terminal.screenText=text;terminal.anetaRecord=aneta;terminal.girlfriendRecord=ewa;terminal.queryDuration=5;EditorUtility.SetDirty(terminal);
  }
  if(!all.Any(t=>t.name=="Coaler - archive terminal guard")){
   var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab),scene);go.name="Coaler - archive terminal guard";go.transform.SetParent(world.transform,false);go.transform.position=new Vector3(12.4f,.8f,.35f);go.transform.rotation=Quaternion.Euler(0,90,0);
   var enemy=go.GetComponent<RampagerController>();enemy.initialState=RampagerController.Behaviour.Roaming;enemy.sleepPoint=G("Archive Coaler sleep point",go.transform,Vector3.zero);var motor=go.GetComponent<RampagerMovement>();motor.leftOffset=-1;motor.rightOffset=1;EditorUtility.SetDirty(enemy);EditorUtility.SetDirty(motor);
  }
  var clock=world.GetComponentInChildren<WindUpArchiveClock>(true);clock.windingDuration=2;EditorUtility.SetDirty(clock);
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
  return "Saved powered archive terminal, dynamic Aneta/Ewa/Leave dialogue, 5s query, information records, Ewa knowledge flag and one idle Coaler beside terminal. Clock winding duration=2s. Single loaded scene.";
 }
}
