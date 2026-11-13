using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEditor;

public static class DoorDialogueVerification
{
    const string Folder="Captures/DoorDialogue/";
    static IEnumerator routine;
    static int lastFrame;
    static RecoilPrototype world;
    static bool playerEnabled;
    static readonly List<string> report=new List<string>();
    static void Need(bool ok,string text){if(!ok)throw new Exception(text);}
    public static string Run(){Need(EditorApplication.isPlaying && routine==null,"Play mode required");Directory.CreateDirectory(Folder);report.Clear();lastFrame=-1;routine=Verify();EditorApplication.update+=Step;return "Door dialogue verification started";}
    static void Step(){
        if(!EditorApplication.isPlaying){EditorApplication.update-=Step;routine=null;return;}
        if(lastFrame==Time.frameCount)return;lastFrame=Time.frameCount;
        try{if(routine.MoveNext())return;report.Add("ALL CHECKS PASSED");Finish();}
        catch(Exception e){report.Add("FAIL "+e);Debug.LogException(e);if(world && world.ConversationActive)typeof(RecoilPrototype).GetMethod("CancelConversation",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(world,null);Finish();}
    }
    static void Finish(){EditorApplication.update-=Step;routine=null;if(world)world.Player.enabled=playerEnabled;File.WriteAllLines(Folder+"Verification.txt",report);Debug.Log(string.Join("\n",report));}
    static void ClearRoutes(){foreach(var flag in new[]{DoorDialogue.PeacefulFlag,DoorDialogue.ThreatFlag,DoorDialogue.SilentFlag})world.SetProgressFlag(flag,false);}
    static IEnumerator Verify(){
        var store=UnityEngine.Object.FindObjectOfType<DoorDialogue>();Need(store && store.role==DoorDialogue.DoorRole.Store,"Open outdoor scene first");world=store.world;
        playerEnabled=world.Player.enabled;world.Player.enabled=false;
        typeof(RecoilPrototype).GetField("started",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(world,true);
        foreach(var e in UnityEngine.Object.FindObjectsOfType<RampagerController>())e.enabled=false;
        foreach(var e in UnityEngine.Object.FindObjectsOfType<ServiceStoreOutdoorEncounter>())e.enabled=false;
        world.SetProgressFlag(DoorDialogue.StoreFlag,false);world.Player.Position=store.door.InteractionPosition+Vector2.left*2.5f;
        world.ViewCamera.GetComponent<RecoilCameraController>().SnapToTarget();yield return null;yield return null;
        Need(world.ConversationActive && world.ConversationPausesWorld && Time.timeScale==0,"Approach did not start paused door warning");
        float clock=world.Clock;float deadline=Time.unscaledTime+1.1f;while(Time.unscaledTime<deadline)yield return null;
        Need(world.Clock==clock && !world.Playing && !world.PlayerControlsAvailable,"World clock advanced during paused invitation");
        ScreenCapture.CaptureScreenshot(Folder+"StoreInvitation.png");yield return null;
        world.RevealConversationText();world.AdvanceConversation();yield return null;
        Need(!world.ConversationActive && Time.timeScale==1 && world.HasItem(DoorDialogue.StoreFlag) && !store.BlocksEntry(world),"Invitation did not restore time/unlock entry");
        report.Add("PASS automatic proximity invitation; world clock frozen; unscaled dialogue; confirmation restores time and entry; one-time flag");
        world.SetProgressFlag(DoorDialogue.StoreFlag,false);Need(store.StartConversation(),"Cancel test failed");world.CancelConversationWith(store.voiceAnchor);
        Need(Time.timeScale==1 && !world.HasItem(DoorDialogue.StoreFlag),"Cancelled warning left time paused or marked complete");world.RememberProgress(DoorDialogue.StoreFlag);yield return null;
        report.Add("PASS interruption restores time and leaves invitation replayable");
        var go=new GameObject("Verification manager door");go.transform.position=world.Player.WorldPosition;
        var door=go.AddComponent<RecoilDoor>();door.requiresClue=false;door.completesLevel=false;door.interactionOffset=Vector2.zero;
        var manager=go.AddComponent<DoorDialogue>();manager.role=DoorDialogue.DoorRole.Manager;manager.world=world;manager.door=door;
        var anchor=new GameObject("Verification voice").transform;anchor.SetParent(go.transform,false);anchor.localPosition=Vector3.right*1.5f+Vector3.up*.7f;manager.voiceAnchor=anchor;
        manager.opening=AssetDatabase.LoadAssetAtPath<NpcDialogueSequence>("Assets/RecoilPrototype/Dialogue/ManagerDoor_FirstConversation.asset");
        manager.peacefulRepeat=AssetDatabase.LoadAssetAtPath<NpcDialogueSequence>("Assets/RecoilPrototype/Dialogue/ManagerDoor_RepeatPeaceful.asset");
        manager.threatRepeat=AssetDatabase.LoadAssetAtPath<NpcDialogueSequence>("Assets/RecoilPrototype/Dialogue/ManagerDoor_RepeatThreat.asset");
        manager.silentRepeat=AssetDatabase.LoadAssetAtPath<NpcDialogueSequence>("Assets/RecoilPrototype/Dialogue/ManagerDoor_RepeatSilent.asset");
        world.SetProgressFlag("manager_glasses",false);world.RememberProgress(RecoilPrototype.UniversalKey);
        Need(!manager.BlocksEntry(world) && door.TryInteract(world) && door.IsOpen,"Universal key must bypass the manager story lock");
        world.SetProgressFlag(RecoilPrototype.UniversalKey,false);door.ResetDoor(world);
        for(int route=0;route<3;route++){
            ClearRoutes();yield return null;Need(door.TryInteract(world) && world.ConversationActive && !door.IsOpen,"Story lock failed without universal key");while(world.ConversationPreparing)yield return null;
            clock=world.Clock;deadline=Time.unscaledTime+.15f;while(Time.unscaledTime<deadline)yield return null;
            Need(world.Clock>clock && Time.timeScale==1,"Manager dialogue paused world");
            var transcript=new List<string>();int choiceCount=0;bool photo=false;
            while(world.ConversationActive){
                transcript.Add(world.CurrentConversationText);world.RevealConversationText();
                if(world.ConversationHasChoices){
                    Need(!world.WaitingForConversationChoice,"Choices opened before sentence confirmation");yield return null;
                    var before=world.ConversationMessageRects;world.AdvanceConversation();yield return null;
                    Need(world.WaitingForConversationChoice,"Confirmation did not open options");var after=world.ConversationMessageRects;
                    Need(before.Length==after.Length,"Options changed history count");for(int i=0;i<before.Length;i++)Need(before[i]==after[i],"Options moved existing bubbles");
                    if(route==1 && choiceCount==1){ScreenCapture.CaptureScreenshot(Folder+"ManagerChoices.png");yield return null;}
                    int selected=choiceCount++==0?(route==2?1:0):(route==1?1:0);
                    var chosen=choiceCount==1?manager.opening.lines[0].choices[selected]:manager.opening.lines[0].choices[0].continuation.lines[0].choices[selected];

                    Need(world.SelectConversationChoice(selected),"Branch selection failed");
                    Need(world.CurrentConversationText==chosen.text && !world.ConversationTextComplete,"Selection did not type Dawn full line");
                }else{
                    if(world.ConversationSlidingPhoto){photo=true;deadline=Time.unscaledTime+.75f;while(Time.unscaledTime<deadline)yield return null;Need(GameObject.Find("Dawn slides photograph under manager door"),"Photo slide cue missing");ScreenCapture.CaptureScreenshot(Folder+"PhotoUnderDoor.png");yield return null;}
                    world.AdvanceConversation();
                }
            }
            Need(choiceCount==(route==2?1:2),"Wrong nested choice count");Need(photo==(route==1),"Wrong photo action branch");
            if(route==2)Need(transcript.FindAll(t=>t=="(Silence.)").Count==4,"Silence sequence incorrect");
            string repeat=route==0?manager.peacefulRepeat.lines[0].text:route==1?manager.threatRepeat.lines[0].text:manager.silentRepeat.lines[0].text;
            yield return null;Need(manager.StartConversation(),"Repeat failed");Need(world.CurrentConversationText==repeat,"Wrong repeat starting point");world.CancelConversationWith(anchor);
            File.WriteAllLines(Folder+"ManagerBranch"+(route+1)+".txt",transcript);report.Add("PASS manager route "+route+": locked without debug key, manual choices, fixed bubble layout, exact repeat start, world continues");
        }
        yield return null;ClearRoutes();Need(manager.StartConversation(),"Early cancel failed");world.CancelConversationWith(anchor);Need(manager.CurrentSequence==manager.opening,"Early interruption changed repeat route");
        world.RememberProgress(ManagerGlassesPickup.InventoryId);Need(manager.BlocksEntry(world),"Glasses bypassed handover");yield return null;Need(door.TryInteract(world)&&world.ConversationActive&&!door.IsOpen,"Handover did not start");while(world.ConversationPreparing)yield return null;
        while(world.ConversationActive){world.RevealConversationText();world.AdvanceConversation();yield return null;}
        Need(world.HasItem(DoorDialogue.GlassesReturnedFlag)&&!world.HasItem(ManagerGlassesPickup.InventoryId),"Handover did not consume glasses");go.GetComponent<ManagerDoorOpening>().Advance(.5f);
        Need(!manager.BlocksEntry(world),"Completed opening did not unlock door");yield return null;Need(door.TryInteract(world) && door.IsOpen,"Unlocked door did not use normal interaction");
        var retained=typeof(RecoilPrototype).GetMethod("RetainedOnRetry",BindingFlags.Static|BindingFlags.NonPublic);foreach(var f in new[]{DoorDialogue.StoreFlag,DoorDialogue.PeacefulFlag,DoorDialogue.ThreatFlag,DoorDialogue.SilentFlag})Need((bool)retained.Invoke(null,new object[]{f}),"Door flag not retained on retry");
        report.Add("PASS early cancellation, glasses handover, visible opening before entry, retry retention");UnityEngine.Object.Destroy(go);
    }
}
