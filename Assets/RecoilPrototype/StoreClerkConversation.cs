using UnityEngine;

[DefaultExecutionOrder(-750),DisallowMultipleComponent]
public sealed class StoreClerkConversation : MonoBehaviour
{
    public const string CompletedFlag="store_clerk_first_conversation_completed";
    public const string ManagerLocationFlag="store_clerk_manager_location_known";
    public RecoilPrototype world;
    public ServiceStoreEncounter rescue;
    public NpcDialogueSequence firstConversation,reminder;
    public Transform headAnchor,interactionPoint;
    public GameObject tableGunDisplay;
    [Min(.1f)] public float interactionRange=1.5f;
    public bool InRange {get{
        if(!world || !world.Player || !headAnchor || !interactionPoint || !isActiveAndEnabled)return false;
        if(rescue && !world.HasInteractionRequirement(ServiceStoreEncounter.CompletedFlag))return false;
        if(!RecoilInteractionRange.Contains(world,interactionPoint.position,interactionRange))return false;
        var p=world.ViewCamera.WorldToViewportPoint(headAnchor.position);
        return p.z>0 && p.x>=0 && p.x<=1 && p.y>=0 && p.y<=1 && world.Clear(world.Player.Position,interactionPoint.position);
    }}
    void Start(){if(world){world.RunReset+=RefreshDisplay;RefreshDisplay();}}
    public bool StartConversation()
    {
        if(!world || !world.PlayerControlsAvailable || !InRange)return false;
        if(world.HasItem(CompletedFlag))return ChildFoodQuest.BeginNpc(world,reminder,null,null,headAnchor,ChildFoodQuest.Provider.CounterSales);
        return ChildFoodQuest.BeginNpc(world,firstConversation,CompleteFirstConversation,null,headAnchor,ChildFoodQuest.Provider.CounterSales);
    }
    void CompleteFirstConversation()
    {
        world.RememberProgress(CompletedFlag);world.RememberProgress(ManagerLocationFlag);
        // Grant only after the final goodbye; interruption never invokes this callback.
        world.GrantItem("water_gun","Water gun acquired.","Water Gun","A water gun Aneta left for you. Use the recoil from firing to help you move.",null);
        RefreshDisplay();
    }
    void RefreshDisplay()
    {
        if(!world)return;
        if(tableGunDisplay)tableGunDisplay.SetActive(!world.OwnsWaterGun);
        world.objectiveWithoutCard=world.objectiveWithCard=world.HasItem(ManagerLocationFlag)?"Visit the manager's office on the factory's fourth floor for news of Aneta.":"Talk to The Counter Sales for clues about Aneta.";
    }
    void Update()
    {
        if(world && world.PlayerControlsAvailable && !world.ReaderInputConsumed && RecoilInput.InteractPressed && InRange)StartConversation();
    }
    void OnGUI()
    {
        if(world && world.PlayerControlsAvailable && InRange)world.DrawConversationTalkPrompt(headAnchor,RecoilInput.UsingGamepad?"○ Talk · The Counter Sales":"E Talk · The Counter Sales");
    }
    void OnDisable(){if(world)world.CancelConversationWith(headAnchor);}
    void OnDestroy(){if(world)world.RunReset-=RefreshDisplay;}
}
