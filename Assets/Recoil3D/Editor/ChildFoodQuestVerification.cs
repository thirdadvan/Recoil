using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

public static class ChildFoodQuestVerification
{
    static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Need(bool ok,string message){if(!ok)throw new Exception(message);}
    static void Set(object o,string f,object v){o.GetType().GetField(f,Private).SetValue(o,v);}
    static void Arm(RecoilPrototype w){Set(w,"conversationInputFrame",-1);Set(w,"conversationReleasePending",false);Set(w,"inventoryReleasePending",false);Set(w,"conversationReadyAt",-1f);}
    static void Step(RecoilPrototype w){Set(w,"conversationReadyAt",-1f);w.RevealConversationText();w.AdvanceConversation();}
    static void Finish(RecoilPrototype w){Set(w,"inventoryReleasePending",true);int guard=0;while(w.ConversationActive&&guard++<60){Need(!w.WaitingForConversationChoice,"Unexpected extra choice");Step(w);}Need(!w.ConversationActive,"Conversation stuck");Arm(w);}
    static NpcDialogueSequence.Choice[] Choices(RecoilPrototype w){return ((List<NpcDialogueSequence.Line>)typeof(RecoilPrototype).GetField("conversationLines",Private).GetValue(w))[w.ConversationLineIndex].choices;}
    static void Choose(RecoilPrototype w,string id){Set(w,"conversationReadyAt",-1f);Need(w.WaitingForConversationChoice,"Menu not automatic");int index=Array.FindIndex(Choices(w),c=>c.id==id);Need(index>=0&&w.SelectConversationChoice(index),"Missing choice "+id);}
    [MenuItem("Recoil/Dialogue/Verify Child Food And Escort")]
    public static string Run()
    {
        Need(!EditorApplication.isPlayingOrWillChangePlaymode,"Edit mode required");var scene=EditorSceneManager.NewPreviewScene();var waiting=EscortChild.CaptureWaitSave();var report=new List<string>();NpcDialogueSequence source=null;
        try{
            Func<string,GameObject> go=name=>{var g=new GameObject(name);g.hideFlags=HideFlags.HideAndDontSave;SceneManager.MoveGameObjectToScene(g,scene);return g;};
            var w=go("Food verification world").AddComponent<RecoilPrototype>();var p=go("Preview Dawn").AddComponent<RecoilPlayerController>();Set(w,"player",p);Set(w,"started",true);Set(p,"world",w);p.RestoreHealth(3);
            var camera=go("Preview camera").AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=5;camera.transform.position=Vector3.back*10;camera.enabled=false;Set(w,"cam",camera);
            var child=go("Preview child").AddComponent<EscortChild>();child.transform.position=Vector3.right*.65f;child.Initialize(w);child.ResetChild(false);typeof(RecoilPrototype).GetField("<Child>k__BackingField",Private).SetValue(w,child);
            var talk=child.GetComponent<ChildFoodConversation>();var anchor=child.transform.Find("Child dialogue anchor");
            Need(!child.TryAutoTakeHand()&&!child.Following,"Unfed child auto-followed");
            foreach(var id in new[]{"child_who","child_aneta"}){
                Need(talk.StartConversation(),"Child question menu failed");Arm(w);Need(Choices(w).Length==2,"Initial menu should have two questions");Choose(w,id);Need(!w.HasItem(ChildFoodQuest.Fed),"Question fed child");Step(w);
                Need(w.CurrentConversationText=="(Crying.) Hungry."&&w.HasItem(ChildFoodQuest.Hungry),"Hungry response/topic missing");Finish(w);
            }
            report.Add("PASS two immediate child thoughts; both answer (Crying.) Hungry.; Food topic learned; no early hand-holding");
            source=ScriptableObject.CreateInstance<NpcDialogueSequence>();source.lines=new[]{new NpcDialogueSequence.Line{role=NpcDialogueSequence.SpeakerRole.Npc,speaker="Test actor",text="Story dialogue."}};
            int storyRewards=0;var npc=go("Preview food provider").transform;npc.position=Vector3.right;
            Need(ChildFoodQuest.BeginNpc(w,source,()=>storyRewards++,null,npc,ChildFoodQuest.Provider.Manager),"Manager food menu failed");Choose(w,"child_food_request");Finish(w);
            Need(ChildFoodQuest.AvailableFood(w)==null&&storyRewards==0,"Manager gave food or committed unrelated story");
            Need(ChildFoodQuest.BeginNpc(w,source,()=>storyRewards++,null,npc,ChildFoodQuest.Provider.CounterSales),"Clerk food menu failed");Choose(w,"child_food_request");Step(w);w.CancelConversationWith(npc);Arm(w);
            Need(!w.HasItem(ChildFoodQuest.Sandwich)&&!w.HasItem(ChildFoodQuest.SandwichGiven),"Interrupted request granted food");
            foreach(var provider in new[]{ChildFoodQuest.Provider.CounterSales,ChildFoodQuest.Provider.DisguisedMan}){
                Need(ChildFoodQuest.BeginNpc(w,source,()=>storyRewards++,null,npc,provider),"Provider failed");Choose(w,"child_food_request");Finish(w);
            }
            Need(w.HasItem(ChildFoodQuest.Sandwich)&&w.HasItem(ChildFoodQuest.Biscuits)&&storyRewards==0,"Food reward or unrelated story reward incorrect");
            report.Add("PASS manager refuses; clerk gives warm sandwich; disguised man gives damp biscuits; interrupted requests grant nothing; unrelated story rewards untouched");
            var ui=new RecoilInventoryUI(w);typeof(RecoilInventoryUI).GetMethod("BuildItems",Private).Invoke(ui,null);var shown=(List<RecoilInventoryUI.Item>)typeof(RecoilInventoryUI).GetField("items",Private).GetValue(ui);Need(shown.Any(i=>i.id==ChildFoodQuest.Sandwich)&&shown.Any(i=>i.id==ChildFoodQuest.Biscuits),"Foods missing from inventory UI");ui.Dispose();
            Need(talk.StartConversation(),"Feeding menu failed");Choose(w,"child_feed");Step(w);Need(w.CurrentConversationText=="(Eats ravenously.)","Missing eating bubble");w.CancelConversationWith(anchor);Arm(w);
            Need(w.HasItem(ChildFoodQuest.Sandwich)&&!w.HasItem(ChildFoodQuest.Fed),"Interrupted feeding consumed food");
            Need(talk.StartConversation(),"Feeding retry failed");Choose(w,"child_feed");Finish(w);
            Need(w.HasItem(ChildFoodQuest.Fed)&&!w.HasItem(ChildFoodQuest.Sandwich)&&w.HasItem(ChildFoodQuest.Biscuits)&&!child.Following&&!child.TryAutoTakeHand(),"Feeding consumed wrong items or started escort early");
            Need(ChildFoodQuest.BeginNpc(w,source,null,null,npc,ChildFoodQuest.Provider.CounterSales),"Repeat food menu failed");Choose(w,"child_food_request");Finish(w);Need(!w.HasItem(ChildFoodQuest.Sandwich),"Repeated food request duplicated reward");
            report.Add("PASS food appears in inventory; eating bubble; interrupted feeding rollback; sandwich used first and only one item consumed; no duplicate food rewards");
            Need(ChildFoodQuest.BeginNpc(w,source,()=>storyRewards++,null,npc,ChildFoodQuest.Provider.Manager),"Story menu failed");Choose(w,"child_food_story");Finish(w);Need(storyRewards==1,"Story route lost its completion callback");
            foreach(var id in new[]{"child_who","child_aneta"}){
                Need(talk.StartConversation(),"Fed child menu failed");Arm(w);Need(Choices(w).Length==2,"Fed child questions missing");Choose(w,id);Step(w);Need(!w.CurrentConversationText.Contains("Hungry"),"Fed child still cannot answer");Step(w);Need(w.CurrentConversationText.Contains("leave")&&!child.Following,"Leave request or escort timing wrong");
                if(id=="child_who"){w.CancelConversationWith(anchor);Arm(w);Need(!w.HasItem(ChildFoodQuest.Ready)&&!child.Following,"Interrupted invitation started escort");}
                else Finish(w);
            }
            Need(w.HasItem(ChildFoodQuest.Ready)&&child.Following&&w.EscortingChild&&w.HasItem(EscortChild.EscortFlag),"Final confirmation did not take hand");
            report.Add("PASS both fed-child placeholder answers; asks to leave; interruption does not escort; final confirmation automatically takes Dawn's hand; original story completion preserved");
            // Verify the lower-quality alternative independently.
            var inventory=(HashSet<string>)typeof(RecoilPrototype).GetField("inventory",Private).GetValue(w);inventory.Clear();w.RememberProgress(ChildFoodQuest.Biscuits);child.ResetChild(false);Arm(w);
            Need(talk.StartConversation(),"Biscuits-only menu failed");Choose(w,"child_feed");Finish(w);Need(w.HasItem(ChildFoodQuest.Fed)&&!w.HasItem(ChildFoodQuest.Biscuits),"Biscuits cannot feed child");
            var retained=typeof(RecoilPrototype).GetMethod("RetainedOnRetry",BindingFlags.Static|BindingFlags.NonPublic);foreach(var f in new[]{ChildFoodQuest.Hungry,ChildFoodQuest.Fed,ChildFoodQuest.Ready,ChildFoodQuest.Sandwich,ChildFoodQuest.Biscuits,ChildFoodQuest.SandwichGiven,ChildFoodQuest.BiscuitsGiven,ChildFoodQuest.SandwichConsumed,ChildFoodQuest.BiscuitsConsumed})Need((bool)retained.Invoke(null,new object[]{f}),"Retry loses "+f);
            w.RememberProgress(ChildFoodQuest.Biscuits);w.RememberProgress(ChildFoodQuest.Sandwich);ChildFoodQuest.RestoreConsumption(w);Need(!w.HasItem(ChildFoodQuest.Biscuits)&&w.HasItem(ChildFoodQuest.Sandwich),"Scene-entry inventory restored consumed food or removed unconsumed food");
            report.Add("PASS biscuits also satisfy hunger; topic, food, consumption and escort milestones persist across retry");
        }finally{EditorSceneManager.ClosePreviewScene(scene);EscortChild.RestoreWaitSave(waiting);if(source)Object.DestroyImmediate(source);}
        System.IO.Directory.CreateDirectory("Captures/ChildFoodQuest");System.IO.File.WriteAllLines("Captures/ChildFoodQuest/Verification.txt",report);return string.Join("\n",report);
    }
}
