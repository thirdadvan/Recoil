using UnityEngine;

// Custom player collision treats only the top face as solid. Rays ignore this surface.
[DisallowMultipleComponent,RequireComponent(typeof(BoxCollider2D)),AddComponentMenu("Recoil/Soft Roof Platform")]
public sealed class SoftRoofPlatform : MonoBehaviour
{
    BoxCollider2D area;
    public Rect Area
    {get{if(!area)area=GetComponent<BoxCollider2D>();var b=area.bounds;return new Rect(b.min.x,b.min.y,b.size.x,b.size.y);}}
    public bool Available {get{if(!area)area=GetComponent<BoxCollider2D>();return isActiveAndEnabled && area.enabled;}}
    void Reset(){GetComponent<BoxCollider2D>().isTrigger=true;}
    void OnDrawGizmosSelected()
    {var b=Area;Gizmos.color=new Color(1,.8f,.3f,.85f);Gizmos.DrawLine(new Vector3(b.xMin,b.yMax,transform.position.z),new Vector3(b.xMax,b.yMax,transform.position.z));}
}
