using UnityEngine;

[DefaultExecutionOrder(-750),DisallowMultipleComponent]
public sealed class ManagerAnetaConversation : MonoBehaviour
{
    public const string EvidenceFlag="information_archive_aneta";
    public const string DenialFlag="manager_aneta_denial_heard";
    public const string AssignmentFlag="manager_aneta_freight_requested";
    public const string PowerRestoredFlag="manager_aneta_freight_restored";
    public const string ReadyFlag="manager_aneta_ready_to_talk";
    public const string StoreroomKey="warehouse_locked_room_key";
    public RecoilPrototype world;
    public Transform headAnchor,interactionPoint;
    public NpcDialogueSequence denial,denialReminder,admissionRequest,requestAlreadyPowered,pendingReminder,powerRestored;
    [Min(.1f)] public float interactionRange=1.8f;
    void Awake(){if(!world)world=GetComponentInParent<RecoilPrototype>();}
    public bool HasEvidence {get{return world && world.HasItem(EvidenceFlag);}}
    public static bool FreightRestored(RecoilPrototype state)
    {return state && (state.HasItem(PowerRestoredFlag)||state.HasItem(WarehousePowerState.FreightPower));}
    public static void RecordPowerRestored(RecoilPrototype state)
    {if(state && state.HasItem(AssignmentFlag) && state.HasItem(WarehousePowerState.FreightPower))state.RememberProgress(PowerRestoredFlag);}
    public NpcDialogueSequence CurrentSequence {get{
        if(!world)return null;
        if(world.HasItem(AssignmentFlag))return FreightRestored(world)?powerRestored:pendingReminder;
        if(HasEvidence)return FreightRestored(world)?requestAlreadyPowered:admissionRequest;
        return world.HasItem(DenialFlag)?denialReminder:denial;
    }}
    public bool InRange {get{
        if(!world || !world.Player || !headAnchor || !interactionPoint || !isActiveAndEnabled)return false;
        if(!RecoilInteractionRange.Contains(world,interactionPoint.position,interactionRange) || !world.Clear(world.Player.Position,interactionPoint.position))return false;
        var camera=world.ViewCamera;if(!camera)return false;var p=camera.WorldToViewportPoint(headAnchor.position);
        return p.z>0 && p.x>=0 && p.x<=1 && p.y>=0 && p.y<=1;
    }}
    public bool StartConversation()
    {
        if(!world || !world.PlayerControlsAvailable || !InRange)return false;
        var sequence=CurrentSequence;
        // Capture this stage: progress/rewards only commit on final confirmation.
        bool accepting=!world.HasItem(AssignmentFlag)&&HasEvidence;
        bool denying=!world.HasItem(AssignmentFlag)&&!HasEvidence;
        bool reporting=world.HasItem(AssignmentFlag)&&FreightRestored(world);
        return ChildFoodQuest.BeginNpc(world,sequence,()=>{
            if(denying)world.RememberProgress(DenialFlag);
            if(accepting){
                world.RememberProgress(AssignmentFlag);RecordPowerRestored(world);
                if(FreightRestored(world))world.RememberProgress(ReadyFlag);
                if(!world.HasItem(StoreroomKey))world.GrantItem(StoreroomKey,"Warehouse storeroom key acquired.","Storeroom Key","The manager's key to the warehouse storeroom. The Freight power panel is inside.",null);
            }
            if(reporting){RecordPowerRestored(world);world.RememberProgress(ReadyFlag);}
        },null,headAnchor,ChildFoodQuest.Provider.Manager);
    }
    void Update(){if(world && world.PlayerControlsAvailable && !world.ReaderInputConsumed && RecoilInput.InteractPressed && InRange)StartConversation();}
    void OnGUI(){if(world && world.PlayerControlsAvailable && InRange)world.DrawConversationTalkPrompt(headAnchor,RecoilInput.UsingGamepad?"CIRCLE / TALK":"E / TALK");}
    void OnDisable(){if(world)world.CancelConversationWith(headAnchor);}
    public static string Objective(RecoilPrototype state)
    {
        if(!state)return null;
        // The child keeps priority once the rescue has begun.
        if(state.EscortingChild || state.HasItem(RecoilPrototype.WarehouseRescueStarted)&&!state.HasItem(RecoilPrototype.WarehouseChildEvacuated))return null;
        if(state.HasItem(ReadyFlag))return "Ask the factory manager about Aneta.";
        if(state.HasItem(AssignmentFlag))return FreightRestored(state)?"Return to the factory manager for information about Aneta.":"Use the storeroom key to enter the warehouse storage room and restore Freight power.";
        if(state.HasItem(EvidenceFlag))return "Show Aneta's electronic employment record to the factory manager.";
        if(state.HasItem(DenialFlag))return "Search the archive terminal for Aneta's employment record.";
        return null;
    }
}
