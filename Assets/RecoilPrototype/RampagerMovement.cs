using UnityEngine;

[DisallowMultipleComponent]
public sealed class RampagerMovement : MonoBehaviour
{
    [Header("Turning (3D presentation)")]
    [Min(1),Tooltip("每秒转身角度。") ] public float turnSpeed=180;
    [Min(.1f),Tooltip("体型倍率；以脚底为锚点，不缩放巡逻路径。") ] public float bodyScale=1;
    public float BodyHeight {get{return 1.6f*bodyScale;}}
    public Vector2 EyePosition {get{return Position+Vector2.up*(1.25f*bodyScale-.8f);}}
    public float VisualYaw {get;private set;}
    public void SetVisualYaw(float yaw){VisualYaw=yaw;}
    public void AdvanceTurn(float dt){if(!TurnRequested)return;VisualYaw=Mathf.MoveTowardsAngle(VisualYaw,Facing>0?-90:90,Mathf.Max(1,turnSpeed)*Mathf.Max(0,dt));}
    public float leftOffset=-2,rightOffset=2;
    public float patrolSpeed=.9f,investigateSpeed=1.25f,chaseSpeed=2.6f;
    [Header("Swarm attack mobility")]
    [Min(1)] public float swarmAttackSpeedMultiplier=1.4f;
    [Min(.01f)] public float jumpHeightInBodies=.7f,jumpCooldown=5,jumpGravity=14,jumpTriggerDistance=2.5f;
    float verticalSpeed,nextJumpAt,supportPlatformHeight;
    bool airborne;
    public bool Airborne {get{return airborne;}}
    public float JumpCooldownRemaining {get{return world?Mathf.Max(0,nextJumpAt-world.Clock):0;}}
    bool TryGetSupportHeight(out float height)
    {
        height=supportPlatformHeight;if(!world)return false;
        foreach(var r in world.SolidRects)
            if(Position.x+.3f*bodyScale>r.xMin && Position.x-.3f*bodyScale<r.xMax && Mathf.Abs(Position.y-.8f-r.yMax)<.035f)
            {height=r.yMax;return true;}
        return false;
    }
    // Chase height is measured from this monster's support surface, never the player's nearest platform.
    // Retain the takeoff platform while jumping; only a real landing changes the reference.
    public float SupportPlatformHeight
    {
        get{float height;if(!airborne && TryGetSupportHeight(out height))supportPlatformHeight=height;return supportPlatformHeight;}
    }
    public bool Grounded
    {get{if(airborne)return false;float height;if(!TryGetSupportHeight(out height))return false;supportPlatformHeight=height;return true;}}
    public bool TryJump(Vector2 target,bool blocked)
    {
        if(airborne || world.Clock<nextJumpAt || !Grounded || Mathf.Abs(target.x-Position.x)>jumpTriggerDistance || (!blocked && target.y<=Position.y+.35f))return false;
        verticalSpeed=Mathf.Sqrt(2*Mathf.Max(.01f,jumpGravity)*1.6f*jumpHeightInBodies);airborne=true;nextJumpAt=world.Clock+Mathf.Max(.01f,jumpCooldown);return true;
    }
    public void SimulateVertical(float dt)
    {
        if(!airborne || dt<=0)return;
        float amount=verticalSpeed*dt-.5f*jumpGravity*dt*dt;verticalSpeed-=jumpGravity*dt;
        Position+=Vector2.up*amount;
        foreach(var r in world.SolidRects)if(Body.Overlaps(r))
        {if(amount<=0){Position=new Vector2(Position.x,r.yMax+.8f);supportPlatformHeight=r.yMax;airborne=false;}else Position=new Vector2(Position.x,r.yMin+.8f-BodyHeight);verticalSpeed=0;}
        ApplyPosition();
    }
    RecoilPrototype world;
    public Vector2 Position {get;set;}
    public Vector2 Home {get;private set;}
    public float MovementPlaneZ {get;set;}
    public void ApplyPosition(){transform.position=new Vector3(Position.x,Position.y,MovementPlaneZ);}
    public void SetNavigationPosition(Vector3 position){Position=position;MovementPlaneZ=position.z;if(!airborne)supportPlatformHeight=position.y-.8f;ApplyPosition();}
    float facing=-1,initialYaw,homeDepth;
    bool capturedSpawn;
    public bool TurnRequested {get;private set;}
    public float InitialYaw {get{return capturedSpawn?initialYaw:transform.eulerAngles.y;}}
    public float InitialFacingSign {get{float x=-Mathf.Sin(InitialYaw*Mathf.Deg2Rad);return Mathf.Abs(x)>.001f?Mathf.Sign(x):-1;}}
    public float Facing {get{return facing;}set{facing=value>0?1:-1;TurnRequested=true;}}
    public float Left {get{return Home.x+leftOffset;}}
    public float Right {get{return Home.x+rightOffset;}}
    public Rect Body {get{return new Rect(Position.x-.33f*bodyScale,Position.y-.8f,.66f*bodyScale,BodyHeight);}}
    public void Initialize(RecoilPrototype owner)
    {
        world=owner;
        if(!capturedSpawn){Home=transform.position;homeDepth=transform.position.z;initialYaw=transform.eulerAngles.y;capturedSpawn=true;}
        ResetMovement();
    }
    public void ResetMovement(){airborne=false;verticalSpeed=nextJumpAt=0;Position=Home;MovementPlaneZ=homeDepth;supportPlatformHeight=Home.y-.8f;facing=InitialFacingSign;TurnRequested=false;VisualYaw=InitialYaw;ApplyPosition();}
    public void LookAt(float x){if(Mathf.Abs(x-Position.x)>.01f)Facing=Mathf.Sign(x-Position.x);}
    public float Toward(float x){return Mathf.Abs(x-Position.x)<.2f?0:Mathf.Sign(x-Position.x);}
    public bool Move(float direction,float distance,bool turnOnBlock)
    {
        if(direction==0 || distance<=0)return false;
        Facing=direction;var proposed=Position+Vector2.right*(direction*distance);
        var body=new Rect(proposed.x-.33f*bodyScale,proposed.y-.8f,.66f*bodyScale,BodyHeight);
        foreach(var obstacle in world.SolidRects)if(body.Overlaps(obstacle)){if(turnOnBlock)Facing=-Facing;return false;}
        Position=proposed;ApplyPosition();return true;
    }
}
