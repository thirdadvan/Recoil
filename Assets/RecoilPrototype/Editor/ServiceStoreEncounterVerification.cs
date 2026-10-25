using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

// Runs against disposable preview clones: preserves loaded scenes and unsaved edits.
public static class ServiceStoreEncounterVerification
{
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Set(object o,string name,object value){o.GetType().GetField(name,Flags).SetValue(o,value);}
    static object Call(object o,string name,params object[] args){return o.GetType().GetMethod(name,Flags).Invoke(o,args);}
    static int assertions;
    static void Check(bool value,string message){if(!value)throw new Exception(message);assertions++;}
    static void FinishDialogue(RecoilPrototype world)
    {while(world.ConversationActive)world.AdvanceConversation();Set(world,"conversationInputFrame",-1);}
    public static string Run()
    {
        assertions=0;
        var source=SceneManager.GetSceneByPath("Assets/Scenes/RecoilServiceStore.unity");
        if(!source.isLoaded)throw new Exception("Open store additively first.");
        var preview=EditorSceneManager.NewPreviewScene();
        try
        {
            foreach(var original in source.GetRootGameObjects())
            {var clone=UnityEngine.Object.Instantiate(original);SceneManager.MoveGameObjectToScene(clone,preview);}
            var roots=preview.GetRootGameObjects();var world=roots.SelectMany(r=>r.GetComponentsInChildren<RecoilPrototype>()).Single();
            var camera=roots.SelectMany(r=>r.GetComponentsInChildren<Camera>()).Single();
            Set(world,"cam",camera);Set(world,"lighting",world.GetComponentInChildren<RecoilLighting2D>());
            Call(world,"BindScene");Call(world,"ResetRun",true);Set(world,"started",true);
            world.Player.Noise.enabled=false;
            var encounter=world.GetComponentInChildren<ServiceStoreEncounter>();Call(encounter,"Start");
            var enemy=encounter.monster;var player=world.Player;var feedback=player.GetComponent<PlayerGazeFeedback>();
            var hatch=world.GetComponentsInChildren<RecoilDoor>(true).Single(d=>d.name=="Central skylight");
            Check(hatch.startsOpened && !hatch.openingBarrier.enabled && hatch.openVisual.gameObject.activeSelf,"Skylight must be open and physically passable.");
            Call(encounter,"Update");
            Check(world.ConversationActive && !world.Playing && world.ConversationLineIndex==0,"Entry starts modal dialogue.");
            Check(enemy.WatchingPlayer && enemy.PlayerUnderGaze,"Visible monster watches player across store.");
            Check(enemy.GetComponentsInChildren<SpriteRenderer>(true).Any(r=>r.enabled) && !enemy.HideStatusLabel,"Monster starts visibly beside counter.");
            float clock=world.Clock;var position=player.Position;Call(world,"Update");
            Check(world.Clock==clock && world.FrameDelta==0 && player.Position==position,"Conversation freezes world simulation.");
            feedback.Simulate(.1f);float phase=(float)typeof(PlayerGazeFeedback).GetField("beatPhase",Flags).GetValue(feedback);
            feedback.Simulate(.1f);
            Check(feedback.WatcherCount==1 && feedback.Visibility>0 && (float)typeof(PlayerGazeFeedback).GetField("beatPhase",Flags).GetValue(feedback)>phase,"Heartbeat continues while dialogue is paused.");
            Check(Time.timeScale==1 && !AudioListener.pause,"Dialogue leaves global timing and audio alone.");
            world.AdvanceConversation();Check(world.ConversationLineIndex==1,"Advance shows second line.");
            FinishDialogue(world);Check(world.Playing,"Last advance resumes gameplay.");
            player.Simulate(0,0,false,false,.02f);
            for(int i=0;i<24;i++){Set(world,"simTime",world.Clock+.02f);player.Simulate(1,0,false,false,.02f);enemy.Simulate(.02f);}
            Check(enemy.State!=RampagerController.Behaviour.Attack,"Running does not attack before half-second threshold.");
            for(int i=0;i<3;i++){Set(world,"simTime",world.Clock+.02f);player.Simulate(1,0,false,false,.02f);enemy.Simulate(.02f);}
            Call(encounter,"LateUpdate");
            Check(enemy.State==RampagerController.Behaviour.Attack && !enemy.HideStatusLabel && enemy.GetComponentsInChildren<SpriteRenderer>(true).Any(r=>r.enabled),"Visible monster attacks normally after running threshold.");
            Check(enemy.Position.x<12.45f && enemy.Position.x>12,"Attack moves naturally without a spawn teleport.");
            Call(world,"ResetRun",false);world.Player.Noise.enabled=false;Call(encounter,"Update");
            Check(world.ConversationActive && enemy.GetComponentsInChildren<SpriteRenderer>(true).Any(r=>r.enabled),"Retry restores visible monster.");
            FinishDialogue(world);player.Simulate(0,0,false,false,.02f);
            camera.aspect=16f/9;
            // Being close enough alone must not trigger a line while monster is off camera.
            player.Position=new Vector2(5,.825f);camera.transform.position=new Vector3(-20,2.8f,-10);Call(encounter,"Update");
            Check(!world.ConversationActive,"Wait until monster is in frame.");player.Position=new Vector2(2,.825f);
            for(int i=0;i<800 && !world.ConversationActive;i++)
            {
                Set(world,"simTime",world.Clock+.02f);player.Simulate(1,0,false,false,.02f,false,true);enemy.Simulate(.02f);camera.transform.position=new Vector3(player.Position.x+1.5f,2.8f,-10);Call(encounter,"Update");
                Check(enemy.State!=RampagerController.Behaviour.Attack,"Stealth approach must remain safe.");
            }
            Check(world.ConversationActive && enemy.State==RampagerController.Behaviour.Sleeping && !enemy.enabled,"Visible approach starts reassurance and sleeps monster.");
            Check(Mathf.Abs(player.Position.x-encounter.approachPoint.position.x)<=5*player.StandingHeight && player.Position.x<6,"Reassurance begins about five body heights from counter.");
            Check(feedback.Visibility==0,"Reassurance removes heartbeat.");
            var visibleBody=enemy.GetComponentsInChildren<SpriteRenderer>(true).First(r=>r.enabled);
            Check(visibleBody.color.a==1,"Sleep remains visible before fading.");
            Call(encounter,"AdvanceFade",encounter.sleepBeforeFade+encounter.fadeDuration*.5f);
            Check(visibleBody.enabled && visibleBody.color.a>.4f && visibleBody.color.a<.6f,"Sleeping monster gradually fades during dialogue.");
            Call(world,"UpdateVisuals",0f);Call(encounter,"AdvanceFade",0f);
            Check(visibleBody.color.a<.6f,"World visuals cannot overwrite fade opacity.");
            Call(encounter,"AdvanceFade",encounter.fadeDuration);
            Check(!enemy.GetComponentsInChildren<SpriteRenderer>(true).Any(r=>r.enabled),"Fade finishes with all monster art hidden.");
            FinishDialogue(world);Check(world.HasItem(encounter.CompletionFlag),"Completed tutorial is recorded.");
            Call(world,"ResetRun",false);Check(enemy.State==RampagerController.Behaviour.Sleeping && !enemy.enabled,"Retry preserves completion and sleeping monster.");
            return assertions+" checks passed: visible spawn, distance/viewport trigger, sleep fade during dialogue, retry, skylight.";
        }
        finally
        {
            foreach(var root in preview.GetRootGameObjects())UnityEngine.Object.DestroyImmediate(root);
            EditorSceneManager.ClosePreviewScene(preview);
        }
    }
}
