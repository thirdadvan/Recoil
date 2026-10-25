using UnityEngine;

// Environmental response carries Dun's unspoken thought; no dialogue is emitted.
[DisallowMultipleComponent,AddComponentMenu("Recoil/Fuel Pump Interaction")]
public sealed class FuelPumpInteraction : ContinuousInteractionItem
{
    public Transform nozzle;
    [Tooltip("原本就已损坏：黑屏且无法取用油枪，不记录为剧情完成。")]
    public bool startsBroken;
    public GameObject serviceDisplay,outOfOrderDisplay;
    [Min(.1f)] public float pickupPoseDuration=.8f;
    RecoilPrototype owner;
    Vector3 restPosition;
    Quaternion restRotation;
    bool restCaptured,holdingNozzle;
    float pickupUntil;

    void OnEnable(){CaptureRestPose();SyncDisplay();}
    void CaptureRestPose()
    {if(!restCaptured && nozzle){restPosition=nozzle.localPosition;restRotation=nozzle.localRotation;restCaptured=true;}}
    void SyncDisplay()
    {if(serviceDisplay)serviceDisplay.SetActive(!startsBroken && !Completed);if(outOfOrderDisplay)outOfOrderDisplay.SetActive(!startsBroken && Completed);}
    void ReturnNozzle()
    {holdingNozzle=false;if(nozzle && restCaptured){nozzle.localPosition=restPosition;nozzle.localRotation=restRotation;}}
    public override bool Advance(RecoilPrototype world,float dt)
    {
        if(startsBroken || Completed)return false;
        CaptureRestPose();Progress=interactionDuration;Completed=true;owner=world;
        world.Player.NotifyForbiddenInteraction();world.RememberProgress(resultId);
        pickupUntil=world.Clock+pickupPoseDuration;holdingNozzle=true;
        // All pumps under the same station fail together; only this nozzle is picked up.
        var group=GetComponentInParent<RecoilFuelStation>();
        var station=group?group.transform:transform.parent ? transform.parent : transform;
        foreach(var pump in station.GetComponentsInChildren<FuelPumpInteraction>(true))
        {
            pump.Progress=pump.interactionDuration; pump.Completed=true;
            world.RememberProgress(pump.resultId); pump.SyncDisplay();
        }
        UpdateNozzle();return true;
    }
    void UpdateNozzle()
    {
        if(!holdingNozzle || !owner || !nozzle)return;
        if(owner.Clock>=pickupUntil || !owner.gameObject.activeInHierarchy){ReturnNozzle();return;}
        nozzle.position=owner.Player.WorldPosition+(Vector3)(owner.Player.Aim*.55f)+Vector3.up*.12f;
        nozzle.rotation=Quaternion.Euler(0,0,Mathf.Atan2(owner.Player.Aim.y,owner.Player.Aim.x)*Mathf.Rad2Deg);
    }
    void LateUpdate(){if(owner && owner.Playing)UpdateNozzle();}
    public override void ResetItem(RecoilPrototype world=null)
    {CaptureRestPose();ReturnNozzle();owner=world;base.ResetItem(world);if(startsBroken)Completed=true;SyncDisplay();}
    void OnDisable(){ReturnNozzle();}
}
