using UnityEngine;

// The player uses custom movement, so overlap checks do not require a Rigidbody2D.
[DisallowMultipleComponent, RequireComponent(typeof(BoxCollider2D))]
public sealed class PlayerDialogueTrigger : MonoBehaviour
{
    [TextArea] public string message="Press Shift to stealth walk";
    [Min(.1f)] public float displayDuration=5;
    public bool triggerOnce=true;
    BoxCollider2D area;
    bool fired, wasInside;

    void Reset(){GetComponent<BoxCollider2D>().isTrigger=true;}
    public void ResetTrigger(){fired=wasInside=false;}
    public void CheckPlayer(RecoilPrototype world)
    {
        if(!isActiveAndEnabled || !world.Player || (triggerOnce && fired))return;
        if(!area)area=GetComponent<BoxCollider2D>();
        if(!area.enabled)return;
        var p=world.Player.Position;
        var bounds=area.bounds;
        float halfHeight=world.Player.BodyHeight*.5f;
        bool inside=p.x+.32f>bounds.min.x && p.x-.32f<bounds.max.x &&
                    p.y+halfHeight>bounds.min.y && p.y-halfHeight<bounds.max.y;
        if(inside && !wasInside){world.ShowDialogue(message,displayDuration);fired=true;}
        wasInside=inside;
    }
    void OnDrawGizmos()
    {
        var box=GetComponent<BoxCollider2D>();if(!box)return;
        Gizmos.color=new Color(.3f,.9f,1,.6f);
        Gizmos.matrix=transform.localToWorldMatrix;
        Gizmos.DrawWireCube(box.offset,box.size);
    }
}
