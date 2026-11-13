using UnityEngine;

// Silent until explicitly wound with the inventory key. Uses ordinary local sound propagation.
public sealed class WindUpArchiveClock : ContinuousInteractionItem
{
 public const string KeyId="winding_key",WoundFlag="archive_clock_wound",ChimedFlag="archive_clock_chimed_eleven";
 public string requiredReadFlag="";
 [Tooltip("Mechanical chimes carry equally in all directions. Player footsteps retain rear hearing attenuation.")] public bool directionalSound;
 public Transform pendulum,hourHand,minuteHand,soundOrigin,windingPoint;
 public AudioSource chime;
 public float beatSpacing=3.5f,firstStrikeDelay=.75f,soundRadius=10,propagationSpeed=5;
 [Range(1,12)] public int strikeCount=11;
 RecoilPrototype world;LocalSoundWaveEmitter waves;
 float elapsed,nextBeat=.75f;int beat;bool audioPaused;
 Quaternion restPendulum,restHour,restMinute;bool captured;
 public bool IsWound {get;private set;}
 public bool Winding {get;private set;}
 // OneShot includes the final gong's long reverb tail; do not unlock at the eleventh strike.
 public bool Chiming {get{return IsWound && (beat<strikeCount || audioPaused || (chime&&chime.isPlaying));}}
 public float windingDuration=2;
 float windingContactProgress=-1;
 Quaternion windingHour,windingMinute;
 public int EmittedPulseCount {get;private set;}
 void Bind()
 {
  if(!world)world=GetComponentInParent<RecoilPrototype>();if(waves==null&&world)waves=new LocalSoundWaveEmitter(world,transform);
  if(!windingPoint&&transform.parent)foreach(var part in transform.parent.GetComponentsInChildren<Transform>(true))if(part.name=="Winding key socket"){windingPoint=part;break;}
  if(!captured){if(pendulum)restPendulum=pendulum.localRotation;if(hourHand)restHour=hourHand.localRotation;if(minuteHand)restMinute=minuteHand.localRotation;captured=true;}
 }
 void Awake(){Bind();if(chime){chime.playOnAwake=false;chime.Stop();}}
 public override bool InRange(RecoilPrototype context)
 {
  // Unlike a pickup, winding is repeatable. Keep the saved history flags intact.
  return isActiveAndEnabled && context && context.Player && !Winding && !Chiming && !context.PropOperationActive && context.HasInteractionRequirement(requiredReadFlag)
   && RecoilInteractionRange.Contains(context,transform.position,interactionRange) && context.Clear(context.Player.Position,transform.position);
 }
 public override bool Advance(RecoilPrototype context,float dt)
 {
  Bind();if(!InRange(context))return false;
  if(!context.HasInteractionRequirement(KeyId)){context.ShowInteractionMessage("The clock has stopped. I need a winding key.");Cancel();return true;}
  windingHour=hourHand?hourHand.localRotation:Quaternion.identity;windingMinute=minuteHand?minuteHand.localRotation:Quaternion.identity;
  Winding=true;windingContactProgress=-1;
  bool started=context.PropOperation.Begin(windingDuration,windingPoint?windingPoint.position:(soundOrigin?soundOrigin.position:transform.position),false,WindHands,()=>{
   Winding=false;context.RememberProgress(WoundFlag);context.SetProgressFlag(ChimedFlag,false);Progress=interactionDuration;Completed=true;IsWound=true;elapsed=0;nextBeat=firstStrikeDelay;beat=0;SetWoundTime(0);
   context.ShowInteractionMessage("The hands settle at eleven. The clock begins to chime.");
  },()=>{Winding=false;if(hourHand)hourHand.localRotation=windingHour;if(minuteHand)minuteHand.localRotation=windingMinute;});
  if(!started)Winding=false;return started;
 }
 void WindHands(float progress)
 {
  if(windingContactProgress<0){if(!world.PropOperation.KeyInserted)return;windingContactProgress=progress;}
  float t=Mathf.SmoothStep(0,1,Mathf.InverseLerp(windingContactProgress,Mathf.Max(windingContactProgress+.01f,.78f),progress));
  if(hourHand)hourHand.localRotation=Quaternion.Slerp(windingHour,Quaternion.Euler(0,0,30),t);
  if(minuteHand){float from=Mathf.DeltaAngle(0,windingMinute.eulerAngles.z);minuteHand.localRotation=Quaternion.Euler(0,0,Mathf.Lerp(from,-720,t));}
 }
 public override void ResetItem(RecoilPrototype context=null)
 {
  Bind();Winding=false;if(context)world=context;base.ResetItem(context);IsWound=context&&context.HasItem(WoundFlag);elapsed=0;nextBeat=firstStrikeDelay;beat=IsWound&&context.HasItem(ChimedFlag)?strikeCount:0;EmittedPulseCount=0;
  if(chime)chime.Stop();audioPaused=false;if(waves!=null)waves.Clear();
  if(pendulum)pendulum.localRotation=restPendulum;if(hourHand)hourHand.localRotation=restHour;if(minuteHand)minuteHand.localRotation=restMinute;if(IsWound)SetWoundTime(0);
 }
 void SetWoundTime(float seconds)
 {
  if(minuteHand)minuteHand.localRotation=Quaternion.Euler(0,0,-seconds*.1f);
  if(hourHand)hourHand.localRotation=Quaternion.Euler(0,0,30-seconds/120f);
 }
 public void Simulate(float dt)
 {
  Bind();if(!IsWound||Winding||dt<=0)return;
  elapsed+=dt;
  if(pendulum)pendulum.localRotation=restPendulum*Quaternion.Euler(0,0,Mathf.Sin(elapsed*3.8f)*8);
  SetWoundTime(elapsed);
  while(beat<strikeCount && elapsed>=nextBeat)
  {
   if(chime&&chime.clip)chime.PlayOneShot(chime.clip);
   waves.Emit(soundOrigin?soundOrigin.position:transform.position,soundRadius,propagationSpeed,.024f,new Color(.69f,.75f,.65f,.32f),world.SprayMaterial,directionalSound);EmittedPulseCount++;
   beat++;nextBeat+=Mathf.Max(.1f,beatSpacing);
   if(beat>=strikeCount)world.RememberProgress(ChimedFlag);
  }
  waves.Simulate(dt);
 }
 void Update()
 {
  Bind();bool active=world&&world.Playing&&Time.timeScale>0;
  if(!active){if(chime&&chime.isPlaying){chime.Pause();audioPaused=true;}return;}
  if(audioPaused){if(chime&&IsWound)chime.UnPause();audioPaused=false;}
  Simulate(world.FrameDelta);
 }
 void OnDisable(){if(chime)chime.Stop();if(waves!=null)waves.Clear();}
}
