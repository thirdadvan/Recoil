using System.Collections.Generic;
using UnityEngine;

// Adds the learned Food topic without changing authored story dialogue or its rewards.
public sealed class ChildFoodTopics : MonoBehaviour
{
    readonly List<NpcDialogueSequence> owned=new List<NpcDialogueSequence>();
    RecoilPrototype world;System.Action storyCompleted;System.Action<int> storyLineShown;
    bool foodSelected,storySelected;ChildFoodQuest.Provider provider;
    NpcDialogueSequence New(params NpcDialogueSequence.Line[] lines){var s=ScriptableObject.CreateInstance<NpcDialogueSequence>();s.hideFlags=HideFlags.HideAndDontSave;s.lines=lines;owned.Add(s);return s;}
    public bool Begin(RecoilPrototype state,NpcDialogueSequence source,System.Action completed,System.Action<int> lineShown,ChildFoodQuest.Provider who)
    {
        Dispose();world=state;provider=who;storyCompleted=completed;storyLineShown=lineShown;foodSelected=storySelected=false;
        string speaker=who==ChildFoodQuest.Provider.CounterSales?"The Counter Sales":who==ChildFoodQuest.Provider.DisguisedMan?"The Disguised Man":"The Manager";
        bool given=who==ChildFoodQuest.Provider.CounterSales?world.HasItem(ChildFoodQuest.SandwichGiven):world.HasItem(ChildFoodQuest.BiscuitsGiven);
        string reply=who==ChildFoodQuest.Provider.Manager?"Food? Oh... I'm barely managing myself. Please, you must ask someone else.":
            given?"I've already given you what I can. Please take it to the child.":
            who==ChildFoodQuest.Provider.CounterSales?"A hungry child? Wait here. I'll heat up a sandwich. Take it to them while it's warm.":
            "I've got a few biscuits. They're a little damp... but they're all I have. Take them.";
        var response=New(new NpcDialogueSequence.Line{role=NpcDialogueSequence.SpeakerRole.Npc,speaker=speaker,text=reply});
        var food=new NpcDialogueSequence.Choice{id="child_food_request",label="Food",text="There's a hungry child in the factory. Do you have any food?",continuation=response};
        NpcDialogueSequence menu;
        if(source&&source.lines.Length>0&&string.IsNullOrWhiteSpace(source.lines[0].text)&&source.lines[0].choices!=null&&source.lines[0].choices.Length>0){
            var lines=(NpcDialogueSequence.Line[])source.lines.Clone();var choices=new List<NpcDialogueSequence.Choice>(lines[0].choices);choices.Insert(0,food);lines[0].choices=choices.ToArray();menu=New(lines);
        }else{
            var choices=new List<NpcDialogueSequence.Choice>{food};
            if(source)choices.Add(new NpcDialogueSequence.Choice{id="child_food_story",label="Talk",text="There's something else I'd like to ask.",continuation=source});
            choices.Add(new NpcDialogueSequence.Choice{id="child_food_leave",label="Leave",text="I'll come back later."});
            menu=New(new NpcDialogueSequence.Line{role=NpcDialogueSequence.SpeakerRole.Player,speaker="Dawn",text="",choices=choices.ToArray()});
        }
        return world.BeginConversation(menu,Complete,LineShown,transform);
    }
    void LineShown(int index)
    {
        var id=world.LastConversationChoiceId;
        if(id=="child_food_request")foodSelected=true;
        else if(!string.IsNullOrEmpty(id)&&id!="child_food_leave"&&!foodSelected)storySelected=true;
        if(storySelected&&storyLineShown!=null)storyLineShown(index);
    }
    void Complete()
    {
        if(storySelected&&storyCompleted!=null)storyCompleted();
        if(!foodSelected||provider==ChildFoodQuest.Provider.Manager)return;
        bool sandwich=provider==ChildFoodQuest.Provider.CounterSales;string flag=sandwich?ChildFoodQuest.SandwichGiven:ChildFoodQuest.BiscuitsGiven;
        if(world.HasItem(flag))return;
        world.RememberProgress(flag);string id=sandwich?ChildFoodQuest.Sandwich:ChildFoodQuest.Biscuits;
        world.GrantItem(id,sandwich?"Warm sandwich acquired.":"Damp biscuits acquired.",null,null,null);
    }
    void Dispose(){foreach(var s in owned)if(s){if(Application.isPlaying)Destroy(s);else DestroyImmediate(s);}owned.Clear();}
    void OnDisable(){if(world)world.CancelConversationWith(transform);}
    void OnDestroy(){Dispose();}
}
