using UnityEngine;

// Only ordinary watching can grab. Uses the same physical reach and wall test as attacks.
[DisallowMultipleComponent,AddComponentMenu("Recoil/Rampager Grab")]
public sealed class RampagerGrab : MonoBehaviour
{
    [Min(.1f),Tooltip("实际三维抓取距离，不会隔墙、隔楼层或从远景抓取。")]
    public float reach=3.5f;
    RampagerController owner;RampagerPerception senses;
    public bool IsGrabbing {get;private set;}
    public bool Evaluate()
    {
        if(!owner)owner=GetComponent<RampagerController>();
        if(!senses)senses=GetComponent<RampagerPerception>();
        IsGrabbing=isActiveAndEnabled && owner && owner.isActiveAndEnabled && owner.WatchingPlayer && !owner.PreparingAttack && senses && senses.CanTouchPlayer(reach);
        return IsGrabbing;
    }
    public void Release(){IsGrabbing=false;}
    void OnDisable(){Release();}
}
