using UnityEngine;

public sealed class WindingKeyPickup : ContinuousInteractionItem
{
 public GameObject model;
 public RecoilCubicleDoor cubicleDoor;
 void Awake(){if(!GetComponent<PickupVisibility>())gameObject.AddComponent<PickupVisibility>();}
 public override bool InRange(RecoilPrototype world){return (!cubicleDoor||cubicleDoor.IsOpen)&&base.InRange(world);}
 public override bool Advance(RecoilPrototype world,float dt)
 {if(Completed||world.HasItem(WindUpArchiveClock.KeyId))return true;if(!InRange(world)||!base.Advance(world,dt))return false;if(model)model.SetActive(false);return true;}
 public override void ResetItem(RecoilPrototype world=null){base.ResetItem(world);if(model)model.SetActive(!Completed);}
}
