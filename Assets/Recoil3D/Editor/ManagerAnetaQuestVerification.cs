using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

public static class ManagerAnetaQuestVerification
{
    static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Need(bool ok,string message){if(!ok)throw new Exception(message);}
    static NpcDialogueSequence Asset(string name){return AssetDatabase.LoadAssetAtPath<NpcDialogueSequence>("Assets/RecoilPrototype/Dialogue/ManagerAneta_"+name+".asset");}
    static void Arm(RecoilPrototype w)
    {
        typeof(RecoilPrototype).GetField("conversationInputFrame",Private).SetValue(w,-1);
        typeof(RecoilPrototype).GetField("conversationReleasePending",Private).SetValue(w,false);
        typeof(RecoilPrototype).GetField("inventoryReleasePending",Private).SetValue(w,false);
    }
    static void Finish(RecoilPrototype w)
    {
        // Keep acquisition UI queued so this isolated preview test never changes the editor cursor/time scale.
        typeof(RecoilPrototype).GetField("inventoryReleasePending",Private).SetValue(w,true);
        typeof(RecoilPrototype).GetField("conversationReadyAt",Private).SetValue(w,-1f);
        int guard=0;while(w.ConversationActive && guard++<100){w.RevealConversationText();w.AdvanceConversation();}
        Need(!w.ConversationActive,"Conversation did not finish");Arm(w);
    }
    static T[] All<T>(Scene scene) where T:Component{return scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true)).ToArray();}
    [MenuItem("Recoil/Dialogue/Verify Manager Aneta Investigation")]
    public static string Run()
    {
        Need(!EditorApplication.isPlayingOrWillChangePlaymode,"Edit mode required; verification preserves all authored scene edits.");
        var report=new List<string>();var preview=EditorSceneManager.NewPreviewScene();
        try{
            var root=new GameObject("Manager quest verification");root.hideFlags=HideFlags.HideAndDontSave;SceneManager.MoveGameObjectToScene(root,preview);
            var world=root.AddComponent<RecoilPrototype>();var hero=new GameObject("Preview Dawn");hero.hideFlags=HideFlags.HideAndDontSave;SceneManager.MoveGameObjectToScene(hero,preview);var player=hero.AddComponent<RecoilPlayerController>();
            typeof(RecoilPrototype).GetField("player",Private).SetValue(world,player);typeof(RecoilPrototype).GetField("started",Private).SetValue(world,true);
            var cameraObject=new GameObject("Preview camera");cameraObject.hideFlags=HideFlags.HideAndDontSave;SceneManager.MoveGameObjectToScene(cameraObject,preview);var camera=cameraObject.AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=5;camera.transform.position=Vector3.back*10;camera.enabled=false;typeof(RecoilPrototype).GetField("cam",Private).SetValue(world,camera);
            var manager=new GameObject("Preview manager");manager.hideFlags=HideFlags.HideAndDontSave;SceneManager.MoveGameObjectToScene(manager,preview);manager.transform.position=Vector3.up*.6f;
            var talk=manager.AddComponent<ManagerAnetaConversation>();talk.world=world;talk.headAnchor=talk.interactionPoint=manager.transform;
            talk.denial=Asset("Denial");talk.denialReminder=Asset("DenialReminder");talk.admissionRequest=Asset("AdmissionRequest");talk.requestAlreadyPowered=Asset("AlreadyPowered");talk.pendingReminder=Asset("Pending");talk.powerRestored=Asset("FreightRestored");
            Need(talk.InRange,"Manager preview not in range");Need(talk.CurrentSequence==talk.denial,"First visit did not deny knowing Aneta");
            Need(talk.StartConversation(),"Denial did not start");Finish(world);
            Need(world.HasItem(ManagerAnetaConversation.DenialFlag)&&!world.HasItem(ManagerAnetaConversation.StoreroomKey)&&talk.CurrentSequence==talk.denialReminder,"Denial granted key or did not persist");
            report.Add("PASS first visit and repeat denial; no evidence means no admission or key");
            world.RememberProgress(ManagerAnetaConversation.EvidenceFlag);Need(talk.CurrentSequence==talk.admissionRequest,"Electronic record did not unlock admission");
            Need(talk.StartConversation(),"Admission interruption test failed");typeof(RecoilPrototype).GetField("conversationReadyAt",Private).SetValue(world,-1f);
            int guard=0;while(!world.CurrentConversationText.Contains("Here. Take the key.")&&guard++<50){world.RevealConversationText();world.AdvanceConversation();}
            Need(world.CurrentConversationText.Contains("Here. Take the key.")&&!world.HasItem(ManagerAnetaConversation.StoreroomKey),"Key awarded before completion");world.CancelConversationWith(talk.headAnchor);Arm(world);
            Need(!world.HasItem(ManagerAnetaConversation.AssignmentFlag)&&!world.HasItem(ManagerAnetaConversation.StoreroomKey),"Interrupted admission committed reward");
            Need(talk.StartConversation(),"Admission retry failed");Finish(world);
            Need(world.HasItem(ManagerAnetaConversation.AssignmentFlag)&&world.HasItem(ManagerAnetaConversation.StoreroomKey)&&talk.CurrentSequence==talk.pendingReminder,"Admission completion did not grant storeroom key/assignment");
            var queue=(System.Collections.ICollection)typeof(RecoilPrototype).GetField("pendingPickups",Private).GetValue(world);int rewards=queue.Count;
            Need(talk.StartConversation(),"Pending repeat failed");Finish(world);Need(queue.Count==rewards,"Pending repeat granted duplicate key");
            report.Add("PASS electronic record gating, interruption rollback, final-confirmation key reward, pending repeat without duplicate reward");
            var power=new WarehousePowerState{LightingCut=true,FreightCut=true,FreightHeld=true};power.ConnectLightingTerminal();power.Save(world);
            Need(world.HasItem(WarehousePowerState.FreightPower)&&world.HasItem(ManagerAnetaConversation.PowerRestoredFlag)&&talk.CurrentSequence==talk.powerRestored,"Existing freight wiring did not advance quest");
            power.FreightConnected=false;power.Save(world);Need(talk.CurrentSequence==talk.powerRestored,"Restored-power milestone was lost after later disconnection");
            Need(talk.StartConversation(),"Power report did not start");Finish(world);Need(world.HasItem(ManagerAnetaConversation.ReadyFlag),"Manager did not become ready to discuss Aneta");
            report.Add("PASS existing cable-routing power state, persistent restoration milestone, return-to-manager handoff");
            world.RememberProgress(RecoilPrototype.WarehouseRescueStarted);Need(ManagerAnetaConversation.Objective(world)==null,"Manager objective displaced child rescue");
            world.SetProgressFlag(RecoilPrototype.WarehouseRescueStarted,false);
            var inventory=(HashSet<string>)typeof(RecoilPrototype).GetField("inventory",Private).GetValue(world);inventory.Clear();world.RememberProgress(ManagerAnetaConversation.EvidenceFlag);world.RememberProgress(WarehousePowerState.FreightPower);Arm(world);
            Need(talk.CurrentSequence==talk.requestAlreadyPowered&&talk.StartConversation(),"Out-of-order restored power was not handled");Finish(world);
            Need(world.HasItem(ManagerAnetaConversation.ReadyFlag)&&world.HasItem(ManagerAnetaConversation.StoreroomKey),"Already-powered route did not complete handoff");
            var retained=typeof(RecoilPrototype).GetMethod("RetainedOnRetry",BindingFlags.Static|BindingFlags.NonPublic);
            foreach(var flag in new[]{ManagerAnetaConversation.EvidenceFlag,ManagerAnetaConversation.DenialFlag,ManagerAnetaConversation.AssignmentFlag,ManagerAnetaConversation.StoreroomKey,ManagerAnetaConversation.PowerRestoredFlag,ManagerAnetaConversation.ReadyFlag,ManagerGlassesPickup.InventoryId})Need((bool)retained.Invoke(null,new object[]{flag}),"Retry lost "+flag);
            report.Add("PASS child rescue objective priority, already-powered path, all investigation/key flags retained on retry");
        }finally{EditorSceneManager.ClosePreviewScene(preview);}
        var original=SceneManager.GetActiveScene();var opened=new List<Scene>();
        try{
            foreach(var prefix in new[]{"Assets/Scenes/3D/","Assets/Scenes/"}){
                Func<string,Scene> open=path=>{var scene=SceneManager.GetSceneByPath(path);if(!scene.isLoaded){scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);opened.Add(scene);}return scene;};
                var office=open(prefix+"FactoryRooms/FactoryManagerOfficeF4.unity");var actor=All<ManagerAnetaConversation>(office).Single();Need(actor.world&&actor.headAnchor&&actor.interactionPoint&&actor.CurrentSequence==actor.denial,"Saved manager actor not configured");
                Need(!All<ContinuousInteractionItem>(office).Any(item=>item.itemId=="factory_manager_warehouse_key"),"Legacy free-key interaction remains");
                var outside=open(prefix+"RecoilPrototype.unity");Need(All<ElectricalPanelInteraction>(outside).Length==0,"Distribution box remains outside");
                var store=open(prefix+"RecoilWarehouseStoreroom.unity");var panel=All<ElectricalPanelInteraction>(store).Single();Need(panel.GetComponentInParent<RecoilPrototype>()&&panel.isActiveAndEnabled,"Storeroom panel inactive or unparented");
                var warehouse=open(prefix+"RecoilFactoryWarehouse.unity");Need(All<RecoilDoor>(warehouse).Any(d=>d.destinationScene.Contains("RecoilWarehouseStoreroom")&&d.requiredItemId==ManagerAnetaConversation.StoreroomKey),"Storeroom door does not use awarded key");
                report.Add("PASS saved "+prefix+": manager stage references, no free-key item, single panel inside storeroom, keyed warehouse door");
            }
            var record=AssetDatabase.LoadAssetAtPath<ArchivePersonRecord>("Assets/Resources/RecoilInformation/archive_aneta.asset");
            Need(record.ProgressFlag==ManagerAnetaConversation.EvidenceFlag&&record.Transcript.Contains("EMPLOYMENT RECORD: CONFIRMED")&&!record.Transcript.ToLowerInvariant().Contains("signature"),"Electronic evidence content mismatch");
            report.Add("PASS 1978 electronic employment record is evidence; no manager signature");
        }finally{if(original.IsValid()&&original.isLoaded)SceneManager.SetActiveScene(original);foreach(var scene in opened.AsEnumerable().Reverse())if(scene.isLoaded)EditorSceneManager.CloseScene(scene,true);}
        Directory.CreateDirectory("Captures/ManagerAnetaQuest");File.WriteAllLines("Captures/ManagerAnetaQuest/Verification.txt",report);return string.Join("\n",report);
    }
}
