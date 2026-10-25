using System.Collections.Generic;
using UnityEngine;

// Shared local sound propagation for actors and machinery. Behaviour remains with the source.
public sealed class LocalSoundWaveEmitter
{
 readonly RecoilPrototype world;readonly Transform owner;
 sealed class Pulse
 {
  public Vector3 origin;public float radius,duration,age;public Color color;public LineRenderer line;
  public readonly HashSet<RampagerController> delivered=new HashSet<RampagerController>();
 }
 readonly List<Pulse> pulses=new List<Pulse>();
 public LocalSoundWaveEmitter(RecoilPrototype context,Transform parent){world=context;owner=parent;}
 public void Clear(){foreach(var p in pulses)if(p.line)Object.Destroy(p.line.gameObject);pulses.Clear();}
 public void Emit(Vector2 position,float radius,float speed,float width,Color color,Material material)
 {Emit(new Vector3(position.x,position.y,owner?owner.position.z:0),radius,speed,width,color,material);}
 public void Emit(Vector3 position,float radius,float speed,float width,Color color,Material material)
 {
  if(!world||radius<=0)return;
  var go=new GameObject("Local sound ripple");go.transform.SetParent(owner,false);
  var line=go.AddComponent<LineRenderer>();line.sharedMaterial=material?material:world.SprayMaterial;
  line.useWorldSpace=true;line.loop=true;line.positionCount=128;line.startWidth=line.endWidth=width;line.sortingOrder=150;line.startColor=line.endColor=color;
  var pulse=new Pulse{origin=position,radius=radius,duration=radius/Mathf.Max(.01f,speed),color=color,line=line};pulses.Add(pulse);DrawRing(pulse,.05f);
 }
 public void Simulate(float dt)
 {
  for(int i=pulses.Count-1;i>=0;i--)
  {
   var pulse=pulses[i];pulse.age+=dt;float progress=Mathf.Clamp01(pulse.age/pulse.duration);
   foreach(var enemy in world.Rampagers)
    if(enemy&&!pulse.delivered.Contains(enemy)&&enemy.DistanceToSound(pulse.origin)<=pulse.radius*progress)
    {enemy.HearNoise(pulse.origin,pulse.radius);pulse.delivered.Add(enemy);}
   if(progress>=1){if(pulse.line)Object.Destroy(pulse.line.gameObject);pulses.RemoveAt(i);continue;}
   var color=pulse.color;color.a*=1-progress;pulse.line.startColor=pulse.line.endColor=color;DrawRing(pulse,Mathf.Max(.05f,pulse.radius*progress));
  }
 }
 static void DrawRing(Pulse pulse,float radius)
 {for(int n=0;n<pulse.line.positionCount;n++){float angle=n*Mathf.PI*2/pulse.line.positionCount;pulse.line.SetPosition(n,pulse.origin+new Vector3(Mathf.Cos(angle),Mathf.Sin(angle),0)*radius);}}
}
