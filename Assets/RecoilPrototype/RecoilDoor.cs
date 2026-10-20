using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent,AddComponentMenu("Recoil/Door")]
public sealed class RecoilDoor : MonoBehaviour
{
    [Min(.1f)] public float interactionRange=1.7f;
    [Tooltip("相对门物体原点的互动位置，使用世界单位。")]
    public Vector2 interactionOffset=new Vector2(0,-.25f);
    public string interactionPrompt="E / Leave the factory";
    [Header("Opening requirements")]
    public bool requiresClue=true;
    [Tooltip("留空表示不要求物品；例如 employee_access_card。")]
    public string requiredItemId="";
    [TextArea] public string lockedMessage="I came here for a person. I need a clue before I leave.";
    [Header("Opening result")]
    public bool completesLevel=true;
    [Tooltip("离开死亡回忆时，使用已记录的 checkpoint 返回目标。")] public bool returnToDeathMemoryCheckpoint;
    [Tooltip("填写 Build Settings 中的场景路径或名称；非空时开门切换场景。")]
    public string destinationScene="";
    [Tooltip("目标场景的出生点名称；留空使用该场景的 Player Spawn。")]
    public string destinationSpawn="";
    [Header("Persistent interaction state")]
    public bool permanentlyBlocked;
    [Tooltip("机械结构严重损坏，万能测试钥匙也无法开门。") ] public bool irreparableDamage;
    public string blockedByItemId="", blockedMessage="The door is damaged and will not open.";
    public string rememberOnTravel="";
    [Tooltip("非空时第一次互动只打开，再次互动切换场景。")]
    public string openStateItemId="";
    public bool requiresFlyingToOpen;
    [TextArea,Tooltip("第一次打开时的提示；留空保留原天窗提示。") ]
    public string firstOpenMessage="";
    [Header("Automatic exit through opening")]
    public bool autoTravelAboveOpening;
    [Min(.1f)] public float autoExitHalfWidth=1;
    public float autoExitHeightOffset;
    public string openPrompt="E / Open skylight", travelPrompt="E / Leave through skylight";
    public Collider2D openingBarrier;
    public Transform closedVisual,openVisual,damagedVisual;
    RecoilPrototype owner;
    bool Blocked {get{return irreparableDamage || !(owner && owner.HasItem(RecoilPrototype.UniversalKey)) && (permanentlyBlocked || (owner && !string.IsNullOrEmpty(blockedByItemId) && owner.HasItem(blockedByItemId)));}}
    bool HatchOpen {get{return owner && !string.IsNullOrEmpty(openStateItemId) && owner.HasItem(openStateItemId);}}
    public string CurrentPrompt {get{return Blocked?"E / DAMAGED DOOR":!string.IsNullOrEmpty(openStateItemId)?HatchOpen?travelPrompt:openPrompt:interactionPrompt;}}
    void SyncPresentation()
    {
        if(openingBarrier)openingBarrier.enabled=!HatchOpen;
        if(closedVisual)closedVisual.gameObject.SetActive(!HatchOpen && !Blocked);
        if(openVisual)openVisual.gameObject.SetActive(HatchOpen && !Blocked);
        if(damagedVisual)damagedVisual.gameObject.SetActive(Blocked);
    }
    string DestinationLoadName
    {get{string path=destinationScene.Trim();return path.EndsWith(".unity",System.StringComparison.OrdinalIgnoreCase)?path.Substring(0,path.Length-6):path;}}
    [TextArea] public string openedMessage="The address is ahead. So is the feeling I've been here before.";
    public UnityEvent onOpened=new UnityEvent();
    public bool IsOpen {get;private set;}
    public Vector2 InteractionPosition {get{return (Vector2)transform.position+interactionOffset;}}
    public bool InRange(RecoilPrototype world)
    {return isActiveAndEnabled && !IsOpen && world && world.Player && Vector2.Distance(world.Player.Position,InteractionPosition)<interactionRange && world.Clear(world.Player.Position,openingBarrier && openingBarrier.enabled?openingBarrier.ClosestPoint(world.Player.Position):InteractionPosition);}
    public bool TryInteract(RecoilPrototype world)
    {
        if(!InRange(world))return false;
        owner=world;
        world.Player.NotifyForbiddenInteraction();
        if(Blocked){world.ShowInteractionMessage(blockedMessage);return true;}
        if(!world.HasItem(RecoilPrototype.UniversalKey) && ((requiresClue && !world.HasClue) || (!string.IsNullOrEmpty(requiredItemId) && !world.HasItem(requiredItemId))))
        {world.ShowInteractionMessage(lockedMessage);return true;}
        if(!string.IsNullOrEmpty(openStateItemId) && !HatchOpen)
        {
            if(requiresFlyingToOpen && !world.Player.Flying && !world.HasItem(RecoilPrototype.UniversalKey)){world.ShowInteractionMessage("Fly up to open the skylight.");return true;}
            world.RememberProgress(openStateItemId);SyncPresentation();world.ShowInteractionMessage(!string.IsNullOrEmpty(firstOpenMessage)?firstOpenMessage:autoTravelAboveOpening?"Skylight opened. Fly through it to leave, or press E again.":"Skylight opened. Press E again to climb out.");return true;
        }
        if(returnToDeathMemoryCheckpoint)
        {
            if(!RecoilPrototype.ReturnFromKitchenMemory())world.ShowInteractionMessage("This memory has no recorded checkpoint. Enter it through a death in the game.");
            return true;
        }
        if(!string.IsNullOrEmpty(destinationScene))
        {
            Travel(world);return true;
        }
        IsOpen=true;
        if(completesLevel)world.CompleteLevel(openedMessage);else world.ShowInteractionMessage(openedMessage);
        onOpened.Invoke();return true;
    }
    bool Travel(RecoilPrototype world)
    {
        if(!RecoilSceneLoader.CanLoad(DestinationLoadName))
        {world.ShowInteractionMessage("Destination scene is missing from Build Settings.");Debug.LogError("Door destination is unavailable: "+destinationScene,this);return false;}
        world.RememberProgress(rememberOnTravel);IsOpen=true;onOpened.Invoke();world.TravelToScene(DestinationLoadName,destinationSpawn);return true;
    }
    public bool TryAutoTravel(RecoilPrototype world)
    {
        if(!autoTravelAboveOpening || !isActiveAndEnabled || IsOpen || !world || !world.Playing || !world.Player || string.IsNullOrEmpty(destinationScene))return false;
        owner=world;
        if(Blocked || !HatchOpen || (!world.HasItem(RecoilPrototype.UniversalKey) && ((requiresClue && !world.HasClue) || (!string.IsNullOrEmpty(requiredItemId) && !world.HasItem(requiredItemId)))))return false;
        var p=world.Player.Position;
        if(Mathf.Abs(p.x-transform.position.x)+.32f>autoExitHalfWidth || p.y-world.Player.BodyHeight*.5f<transform.position.y+autoExitHeightOffset)return false;
        return Travel(world);
    }
    public void ResetDoor(RecoilPrototype world=null){owner=world;IsOpen=false;SyncPresentation();}
    void OnDrawGizmosSelected(){Gizmos.color=new Color(.4f,1,.7f,.8f);Gizmos.DrawWireSphere(InteractionPosition,interactionRange);}
}
