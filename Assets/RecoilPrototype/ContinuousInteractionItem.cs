using UnityEngine;

// Reusable hold-to-interact item. Rewards are configured independently of the item type.
public class ContinuousInteractionItem : MonoBehaviour
{
    public string itemId="item", displayName="Item";
    [TextArea] public string description="";
    public float interactionDuration=1.5f, interactionRange=1.5f;
    public string resultId="", resultMessage="";
    Color availableColor;bool colorRecorded;
    public float Progress {get;protected set;}
    public bool Completed {get;protected set;}
    public int Session {get;private set;}
    public bool InRange(RecoilPrototype world)
    {return !Completed && !world.HasItem(resultId) && Vector2.Distance(world.Player.Position,transform.position)<=interactionRange && world.Clear(world.Player.Position,transform.position);}
    public void Begin(int session){Session=session;Progress=0;}
    public void Cancel(){Progress=0;Session=0;}
    public virtual bool Advance(RecoilPrototype world,float dt)
    {
        Progress+=dt;
        if(Progress<interactionDuration)return false;
        Progress=interactionDuration;Completed=true;
        world.GrantItem(resultId,resultMessage);
        var sprite=GetComponent<SpriteRenderer>();if(sprite)sprite.color=RecoilPrototype.Hex(0x536361);
        return true;
    }
    public virtual void ResetItem(RecoilPrototype world=null){Cancel();Completed=world && !string.IsNullOrEmpty(resultId) && world.HasItem(resultId);var sprite=GetComponent<SpriteRenderer>();if(sprite){if(!colorRecorded){availableColor=sprite.color;colorRecorded=true;}sprite.color=Completed?RecoilPrototype.Hex(0x536361):availableColor;}}
}
