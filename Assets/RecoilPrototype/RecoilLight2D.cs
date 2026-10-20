using UnityEngine;
using System.Collections.Generic;

// A light's position, radius and brightness can be edited directly in the scene.
[ExecuteAlways,DisallowMultipleComponent]
public class RecoilLight2D : MonoBehaviour
{
    static readonly List<RecoilLight2D> active=new List<RecoilLight2D>();
    public static IReadOnlyList<RecoilLight2D> Active {get{return active;}}
    public static int Revision {get;private set;}
    [Min(.1f)] public float radius=2;
    [Range(0,1)] public float intensity=.25f;
    public virtual float InfluenceRadius {get{return radius;}}
    public virtual float BrightnessAt(Vector2 point){return RadialBrightness(point);}
    protected float RadialBrightness(Vector2 point){return intensity*(1-Mathf.SmoothStep(0,1,Mathf.Clamp01(Vector2.Distance(point,transform.position)/Mathf.Max(.1f,radius))));}
    protected virtual void OnEnable(){if(!active.Contains(this))active.Add(this);Revision++;}
    protected virtual void OnDisable(){active.Remove(this);Revision++;}
    protected virtual void OnValidate(){radius=Mathf.Max(.1f,radius);Revision++;}
    protected virtual void OnTransformParentChanged(){Revision++;}
    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color=new Color(1,.85f,.45f,.7f);
        Gizmos.DrawWireSphere(transform.position,radius);
    }
}
