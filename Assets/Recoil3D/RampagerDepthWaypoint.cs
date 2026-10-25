using UnityEngine;

[DisallowMultipleComponent,AddComponentMenu("Recoil/3D/Rampager Depth Waypoint")]
public sealed class RampagerDepthWaypoint : MonoBehaviour
{
    [Tooltip("连接是双向的。只连接同一条实际通道内能走通的点。")]
    public RampagerDepthWaypoint[] connections=new RampagerDepthWaypoint[0];
    [Tooltip("此点位于玩家移动平面 Z=0，是可进入的位置。")]
    public bool gameplayEntrance;
    public bool available=true;
    public bool Open {get{return available&&isActiveAndEnabled&&gameObject.activeInHierarchy;}}
    void OnDrawGizmosSelected()
    {
        Gizmos.color=gameplayEntrance?Color.yellow:new Color(.2f,.8f,1,.8f);Gizmos.DrawWireSphere(transform.position,.15f);
        if(connections!=null)foreach(var node in connections)if(node)Gizmos.DrawLine(transform.position,node.transform.position);
    }
}
