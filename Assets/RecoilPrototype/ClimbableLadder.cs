using UnityEngine;

[DisallowMultipleComponent,RequireComponent(typeof(BoxCollider2D)),AddComponentMenu("Recoil/Climbable Ladder")]
public sealed class ClimbableLadder : MonoBehaviour
{
    [Min(.1f)] public float climbSpeed=2.4f;
    [Min(0),Tooltip("按左右键离开时，脚边地面的最大距离（世界单位）。")] public float groundExitDistance=.12f;
    BoxCollider2D area;
    public Rect Area
    {get{if(!area)area=GetComponent<BoxCollider2D>();var b=area.bounds;return new Rect(b.min.x,b.min.y,b.size.x,b.size.y);}}
    public bool Available {get{if(!area)area=GetComponent<BoxCollider2D>();return isActiveAndEnabled && area.enabled;}}
    void Reset(){GetComponent<BoxCollider2D>().isTrigger=true;}
    public bool Contains(Vector2 p,float height)
    {
        if(!Available)return false;var r=Area;
        return p.x+.32f>r.xMin && p.x-.32f<r.xMax && p.y+height*.5f>=r.yMin-.03f && p.y-height*.5f<=r.yMax+.03f;
    }
    void OnDrawGizmosSelected()
    {Gizmos.color=new Color(.4f,1,.55f,.8f);var box=GetComponent<BoxCollider2D>();Gizmos.matrix=transform.localToWorldMatrix;Gizmos.DrawWireCube(box.offset,box.size);}
}
