using UnityEngine;

// Directional workplace lighting with a soft cone and weaker radial spill.
[ExecuteAlways,AddComponentMenu("Recoil/Industrial Spot Lamp")]
public sealed class RecoilIndustrialLamp : RecoilLight2D
{
 [Header("Downward main cone")]
 [Min(.1f)] public float coneLength=8;
 [Min(.1f)] public float coneHalfWidth=2.2f;
 [Range(.01f,1),Tooltip("光锥外围渐暗区域占半宽的比例。")]
 public float edgeSoftness=.25f;
 [Header("Scattered illumination")]
 [Range(0,1),Tooltip("外围散射光相对主光强度的比例。继承的 Radius 控制散射范围。")]
 public float scatterIntensity=.18f;
 public Vector2 Direction {get{return -(Vector2)transform.up;}}
 public override float InfluenceRadius {get{return Mathf.Max(radius,Mathf.Sqrt(coneLength*coneLength+coneHalfWidth*coneHalfWidth));}}
 public override float BrightnessAt(Vector2 point)
 {
  Vector2 delta=point-(Vector2)transform.position,dir=Direction;
  float depth=Vector2.Dot(delta,dir),side=Mathf.Abs(delta.x*dir.y-delta.y*dir.x);
  float main=0;
  if(depth>=0&&depth<=coneLength)
  {
   float width=Mathf.Max(.05f,coneHalfWidth*depth/Mathf.Max(.1f,coneLength));
   float edge=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(width*(1-edgeSoftness),width,side));
   float end=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(coneLength*.85f,coneLength,depth));
   main=intensity*edge*end*(1-.35f*depth/Mathf.Max(.1f,coneLength));
  }
  return Mathf.Max(main,RadialBrightness(point)*scatterIntensity);
 }
 protected override void OnValidate(){coneLength=Mathf.Max(.1f,coneLength);coneHalfWidth=Mathf.Max(.1f,coneHalfWidth);edgeSoftness=Mathf.Clamp(edgeSoftness,.01f,1);base.OnValidate();}
 protected override void OnDrawGizmosSelected()
 {
  Vector2 origin=transform.position,dir=Direction,side=new Vector2(-dir.y,dir.x),end=origin+dir*coneLength;
  Gizmos.color=new Color(1,.85f,.4f,.85f);Gizmos.DrawLine(origin,end+side*coneHalfWidth);Gizmos.DrawLine(origin,end-side*coneHalfWidth);Gizmos.DrawLine(end-side*coneHalfWidth,end+side*coneHalfWidth);
  Gizmos.color=new Color(1,.85f,.4f,.25f);Gizmos.DrawWireSphere(origin,radius);
 }
}
