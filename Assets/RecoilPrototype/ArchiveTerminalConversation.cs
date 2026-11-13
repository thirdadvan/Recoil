using UnityEngine;
using System.Collections.Generic;

[DefaultExecutionOrder(-750),DisallowMultipleComponent]
public sealed class ArchiveTerminalConversation : MonoBehaviour
{
    public RecoilPrototype world;
    public Transform interactionPoint,screenAnchor,keyboard;
    public TextMesh screenText;
    public ArchivePersonRecord anetaRecord,girlfriendRecord;
    public float queryDuration=5;
    NpcDialogueSequence menu;readonly List<ArchivePersonRecord> found=new List<ArchivePersonRecord>();int queryLine=-1;bool ownsSession;
    public bool InRange {get{return world&&world.Player&&interactionPoint&&RecoilInteractionRange.Contains(world,interactionPoint.position,.5f)&&world.Clear(world.Player.Position,interactionPoint.position);}}
    public bool GirlfriendOptionVisible {get{return world&&world.HasInteractionRequirement(CoalerCosplayerConversation.GirlfriendNameFlag);}}
    void Awake(){if(!world)world=GetComponentInParent<RecoilPrototype>();}
    void Start(){ReadyScreen();}
    void ReadyScreen(){if(screenText)screenText.text="FACTORY RECORDS\nINDEX / 1978\n\nNAME QUERY >\nREADY_";}
    NpcDialogueSequence.Choice NameChoice(string id,string label,ArchivePersonRecord record)
    {return new NpcDialogueSequence.Choice{id=id,label="Find "+label,text="Search record: "+label+".",continuation=menu,response=new[]{new NpcDialogueSequence.ResponseLine{role=NpcDialogueSequence.SpeakerRole.Npc,speaker="Archive terminal",text=record.dialogueSummary}}};}
    public bool StartConversation()
    {
        if(!world||!world.PlayerControlsAvailable||!InRange)return false;
        DisposeMenu();menu=ScriptableObject.CreateInstance<NpcDialogueSequence>();menu.hideFlags=HideFlags.HideAndDontSave;
        var choices=new List<NpcDialogueSequence.Choice>{NameChoice("aneta","Aneta",anetaRecord)};
        if(GirlfriendOptionVisible)choices.Add(NameChoice("ewa","Ewa",girlfriendRecord));
        choices.Add(new NpcDialogueSequence.Choice{id="leave",label="Leave terminal",text="Leave the terminal."});
        menu.lines=new[]{new NpcDialogueSequence.Line{role=NpcDialogueSequence.SpeakerRole.Player,speaker="Dawn",text="",choices=choices.ToArray()}};
        ReadyScreen();found.Clear();queryLine=-1;
        if(!world.BeginConversation(menu,CompleteConversation,OnLineShown,screenAnchor,false,false))return false;
        ownsSession=world.PropOperation.BeginTerminalSession(keyboard?keyboard.position:interactionPoint.position);
        if(!ownsSession)world.CancelConversationWith(screenAnchor);return ownsSession;
    }
    void OnLineShown(int index)
    {
        if(world.LastConversationChoiceId=="leave" && world.CurrentConversationText=="Leave the terminal."){world.RevealConversationText();world.AdvanceConversation();return;}
        if(index==queryLine||!world.CurrentConversationText.StartsWith("Search record: "))return;
        var record=world.LastConversationChoiceId=="ewa"?girlfriendRecord:anetaRecord;queryLine=index;
        if(screenText)screenText.text="FACTORY RECORDS\n> FIND "+record.title+"\n\nSEARCHING INDEX...\nPLEASE WAIT_";
        world.PropOperation.BeginTerminalQuery(queryDuration,()=>{
            if(!world.ConversationActive)return;
            if(!found.Contains(record))found.Add(record);
            if(screenText)screenText.text="FACTORY RECORDS\n"+record.title+"\n\nMATCH READY_";
            world.RevealConversationText();world.AdvanceConversation();
        });
    }
    void CompleteConversation()
    {
        ReadyScreen();
        System.Action done=()=>{ownsSession=false;if(found.Count>0){foreach(var record in found)world.RememberProgress(record.ProgressFlag);world.InformationJournal.Acquire(found[found.Count-1]);}};
        if(!world.PropOperation.EndTerminalSession(done))done();
    }
    void Update(){
        if(ownsSession&&world&&!world.ConversationActive&&world.PropOperation.TerminalSession&&!world.PropOperation.TerminalExiting)world.PropOperation.EndTerminalSession(()=>{ownsSession=false;ReadyScreen();});
        if(world&&world.PlayerControlsAvailable&&!world.ReaderInputConsumed&&RecoilInput.InteractPressed&&InRange)StartConversation();}
    void OnGUI(){if(world&&world.PlayerControlsAvailable&&InRange)world.DrawConversationTalkPrompt(screenAnchor,RecoilInput.UsingGamepad?"CIRCLE / USE TERMINAL":"E / USE TERMINAL");}
    void OnDisable(){if(world){world.CancelConversationWith(screenAnchor);if(ownsSession&&world.PropOperation.TerminalSession)world.PropOperation.Cancel();ownsSession=false;}}
    void DisposeMenu(){if(menu){if(Application.isPlaying)Destroy(menu);else DestroyImmediate(menu);}menu=null;}
    void OnDestroy(){DisposeMenu();}
}
