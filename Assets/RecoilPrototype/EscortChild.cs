using UnityEngine;

// A separate vulnerable actor; following, footsteps and hand contact belong to the child.
[DefaultExecutionOrder(-400),DisallowMultipleComponent,RequireComponent(typeof(PlayerNoiseEmitter))]
[AddComponentMenu("Recoil/Escort Child")]
public sealed class EscortChild : MonoBehaviour
{
 public const string EscortFlag="child_escorted";
 [Min(.1f)] public float interactionRange=1.4f,bodyHeight=1.05f;
 [Min(.1f)] public float handDistance=.75f,maxHandDistance=1.8f;
 [Min(.1f),Tooltip("长按 E 让小孩停留所需的秒数。") ] public float waitHoldDuration=.6f;
 [Header("Automatic hand-holding")]
 [Tooltip("满足条件并靠近时自动牵手；让孩子等待后，先走开再靠近即可重新牵手。")] public bool autoTakeHandOnApproach=true;
 [Tooltip("剧情条件预留，目前默认满足；以后可由剧情控制。")] public bool storyConditionSatisfied=true;
 [Tooltip("可选：需要游戏进度中已有此标记。留空不额外限制。")] public string requiredStoryFlag="";
 [Min(.1f)] public float autoHandRange=.9f;
 // Re-arm only after walking away, so releasing the hand does not immediately undo itself.
 bool awaitingSeparation;
 public void SetStoryConditionSatisfied(bool satisfied){storyConditionSatisfied=satisfied;}
 bool eTracking,eReleaseRequired;float eHeldTime;
 public float WaitHoldProgress {get{return eTracking?Mathf.Clamp01(eHeldTime/waitHoldDuration):0;}}
 static string waitingScene;static Vector2 waitingPosition;
 public static bool WaitingElsewhere(string scene){return !string.IsNullOrEmpty(waitingScene)&&waitingScene!=scene;}
 public static bool TryWaitingPosition(string scene,out Vector2 position){position=waitingPosition;return waitingScene==scene;}
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
 public static void ClearWaiting(){waitingScene=null;}
 public void RestoreWaiting(Vector2 position){Position=position;Following=false;awaitingSeparation=true;SyncVisuals();}
 // Short press interactions are dispatched on release, allowing a hold to take priority.
 public bool RouteE(bool held,bool pressed,bool released,float dt,out bool tap)
 {
  tap=pressed;
  if(eReleaseRequired){tap=false;if(!held)eReleaseRequired=false;return true;}
  if(Following && world.InObjectInteractionRange)
  {
   if(eTracking){eTracking=false;eHeldTime=0;eReleaseRequired=held;tap=false;return true;}
   return false;
  }
  if(Following&&pressed){eTracking=true;eHeldTime=0;}
  if(!eTracking)return false;
  tap=false;
  if(held){eHeldTime+=dt;if(eHeldTime>=waitHoldDuration){AskToWait();eTracking=false;eReleaseRequired=true;}return true;}
  eTracking=false;tap=released;return !tap;
 }
 public void AskToWait()
 {
  if(!Following||!Alive||world.InObjectInteractionRange)return;
  Following=false;awaitingSeparation=true;SilentWalking=false;waitingScene=gameObject.scene.path;waitingPosition=Position;
  world.SetProgressFlag(EscortFlag,false);world.InterruptInteraction();noise.ResetNoise();SyncVisuals();world.ShowInteractionMessage("Stay here. I'll come back for you.");
 }
 public bool Following {get;private set;}
 public bool Alive {get;private set;}=true;
 public bool SilentWalking {get;private set;}
 public bool Grounded {get;private set;}
 public Vector2 Position {get{return transform.position;}private set{transform.position=new Vector3(value.x,value.y,0);}}
 public float Illumination {get{bool cone;float b=world.EnvironmentBrightness(Position,out cone);return cone?1:Mathf.Lerp(.4f,1,Mathf.Clamp01(b/.45f));}}
 RecoilPrototype world;PlayerNoiseEmitter noise;Vector2 home;float verticalSpeed,followSide=-1,fallDistance;
 WalkableStairway lastFollowedStair;
 LineRenderer hand;Transform body,head;
 public void Initialize(RecoilPrototype context){world=context;home=Position;noise=GetComponent<PlayerNoiseEmitter>();noise.Initialize(world);body=transform.Find("Body");head=transform.Find("Head");hand=GetComponent<LineRenderer>();}
 public void ResetChild(bool following)
 {
  awaitingSeparation=!following && waitingScene==gameObject.scene.path;
  eTracking=eReleaseRequired=false;eHeldTime=0;Following=following;Alive=true;SilentWalking=false;Grounded=false;verticalSpeed=fallDistance=0;followSide=-1;lastFollowedStair=null;noise.ResetNoise();
  Position=following?world.Player.Position+new Vector2(-handDistance,(bodyHeight-world.Player.BodyHeight)*.5f):home;
  if(hand)hand.enabled=false;
 }
 public bool InRange(){return Alive&&!Following&&isActiveAndEnabled&&world&&Vector2.Distance(Position,world.Player.Position)<=interactionRange&&world.Clear(Position,world.Player.Position);}
 public bool TryAutoTakeHand()
 {
  if(!Alive || !world || !world.Player)return false;
  if(awaitingSeparation)
  {
   if(Vector2.Distance(Position,world.Player.Position)<=autoHandRange+.25f)return false;
   awaitingSeparation=false;
  }
  if(!autoTakeHandOnApproach || !storyConditionSatisfied || !InRange())return false;
  if(!string.IsNullOrEmpty(requiredStoryFlag) && !world.HasItem(requiredStoryFlag))return false;
  var p=world.Player;
  if(!p.Grounded || Vector2.Distance(Position,p.Position)>autoHandRange || Mathf.Abs((p.Position.y-p.BodyHeight*.5f)-(Position.y-bodyHeight*.5f))>.35f)return false;
  TakeHand();return true;
 }
 void TakeHand()
 {
  Following=true;awaitingSeparation=false;ClearWaiting();eTracking=false;eHeldTime=0;eReleaseRequired=true;
  world.InterruptInteraction();followSide=Position.x<world.Player.Position.x?-1:1;
  world.RememberProgress(EscortFlag);world.Player.CancelEscortFlight();
  world.ShowInteractionMessage("The child takes your hand.");
 }
 public void TakeHit(){if(!Alive)return;Alive=false;Following=false;if(hand)hand.enabled=false;world.ChildKilled();}
 public Vector2 LimitPlayerStep(Vector2 from,Vector2 amount)
 {
  if(!Following||!Alive)return amount;
  // Stop the hand from stretching through a wall when the child cannot follow.
  float next=from.x+amount.x,d=next-Position.x;
  if(Mathf.Abs(d)>maxHandDistance&&Mathf.Abs(d)>Mathf.Abs(from.x-Position.x))amount.x=Mathf.Clamp(next,Position.x-maxHandDistance,Position.x+maxHandDistance)-from.x;
  return amount;
 }
 public void CarryBySoftPlatform(SoftRoofPlatform roof,Rect previous,Vector2 delta)
 {
  if(!Alive||!Grounded||!roof.Available)return;
  if(Position.x+.22f<=previous.xMin||Position.x-.22f>=previous.xMax||Mathf.Abs(Position.y-bodyHeight*.5f-previous.yMax)>.035f)return;
  Move(delta);Grounded=true;SyncVisuals();
 }
 void Update(){if(world&&world.Playing)Simulate(world.FrameDelta);}
 public void Simulate(float dt)
 {
  if(!Alive||!world||dt<=0)return;
  TryAutoTakeHand();
  var p=world.Player;Vector2 move=Vector2.zero,old=Position;bool wasGrounded=Grounded;
  SilentWalking=Following&&(p.SilentWalking||p.Crouched);
  bool stairLandingSupport=false;
  if(!Following)lastFollowedStair=null;
  if(Following)
  {
   float gap=p.Position.x-Position.x;if(Mathf.Abs(p.HorizontalIntent)>.1f)followSide=-Mathf.Sign(p.HorizontalIntent);float desired=p.Position.x+followSide*handDistance-Position.x;float speed=(SilentWalking?p.sneakSpeed:p.walkSpeed);
   if(p.WalkingStairs)lastFollowedStair=p.CurrentStairway;
   else if(TryStairLanding(out var landing))
   {
    float target=Mathf.Clamp(p.Position.x+followSide*handDistance,landing.xMin+.22f,landing.xMax-.22f);
    desired=target-Position.x;move.y=landing.yMax+bodyHeight*.5f-Position.y;verticalSpeed=0;stairLandingSupport=true;
    float normalTarget=p.Position.x+followSide*handDistance;
    if(Position.x-.22f>=landing.xMin && Position.x+.22f<=landing.xMax && normalTarget-.22f>=landing.xMin && normalTarget+.22f<=landing.xMax)lastFollowedStair=null;
   }
   move.x=Mathf.Sign(desired)*Mathf.Min(Mathf.Abs(desired),speed*dt);
   float playerFeet=p.Position.y-p.BodyHeight*.5f;
   if(!stairLandingSupport&&Grounded&&playerFeet>Position.y-bodyHeight*.5f+.18f&&Mathf.Abs(gap)<maxHandDistance)verticalSpeed=3.2f;
   // Stairways provide an authored walking surface rather than a physical ramp collider.
   if(p.WalkingStairs&&Mathf.Abs(gap)<maxHandDistance){move.y=playerFeet+bodyHeight*.5f-Position.y;verticalSpeed=0;}
  }
  if(!(Following&&p.WalkingStairs)&&!stairLandingSupport){verticalSpeed-=p.gravity*dt;move.y=verticalSpeed*dt;}
  Move(move);if(Following&&p.WalkingStairs || stairLandingSupport)Grounded=true;
  if(Following)noise.UpdateFootsteps(Position,Grounded,SilentWalking,(Position.x-old.x)/Mathf.Max(dt,.0001f));
  if(!wasGrounded)fallDistance+=Mathf.Max(0,old.y-Position.y);
  if(Grounded){if(!wasGrounded&&fallDistance>p.StandingHeight)noise.EmitLanding(Position);fallDistance=0;}else if(Position.y>old.y)fallDistance=0;
  if(!Following&&waitingScene==gameObject.scene.path)waitingPosition=Position;
  SyncVisuals();
 }
 bool TryStairLanding(out Rect landing)
 {
  landing=default(Rect);if(!lastFollowedStair || !lastFollowedStair.Available)return false;
  var feet=world.Player.Position-Vector2.up*world.Player.BodyHeight*.5f;
  var low=lastFollowedStair.PositionAt(0);var high=lastFollowedStair.PositionAt(lastFollowedStair.Length);
  bool upper=Vector2.Distance(feet,high)<Vector2.Distance(feet,low);var endpoint=upper?high:low;
  if((upper&&!lastFollowedStair.upperLandingAvailable) || Mathf.Abs(feet.y-endpoint.y)>.2f || Mathf.Abs(feet.x-endpoint.x)>maxHandDistance)
  {lastFollowedStair=null;return false;}
  foreach(var r in world.SolidRects)if(IsLanding(r,endpoint)){landing=r;return true;}
  foreach(var roof in world.SoftRoofs)if(roof&&roof.Available&&IsLanding(roof.Area,endpoint)){landing=roof.Area;return true;}
  lastFollowedStair=null;return false;
 }
 static bool IsLanding(Rect r,Vector2 endpoint)
 {return r.width>.44f && Mathf.Abs(r.yMax-endpoint.y)<.12f && endpoint.x>=r.xMin-.22f && endpoint.x<=r.xMax+.22f;}
 void Move(Vector2 amount)
 {
  Vector2 pos=Position;float half=bodyHeight*.5f,oldX=pos.x;const float width=.22f,eps=.0001f;
  float nextX=pos.x+amount.x;
  foreach(var r in world.SolidRects)
  {
   if(pos.y+half<=r.yMin+eps||pos.y-half>=r.yMax-eps)continue;
   if(amount.x>0&&oldX+width<=r.xMin+eps&&nextX+width>=r.xMin)nextX=Mathf.Min(nextX,r.xMin-width);
   if(amount.x<0&&oldX-width>=r.xMax-eps&&nextX-width<=r.xMax)nextX=Mathf.Max(nextX,r.xMax+width);
  }
  pos.x=nextX;float oldY=pos.y;pos.y+=amount.y;Grounded=false;
  foreach(var r in world.SolidRects)
  {
   if(pos.x+width<=r.xMin||pos.x-width>=r.xMax)continue;
   if(amount.y<=0&&oldY-half>=r.yMax-eps&&pos.y-half<=r.yMax){pos.y=Mathf.Max(pos.y,r.yMax+half);Grounded=true;verticalSpeed=0;}
   else if(amount.y>0&&oldY+half<=r.yMin+eps&&pos.y+half>=r.yMin){pos.y=Mathf.Min(pos.y,r.yMin-half);verticalSpeed=0;}
  }
  foreach(var roof in world.SoftRoofs)if(roof&&roof.Available)
  {var r=roof.Area;if(!(Following&&world.Player.Position.y-world.Player.BodyHeight*.5f<r.yMax-.08f)&&pos.x+width>r.xMin&&pos.x-width<r.xMax&&amount.y<=0&&oldY-half>=r.yMax-eps&&pos.y-half<=r.yMax){pos.y=Mathf.Max(pos.y,r.yMax+half);Grounded=true;verticalSpeed=0;}}
  Position=pos;
 }
 void SyncVisuals()
 {
  if(body){body.localPosition=new Vector3(0,SilentWalking?-.12f:-.06f,0);body.localScale=new Vector3(.38f,SilentWalking?.48f:.68f,1);}
  if(head)head.localPosition=new Vector3(0,SilentWalking?.21f:.37f,0);
  if(hand){hand.enabled=Following&&Alive;hand.SetPosition(0,Position+Vector2.up*.1f);hand.SetPosition(1,world.Player.Position-Vector2.up*.1f);}
 }
 void OnDrawGizmosSelected(){Gizmos.color=Color.cyan;Gizmos.DrawWireSphere(transform.position,interactionRange);}
}
