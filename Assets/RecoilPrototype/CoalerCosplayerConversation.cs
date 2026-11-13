using UnityEngine;

// Runs between the world's input pass (-1000) and the player's movement pass (-500).
[DefaultExecutionOrder(-750),DisallowMultipleComponent]
public sealed class CoalerCosplayerConversation : MonoBehaviour
{
    public const string GirlfriendNameFlag="cosplayer_girlfriend_ewa_known";
    public const string IntroductionFlag="cosplayer_introduction_heard";
    public const string GlassesLocationFlag="manager_glasses_roof_known";
    public RecoilPrototype world;
    public NpcDialogueSequence dialogue;
    public const string IntroductionResource="RecoilDialogue/Cosplayer_Introduction";
    public const string ReminderResource="RecoilDialogue/Cosplayer_GirlfriendReminder";
    NpcDialogueSequence session;
    public bool GlassesTopicAvailable {get{return world && (world.HasItem(DoorDialogue.GlassesMentionedFlag)||world.HasItem(DoorDialogue.PeacefulFlag)||world.HasItem(DoorDialogue.ThreatFlag)||world.HasItem(DoorDialogue.SilentFlag));}}
    public NpcDialogueSequence CreateConversationSequence()
    {
        var source=Resources.Load<NpcDialogueSequence>(world && world.HasItem(IntroductionFlag)?ReminderResource:IntroductionResource);
        if(!source)return null;
        var copy=Instantiate(source);copy.hideFlags=HideFlags.HideAndDontSave;
        if(GlassesTopicAvailable){
            var line=copy.lines[copy.lines.Length-1];
            line.choices=new[]{
                new NpcDialogueSequence.Choice{id="glasses_topic",label="Glasses",text="Did you take the manager's glasses?",continuation=dialogue},
                new NpcDialogueSequence.Choice{id="leave_cosplayer",label="Keep looking",text="I'll let you keep looking."}
            };
            if(world.HasItem(IntroductionFlag)){
                // After the introduction there is no new unsolicited speech; let Dawn choose a topic.
                line.role=NpcDialogueSequence.SpeakerRole.Player;line.speaker="Dawn";line.text="";line.progressFlag="";copy.lines=new[]{line};
            }else copy.lines[copy.lines.Length-1]=line;
        }
        return copy;
    }
    public Transform headAnchor,distanceOrigin;
    [Min(.1f)] public float interactionRange=2.7f;
    public bool InRange
    {
        get {
            if(!world || !world.Player || !headAnchor || !isActiveAndEnabled)return false;
            var origin=distanceOrigin?distanceOrigin.position:transform.position;
            if(Vector2.Distance(world.Player.Position,origin)>interactionRange)return false;
            var p=world.ViewCamera.WorldToViewportPoint(headAnchor.position);
            return p.z>0 && p.x>=0 && p.x<=1 && p.y>=0 && p.y<=1 &&
                world.Clear(world.Player.Position,new Vector2(origin.x,world.Player.Position.y));
        }
    }
    void Start()
    {
        if(!world)foreach(var candidate in FindObjectsOfType<RecoilPrototype>())
            if(candidate.gameObject.scene==gameObject.scene){world=candidate;break;}
    }
    public bool StartConversation()
    {
        if(!world || !InRange || !world.PlayerControlsAvailable)return false;
        DisposeSession();session=CreateConversationSequence();
        if(!session)return false;
        if(ChildFoodQuest.BeginNpc(world,session,()=>world.RememberProgress(IntroductionFlag),null,headAnchor,ChildFoodQuest.Provider.DisguisedMan))return true;
        DisposeSession();return false;
    }
    void Update()
    {
        if(world && world.PlayerControlsAvailable && !world.ReaderInputConsumed && RecoilInput.InteractPressed && InRange)StartConversation();
    }
    void OnGUI()
    {
        if(world && world.PlayerControlsAvailable && InRange)
            world.DrawConversationTalkPrompt(headAnchor,RecoilInput.UsingGamepad?"○ Talk · The Disguised Man":"E Talk · The Disguised Man");
    }
    void DisposeSession(){if(session){if(Application.isPlaying)Destroy(session);else DestroyImmediate(session);}session=null;}
    void OnDisable(){if(world)world.CancelConversationWith(headAnchor);DisposeSession();}
    void OnDestroy(){DisposeSession();}
}
