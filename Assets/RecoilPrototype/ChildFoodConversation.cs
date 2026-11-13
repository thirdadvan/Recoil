using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-750),DisallowMultipleComponent]
public sealed class ChildFoodConversation : MonoBehaviour
{
    RecoilPrototype world;EscortChild child;Transform anchor;string offeredFood;
    readonly List<NpcDialogueSequence> owned=new List<NpcDialogueSequence>();
    public void Initialize(RecoilPrototype state,EscortChild actor)
    {
        world=state;child=actor;anchor=transform.Find("Child dialogue anchor");
        if(!anchor){anchor=new GameObject("Child dialogue anchor").transform;anchor.SetParent(transform,false);anchor.localPosition=Vector3.up*child.bodyHeight*.48f;}
    }
    NpcDialogueSequence New(params NpcDialogueSequence.Line[] lines){var s=ScriptableObject.CreateInstance<NpcDialogueSequence>();s.hideFlags=HideFlags.HideAndDontSave;s.lines=lines;owned.Add(s);return s;}
    static NpcDialogueSequence.Line Say(string text,string flag="",bool dawn=false){return new NpcDialogueSequence.Line{role=dawn?NpcDialogueSequence.SpeakerRole.Player:NpcDialogueSequence.SpeakerRole.Npc,speaker=dawn?"Dawn":"The Child",text=text,progressFlag=flag};}
    NpcDialogueSequence.Choice Question(string id,string label,string question,string answer)
    {
        bool fed=world.HasItem(ChildFoodQuest.Fed);
        var reply=fed?New(Say(answer),Say("I want to leave... Please take me out of this factory."),Say("All right. Stay close. Take my hand.","",true)):
            New(Say("(Crying.) Hungry.",ChildFoodQuest.Hungry));
        return new NpcDialogueSequence.Choice{id=id,label=label,text=question,continuation=reply};
    }
    public bool StartConversation()
    {
        if(!world||!world.PlayerControlsAvailable||!child||!child.InRange())return false;
        Dispose();offeredFood=null;
        var choices=new List<NpcDialogueSequence.Choice>{
            Question("child_who","Who are you?","Who are you?","I'm... I live near here. I got lost."),
            Question("child_aneta","Aneta?","Do you know Aneta?","Aneta... She helped me once. She was kind to me.")};
        if(!world.HasItem(ChildFoodQuest.Fed)){
            offeredFood=ChildFoodQuest.AvailableFood(world);
            if(!string.IsNullOrEmpty(offeredFood))choices.Add(new NpcDialogueSequence.Choice{id="child_feed",label="Food",text="Here. Eat this.",continuation=New(Say("(Eats ravenously.)"),Say("...Thank you."))});
        }
        var menu=New(new NpcDialogueSequence.Line{role=NpcDialogueSequence.SpeakerRole.Player,speaker="Dawn",text="",choices=choices.ToArray()});
        return world.BeginConversation(menu,Complete,null,anchor);
    }
    void Complete()
    {
        if(world.LastConversationChoiceId=="child_feed"){
            if(!string.IsNullOrEmpty(offeredFood)&&world.HasItem(offeredFood)){world.SetProgressFlag(offeredFood,false);world.RememberProgress(offeredFood==ChildFoodQuest.Sandwich?ChildFoodQuest.SandwichConsumed:ChildFoodQuest.BiscuitsConsumed);world.RememberProgress(ChildFoodQuest.Hungry);world.RememberProgress(ChildFoodQuest.Fed);}
            return;
        }
        if(world.HasItem(ChildFoodQuest.Fed)){
            world.RememberProgress(ChildFoodQuest.Ready);child.TakeHandAfterConversation();
        }
    }
    void Update(){if(world&&world.PlayerControlsAvailable&&!world.ReaderInputConsumed&&RecoilInput.InteractPressed&&child&&child.InRange())StartConversation();}
    void OnGUI(){if(world&&world.PlayerControlsAvailable&&child&&child.InRange())world.DrawConversationTalkPrompt(anchor,RecoilInput.UsingGamepad?"CIRCLE / TALK":"E / TALK");}
    void Dispose(){foreach(var s in owned)if(s){if(Application.isPlaying)Destroy(s);else DestroyImmediate(s);}owned.Clear();}
    void OnDisable(){if(world)world.CancelConversationWith(anchor);}
    void OnDestroy(){Dispose();}
}
