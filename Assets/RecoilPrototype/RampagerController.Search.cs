using UnityEngine;

public sealed partial class RampagerController
{
    [Header("Attack search after losing the target")]
    [Min(.1f)] public float attackSearchRadius=2;
    public Vector2 attackSearchMoveInterval=new Vector2(1.5f,2.5f);
    [Min(.1f)] public float searchRoarDuration=.9f;
    bool searchingForPlayer,searchMoving;
    Vector3 searchCentre,searchDestination,previousSearchPosition;
    float nextSearchMoveAt,roarUntil,attackNoProgressTime;
    int roarSequence;
    public bool SearchingForPlayer {get{return State==Behaviour.Attack && searchingForPlayer;}}
    public int RoarSequence {get{return roarSequence;}}
    public float SearchRoarAmount
    {get{if(!SearchingForPlayer || !world || world.Clock>=roarUntil)return 0;float p=1-(roarUntil-world.Clock)/Mathf.Max(.1f,searchRoarDuration);return Mathf.Sin(Mathf.Clamp01(p)*Mathf.PI);}}
    public Vector3 SearchCentre {get{return searchCentre;}}
    void StopAttackSearch(){searchingForPlayer=searchMoving=false;attackNoProgressTime=0;}
    void ConstrainAttackSearchStep(Vector3 previous)
    {
        if(!SearchingForPlayer)return;var d=transform.position-searchCentre;
        if(new Vector2(d.x,d.z).sqrMagnitude>attackSearchRadius*attackSearchRadius+.0001f)
        {Motor.SetNavigationPosition(previous);searchMoving=false;}
    }
    void UpdateAttackSearch(float dt,ref Vector3 destination,ref float budget,ref bool navigate,ref float stopDistance)
    {
        if(!searchingForPlayer)
        {
            var p=transform.position;
            attackNoProgressTime=(p-previousSearchPosition).sqrMagnitude<dt*dt*.0004f?attackNoProgressTime+dt:0;previousSearchPosition=p;
            float gap=new Vector2(destination.x-Position.x,destination.z-Motor.MovementPlaneZ).magnitude;
            if(gap>.35f && attackNoProgressTime<1.2f)return;
            searchingForPlayer=true;searchMoving=false;searchCentre=searchDestination=transform.position;
            roarUntil=world.Clock+Mathf.Max(.1f,searchRoarDuration);roarSequence++;
            nextSearchMoveAt=world.Clock+Delay(attackSearchMoveInterval);
        }
        navigate=false;budget=0;stopDistance=.12f;
        if(world.Clock<roarUntil)return;
        if(!searchMoving && world.Clock>=nextSearchMoveAt)
        {
            Vector2 offset=Uses3DNavigation?Random.insideUnitCircle*attackSearchRadius:new Vector2(Random.Range(-attackSearchRadius,attackSearchRadius),0);
            searchDestination=searchCentre+new Vector3(offset.x,0,offset.y);searchMoving=true;
            nextSearchMoveAt=world.Clock+Delay(attackSearchMoveInterval);
        }
        if(!searchMoving)return;
        destination=searchDestination;
        float distance=new Vector2(destination.x-Position.x,destination.z-Motor.MovementPlaneZ).magnitude;
        if(distance<=.16f || world.Clock>=nextSearchMoveAt){searchMoving=false;return;}
        budget=Mathf.Min(distance-.12f,CurrentMoveSpeed*dt);navigate=true;TrackPosition(destination);
    }
}
