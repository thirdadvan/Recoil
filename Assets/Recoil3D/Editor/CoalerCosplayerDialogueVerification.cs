using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

// Isolated editor preview: no scene switching, no writes to the user's authored scenes.
public static class CoalerCosplayerDialogueVerification
{
    static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Need(bool ok,string text){if(!ok)throw new Exception(text);}
    static void Arm(RecoilPrototype w)
    {
        typeof(RecoilPrototype).GetField("conversationInputFrame",Private).SetValue(w,-1);
        typeof(RecoilPrototype).GetField("conversationReleasePending",Private).SetValue(w,false);
    }
    static void Ready(RecoilPrototype w){typeof(RecoilPrototype).GetField("conversationReadyAt",Private).SetValue(w,-1f);}
    static void Step(RecoilPrototype w){Ready(w);w.RevealConversationText();w.AdvanceConversation();}
    static void Finish(RecoilPrototype w)
    {
        int guard=0;while(w.ConversationActive && guard++<100){Need(!w.ConversationHasChoices,"Unexpected choice in linear finish");Step(w);}Need(!w.ConversationActive,"Conversation did not end");Arm(w);
    }
    static NpcDialogueSequence.Choice[] Choices(RecoilPrototype w)
    {return ((List<NpcDialogueSequence.Line>)typeof(RecoilPrototype).GetField("conversationLines",Private).GetValue(w))[w.ConversationLineIndex].choices;}
    static void OpenChoices(RecoilPrototype w)
    {
        int guard=0;while(!w.ConversationHasChoices && guard++<80)Step(w);
        Need(w.ConversationHasChoices,"Choice node missing");Ready(w);w.RevealConversationText();
        if(string.IsNullOrWhiteSpace(w.CurrentConversationText)){Need(w.WaitingForConversationChoice,"Silent topic menu did not open automatically");w.CalculateConversationBubbleLayout();return;}
        Need(!w.WaitingForConversationChoice,"Menu opened without confirmation");
        w.CalculateConversationBubbleLayout();var before=w.ConversationMessageRects;w.AdvanceConversation();w.CalculateConversationBubbleLayout();var after=w.ConversationMessageRects;
        Need(before.Length==after.Length,"Choice menu removed history");for(int i=0;i<before.Length;i++)Need(before[i]==after[i],"Choice menu moved history");
        Need(w.WaitingForConversationChoice,"Choice menu did not open");
    }
    [MenuItem("Recoil/Dialogue/Verify Cosplayer Topic Progression")]
    public static string Run()
    {
        Need(!EditorApplication.isPlayingOrWillChangePlaymode,"Run in edit mode; checks use a preview scene.");
        var preview=EditorSceneManager.NewPreviewScene();Font font=null;var report=new List<string>();
        try{
            Func<string,GameObject> go=name=>{var g=new GameObject(name);g.hideFlags=HideFlags.HideAndDontSave;SceneManager.MoveGameObjectToScene(g,preview);return g;};
            var world=go("Cosplayer verification world").AddComponent<RecoilPrototype>();var player=go("Preview Dawn").AddComponent<RecoilPlayerController>();
            typeof(RecoilPrototype).GetField("player",Private).SetValue(world,player);typeof(RecoilPrototype).GetField("started",Private).SetValue(world,true);
            typeof(RecoilPlayerController).GetField("pos",Private).SetValue(player,new Vector2(0,-2));
            var camera=go("Preview camera").AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=5;camera.transform.position=Vector3.back*10;camera.enabled=false;typeof(RecoilPrototype).GetField("cam",Private).SetValue(world,camera);
            font=Font.CreateDynamicFontFromOSFont(new[]{"Arial","PingFang SC"},20);var body=new GUIStyle{font=font,fontSize=20,wordWrap=true};
            typeof(RecoilPrototype).GetField("bubbleBody",Private).SetValue(world,body);typeof(RecoilPrototype).GetField("choiceBody",Private).SetValue(world,new GUIStyle(body){fontSize=18});
            var actor=go("Preview disguised man").AddComponent<CoalerCosplayerConversation>();actor.world=world;actor.transform.position=new Vector3(1,-2,0);actor.distanceOrigin=actor.transform;actor.headAnchor=go("Preview disguised man head").transform;actor.headAnchor.position=new Vector3(1,-1.2f,0);
            actor.dialogue=AssetDatabase.LoadAssetAtPath<NpcDialogueSequence>(CoalerCosplayerDialogueSetup.AssetPath);
            var intro=Resources.Load<NpcDialogueSequence>(CoalerCosplayerConversation.IntroductionResource);var reminder=Resources.Load<NpcDialogueSequence>(CoalerCosplayerConversation.ReminderResource);
            Need(intro&&reminder&&actor.dialogue,"Content missing");
            foreach(var line in intro.lines.Concat(reminder.lines)){string text=(line.text??"").ToLowerInvariant();Need(!text.Contains("manager")&&!text.Contains("glasses"),"Girlfriend introduction leaks manager/glasses");}
            Need(!actor.GlassesTopicAvailable&&actor.StartConversation(),"Initial conversation failed");
            int guard=0;while(world.ConversationActive&&guard++<30){Need(!world.ConversationHasChoices,"Glasses topic visible before manager mention");Step(world);}
            Arm(world);Need(world.HasItem(CoalerCosplayerConversation.GirlfriendNameFlag)&&world.HasItem(CoalerCosplayerConversation.IntroductionFlag),"Ewa name/intro flag missing");Need(!world.HasItem(CoalerCosplayerConversation.GlassesLocationFlag),"Girlfriend intro granted roof clue");
            Need(actor.StartConversation(),"Girlfriend repeat failed");Need(world.CurrentConversationText.Contains("Ewa"),"Wrong repeat topic");Finish(world);
            Need(!world.HasItem(CoalerCosplayerConversation.GlassesLocationFlag),"Girlfriend repeat granted roof clue");
            report.Add("PASS first/repeat visits discuss only love and search for Ewa; name learned for archive; no glasses, manager, or roof clue");
            var inventory=(HashSet<string>)typeof(RecoilPrototype).GetField("inventory",Private).GetValue(world);
            foreach(var name in new[]{"ManagerDoor_RepeatPeaceful","ManagerDoor_RepeatThreat","ManagerDoor_RepeatSilent"}){
                inventory.Clear();Arm(world);var opening=AssetDatabase.LoadAssetAtPath<NpcDialogueSequence>("Assets/RecoilPrototype/Dialogue/ManagerDoor_FirstConversation.asset");
                Need(world.BeginConversation(opening,null,null,actor.headAnchor),"Manager opening failed");Need(!actor.GlassesTopicAvailable,"Topic unlocked just by meeting manager");world.CancelConversationWith(actor.headAnchor);Arm(world);
                var manager=AssetDatabase.LoadAssetAtPath<NpcDialogueSequence>("Assets/RecoilPrototype/Dialogue/"+name+".asset");Need(world.BeginConversation(manager,null,null,actor.headAnchor),"Manager glasses line failed");Need(actor.GlassesTopicAvailable,"Manager glasses line did not unlock topic: "+name);world.CancelConversationWith(actor.headAnchor);Arm(world);
                Need(actor.StartConversation(),"Post-manager intro failed");OpenChoices(world);
                var choices=Choices(world);Need(choices.Length==2&&choices[0].id=="glasses_topic"&&choices[0].label=="Glasses","Glasses bubble missing");Need(!world.HasItem(CoalerCosplayerConversation.GlassesLocationFlag),"Opening menu revealed roof");
                Need(world.SelectConversationChoice(1),"Leave choice failed");Finish(world);Need(!world.HasItem(CoalerCosplayerConversation.GlassesLocationFlag),"Leaving granted roof clue");
            }
            report.Add("PASS all three actual manager glasses lines unlock Glasses; initial manager greeting does not; leaving the topic reveals nothing");
            for(int side=0;side<2;side++)for(int branch=0;branch<3;branch++){
                inventory.Clear();world.RememberProgress(DoorDialogue.PeacefulFlag);world.RememberProgress(CoalerCosplayerConversation.IntroductionFlag);Arm(world);
                actor.transform.position=new Vector3(side==0?1:-1,-2,0);actor.headAnchor.position=actor.transform.position+Vector3.up*.8f;
                Need(actor.StartConversation(),"Topic menu failed");OpenChoices(world);
                Need(world.SelectConversationChoice(0)&&world.CurrentConversationText=="Did you take the manager's glasses?"&&!world.ConversationTextComplete,"Glasses did not emit full Dawn question");
                Need(!world.HasItem(CoalerCosplayerConversation.GlassesLocationFlag),"Selected topic revealed roof too early");Step(world);
                Need(world.CurrentConversationText.StartsWith("I did."),"Confession did not follow topic question");
                OpenChoices(world);var tone=Choices(world)[branch];
                Need(Choices(world).Length==3,"Tone branches missing");
                foreach(var r in world.ConversationChoiceRects){Need(r.xMin>=0&&r.yMin>=0&&r.xMax<=world.ConversationCanvasWidth&&r.yMax<=world.ConversationCanvasHeight,"Thought escaped viewport");foreach(var message in world.ConversationMessageRects)Need(!r.Overlaps(message),"Thought overlaps dialogue");}
                Need(!world.HasItem(CoalerCosplayerConversation.GlassesLocationFlag),"Tone menu revealed roof");
                Need(world.SelectConversationChoice(branch)&&world.CurrentConversationText==tone.text&&!world.ConversationTextComplete,"Tone did not emit complete Dawn line");Step(world);Need(world.CurrentConversationText==tone.response[0].text,"Wrong tone response");Step(world);
                Need(world.CurrentConversationText.Contains("warehouse roof")&&world.HasItem(CoalerCosplayerConversation.GlassesLocationFlag),"Roof clue missing at actual reveal");Finish(world);
                report.Add("PASS "+(side==0?"Dawn left":"Dawn right")+" / "+tone.id+": gated topic, typed Dawn lines, confession, tone response, roof reveal, stable thought layout");
            }
            // Terminal menu must open on entry and on return from a query result.
            typeof(RecoilPlayerController).GetField("world",Private).SetValue(player,world);player.RestoreHealth(3);Arm(world);
            var terminal=go("Preview archive terminal").AddComponent<ArchiveTerminalConversation>();terminal.world=world;
            terminal.interactionPoint=go("Preview keyboard").transform;terminal.interactionPoint.position=player.WorldPosition;
            terminal.screenAnchor=go("Preview terminal screen").transform;terminal.screenAnchor.position=player.WorldPosition+Vector3.right+Vector3.up*.8f;
            terminal.anetaRecord=Resources.Load<ArchivePersonRecord>("RecoilInformation/archive_aneta");terminal.girlfriendRecord=Resources.Load<ArchivePersonRecord>("RecoilInformation/archive_ewa");
            Need(terminal.StartConversation(),"Terminal interaction failed");Ready(world);
            Need(world.WaitingForConversationChoice&&string.IsNullOrEmpty(world.CurrentConversationText),"Terminal still requires a preliminary confirmation");
            world.CalculateConversationBubbleLayout();Need(world.ConversationMessageRects.Length==0&&world.ConversationChoiceRects.Length>=2,"Silent terminal rendered an empty speech bubble or no thoughts");
            Need(world.SelectConversationChoice(0)&&world.CurrentConversationText=="Search record: Aneta.","Query selection did not emit Dawn line");
            Need(world.PropOperationBlocksConversation,"Query timer missing");world.PropOperation.Simulate(5.1f);
            Need(world.CurrentConversationText==terminal.anetaRecord.dialogueSummary&&!world.WaitingForConversationChoice,"Result was skipped");
            Step(world);Need(world.WaitingForConversationChoice&&string.IsNullOrEmpty(world.CurrentConversationText),"Returning to query menu requires an extra confirmation");
            world.CalculateConversationBubbleLayout();Need(world.ConversationChoiceRects.Length>=2,"Repeated query thoughts missing");
            world.CancelConversationWith(terminal.screenAnchor);world.PropOperation.Cancel();Arm(world);
            Need(terminal.StartConversation(),"Terminal reopen failed");Ready(world);Need(world.WaitingForConversationChoice,"Reopened terminal menu not automatic");
            Need(world.SelectConversationChoice(Choices(world).Length-1)&&!world.ConversationActive,"Leave terminal did not close dialogue");world.PropOperation.Simulate(.6f);Arm(world);
            report.Add("PASS terminal immediately opens thought-only menu without an empty bubble; query timer/result confirmation preserved; returning/reopening auto-opens choices; leave works");
            Need(intro.lines.All(l=>l.choices==null||l.choices.Length==0)&&reminder.lines.All(l=>l.choices==null||l.choices.Length==0),"Session menu mutated shared templates");
            var retained=typeof(RecoilPrototype).GetMethod("RetainedOnRetry",BindingFlags.Static|BindingFlags.NonPublic);
            foreach(var flag in new[]{CoalerCosplayerConversation.IntroductionFlag,CoalerCosplayerConversation.GirlfriendNameFlag,CoalerCosplayerConversation.GlassesLocationFlag,DoorDialogue.PeacefulFlag,DoorDialogue.ThreatFlag,DoorDialogue.SilentFlag})Need((bool)retained.Invoke(null,new object[]{flag}),"Retry loses "+flag);
            report.Add("PASS templates remain unchanged and dialogue/name/topic/clue progress survives retry");
        }finally{EditorSceneManager.ClosePreviewScene(preview);if(font)UnityEngine.Object.DestroyImmediate(font);}
        Directory.CreateDirectory("Captures/CosplayerDialogue");File.WriteAllLines("Captures/CosplayerDialogue/Verification.txt",report);return string.Join("\n",report);
    }
}
