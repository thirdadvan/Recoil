using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class StoreClerkConversationVerification
{
    const string Folder="Captures/StoreClerkConversation/";
    static IEnumerator routine;
    static int lastFrame;
    static readonly List<string> report=new List<string>();
    static StoreClerkConversation clerk;
    static RecoilPrototype world;
    static bool playerEnabled;
    static void Need(bool ok,string message){if(!ok)throw new Exception(message);}
    static void Step()
    {
        if(!EditorApplication.isPlaying){EditorApplication.update-=Step;routine=null;return;}
        if(lastFrame==Time.frameCount)return;lastFrame=Time.frameCount;
        try{if(!routine.MoveNext()){EditorApplication.update-=Step;routine=null;if(world)world.Player.enabled=playerEnabled;File.WriteAllLines(Folder+"Verification.txt",report);Debug.Log("Clerk conversation PASS\n"+string.Join("\n",report));}}
        catch(Exception e){EditorApplication.update-=Step;routine=null;if(world)world.Player.enabled=playerEnabled;report.Add("FAIL "+e);File.WriteAllLines(Folder+"Verification.txt",report);Debug.LogException(e);}
    }
    public static string Run()
    {
        Need(EditorApplication.isPlaying && routine==null,"Run once in Play mode.");Directory.CreateDirectory(Folder);report.Clear();lastFrame=-1;routine=Verify();EditorApplication.update+=Step;return "Clerk verification started";
    }
    static void ClearStory()
    {
        world.SetProgressFlag(StoreClerkConversation.CompletedFlag,false);world.SetProgressFlag(StoreClerkConversation.ManagerLocationFlag,false);world.SetProgressFlag("water_gun",false);world.SetProgressFlag("water_gun_unequipped",false);
        typeof(StoreClerkConversation).GetMethod("RefreshDisplay",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(clerk,null);
    }
    static IEnumerator Verify()
    {
        clerk=UnityEngine.Object.FindObjectOfType<StoreClerkConversation>();Need(clerk!=null,"Missing clerk conversation");world=clerk.world;
        playerEnabled=world.Player.enabled;world.Player.enabled=false;
        typeof(RecoilPrototype).GetField("started",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(world,true);
        if(clerk.rescue)world.RememberProgress(ServiceStoreEncounter.CompletedFlag);
        world.startWithWaterGun=false;ClearStory();world.Player.Position=clerk.interactionPoint.position;
        world.ViewCamera.GetComponent<RecoilCameraController>().SnapToTarget();yield return null;
        Need(clerk.InRange,"Clerk not interactable from counter front");
        Need(clerk.tableGunDisplay && !clerk.tableGunDisplay.GetComponent<ContinuousInteractionItem>(),"Old pickup interaction still present");
        Need(clerk.tableGunDisplay.GetComponentInChildren<RetroWaterBlaster>(true),"Latest gun display missing");
        report.Add("PASS clerk interaction point, no table pickup component, latest modular gun display");
        // Hearing the promise alone does not grant the gun; the final conversation confirmation does.
        Need(clerk.StartConversation(),"Could not start interruption test");while(world.ConversationPreparing)yield return null;
        world.RevealConversationText();world.AdvanceConversation();
        float textDeadline=Time.unscaledTime+(world.CurrentConversationText??"").Length/30f+.2f;
        while(Time.unscaledTime<textDeadline)yield return null;
        Need(world.ConversationTextComplete && world.ConversationHasChoices && !world.WaitingForConversationChoice,"Text completion automatically opened options");
        Need(!world.SelectConversationChoice(0),"Choice accepted before the sentence was confirmed");
        ScreenCapture.CaptureScreenshot(Folder+"SentenceAwaitingConfirmation.png");yield return null;
        int sentenceIndex=world.ConversationLineIndex;world.AdvanceConversation();
        Need(world.WaitingForConversationChoice && world.ConversationLineIndex==sentenceIndex,"Opening options also selected or skipped dialogue");
        ScreenCapture.CaptureScreenshot(Folder+"OptionsAfterConfirmation.png");yield return null;
        report.Add("PASS natural text completion waits for confirmation; first confirmation only opens choices");
        while(world.ConversationActive && !world.CurrentConversationText.Contains("left this water gun for you")){
            world.RevealConversationText();if(world.WaitingForConversationChoice)world.SelectConversationChoice(0);else world.AdvanceConversation();
        }
        Need(world.ConversationActive,"Gift promise line not found");
        Need(!world.OwnsWaterGun,"Gun granted before final goodbye");world.CancelConversationWith(clerk.headAnchor);yield return null;
        Need(!world.OwnsWaterGun && !world.HasItem(StoreClerkConversation.CompletedFlag) && clerk.tableGunDisplay.activeSelf,"Interrupted conversation granted reward");
        report.Add("PASS interruption after gun promise does not grant gun or completion flags");
        for(int choice=0;choice<2;choice++){
            ClearStory();yield return null;Need(clerk.StartConversation(),"Could not start branch "+choice);
            while(world.ConversationPreparing)yield return null;
            var transcript=new List<string>();int photos=0,choices=0;
            while(world.ConversationActive){
                string text=world.CurrentConversationText;transcript.Add(text);
                Need(!world.OwnsWaterGun && clerk.tableGunDisplay.activeSelf,"Reward granted during dialogue");
                if(world.ConversationOfferingPhoto){photos++;Need(text=="I'm looking for someone.","Wrong photo cue line");
                    float until=Time.unscaledTime+.4f;while(Time.unscaledTime<until)yield return null;
                    var presentation=world.Player.GetComponentInChildren<DawnPlayerAnimation>();
                    if(presentation){
                        var hand=presentation.animator.GetBoneTransform(HumanBodyBones.RightHand);
                        var card=hand.Find("Conversation photograph - handed to clerk");
                        Need(card && card.gameObject.activeSelf && Vector3.Distance(card.position,hand.position)<.12f,"Photo did not stay in the hand");
                    }
                    if(choice==0){ScreenCapture.CaptureScreenshot(Folder+"PhotoOffer.png");yield return null;}
                }
                world.RevealConversationText();
                if(world.ConversationHasChoices && !world.WaitingForConversationChoice){
                    int index=world.ConversationLineIndex;
                    Need(!world.SelectConversationChoice(choice),"Choice accepted before confirming the NPC sentence");
                    world.AdvanceConversation();
                    Need(world.WaitingForConversationChoice && world.ConversationLineIndex==index,"Confirmation did not open choices without selecting one");
                }
                if(world.WaitingForConversationChoice){choices++;Need(choices==1,"Options repeated instead of merging into photo topic");
                    if(choice==0){ScreenCapture.CaptureScreenshot(Folder+"ClerkChoices.png");yield return null;}
                    var chosen=clerk.firstConversation.lines[1].choices[choice];
                    Need(world.SelectConversationChoice(choice),"Choice rejected");
                    Need(world.CurrentConversationText==chosen.text && !world.ConversationTextComplete,"Selection did not type Dawn full line");
                }else world.AdvanceConversation();
            }
            Need(photos==1 && choices==1,"Photo conversation did not occur exactly once");
            Need(transcript[0]=="Thank you for saving me back there." && transcript[1]=="We don't get many strangers here.","Opening changed");
            int photoIndex=transcript.IndexOf("I'm looking for someone.");
            if(choice==1){int help=transcript.IndexOf("Someone always needs a little help.");Need(help>=0 && photoIndex==help+1,"Choice 2 did not proceed directly to choice 1");}
            Need(transcript[photoIndex+1]=="Aneta. I know her. She is well known around this town." && transcript[photoIndex+2]=="Well known?","Photo topic did not merge correctly");
            Need(transcript.Exists(t=>t.Contains("fourth floor of the factory")),"Manager location missing");
            Need(transcript[transcript.Count-1]=="The town is dangerous these days. Please be careful.","Final safety line missing");
            Need(world.HasItem("water_gun") && world.OwnsWaterGun && world.HasItem(StoreClerkConversation.CompletedFlag) && world.HasItem(StoreClerkConversation.ManagerLocationFlag),"End reward or progress flags missing");
            Need(!clerk.tableGunDisplay.activeSelf,"Display gun did not disappear after handover");
            Need(world.PickupOpen,"Gun acquisition page did not open");
            File.WriteAllLines(Folder+"Branch"+(choice+1)+".txt",transcript);
            if(choice==1){ScreenCapture.CaptureScreenshot(Folder+"GunAcquired.png");yield return null;}
            world.CloseInventory();while(!world.PlayerControlsAvailable)yield return null;
            report.Add("PASS choice "+(choice+1)+": exact photo merge, all story lines, final-confirmation reward, pickup page, hidden table display");
            // Repeated interaction gives a brief reminder, never another reward or acquisition page.
            Need(clerk.StartConversation(),"Reminder failed to start");while(world.ConversationPreparing)yield return null;
            while(world.ConversationActive){world.RevealConversationText();world.AdvanceConversation();}yield return null;
            Need(world.HasItem("water_gun") && !world.PickupOpen,"Reminder gave a duplicate reward");
        }
        report.Add("PASS repeat conversation uses reminder; no duplicate gun or pickup page");
        var retained=typeof(RecoilPrototype).GetMethod("RetainedOnRetry",BindingFlags.Static|BindingFlags.NonPublic);
        Need((bool)retained.Invoke(null,new object[]{StoreClerkConversation.CompletedFlag}) && (bool)retained.Invoke(null,new object[]{StoreClerkConversation.ManagerLocationFlag}),"Story progress lost on retry");
        report.Add("PASS dialogue completion and manager clue retained on retry");
        // A fresh opening remains in the Game view for review; no disk save is modified.
        ClearStory();yield return null;Need(clerk.StartConversation(),"Final preview failed");
    }
}
