using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-500)]
[DisallowMultipleComponent,RequireComponent(typeof(PlayerNoiseEmitter))]
public sealed class RecoilPlayerController : MonoBehaviour
{
    [SerializeField] RecoilPrototype world;
    [SerializeField] Camera aimCamera;
    [SerializeField] Transform coat, head, hat, gun;
    SpriteRenderer gunRenderer;
    [Header("Movement")]
    public float walkSpeed=2.52f, sneakSpeed=.99f, hoverSpeed=2.2f, verticalHoverSpeed=1.5f;
    public float gravity=14, jumpSpeed=5;
    [Header("Damage")]
    [Min(1)] public int maxHealth=3;
    public int Health {get;private set;}
    public void RestoreHealth(int value){Health=Mathf.Clamp(value,1,Mathf.Max(1,maxHealth));healthRecoveryElapsed=0;}
    [Min(.01f)] public float healthRecoveryInterval=1;
    float healthRecoveryElapsed, lastHitAt=-100;
    Vector2 monsterKnockback;
    float monsterKnockbackRemaining;
    const float MonsterKnockbackDuration=.3f;
    public void ApplyMonsterKnockback(Vector2 impulse)
    {
        monsterKnockback=impulse;monsterKnockbackRemaining=MonsterKnockbackDuration;grounded=false;
        ladderReattachAt=handholdReattachAt=stairReattachAt=world.Clock+MonsterKnockbackDuration;
    }
    public void AdvanceRecovery(float dt,bool observed)
    {
        if(Health<=0 || Health>=maxHealth || observed || world.Clock<=lastHitAt){healthRecoveryElapsed=0;return;}
        healthRecoveryElapsed+=dt;
        while(healthRecoveryElapsed>=healthRecoveryInterval-.000001f && Health<maxHealth)
        {healthRecoveryElapsed=Mathf.Max(0,healthRecoveryElapsed-Mathf.Max(.01f,healthRecoveryInterval));Health++;}
        if(Health>=maxHealth)healthRecoveryElapsed=0;
    }
    public bool TakeMonsterHit()
    {
        if(Health<=0)return false;
        Health--;lastHitAt=world.Clock;healthRecoveryElapsed=0;
        flying=false;climbingLadder=null;ReleaseChain();ReleaseHandhold();ReleaseStairs();pool=Vector2.zero;upwardAt=poolAt=-10;velocity.y=Mathf.Min(0,velocity.y);
        if(Noise)Noise.UpdateFlightNoise(pos,false,false);
        world.InterruptInteraction();
        world.Say("HIT / HEALTH "+Health+" / "+maxHealth);
        return true;
    }
    [Header("Water Gun")]
    public float shotInterval=1f, shotRange=6;
    [Tooltip("二号橙色水枪：自动使用全部反冲速度，无需按相同方向键。")]
    public bool automaticRecoil=true;
    [Min(0)] public float postRecoilFlightDuration=.5f;
    float ActiveFlightDuration {get{return automaticRecoil?recoilDecayDuration+postRecoilFlightDuration:flightDuration;}}
    [Min(.01f)] public float recoilDecayDuration=.75f;
    [Min(.01f)] public float flightDuration=1.25f;
    [Min(1)] public int ammoCapacity=3;
    [Min(.1f)] public float groundRefillInterval=1;
    class Spray { public LineRenderer line; public float life; }
    readonly List<Spray> sprays=new List<Spray>();
    Vector2 pos, velocity, pool, aim=Vector2.right;
    float poolAt=-10, shotAt=-10, upwardAt=-10, nextShot;
    PlayerNoiseEmitter noise;
    public PlayerNoiseEmitter Noise {get{if(!noise)noise=GetComponent<PlayerNoiseEmitter>();return noise;}}
    public void InitializeNoise(RecoilPrototype context)
    {if(!noise)noise=GetComponent<PlayerNoiseEmitter>();if(!noise)noise=gameObject.AddComponent<PlayerNoiseEmitter>();noise.Initialize(context);}
    bool grounded, flying, crouched, silentWalking;
    ClimbableLadder climbingLadder;
    SwingingChain climbingChain;
    WalkableStairway walkingStairway;
    float stairDistance,stairReattachAt,stairHeading=1;
    bool stairApproaching;
    public bool WalkingStairs {get{return walkingStairway;}}
    public WalkableStairway CurrentStairway {get{return walkingStairway;}}
    void ReleaseStairs(){if(walkingStairway){walkingStairway=null;stairReattachAt=world.Clock+.2f;}}
    EquipmentHandhold holdingHandhold;
    float handholdReattachAt;
    public bool HoldingHandhold {get{return holdingHandhold;}}
    void ReleaseHandhold()
    {if(holdingHandhold){handholdReattachAt=world.Clock+holdingHandhold.releaseCooldown;holdingHandhold=null;}}
    SoftRoofPlatform dropThroughRoof;
    float ladderReattachAt;
    public bool Climbing {get{return climbingLadder || climbingChain;}}
    public bool OnLadder {get{return climbingLadder;}}
    public Vector2 CameraFollowPosition {get{return climbingChain?climbingChain.CameraPosition:pos;}}
    void ReleaseChain(){if(climbingChain){velocity=climbingChain.RiderVelocity;climbingChain.Release();climbingChain=null;}}
    PlayerLightExposure lightExposure;
    public float Illumination {get{if(!lightExposure)lightExposure=GetComponent<PlayerLightExposure>();return lightExposure?lightExposure.Coefficient:PlayerLightExposure.MinimumCoefficient;}}
    float refillElapsed,fallDistance;bool fallActive;
    bool refillStarted;
    public float LastAmmoUITrigger {get;private set;}=float.NegativeInfinity;
    public int Ammo {get;private set;}
    public int FallSequence {get;private set;}
    public bool HighFall {get;private set;}
    public float FallDistance {get{return fallDistance;}}
    public Vector2 Position { get { return pos; } set { pos=value;transform.position=value; } }
    public Vector2 Aim { get { return aim; } set { aim=value.normalized; } }
    public Vector2 RecoilPool { get { return pool; } }
    public bool Flying { get { return flying; } }
    public bool Grounded { get { return grounded; } }
    public bool Crouched { get { return crouched; } }
    public int ShotSequence {get;private set;}
    public int InteractionSequence {get;private set;}
    public float LastShot { get { return shotAt; } }
    public float HorizontalIntent {get;private set;}
    public bool MovementBlocked {get;private set;}
    public bool SilentWalking {get{return silentWalking;}}
    float forbiddenUntil=-100;
    public bool RecentInteraction {get{return world.Clock<forbiddenUntil;}}
    public bool Violating {get{return flying || world.Clock-shotAt<.18f || world.Clock<forbiddenUntil || world.ActiveInteraction!=null;}}
    public void NotifyForbiddenInteraction(float duration=.2f){forbiddenUntil=world.Clock+duration;InteractionSequence++;}
    RecoilPlayerBodyProfile bodyProfile;bool bodyProfileResolved;
    RecoilPlayerBodyProfile BodyProfile {get{if(!bodyProfileResolved){bodyProfile=GetComponent<RecoilPlayerBodyProfile>();bodyProfileResolved=true;}return bodyProfile;}}
    public float StandingHeight {get{return BodyProfile?BodyProfile.standingHeight:1.65f;}}
    float CrouchingHeight {get{return BodyProfile?BodyProfile.crouchingHeight:.85f;}}
    float HalfWidth {get{return BodyProfile?BodyProfile.halfWidth:.32f;}}
    float SupportHalfWidth {get{return Mathf.Max(.01f,HalfWidth-.02f);}}
    float CrouchCenterDelta {get{return (StandingHeight-CrouchingHeight)*.5f;}}
    public float BodyHeight {get{return Height;}}
    public bool HasWaterGun {get{return world && world.HasWaterGun;}}
    float Height { get { return crouched ? CrouchingHeight : StandingHeight; } }
    Rect Body { get { return new Rect(pos.x-HalfWidth,pos.y-Height/2,HalfWidth*2,Height); } }
    public void Configure(RecoilPrototype owner,Camera camera,Transform body,Transform face,Transform cap,Transform weapon)
    { bodyProfileResolved=false;world=owner;aimCamera=camera;coat=body;head=face;hat=cap;gun=weapon; }
    public void ResetController(Vector2 position)
    {
        Position=position;velocity=pool=Vector2.zero;aim=Vector2.right;
        monsterKnockback=Vector2.zero;monsterKnockbackRemaining=0;
        HorizontalIntent=0;MovementBlocked=false;forbiddenUntil=-100;ShotSequence=InteractionSequence=0;
        Health=Mathf.Max(1,maxHealth);lastHitAt=-100;healthRecoveryElapsed=0;
        grounded=flying=crouched=silentWalking=false;shotAt=upwardAt=poolAt=-10;nextShot=0;
        ReleaseChain();ReleaseHandhold();ReleaseStairs();stairReattachAt=0;handholdReattachAt=0;velocity=Vector2.zero;climbingLadder=null;dropThroughRoof=null;ladderReattachAt=0;
        if(Noise)Noise.ResetNoise();
        Ammo=Mathf.Max(1,ammoCapacity);refillElapsed=fallDistance=0;fallActive=HighFall=false;FallSequence=0;
        refillStarted=false;LastAmmoUITrigger=float.NegativeInfinity;
        foreach(var spray in sprays)if(spray.line)Destroy(spray.line.gameObject);sprays.Clear();
        SyncVisuals(0);
    }
    void Update()
    {
        if(!world || !world.Playing || world.ReaderInputConsumed)return;
        bool eHeld=Input.GetKey(KeyCode.E),ePressed=Input.GetKeyDown(KeyCode.E);
        bool eConsumed=world.Child && world.Child.RouteE(eHeld,ePressed,Input.GetKeyUp(KeyCode.E),world.FrameDelta,out ePressed);
        if(!eConsumed && world.TryInteractElectricalPanel(ePressed))return;
        if(!eConsumed && world.TryInteractReadable(ePressed))return;
        if(Input.GetKeyDown(KeyCode.K))world.EquipUniversalKey();
        if(Input.GetKeyDown(KeyCode.T))EquipTestWaterGun();
        Vector2 mouse=aimCamera.ScreenToWorldPoint(Input.mousePosition);
        if((mouse-pos).sqrMagnitude>.02f)aim=(mouse-pos).normalized;
        float horizontal=(Input.GetKey(KeyCode.D)||Input.GetKey(KeyCode.RightArrow)?1:0)-(Input.GetKey(KeyCode.A)||Input.GetKey(KeyCode.LeftArrow)?1:0);
        float vertical=(Input.GetKey(KeyCode.W)||Input.GetKey(KeyCode.UpArrow)||Input.GetKey(KeyCode.Space)?1:0)-(Input.GetKey(KeyCode.S)||Input.GetKey(KeyCode.DownArrow)?1:0);
        if(Input.GetKey(KeyCode.S)||Input.GetKey(KeyCode.DownArrow))vertical=-1;
        Simulate(horizontal,vertical,Input.GetKey(KeyCode.C)||Input.GetKey(KeyCode.LeftControl),Input.GetMouseButton(0),Input.GetKeyDown(KeyCode.Space),world.FrameDelta,
            Input.GetKeyDown(KeyCode.W)||Input.GetKeyDown(KeyCode.UpArrow),Input.GetKey(KeyCode.LeftShift)||Input.GetKey(KeyCode.RightShift));
        if(!eConsumed){world.UpdateContinuousInteraction(eHeld||ePressed,world.FrameDelta);world.ObservePlayer(ePressed);}
    }
    public void EquipTestWaterGun()
    {
        world.RememberProgress("water_gun");ammoCapacity=15;Ammo=15;
        refillElapsed=0;refillStarted=false;LastAmmoUITrigger=world.Clock;
    }
    public void Simulate(float horizontal,float vertical,bool sneak,bool shoot,bool jump,float dt,bool launch=false,bool silentWalk=false)
    {
        if(world.EscortingChild){flying=false;climbingLadder=null;}
        HorizontalIntent=horizontal;
        bool wasGrounded=grounded,wasFlying=flying;
        bool wasSupported=grounded || Climbing;
        bool wasCrouched=crouched;
        crouched=grounded && sneak;
        silentWalking=grounded && silentWalk;
        if(crouched!=wasCrouched) pos.y += (crouched ? -CrouchCenterDelta : CrouchCenterDelta);
        if(shoot && world.Clock>=nextShot) Shoot();
        if(walkingStairway && (!walkingStairway.Available || jump))ReleaseStairs();
        bool stairCaughtThisFrame=false;
        if(!walkingStairway && !Climbing && !holdingHandhold && !crouched && !jump && !shoot && world.Clock>=stairReattachAt)
        {
            var feet=pos-Vector2.up*Height*.5f;
            if(grounded && !flying && Mathf.Abs(vertical)>.1f)
            {walkingStairway=world.FindStairway(feet,vertical,out stairDistance);stairApproaching=walkingStairway;stairHeading=Mathf.Sign(vertical);}
            if(!walkingStairway && (Mathf.Abs(vertical)>.1f || ((flying || (!grounded && Mathf.Abs(velocity.y)>.1f)) && Mathf.Abs(horizontal)>.1f)))
            {
                walkingStairway=world.FindStairwayCatch(feet,out stairDistance);
                if(walkingStairway){stairApproaching=false;stairCaughtThisFrame=true;stairHeading=vertical<-.1f?-1:1;Position=walkingStairway.PositionAt(stairDistance)+Vector2.up*Height*.5f;}
            }
            if(walkingStairway){flying=false;pool=velocity=Vector2.zero;upwardAt=shotAt=poolAt=-10;}
        }
        if(holdingHandhold && (!holdingHandhold.isActiveAndEnabled || jump))
        {ReleaseHandhold();if(jump)velocity.y=jumpSpeed;}
        if(!walkingStairway && !holdingHandhold && !Climbing && !jump && !shoot && world.Clock>=handholdReattachAt)
        {
            holdingHandhold=world.FindHandhold(pos,Height);
            if(holdingHandhold){flying=grounded=crouched=false;pool=velocity=Vector2.zero;upwardAt=shotAt=poolAt=-10;}
        }
        if(climbingChain && (!climbingChain.isActiveAndEnabled || jump))
        {ReleaseChain();ladderReattachAt=world.Clock+.25f;if(jump)velocity.y+=jumpSpeed;}
        bool dropping=false;
        if(jump && vertical<-.1f && grounded)
        {
            foreach(var roof in world.SoftRoofs)if(roof && roof.Available)
            {
                var r=roof.Area;
                if(pos.x+SupportHalfWidth>r.xMin && pos.x-SupportHalfWidth<r.xMax && Mathf.Abs(pos.y-Height*.5f-r.yMax)<.035f)
                {dropThroughRoof=roof;grounded=flying=false;climbingLadder=null;ladderReattachAt=world.Clock+.25f;velocity.y=-2;dropping=true;break;}
            }
        }
        if(climbingLadder && (!climbingLadder.Contains(pos,Height) || jump || (Mathf.Abs(horizontal)>.1f && GroundNearLadderFeet(horizontal))))
        {climbingLadder=null;ladderReattachAt=world.Clock+.25f;if(jump && !dropping)velocity.y=jumpSpeed;}
        if(!walkingStairway && !holdingHandhold && !Climbing && !flying && !jump && world.Clock>=ladderReattachAt && Mathf.Abs(vertical)>.1f)
        {
            climbingChain=world.FindChain(pos,Height);
            if(climbingChain)climbingChain.Attach(this,pos);
            else if(!world.EscortingChild)climbingLadder=world.FindLadder(pos,Height);
        }
        if(climbingLadder){if(crouched){crouched=false;pos.y+=CrouchCenterDelta;}flying=grounded=false;velocity=Vector2.zero;}
        if(climbingChain){if(crouched){crouched=false;pos.y+=CrouchCenterDelta;}flying=grounded=false;velocity=Vector2.zero;}
        if(!walkingStairway && !holdingHandhold && !Climbing && !dropping && (jump || launch || (!flying && vertical>0)))
        {
            if(!world.EscortingChild && world.Clock-upwardAt<ActiveFlightDuration && !crouched) { flying=true; velocity.y=0; }
            else if(jump && grounded && !crouched) velocity.y=world.EscortingChild?Mathf.Min(jumpSpeed,3.2f):jumpSpeed;
        }
        if(flying && world.Clock-shotAt>ActiveFlightDuration) { flying=false; world.Say("It stops holding me up when I stop firing."); }
        float decay=Mathf.Clamp01(1-(world.Clock-poolAt)/Mathf.Max(.01f,recoilDecayDuration));
        Vector2 stored=pool*decay;
        float speed=flying ? hoverSpeed : (crouched || silentWalking) ? sneakSpeed : walkSpeed;
        velocity.x=Mathf.MoveTowards(velocity.x,horizontal*speed,dt*22);
        Vector2 move=new Vector2(velocity.x,0);
        bool usingRecoil=flying && !wasFlying && stored.sqrMagnitude>0;
        bool onStairs=walkingStairway;bool stairFinished=false;
        if(walkingStairway){var feet=walkingStairway.Advance(pos-Vector2.up*Height*.5f,stairCaughtThisFrame?0:horizontal,stairCaughtThisFrame?0:vertical,(crouched || silentWalking)?sneakSpeed/Mathf.Max(.01f,walkSpeed):1,dt,ref stairDistance,ref stairApproaching,ref stairHeading,out stairFinished);move=(feet+Vector2.up*Height*.5f-pos)/Mathf.Max(.0001f,dt);velocity=Vector2.zero;flying=false;}
        else if(holdingHandhold)move=(holdingHandhold.Position-pos)/Mathf.Max(.0001f,dt);
        else if(climbingChain)
        {
            climbingChain.Drive(horizontal,vertical,dt,1);
            move=(climbingChain.RiderPosition-pos)/Mathf.Max(.0001f,dt);
        }
        else if(climbingLadder)
        {
            var area=climbingLadder.Area;
            move.x=Mathf.Clamp(area.center.x-pos.x,-climbingLadder.climbSpeed*dt,climbingLadder.climbSpeed*dt)/Mathf.Max(.0001f,dt);
            move.y=(Mathf.Clamp(pos.y+vertical*climbingLadder.climbSpeed*dt,area.yMin+Height*.5f,area.yMax+Height*.5f)-pos.y)/Mathf.Max(.0001f,dt);
        }
        else if(flying)
        {
            velocity.y=0;
            move.y=vertical*verticalHoverSpeed;
            if(automaticRecoil){move+=stored;usingRecoil=stored.sqrMagnitude>.000001f;}
            else{
                if(horizontal*stored.x>0){move.x+=stored.x;usingRecoil=true;}
                if(vertical*stored.y>0){move.y+=stored.y;usingRecoil=true;}
            }
        }
        else { velocity.y-=gravity*dt; move.y=velocity.y;
            if(world.EscortingChild){float escortDecay=Mathf.Clamp01(1-(world.Clock-poolAt)/.22f);move.x+=pool.x*escortDecay;}
        }
        if(monsterKnockbackRemaining>0)
        {
            move+=monsterKnockback*(monsterKnockbackRemaining/MonsterKnockbackDuration);
            monsterKnockbackRemaining=Mathf.Max(0,monsterKnockbackRemaining-dt);
        }
        bool falling=!holdingHandhold && !flying && !Climbing && !grounded && velocity.y<0;
        // Walking off an edge starts a fall even though grounded was true at frame start.
        float oldX=pos.x,oldY=pos.y;
        if(onStairs){pos+=move*dt;grounded=true;if(stairFinished)ReleaseStairs();}
        else Move(world.EscortingChild?world.Child.LimitPlayerStep(pos,move*dt):move*dt);
        if(climbingChain && (pos-climbingChain.RiderPosition).sqrMagnitude>.0025f)climbingChain.StopAt(pos);
        falling=falling || (!holdingHandhold && !grounded && !flying && !Climbing && move.y<0);
        HighFall=false;
        float landingFallDistance=fallDistance+Mathf.Max(0,oldY-pos.y);
        if(falling)
        {
            if(!fallActive){fallActive=true;fallDistance=0;FallSequence++;}
            fallDistance+=Mathf.Max(0,oldY-pos.y);HighFall=fallDistance>StandingHeight;
        }
        else{fallActive=false;fallDistance=0;}
        if(!(grounded || Climbing) || Ammo>=ammoCapacity){refillElapsed=0;refillStarted=false;}
        else if(wasSupported)
        {
            refillElapsed+=dt;
            while(refillElapsed>=Mathf.Max(.1f,groundRefillInterval)-.000001f && Ammo<ammoCapacity)
            {
                refillElapsed=Mathf.Max(0,refillElapsed-Mathf.Max(.1f,groundRefillInterval));Ammo++;
                if(!refillStarted){LastAmmoUITrigger=world.Clock;refillStarted=true;}
            }
            if(Ammo>=ammoCapacity)refillElapsed=0;
        }
        MovementBlocked=Mathf.Abs(horizontal)>.1f && Mathf.Abs(pos.x-oldX)<Mathf.Abs(move.x*dt)*.2f;
        transform.position=pos;
        if(Noise)
        {
            Noise.UpdateFlightNoise(pos,flying,usingRecoil);
            if(!wasGrounded && grounded && !Climbing && move.y<0)
            {if(landingFallDistance>StandingHeight)Noise.EmitLanding(pos);}
            else Noise.UpdateFootsteps(pos,grounded,crouched || silentWalking,onStairs?(pos-new Vector2(oldX,oldY)).magnitude/Mathf.Max(.0001f,dt):horizontal);
        }
    }
    const float CollisionEpsilon=.0001f;
    void ResolveInitialOverlap(float halfHeight)
    {
        // Recover only genuine penetration, using the nearest surface instead of every obstacle's top.
        for(int pass=0;pass<8;pass++)
        {
            Vector2 correction=Vector2.zero;float best=float.PositiveInfinity;
            foreach(var r in world.SolidRects)
            {
                if(pos.x+HalfWidth<=r.xMin+CollisionEpsilon || pos.x-HalfWidth>=r.xMax-CollisionEpsilon || pos.y+halfHeight<=r.yMin+CollisionEpsilon || pos.y-halfHeight>=r.yMax-CollisionEpsilon)continue;
                float left=r.xMin-(pos.x+HalfWidth),right=r.xMax-(pos.x-HalfWidth),down=r.yMin-(pos.y+halfHeight),up=r.yMax-(pos.y-halfHeight);
                if(Mathf.Abs(left)<best){best=Mathf.Abs(left);correction=new Vector2(left,0);}
                if(Mathf.Abs(right)<best){best=Mathf.Abs(right);correction=new Vector2(right,0);}
                if(Mathf.Abs(down)<best){best=Mathf.Abs(down);correction=new Vector2(0,down);}
                if(Mathf.Abs(up)<best){best=Mathf.Abs(up);correction=new Vector2(0,up);}
            }
            if(float.IsPositiveInfinity(best))break;
            pos+=correction;
            if(correction.x!=0)velocity.x=0;if(correction.y!=0)velocity.y=0;
        }
    }
    bool GroundNearLadderFeet(float direction)
    {
        float distance=climbingLadder.groundExitDistance;
        float feetY=pos.y-Height*.5f;
        float stepX=pos.x+Mathf.Sign(direction)*SupportHalfWidth;
        foreach(var r in world.SolidRects)
            if(SupportNearFeet(r,stepX,feetY,distance))return true;
        foreach(var roof in world.SoftRoofs)
            if(roof && roof.Available && roof!=dropThroughRoof && SupportNearFeet(roof.Area,stepX,feetY,distance))return true;
        return false;
    }
    static bool SupportNearFeet(Rect surface,float x,float feetY,float distance)
    {
        // Only top faces beneath the feet count; a wall beside the body cannot release climbing.
        return x>=surface.xMin-distance && x<=surface.xMax+distance &&
            feetY>=surface.yMax-CollisionEpsilon && feetY<=surface.yMax+distance;
    }
    public void CarryBySoftPlatform(SoftRoofPlatform roof,Rect previousArea,Vector2 delta)
    {
        if(!grounded || flying || Climbing || walkingStairway || holdingHandhold || roof==dropThroughRoof || !roof.Available)return;
        if(pos.x+SupportHalfWidth<=previousArea.xMin || pos.x-SupportHalfWidth>=previousArea.xMax || Mathf.Abs(pos.y-Height*.5f-previousArea.yMax)>.035f)return;
        Move(delta);
        var next=roof.Area;
        if(pos.x+SupportHalfWidth>next.xMin && pos.x-SupportHalfWidth<next.xMax && Mathf.Abs(pos.y-Height*.5f-next.yMax)<.035f){grounded=true;velocity.y=0;}
        SyncVisuals(0);
    }
    void Move(Vector2 amount)
    {
        float halfHeight=Height/2;
        ResolveInitialOverlap(halfHeight);
        float oldX=pos.x,nextX=oldX+amount.x;
        foreach(var r in world.SolidRects)
        {
            // Foot contact alone cannot obstruct horizontal travel along a platform.
            if(pos.y+halfHeight<=r.yMin+CollisionEpsilon || pos.y-halfHeight>=r.yMax-CollisionEpsilon)continue;
            if(amount.x>0 && oldX+HalfWidth<=r.xMin+CollisionEpsilon && nextX+HalfWidth>=r.xMin)nextX=Mathf.Min(nextX,r.xMin-.32f);
            else if(amount.x<0 && oldX-HalfWidth>=r.xMax-CollisionEpsilon && nextX-HalfWidth<=r.xMax)nextX=Mathf.Max(nextX,r.xMax+.32f);
        }
        pos.x=nextX;
        if(Mathf.Abs(nextX-(oldX+amount.x))>CollisionEpsilon)velocity.x=0;
        float oldY=pos.y,nextY=oldY+amount.y;grounded=false;bool verticalHit=false;
        foreach(var r in world.SolidRects)
        {
            if(pos.x+HalfWidth<=r.xMin+CollisionEpsilon || pos.x-HalfWidth>=r.xMax-CollisionEpsilon)continue;
            // Land only when feet cross a top face from above; a wall side is never a landing.
            if(amount.y<0 && oldY-halfHeight>=r.yMax-CollisionEpsilon && nextY-halfHeight<=r.yMax)
            {nextY=Mathf.Max(nextY,r.yMax+halfHeight);grounded=true;verticalHit=true;}
            else if(amount.y>0 && oldY+halfHeight<=r.yMin+CollisionEpsilon && nextY+halfHeight>=r.yMin)
            {nextY=Mathf.Min(nextY,r.yMin-halfHeight);verticalHit=true;}
        }
        pos.y=nextY;
        foreach(var roof in world.SoftRoofs)if(!Climbing && roof && roof.Available && roof!=dropThroughRoof)
        {
            var r=roof.Area;
            if(pos.x+SupportHalfWidth<=r.xMin || pos.x-SupportHalfWidth>=r.xMax)continue;
            if(amount.y<0 && oldY-halfHeight>=r.yMax-CollisionEpsilon && pos.y-halfHeight<=r.yMax)
            {pos.y=Mathf.Max(pos.y,r.yMax+halfHeight);grounded=true;verticalHit=true;}
            else if(!flying && !climbingLadder && amount.y<=0 && Mathf.Abs(pos.y-halfHeight-r.yMax)<.015f)grounded=true;
        }
        if(dropThroughRoof && (pos.y-halfHeight<dropThroughRoof.Area.yMax-.05f || !dropThroughRoof.Available))dropThroughRoof=null;
        if(verticalHit)velocity.y=0;
        if(!flying && !Climbing && amount.y<=0)foreach(var r in world.SolidRects)
            if(pos.x+SupportHalfWidth>r.xMin && pos.x-SupportHalfWidth<r.xMax && Mathf.Abs(pos.y-halfHeight-r.yMax)<.015f){grounded=true;break;}
        if(grounded && !(automaticRecoil && world.Clock-shotAt<=ActiveFlightDuration))flying=false;
    }
    public void CancelEscortFlight(){climbingLadder=null;flying=false;pool=Vector2.zero;upwardAt=poolAt=-10;velocity.y=Mathf.Min(0,velocity.y);}
    public void Shoot()
    {
        if(!HasWaterGun || Ammo<=0 || world.Clock<nextShot)return;
        Ammo--;
        LastAmmoUITrigger=world.Clock;refillStarted=false;
        ShotSequence++;nextShot=world.Clock+shotInterval; shotAt=poolAt=world.Clock;
        ReleaseHandhold();ReleaseStairs();
        pool=automaticRecoil?new Vector2(-aim.x*7,-aim.y*8):aim.y>.999f ? Vector2.zero : new Vector2(-aim.x*7,Mathf.Max(0,-aim.y)*8);
        upwardAt=world.Clock;
        if(world.EscortingChild)
        {
            pool=new Vector2(-aim.x*1.6f,0);upwardAt=-10;flying=false;
            if(grounded && -aim.y>.15f){grounded=false;velocity.y=3.2f*(-aim.y);}
        }
        else if(climbingChain){climbingChain.ApplyShot(aim);flying=false;grounded=false;velocity.y=0;}
        else{flying=true;climbingLadder=null;grounded=false;velocity.y=0;}
        var start=pos+aim*.55f; float range=shotRange;
        foreach(var r in world.SolidRects) range=Mathf.Min(range,RecoilPrototype.RayRect(start,aim,r,range));
        var end=start+aim*range;
        var go=new GameObject("Water spray"); var line=go.AddComponent<LineRenderer>();
        line.sharedMaterial=world.SprayMaterial; line.startColor=Hex(0x8ae5ff); line.endColor=Hex(0x8ae5ff,.15f);
        line.startWidth=.075f; line.endWidth=.025f; line.positionCount=2; line.sortingOrder=15;
        line.SetPosition(0,new Vector3(start.x,start.y,-.1f)); line.SetPosition(1,new Vector3(end.x,end.y,-.1f));
        sprays.Add(new Spray {life=.15f,line=line});
        world.ApplyWaterHit(start,aim,range,pos);
    }
    public void SyncVisuals(float dt)
    {
        transform.position=pos;
        coat.localPosition=new Vector2(0,-.1f);
        coat.localScale=new Vector3(.6f,crouched?.62f:1.15f,1);
        head.localPosition=new Vector2(0,crouched?.2f:.58f);
        hat.localPosition=new Vector2(0,crouched?.44f:.82f);
        if(BodyProfile)BodyProfile.Apply(coat,head,hat,crouched);
        gun.gameObject.SetActive(HasWaterGun);gun.position=pos+aim*.5f;gun.rotation=Quaternion.Euler(0,0,Mathf.Atan2(aim.y,aim.x)*Mathf.Rad2Deg);
        if(!gunRenderer)gunRenderer=gun.GetComponent<SpriteRenderer>();
        if(gunRenderer)gunRenderer.color=automaticRecoil?Hex(0xf28a32):Hex(0x50ceef);
        for(int i=sprays.Count-1;i>=0;i--){sprays[i].life-=dt;if(sprays[i].life<=0){Destroy(sprays[i].line.gameObject);sprays.RemoveAt(i);}}
    }
    void OnDestroy(){foreach(var spray in sprays)if(spray.line)Destroy(spray.line.gameObject);}
    static Color Hex(int n,float a=1){return RecoilPrototype.Hex(n,a);}
}
