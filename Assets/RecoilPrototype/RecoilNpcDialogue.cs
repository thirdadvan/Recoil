using UnityEngine;

// Content is an asset; this runner owns pausing, advancement and presentation.
// No Time.timeScale / AudioListener changes: ambience and private heartbeat keep playing.
public sealed partial class RecoilPrototype
{
    NpcDialogueSequence conversation;
    int conversationLine,conversationInputFrame=-1;
    bool conversationReleasePending;
    System.Action conversationCompleted;
    System.Action<int> conversationLineShown;
    public event System.Action RunReset;
    public bool ConversationActive {get{return conversation!=null;}}
    bool ConversationBlocksInput {get{return ConversationActive || conversationReleasePending || conversationInputFrame==Time.frameCount;}}
    public bool ConversationKeepsAudioPlaying {get{return ConversationBlocksInput;}}
    public int ConversationLineIndex {get{return conversationLine;}}
    public bool BeginConversation(NpcDialogueSequence sequence,System.Action completed=null,System.Action<int> lineShown=null)
    {
        if(!sequence || sequence.lines==null || sequence.lines.Length==0 || !Playing)return false;
        InterruptInteraction();conversation=sequence;conversationLine=0;
        conversationCompleted=completed;conversationLineShown=lineShown;
        conversationInputFrame=Time.frameCount;FrameDelta=0;ReaderInputConsumed=true;
        if(conversationLineShown!=null)conversationLineShown(0);
        return true;
    }
    public void AdvanceConversation()
    {
        if(!ConversationActive)return;
        conversationInputFrame=Time.frameCount;ReaderInputConsumed=true;FrameDelta=0;
        if(conversationLine+1<conversation.lines.Length)
        {conversationLine++;if(conversationLineShown!=null)conversationLineShown(conversationLine);return;}
        var completed=conversationCompleted;CancelConversation();
        conversationReleasePending=Application.isPlaying && (Input.GetMouseButton(0)||Input.GetKey(KeyCode.Space));
        if(completed!=null)completed();
    }
    void CancelConversation()
    {conversation=null;conversationCompleted=null;conversationLineShown=null;conversationReleasePending=false;}
    bool ConsumeConversationInput()
    {
        if(!ConversationBlocksInput)return false;
        ReaderInputConsumed=true;FrameDelta=0;
        if(conversationReleasePending && !Input.GetMouseButton(0) && !Input.GetKey(KeyCode.Space))conversationReleasePending=false;
        if(ConversationActive && conversationInputFrame!=Time.frameCount &&
            (Input.GetMouseButtonDown(0)||Input.GetKeyDown(KeyCode.Space)))AdvanceConversation();
        return true;
    }
    GUIStyle conversationText;
    void DrawConversation()
    {
        if(conversationText==null)conversationText=new GUIStyle(label){wordWrap=true,fontSize=24};
        var line=conversation.lines[conversationLine];
        Panel(new Rect(145,534,990,156),Hex(0x091216,.96f));
        Text(176,547,920,25,line.speaker,small);
        Text(176,580,920,65,line.text,conversationText);
        Text(176,656,920,22,"CLICK / SPACE  •  CONTINUE",small);
    }
}
