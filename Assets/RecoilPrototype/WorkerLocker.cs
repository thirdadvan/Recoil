using UnityEngine;

// The world owns input and the modal; this component only owns the physical door.
[DisallowMultipleComponent]
public sealed class WorkerLocker : MonoBehaviour
{
    public Transform doorHinge;
    public float interactionRange=1.45f,openingDuration=.45f,openAngle=105;
    float elapsed;
    public bool PageReady {get;private set;}
    public bool InRange(RecoilPrototype world)
    {return isActiveAndEnabled&&world&&world.Player&&RecoilInteractionRange.Contains(world,transform.position,interactionRange)&&world.Clear(world.Player.Position,transform.position);}
    public void BeginOpen(){elapsed=0;PageReady=false;SetDoor(0);}
    public void AdvanceOpening(float dt)
    {elapsed+=Mathf.Max(0,dt);float t=Mathf.Clamp01(elapsed/Mathf.Max(.05f,openingDuration));SetDoor(t*t*(3-2*t));PageReady=t>=1;}
    void SetDoor(float t){if(doorHinge)doorHinge.localRotation=Quaternion.Euler(0,openAngle*t,0);}
    public void CloseDoor(){elapsed=0;PageReady=false;SetDoor(0);}
}
