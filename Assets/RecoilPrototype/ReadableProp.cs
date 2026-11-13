using UnityEngine;

// Reusable wall poster or text note. World context owns the modal and input routing.
[DisallowMultipleComponent,AddComponentMenu("Recoil/Readable Poster or Note")]
public sealed class ReadableProp : MonoBehaviour
{
    public string displayName="Poster";
    public string interactionPrompt="E / READ POSTER";
    [Min(.1f)] public float interactionRange=1.5f;
    public Texture2D pageImage;
    public Mesh pageMesh;
    [Range(.25f,1f)] public float pageDisplayScale=1f;
    [TextArea(4,20)] public string pageText="";
    public string readProgressFlag="";
    public bool disappearAfterReading;
    public GameObject worldVisual;
    RecoilPrototype owner;
    public bool HasBeenRead(RecoilPrototype world)
    {return world && !string.IsNullOrEmpty(readProgressFlag) && world.HasItem(readProgressFlag);}
    public void RefreshReadState(RecoilPrototype world)
    {
        if(worldVisual && worldVisual!=gameObject && disappearAfterReading)
        {bool visible=!HasBeenRead(world);if(worldVisual.activeSelf!=visible)worldVisual.SetActive(visible);}
    }
    public void OnReadingCompleted(RecoilPrototype world)
    {
        if(!world)return;
        if(!string.IsNullOrEmpty(readProgressFlag))world.RememberProgress(readProgressFlag);
        RefreshReadState(world);
    }
    void LateUpdate()
    {
        if(!disappearAfterReading || string.IsNullOrEmpty(readProgressFlag))return;
        if(!owner)owner=GetComponentInParent<RecoilPrototype>();
        if(owner)RefreshReadState(owner);
    }
    public bool InRange(RecoilPrototype world)
    {
        return isActiveAndEnabled && world && world.Player && (!disappearAfterReading || !HasBeenRead(world)) &&
            RecoilInteractionRange.Contains(world,transform.position,interactionRange) &&
            world.Clear(world.Player.Position,transform.position);
    }
    public bool TryRead(RecoilPrototype world)
    {return world && world.OpenReadable(this);}
    void OnDrawGizmosSelected(){Gizmos.color=new Color(.8f,.7f,.4f);Gizmos.DrawWireSphere(transform.position,interactionRange);}
}
