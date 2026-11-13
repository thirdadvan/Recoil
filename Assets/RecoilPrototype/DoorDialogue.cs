using UnityEngine;

// A voice behind a door uses the normal character bubbles and interaction rules.
[DefaultExecutionOrder(-800),DisallowMultipleComponent]
public sealed class DoorDialogue : MonoBehaviour
{
    public enum DoorRole { Store, Manager }
    public const string GlassesReturnedFlag="door_dialogue_manager_glasses_returned";
    public const string ManagerDoorOpenFlag="door_dialogue_manager_open";
    public const string GlassesMentionedFlag="door_dialogue_manager_glasses_mentioned";
    public const string HandoverResource="RecoilDialogue/ManagerDoor_GlassesHandover";
    ManagerDoorOpening managerPresentation;
    public string BlockedPrompt {get{return role==DoorRole.Manager && world && world.HasItem(GlassesReturnedFlag)?"Opening...":"E / Talk through door";}}
    void Awake(){EnsureManagerPresentation();}
    void Start(){EnsureManagerPresentation();}
    void EnsureManagerPresentation()
    {
        if(role!=DoorRole.Manager)return;
        if(!world)world=GetComponentInParent<RecoilPrototype>();if(!door)door=GetComponent<RecoilDoor>();
        if(!world||!door)return;
        if(!managerPresentation)managerPresentation=GetComponent<ManagerDoorOpening>();
        if(!managerPresentation)managerPresentation=gameObject.AddComponent<ManagerDoorOpening>();
        managerPresentation.Initialize(world,door);
    }
    public const string StoreFlag="door_dialogue_store_invitation";
    public const string PeacefulFlag="door_dialogue_manager_peaceful";
    public const string ThreatFlag="door_dialogue_manager_threat";
    public const string SilentFlag="door_dialogue_manager_silent";
    public DoorRole role;
    public RecoilPrototype world;
    public RecoilDoor door;
    public Transform voiceAnchor;
    public NpcDialogueSequence opening,peacefulRepeat,threatRepeat,silentRepeat;
    [Min(.1f)] public float approachRange=3;
    GameObject photo;
    Material photoMaterial;
    float photoStarted;
    Vector3 photoStart,photoEnd;
    public bool BlocksEntry(RecoilPrototype state)
    {
        if(!isActiveAndEnabled)return false;
        if(!state)state=world;
        return state && !state.UniversalInteractionEnabled && (role==DoorRole.Store?!state.HasItem(StoreFlag):!state.HasItem(ManagerDoorOpenFlag));
    }
    public NpcDialogueSequence CurrentSequence {get{
        if(role==DoorRole.Manager && world){
            if(world.HasItem(GlassesReturnedFlag)||world.HasItem(ManagerDoorOpenFlag))return null;
            if(world.HasItem(ManagerGlassesPickup.InventoryId))return Resources.Load<NpcDialogueSequence>(HandoverResource);
            if(world.HasItem(ThreatFlag))return threatRepeat;
            if(world.HasItem(PeacefulFlag))return peacefulRepeat;
            if(world.HasItem(SilentFlag))return silentRepeat;
        }
        return opening;
    }}
    public bool StartConversation()
    {
        if(!world || !world.PlayerControlsAvailable || !BlocksEntry(world))return false;
        EnsureManagerPresentation();
        if(role==DoorRole.Manager && world.HasItem(GlassesReturnedFlag))return false;
        bool returning=role==DoorRole.Manager && world.HasItem(ManagerGlassesPickup.InventoryId);
        return world.BeginConversation(CurrentSequence,()=>Finish(returning),LineShown,voiceAnchor,role==DoorRole.Store);
    }
    void Finish(bool returning)
    {
        if(role==DoorRole.Store)world.RememberProgress(StoreFlag);
        if(returning && world.HasItem(ManagerGlassesPickup.InventoryId)){
            world.RememberProgress(ManagerGlassesPickup.CollectedFlag);world.SetProgressFlag(ManagerGlassesPickup.InventoryId,false);
            world.RememberProgress(GlassesReturnedFlag);if(managerPresentation)managerPresentation.Open();
        }
        ClearPhoto();
    }
    void Update()
    {
        if(photo){
            if(!world || !world.ConversationActive){ClearPhoto();}
            else photo.transform.position=Vector3.Lerp(photoStart,photoEnd,Mathf.SmoothStep(0,1,(Time.unscaledTime-photoStarted)/.7f));
        }
        if(role!=DoorRole.Store || !world || !door || !world.PlayerControlsAvailable || !BlocksEntry(world))return;
        var delta=world.Player.Position-door.InteractionPosition;
        if(Mathf.Abs(delta.x)>approachRange || Mathf.Abs(delta.y)>1.8f)return;
        var camera=world.ViewCamera;if(!camera || !voiceAnchor)return;
        var v=camera.WorldToViewportPoint(voiceAnchor.position);
        if(v.z>0 && v.x>=0 && v.x<=1 && v.y>=0 && v.y<=1 && world.Clear(world.Player.Position,door.InteractionPosition))StartConversation();
    }
    void LineShown(int index)
    {
        if(!world.ConversationSlidingPhoto)return;
        ClearPhoto();
        photo=GameObject.CreatePrimitive(PrimitiveType.Cube);photo.name="Dawn slides photograph under manager door";
        var collider=photo.GetComponent<Collider>();if(collider)Destroy(collider);
        photo.transform.localScale=new Vector3(.20f,.008f,.14f);
        var shader=Shader.Find("Universal Render Pipeline/Unlit");if(!shader)shader=Shader.Find("Unlit/Color");
        photoMaterial=new Material(shader);photoMaterial.color=new Color(.86f,.82f,.70f);photo.GetComponent<Renderer>().sharedMaterial=photoMaterial;
        photoEnd=new Vector3(door.InteractionPosition.x,world.Player.Position.y-world.Player.BodyHeight*.5f+.025f,voiceAnchor.position.z);
        photoStart=photoEnd;photoStart.x=world.Player.Position.x;photoStarted=Time.unscaledTime;photo.transform.position=photoStart;
    }
    void ClearPhoto(){if(photo)Destroy(photo);if(photoMaterial)Destroy(photoMaterial);photo=null;photoMaterial=null;}
    void OnDisable(){if(world)world.CancelConversationWith(voiceAnchor);ClearPhoto();}
}
