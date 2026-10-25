using UnityEngine;

// One owner for gameplay Z. XY movement, recoil and gravity remain in PlayerController.
[DisallowMultipleComponent,RequireComponent(typeof(RecoilPlayerController))]
[AddComponentMenu("Recoil/Player Depth Planes")]
public sealed class PlayerDepthPlane : MonoBehaviour
{
    public enum DepthState { Default, ApproachingPlatform, OnPlatform, OnStairs, Departing, Returning, Attached, EnteringStairs }
    public float defaultZ;
    [Min(.1f),Tooltip("平台/楼梯操作面向远景移动的距离，默认约一个角色宽度。")]
    public float platformOffset=.7f;
    [Min(.05f)] public float transitionTime=.28f;
    [Min(.1f),Tooltip("从普通地面入梯时，先走向后方操作面，再沿楼梯上行。")]
    public float stairEntryDuration=.35f;
    [Min(.05f)] public float departureDuration=.35f;
    [Min(.01f)] public float departureHopHeight=.18f;
    [Min(.1f)] public float landingLookAhead=.45f;
    [Min(.1f)] public float minimumApproachHeight=.9f;
    [Min(.01f)] public float landingTolerance=.12f;
    [SerializeField] DepthState state;
    public DepthState State {get{return state;}}
    public float CurrentZ {get;private set;}
    public float PlatformZ {get{return defaultZ+platformOffset;}}
    public float DepartureFacing {get;private set;}
    public bool Departing {get{return departureElapsed<departureDuration;}}
    public bool StairEntering {get{return stairEntryElapsed<stairEntryDuration;}}
    public SoftRoofPlatform LandingPlatform {get{return candidate;}}
    RecoilPrototype world;RecoilPlayerController player;
    SoftRoofPlatform candidate,support,ignoredRoof,lowStepSupport;
    float lowStepZ;
    WalkableStairway ignoredStair;
    float departureElapsed=100,departureZ,zVelocity,captureStartZ,captureHeight;
    float stairEntryElapsed=100,stairEntryZ;
    void ResolveOwners(){if(!world)world=GetComponentInParent<RecoilPrototype>();if(!player)player=GetComponent<RecoilPlayerController>();}
    void OnEnable(){ResolveOwners();}
    public void Initialize(RecoilPrototype context,RecoilPlayerController actor){world=context;player=actor;}
    public void ResetPlane(Vector2 position)
    {
        ResolveOwners();CurrentZ=defaultZ;zVelocity=0;departureElapsed=stairEntryElapsed=100;DepartureFacing=0;candidate=support=ignoredRoof=lowStepSupport=null;ignoredStair=null;state=DepthState.Default;
        if(world)foreach(var roof in world.SoftRoofs)if(StandingOn(roof,position))
        {support=roof;CurrentZ=PlatformZ;state=DepthState.OnPlatform;break;}
    }
    bool StandingOn(SoftRoofPlatform roof,Vector2 position)
    {
        if(!roof||!roof.Available||roof==ignoredRoof)return false;
        var r=roof.Area;float feet=position.y-player.BodyHeight*.5f;
        return position.x+.3f>r.xMin && position.x-.3f<r.xMax && Mathf.Abs(feet-r.yMax)<.04f && !SharesHardFloor(r,position.x);
    }
    bool SharesHardFloor(Rect platform,float x)
    {
        // A rear stair's ground-level deck is not a reason to leave the foreground walking lane.
        foreach(var solid in world.SolidRects)
            if(Mathf.Abs(solid.yMax-platform.yMax)<.02f && x+.28f>solid.xMin && x-.28f<solid.xMax)return true;
        return false;
    }
    public bool BeginDeparture(Vector2 position,WalkableStairway stairs,bool grounded,out SoftRoofPlatform source)
    {
        source=null;if(!world)return false;
        if(!stairs && grounded)foreach(var roof in world.SoftRoofs)if(StandingOn(roof,position)){source=roof;break;}
        if(!stairs&&!source)return false;
        ignoredRoof=source;ignoredStair=stairs;support=candidate=null;stairEntryElapsed=100;
        departureElapsed=0;departureZ=CurrentZ;zVelocity=0;state=DepthState.Departing;return true;
    }
    public bool CanAttachStair(WalkableStairway stairs){return !Departing && stairs!=ignoredStair;}
    public void BeginStairEntry()
    {
        if(Mathf.Abs(CurrentZ-PlatformZ)<.01f)return;
        stairEntryZ=CurrentZ;stairEntryElapsed=0;zVelocity=0;
        state=DepthState.EnteringStairs;
    }
    public bool CanLand(SoftRoofPlatform roof)
    {return !Departing && roof!=ignoredRoof && (roof==candidate || roof==support) && (roof==lowStepSupport || Mathf.Abs(CurrentZ-PlatformZ)<=landingTolerance);}
    public void BeginLowStep(SoftRoofPlatform roof)
    {if(!roof||Departing)return;lowStepSupport=roof;lowStepZ=CurrentZ;support=candidate=roof;state=DepthState.OnPlatform;}
    public void Simulate(Vector2 position,Vector2 motion,bool grounded,bool flying,bool attached,bool stairs,float dt)
    {
        ResolveOwners();if(!world||!player||dt<=0)return;
        if(!stairs || flying)stairEntryElapsed=100;
        float feet=position.y-player.BodyHeight*.5f;
        if(ignoredRoof && !Departing && feet<ignoredRoof.Area.yMax-.08f)ignoredRoof=null;
        if(ignoredStair && !Departing){float at,separation;if(!ignoredStair.TryCatch(position-Vector2.up*player.BodyHeight*.5f,out at,out separation))ignoredStair=null;}
        if(flying){lowStepSupport=null;departureElapsed=100;DepartureFacing=0;support=candidate=ignoredRoof=null;ignoredStair=null;}
        if(Departing)
        {
            departureElapsed=Mathf.Min(departureDuration,departureElapsed+dt);
            float t=departureElapsed/departureDuration;
            CurrentZ=Mathf.Lerp(departureZ,defaultZ,Mathf.SmoothStep(0,1,t));
            DepartureFacing=Mathf.SmoothStep(0,1,t/.22f)*(1-Mathf.SmoothStep(0,1,(t-.55f)/.45f));
            state=DepthState.Departing;return;
        }
        DepartureFacing=0;
        if(stairs)
        {
            candidate=support=null;
            if(StairEntering)
            {
                stairEntryElapsed=Mathf.Min(stairEntryDuration,stairEntryElapsed+dt);
                CurrentZ=Mathf.Lerp(stairEntryZ,PlatformZ,Mathf.SmoothStep(0,1,stairEntryElapsed/stairEntryDuration));
                state=DepthState.EnteringStairs;return;
            }
            state=DepthState.OnStairs;Blend(PlatformZ,dt);return;
        }
        if(attached){candidate=support=null;state=DepthState.Attached;float target=player.OnLadder?player.LadderDepthZ:defaultZ;
            if(player.OnLadder)foreach(var roof in world.SoftRoofs)if(StandingOn(roof,position)){target=PlatformZ;break;}
            Blend(target,dt);return;}
        if(grounded && !flying)
        {
            support=null;
            if(lowStepSupport&&lowStepSupport.Available)
            {
                var step=lowStepSupport.Area;
                if(position.x+.32f>step.xMin&&position.x-.32f<step.xMax&&Mathf.Abs(feet-step.yMax)<.04f)support=lowStepSupport;
            }
            if(!support)foreach(var roof in world.SoftRoofs)if(StandingOn(roof,position)){support=roof;break;}
            if(support){state=DepthState.OnPlatform;Blend(support==lowStepSupport?lowStepZ:PlatformZ,dt);return;}
            lowStepSupport=null;
        }
        support=null;
        if(!flying && motion.y<-.01f)
        {
            SoftRoofPlatform selected=null;float highest=float.NegativeInfinity;
            foreach(var roof in world.SoftRoofs)
            {
                if(!roof||!roof.Available||roof==ignoredRoof||roof==lowStepSupport)continue;
                var r=roof.Area;float h=feet-r.yMax;if(h<-.001f)continue;
                float g=Mathf.Max(.01f,player.gravity),time=(Mathf.Sqrt(motion.y*motion.y+2*g*h)+motion.y)/g;
                float approach=Mathf.Max(minimumApproachHeight,-motion.y*landingLookAhead);
                if(h>approach)continue;
                float predictedX=position.x+motion.x*time,margin=roof==candidate ? .38f : .28f;
                if(predictedX+margin<=r.xMin||predictedX-margin>=r.xMax)continue;
                if(SharesHardFloor(r,predictedX))continue;
                if(r.yMax>highest){highest=r.yMax;selected=roof;}
            }
            // A hard surface that will be hit first takes precedence over a lower soft platform.
            if(selected)foreach(var r in world.SolidRects)
            {
                if(r.yMax>feet+.001f || r.yMax<=highest+.001f)continue;
                float h=feet-r.yMax,g=Mathf.Max(.01f,player.gravity),time=(Mathf.Sqrt(motion.y*motion.y+2*g*h)+motion.y)/g;
                float x=position.x+motion.x*time;if(x+.28f>r.xMin&&x-.28f<r.xMax){selected=null;break;}
            }
            if(selected)
            {
                if(candidate!=selected){captureStartZ=CurrentZ;captureHeight=Mathf.Max(.001f,feet-selected.Area.yMax);}
                candidate=selected;float remaining=Mathf.Max(0,feet+motion.y*dt-selected.Area.yMax);
                float progress=1-Mathf.Clamp01(remaining/captureHeight);
                CurrentZ=Mathf.Lerp(captureStartZ,PlatformZ,Mathf.SmoothStep(0,1,progress));zVelocity=0;
                state=DepthState.ApproachingPlatform;return;
            }
        }
        candidate=null;state=Mathf.Abs(CurrentZ-defaultZ)>.01f?DepthState.Returning:DepthState.Default;Blend(defaultZ,dt);
    }
    void Blend(float target,float dt){CurrentZ=Mathf.SmoothDamp(CurrentZ,target,ref zVelocity,transitionTime,float.PositiveInfinity,dt);if(Mathf.Abs(CurrentZ-target)<.001f){CurrentZ=target;zVelocity=0;}}
}
