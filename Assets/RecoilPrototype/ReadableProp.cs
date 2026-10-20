using UnityEngine;

// Reusable wall poster or text note. World context owns the modal and input routing.
[DisallowMultipleComponent,AddComponentMenu("Recoil/Readable Poster or Note")]
public sealed class ReadableProp : MonoBehaviour
{
    public string displayName="Poster";
    public string interactionPrompt="E / READ POSTER";
    [Min(.1f)] public float interactionRange=1.5f;
    public Texture2D pageImage;
    [TextArea(4,20)] public string pageText="";
    public bool InRange(RecoilPrototype world)
    {
        return isActiveAndEnabled && world && world.Player &&
            Vector2.Distance(world.Player.Position,transform.position)<=interactionRange &&
            world.Clear(world.Player.Position,transform.position);
    }
    public bool TryRead(RecoilPrototype world)
    {return world && world.OpenReadable(this);}
    void OnDrawGizmosSelected(){Gizmos.color=new Color(.8f,.7f,.4f);Gizmos.DrawWireSphere(transform.position,interactionRange);}
}
