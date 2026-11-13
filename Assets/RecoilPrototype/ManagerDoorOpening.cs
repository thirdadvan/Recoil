using UnityEngine;

// Presentation for the authored manager door. Only the leaf/inset/handle move;
// corridor collision, frame and destination remain on RecoilDoor.
[DisallowMultipleComponent]
public sealed class ManagerDoorOpening : MonoBehaviour
{
    RecoilPrototype world;
    RecoilDoor door;
    Transform hinge;
    GameObject aperture;
    Material apertureMaterial;
    bool initialized,is3D;
    float amount;
    public float Progress {get{return amount;}}
    public bool FullyOpen {get{return amount>=.999f;}}
    public void Initialize(RecoilPrototype state,RecoilDoor exit)
    {
        world=state;door=exit;if(initialized)return;initialized=true;
        var leaf=door.transform.Find("Door leaf");
        if(leaf){
            var visual=leaf.GetComponent<Recoil3DSpriteVisual>();is3D=visual&&visual.meshRenderer;
            Bounds bounds=is3D?visual.meshRenderer.bounds:leaf.GetComponent<SpriteRenderer>()?leaf.GetComponent<SpriteRenderer>().bounds:new Bounds(leaf.position,new Vector3(1.13f,2.38f,.06f));
            hinge=new GameObject("Manager door opening hinge").transform;hinge.SetParent(door.transform,true);hinge.position=new Vector3(bounds.min.x,bounds.center.y,is3D?bounds.center.z:leaf.position.z);
            foreach(var name in new[]{"Door leaf","Door inset","Handle"}){
                var part=door.transform.Find(name);if(!part)continue;
                var mesh=part.GetComponent<Recoil3DSpriteVisual>();
                if(is3D){if(mesh&&mesh.meshRenderer)mesh.meshRenderer.transform.SetParent(hinge,true);}
                else part.SetParent(hinge,true);
            }
            if(is3D){
                aperture=GameObject.CreatePrimitive(PrimitiveType.Cube);aperture.name="Manager office open doorway";aperture.transform.SetParent(door.transform,true);
                aperture.transform.position=new Vector3(bounds.center.x,bounds.center.y,bounds.min.z-.10f);aperture.transform.localScale=new Vector3(bounds.size.x*.98f,bounds.size.y*.98f,.015f);
                var collider=aperture.GetComponent<Collider>();if(collider){if(Application.isPlaying)Destroy(collider);else DestroyImmediate(collider);}
                var shader=Shader.Find("Unlit/Color");if(!shader)shader=Shader.Find("Universal Render Pipeline/Unlit");
                apertureMaterial=new Material(shader){color=new Color(.035f,.045f,.04f)};aperture.GetComponent<Renderer>().sharedMaterial=apertureMaterial;
            }else{
                var source=leaf.GetComponent<SpriteRenderer>();if(source&&source.sprite){
                    aperture=new GameObject("Manager office open doorway");aperture.transform.SetParent(door.transform,true);aperture.transform.position=bounds.center;
                    var r=aperture.AddComponent<SpriteRenderer>();r.sprite=source.sprite;r.sharedMaterial=source.sharedMaterial;r.color=new Color(.035f,.045f,.04f);r.sortingLayerID=source.sortingLayerID;r.sortingOrder=source.sortingOrder-1;
                    var size=source.sprite.bounds.size;aperture.transform.localScale=new Vector3(bounds.size.x/Mathf.Max(.01f,size.x),bounds.size.y/Mathf.Max(.01f,size.y),1);
                }
            }
        }
        if(world.HasItem(DoorDialogue.GlassesReturnedFlag)||world.HasItem(DoorDialogue.ManagerDoorOpenFlag)){amount=1;world.RememberProgress(DoorDialogue.ManagerDoorOpenFlag);}
        Apply();
    }
    public void Open(){Advance(0);}
    public void Advance(float dt)
    {
        if(!initialized||!world)return;
        bool open=world.HasItem(DoorDialogue.GlassesReturnedFlag)||world.HasItem(DoorDialogue.ManagerDoorOpenFlag);
        amount=Mathf.MoveTowards(amount,open?1:0,Mathf.Max(0,dt)/.45f);Apply();
        if(open&&FullyOpen)world.RememberProgress(DoorDialogue.ManagerDoorOpenFlag);
    }
    void Apply()
    {
        if(hinge){if(is3D)hinge.localRotation=Quaternion.Euler(0,78*Mathf.SmoothStep(0,1,amount),0);else hinge.localScale=new Vector3(Mathf.Lerp(1,.14f,Mathf.SmoothStep(0,1,amount)),1,1);}
        if(aperture)aperture.SetActive(amount>.01f);
    }
    void Update(){Advance(Time.unscaledDeltaTime);}
    void OnDestroy(){if(apertureMaterial){if(Application.isPlaying)Destroy(apertureMaterial);else DestroyImmediate(apertureMaterial);}}
}
