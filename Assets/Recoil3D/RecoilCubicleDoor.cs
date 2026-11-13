using UnityEngine;

// Local hinged prop: uses the world's existing E prompt and interaction routing.
[AddComponentMenu("Recoil/Interactive Cubicle Door")]
public sealed class RecoilCubicleDoor : ContinuousInteractionItem
{
    public Transform hinge;
    public float openAngle=-82f;
    public float swingDuration=.45f;
    bool open;
    float angle;
    public bool IsOpen { get { return open; } }
    void Awake(){interactionDuration=0;UpdatePrompt();}
    void UpdatePrompt(){displayName=open?"CLOSE CUBICLE DOOR":"OPEN CUBICLE DOOR";}
    public override bool Advance(RecoilPrototype world,float dt)
    {
        open=!open;UpdatePrompt();Cancel();return true;
    }
    public override void ResetItem(RecoilPrototype world=null)
    {
        base.ResetItem(world);Completed=false;open=false;angle=0;
        if(hinge)hinge.localRotation=Quaternion.identity;
        UpdatePrompt();
    }
    void Update()
    {
        if(!Application.isPlaying||!hinge)return;
        var world=GetComponentInParent<RecoilPrototype>();
        if(!world||!world.Playing)return;
        angle=Mathf.MoveTowards(angle,open?openAngle:0,Mathf.Abs(openAngle)/Mathf.Max(.05f,swingDuration)*Time.deltaTime);
        hinge.localRotation=Quaternion.Euler(0,angle,0);
    }
}
