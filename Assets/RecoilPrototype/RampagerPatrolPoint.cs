using UnityEngine;

// Author these as children of the Rampager. The route captures their world positions at startup.
[AddComponentMenu("Recoil/Rampager Patrol Point")]
[DisallowMultipleComponent]
public sealed class RampagerPatrolPoint : MonoBehaviour
{
    public enum Action { Stay, LookAround, LookLeft, LookRight }
    [Tooltip("序号越小越先访问。相同序号按 Hierarchy 中的顺序执行。")]
    public int order;
    [Min(0), Tooltip("到达后的基础行为时间，单位秒。触须张望按此时间的两倍执行；0 表示直接前往下一个点。")]
    public float duration=2;
    public Action action=Action.Stay;
    [Min(.05f), Tooltip("左右触须扫动的基础间隔；实际间隔为此值的两倍，不改变身体朝向。")]
    public float lookInterval=.75f;
    [Tooltip("可选：自定义行为资源，设置后覆盖上面的内置行为。")]
    public RampagerPatrolAction customAction;

    Vector3 capturedPosition;
    bool captured;
    public Vector3 Destination {get{return Application.isPlaying && captured?capturedPosition:transform.position;}}
    public void CapturePosition(){CapturePosition(transform.position);}
    public void CapturePosition(Vector3 position){capturedPosition=position;captured=true;}
    void OnValidate(){duration=Mathf.Max(0,duration);lookInterval=Mathf.Max(.05f,lookInterval);}
    void OnDrawGizmos()
    {
        Gizmos.color=new Color(.3f,.85f,1,.8f);
        Gizmos.DrawWireSphere(Destination,.16f);
        if(action==Action.LookLeft || action==Action.LookRight)
            Gizmos.DrawRay(Destination,Vector3.right*(action==Action.LookLeft?-.5f:.5f));
    }
}
