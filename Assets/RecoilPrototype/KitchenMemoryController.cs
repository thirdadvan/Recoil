using UnityEngine;

// Narrative only. Player, input, interaction UI, camera and collision belong to the shared game systems.
[DisallowMultipleComponent,AddComponentMenu("Recoil/Kitchen Memory Story")]
public sealed class KitchenMemoryController : MonoBehaviour
{
    public const string ExitPermission="kitchen_memory_exit_ready";
    public RecoilPrototype world;
    public Transform mother,motherCookingPoint,bedroomRestPoint,bedroomDoor;
    public GameObject bedroomOcclusion,closedBedroomDoor,openBedroomDoor;
    [Min(.1f)] public float motherWalkSpeed=2.1f;
    [TextArea] public string boyLine="Mum?";
    public bool MotherLeaving {get;private set;}
    public bool BedroomClosed {get;private set;}
    public void ResetStory(RecoilPrototype context)
    {
        world=context;MotherLeaving=BedroomClosed=false;
        if(motherCookingPoint)mother.position=motherCookingPoint.position;
        mother.gameObject.SetActive(true);
        if(bedroomOcclusion)bedroomOcclusion.SetActive(false);
        if(openBedroomDoor)openBedroomDoor.SetActive(true);
        if(closedBedroomDoor)closedBedroomDoor.SetActive(false);
        if(world)world.SetProgressFlag(ExitPermission,false);
    }
    public bool TalkToMother()
    {
        if(MotherLeaving || BedroomClosed)return false;
        MotherLeaving=true;if(world)world.ShowDialogue(boyLine);return true;
    }
    public void AdvanceMother(float dt)
    {
        if(!MotherLeaving || BedroomClosed || !bedroomRestPoint)return;
        mother.position=Vector3.MoveTowards(mother.position,bedroomRestPoint.position,motherWalkSpeed*dt);
        if(Vector3.Distance(mother.position,bedroomRestPoint.position)>.035f)return;
        // Wait until the player is outside rather than trap them in the concealed bedroom.
        if(world && bedroomDoor && world.Player.Position.x<bedroomDoor.position.x+.35f)return;
        BedroomClosed=true;
        if(openBedroomDoor)openBedroomDoor.SetActive(false);
        if(closedBedroomDoor)closedBedroomDoor.SetActive(true);
        if(bedroomOcclusion)bedroomOcclusion.SetActive(true);
        mother.gameObject.SetActive(false);
        if(world)world.RememberProgress(ExitPermission);
    }
    void Update(){if(world && world.Playing)AdvanceMother(world.FrameDelta);}
}
