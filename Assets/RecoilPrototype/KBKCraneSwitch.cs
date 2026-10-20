using UnityEngine;

[DisallowMultipleComponent,AddComponentMenu("Recoil/KBK Crane Switch")]
public sealed class KBKCraneSwitch : ContinuousInteractionItem
{
    public KBKStackerCrane crane;
    public override bool Advance(RecoilPrototype world,float dt)
    {
        if(!crane)return true;
        if(crane.Activate())world.ShowInteractionMessage("Stacker crane: lift, travel, lower.");
        else world.ShowInteractionMessage(!crane.HasPower?"Freight has no power. Check the distribution box outside.":crane.Busy?"The crane is already moving.":"The crane is at its destination.");
        Cancel();return true;
    }
    public override void ResetItem(RecoilPrototype world=null){base.ResetItem(world);if(crane)crane.ResetMachine();}
}
