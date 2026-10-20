using UnityEngine;

[DisallowMultipleComponent,RequireComponent(typeof(BoxCollider2D)),AddComponentMenu("Recoil/Equipment Handhold")]
public sealed class EquipmentHandhold : MonoBehaviour
{
    public Transform holdPoint;
    [Min(.1f)] public float releaseCooldown=.5f;
    BoxCollider2D area;
    public Vector2 Position {get{return holdPoint?holdPoint.position:transform.position;}}
    public bool Contains(Vector2 p,float height)
    {
        if(!isActiveAndEnabled)return false;if(!area)area=GetComponent<BoxCollider2D>();
        if(!area.enabled)return false;var b=area.bounds;
        return p.x+.32f>b.min.x && p.x-.32f<b.max.x && p.y+height*.5f>b.min.y && p.y-height*.5f<b.max.y;
    }
    void Reset(){GetComponent<BoxCollider2D>().isTrigger=true;}
    void OnDrawGizmosSelected(){Gizmos.color=Color.yellow;Gizmos.DrawWireSphere(Position,.18f);}
}
