using UnityEngine;

[DisallowMultipleComponent]
public sealed class RampagerMovement : MonoBehaviour
{
    public float leftOffset=-2,rightOffset=2;
    public float patrolSpeed=.9f,investigateSpeed=1.25f,chaseSpeed=2.6f;
    [Header("Swarm attack mobility")]
    [Min(1)] public float swarmAttackSpeedMultiplier=1.4f;
    [Min(.01f)] public float jumpHeightInBodies=.7f,jumpCooldown=5,jumpGravity=14,jumpTriggerDistance=2.5f;
    float verticalSpeed,nextJumpAt;
    bool airborne;
    public bool Airborne {get{return airborne;}}
    public float JumpCooldownRemaining {get{return world?Mathf.Max(0,nextJumpAt-world.Clock):0;}}
    public bool Grounded
    {get{foreach(var r in world.SolidRects)if(Position.x+.3f>r.xMin && Position.x-.3f<r.xMax && Mathf.Abs(Position.y-.8f-r.yMax)<.035f)return true;return false;}}
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
        {if(amount<=0){Position=new Vector2(Position.x,r.yMax+.8f);airborne=false;}else Position=new Vector2(Position.x,r.yMin-.8f);verticalSpeed=0;}
        transform.position=Position;
    }
    RecoilPrototype world;
    public Vector2 Position {get;set;}
    public Vector2 Home {get;private set;}
    public float Facing {get;set;}=-1;
    public float Left {get{return Home.x+leftOffset;}}
    public float Right {get{return Home.x+rightOffset;}}
    public Rect Body {get{return new Rect(Position.x-.33f,Position.y-.8f,.66f,1.6f);}}
    public void Initialize(RecoilPrototype owner){world=owner;Home=transform.position;ResetMovement();}
    public void ResetMovement(float initialFacing=-1){airborne=false;verticalSpeed=nextJumpAt=0;Position=Home;Facing=initialFacing>0?1:-1;transform.position=Position;}
    public void LookAt(float x){if(Mathf.Abs(x-Position.x)>.01f)Facing=Mathf.Sign(x-Position.x);}
    public float Toward(float x){return Mathf.Abs(x-Position.x)<.2f?0:Mathf.Sign(x-Position.x);}
    public bool Move(float direction,float distance,bool turnOnBlock)
    {
        if(direction==0 || distance<=0)return false;
        Facing=direction;var proposed=Position+Vector2.right*(direction*distance);
        var body=new Rect(proposed.x-.33f,proposed.y-.8f,.66f,1.6f);
        foreach(var obstacle in world.SolidRects)if(body.Overlaps(obstacle)){if(turnOnBlock)Facing=-Facing;return false;}
        Position=proposed;transform.position=Position;return true;
    }
}
