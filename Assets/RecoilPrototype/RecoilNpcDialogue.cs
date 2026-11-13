using System.Collections.Generic;
using UnityEngine;

// Conversations normally leave the world running; authored encounters can explicitly pause it.
public sealed partial class RecoilPrototype
{
    NpcDialogueSequence conversation;
    int conversationChoiceSourceLine=-1,conversationChoicePlayerLine=-1;
    bool conversationPausesWorld;
    float conversationPreviousTimeScale;
    public bool ConversationPausesWorld {get{return ConversationActive && conversationPausesWorld;}}
    public bool ConversationSlidingPhoto {get{return ConversationActive && conversationLines[conversationLine].slidePhotoUnderDoor;}}
    readonly List<NpcDialogueSequence.Line> conversationLines=new List<NpcDialogueSequence.Line>();
    int conversationLine,conversationInputFrame=-1,selectedConversationChoice;
    bool conversationReleasePending,conversationPlayerOnLeft;
    float conversationLineStarted;
    float conversationReadyAt;
    DawnPlayerAnimation conversationPresentation;
    bool conversationTextRevealed,conversationChoicesOpen;
    Transform conversationNpcAnchor;
    System.Action conversationCompleted;
    System.Action<int> conversationLineShown;
    public event System.Action RunReset;
    public bool ConversationActive {get{return conversation!=null;}}
    public bool ConversationPreparing {get{return ConversationActive && Time.unscaledTime<conversationReadyAt;}}
    bool ConversationBlocksInput {get{return ConversationActive || conversationReleasePending || conversationInputFrame==Time.frameCount;}}
    public bool ConversationKeepsAudioPlaying {get{return ConversationBlocksInput;}}
    public int ConversationLineIndex {get{return conversationLine;}}
    public bool ConversationPlayerOnLeft {get{return conversationPlayerOnLeft;}}
    public bool ConversationChoicesOnLeft {get{return conversationPlayerOnLeft;}}
    public string LastConversationChoiceId {get;private set;}
    public int SelectedConversationChoice {get{return selectedConversationChoice;}}
    public string CurrentConversationText {get{return ConversationActive?conversationLines[conversationLine].text:"";}}
    public bool ConversationOfferingPhoto {get{return ConversationActive && conversationLines[conversationLine].offerPhoto;}}
    public bool ConversationTextComplete {get{return !ConversationPreparing && (!conversationNpcAnchor || conversationTextRevealed ||
        Time.unscaledTime-conversationLineStarted>=new System.Globalization.StringInfo(CurrentConversationText??"").LengthInTextElements/30f);}}
    public bool ConversationHasChoices {get{return ConversationActive &&
        conversationLines[conversationLine].choices!=null && conversationLines[conversationLine].choices.Length>0;}}
    public bool WaitingForConversationChoice {get{return conversationChoicesOpen && ConversationTextComplete && ConversationHasChoices;}}
    public bool BeginConversation(NpcDialogueSequence sequence,System.Action completed=null,System.Action<int> lineShown=null,Transform npcAnchor=null,bool pauseWorld=false,bool animatePlayer=true)
    {
        if(!sequence || sequence.lines==null || sequence.lines.Length==0 || !PlayerControlsAvailable)return false;
        InterruptInteraction();conversationPreviousTimeScale=Time.timeScale;conversationPausesWorld=pauseWorld;
        if(pauseWorld)Time.timeScale=0;
        conversation=sequence;conversationLine=0;conversationNpcAnchor=npcAnchor;
        conversationLines.Clear();conversationLines.AddRange(sequence.lines);LastConversationChoiceId=null;thoughtMotionLine=-1;conversationChoiceSourceLine=conversationChoicePlayerLine=-1;
        conversationCompleted=completed;conversationLineShown=lineShown;
        if(player)player.StopForConversation(npcAnchor?(Vector3?)npcAnchor.position:null);
        conversationReadyAt=Time.unscaledTime+(npcAnchor?(animatePlayer?DawnPlayerAnimation.ConversationPreparationDuration:.35f):0);
        conversationPresentation=animatePlayer&&player?player.GetComponentInChildren<DawnPlayerAnimation>():null;
        if(npcAnchor && conversationPresentation)conversationPresentation.BeginConversationPresentation(npcAnchor.position);
        conversationPlayerOnLeft=!npcAnchor || !cam || cam.WorldToScreenPoint(player.WorldPosition).x<=cam.WorldToScreenPoint(npcAnchor.position).x;
        conversationInputFrame=Time.frameCount;ReaderInputConsumed=true;
        StartConversationLine();return true;
    }
    // An empty line with choices is a player-led topic menu, not a spoken sentence.
    bool ConversationChoicePrompt(int index)
    {var line=conversationLines[index];return string.IsNullOrWhiteSpace(line.text)&&line.choices!=null&&line.choices.Length>0;}
    void StartConversationLine()
    {
        if(!string.IsNullOrEmpty(conversationLines[conversationLine].progressFlag))RememberProgress(conversationLines[conversationLine].progressFlag);
        selectedConversationChoice=0;conversationLineStarted=Mathf.Max(Time.unscaledTime,conversationReadyAt);conversationTextRevealed=ConversationChoicePrompt(conversationLine);conversationChoicesOpen=conversationTextRevealed;
        if(conversationPresentation)conversationPresentation.SetConversationPhotoOffer(conversationLines[conversationLine].offerPhoto);
        if(conversationLineShown!=null)conversationLineShown(conversationLine);
    }
    public void RevealConversationText(){if(!ConversationPreparing)conversationTextRevealed=true;}
    public void MoveConversationChoice(int delta)
    {
        if(!WaitingForConversationChoice)return;
        selectedConversationChoice=Mathf.Clamp(selectedConversationChoice+delta,0,conversationLines[conversationLine].choices.Length-1);
    }
    public bool SelectConversationChoice(int index)
    {
        if(!WaitingForConversationChoice||PropOperationBlocksConversation)return false;
        var choices=conversationLines[conversationLine].choices;
        if(index<0 || index>=choices.Length || choices[index]==null)return false;
        var choice=choices[index];LastConversationChoiceId=choice.id;
        var branch=new List<NpcDialogueSequence.Line>();
        branch.Add(new NpcDialogueSequence.Line{role=NpcDialogueSequence.SpeakerRole.Player,speaker="Dawn",text=choice.text,offerPhoto=choice.offerPhoto,slidePhotoUnderDoor=choice.slidePhotoUnderDoor});
        if(choice.response!=null)foreach(var response in choice.response)
            branch.Add(new NpcDialogueSequence.Line{role=response.role,speaker=response.speaker,text=response.text,offerPhoto=response.offerPhoto,slidePhotoUnderDoor=response.slidePhotoUnderDoor});
        if(choice.continuation && choice.continuation.lines!=null)branch.AddRange(choice.continuation.lines);
        conversationChoiceSourceLine=conversationLine;conversationChoicePlayerLine=conversationLine+1;
        conversationLines.InsertRange(conversationLine+1,branch);
        // Choosing an intention starts Dawn's full line with the usual typewriter
        // and confirmation gate; only the next advance starts the NPC response.
        conversationLine++;conversationInputFrame=Time.frameCount;ReaderInputConsumed=true;
        StartConversationLine();return true;
    }
    public void AdvanceConversation()
    {
        if(!ConversationActive || ConversationPreparing || PropOperationBlocksConversation)return;
        conversationInputFrame=Time.frameCount;ReaderInputConsumed=true;
        if(!ConversationTextComplete){RevealConversationText();return;}
        if(ConversationHasChoices)
        {
            // Spoken sentences still need confirmation. Choice-only prompts open
            // on entry and the next confirmation submits the selected thought.
            if(!conversationChoicesOpen){conversationChoicesOpen=true;return;}
            SelectConversationChoice(selectedConversationChoice);return;
        }
        if(conversationLine+1<conversationLines.Count){conversationLine++;StartConversationLine();return;}
        FinishConversation();
    }
    void FinishConversation()
    {
        var completed=conversationCompleted;CancelConversation();
        conversationReleasePending=Application.isPlaying && ConversationConfirmHeld;
        if(completed!=null)completed();
    }
    void CancelConversation()
    {
        if(conversationPausesWorld)Time.timeScale=conversationPreviousTimeScale;
        conversationPausesWorld=false;
        if(conversationPresentation)conversationPresentation.EndConversationPresentation();conversationPresentation=null;
        conversation=null;conversationNpcAnchor=null;conversationLines.Clear();conversationChoicesOpen=false;
        conversationCompleted=null;conversationLineShown=null;conversationReleasePending=false;
    }
    public void CancelConversationWith(Transform anchor)
    {
        if(ConversationActive && conversationNpcAnchor==anchor){conversationInputFrame=Time.frameCount;CancelConversation();}
    }
    bool ConversationConfirmHeld {get{return Input.GetMouseButton(0)||Input.GetKey(KeyCode.Space)||Input.GetKey(KeyCode.Return)||Input.GetKey(KeyCode.KeypadEnter)||RecoilInput.ConfirmHeld;}}
    bool ConsumeConversationInput()
    {
        if(!ConversationBlocksInput)return false;
        ReaderInputConsumed=true;
        if(conversationReleasePending && !ConversationConfirmHeld)conversationReleasePending=false;
        if(ConversationActive && conversationInputFrame!=Time.frameCount)
        {
            if(WaitingForConversationChoice)
            {
                if(RecoilInput.UpPressed)MoveConversationChoice(-1);
                if(RecoilInput.DownPressed)MoveConversationChoice(1);
            }
            if(Input.GetKeyDown(KeyCode.Space)||Input.GetKeyDown(KeyCode.Return)||Input.GetKeyDown(KeyCode.KeypadEnter)||RecoilInput.ConfirmPressed ||
                (!WaitingForConversationChoice && Input.GetMouseButtonDown(0)))AdvanceConversation();
        }
        return true;
    }
    GUIStyle conversationText;
    void DrawConversation()
    {
        if(ConversationPreparing)return;
        if(conversationNpcAnchor){DrawConversationBubbles();return;}
        if(conversationText==null){conversationText=new GUIStyle(label){wordWrap=true,fontSize=24};SetConversationTextColor(conversationText,conversationText.normal.textColor);}
        var line=conversationLines[conversationLine];
        Panel(new Rect(145,534,990,156),Hex(0x091216,.96f));
        Text(176,556,920,84,line.text,conversationText);
        Text(176,656,920,22,"CLICK / SPACE  •  CONTINUE",small);
    }
}
