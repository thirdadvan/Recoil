using UnityEngine;
public sealed class Recoil3DMachineMotion : MonoBehaviour
{
    public float degreesPerSecond=12;
    RecoilPrototype world;
    void Start(){world=GetComponentInParent<RecoilPrototype>();}
    void Update(){if(world && world.Playing)transform.Rotate(Vector3.up,degreesPerSecond*world.FrameDelta,Space.Self);}
}
