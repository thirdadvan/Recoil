using UnityEngine;

[DefaultExecutionOrder(-600),DisallowMultipleComponent,RequireComponent(typeof(KBKStackerCrane))]
[AddComponentMenu("Recoil/Freight Noise Emitter")]
public sealed class FreightNoiseEmitter : MonoBehaviour
{
 public Transform soundOrigin;
 [Min(.1f)] public float soundRadiusMultiplier=1.6f;
 public Transform ActiveOrigin {get{Bind();return soundOrigin?soundOrigin:crane&&crane.travelRig?crane.travelRig:transform;}}
 KBKStackerCrane crane;RecoilPrototype world;LocalSoundWaveEmitter waves;
 float nextPulse;bool wasOperating;
 public int EmittedPulseCount {get;private set;}
 void Bind(){if(!crane)crane=GetComponent<KBKStackerCrane>();if(!world)world=GetComponentInParent<RecoilPrototype>();if(waves==null&&world)waves=new LocalSoundWaveEmitter(world,transform);}
 public void ResetNoise(){if(waves!=null)waves.Clear();wasOperating=false;nextPulse=0;EmittedPulseCount=0;}
 public void Simulate(float dt,bool operating)
 {
  Bind();if(!world||dt<=0)return;
  var steps=world.Player.Noise;
  if(operating&&steps&&(!wasOperating||world.Clock>=nextPulse))
  {
   Vector3 source=ActiveOrigin.position;
   waves.Emit(source,steps.footstepRadius*soundRadiusMultiplier,steps.propagationSpeed,steps.rippleWidth,steps.rippleColor,world.SprayMaterial);
   nextPulse=world.Clock+Mathf.Max(.01f,steps.footstepInterval);EmittedPulseCount++;
  }
  wasOperating=operating;waves.Simulate(dt);
 }
 void Update(){Bind();if(world&&world.Playing)Simulate(world.FrameDelta,crane.Busy&&crane.HasPower);}
 void OnDisable(){ResetNoise();}
}
