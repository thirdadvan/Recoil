using UnityEngine;

// Each visit gets a separate context; shared assets must not store mutable per-monster state.
public sealed class RampagerPatrolContext
{
    public RampagerController Owner {get;private set;}
    public RampagerPatrolPoint Point {get;private set;}
    public float Elapsed {get;internal set;}
    public float Facing {get;set;}
    public object Data {get;set;}
    public RampagerPatrolContext(RampagerController owner,RampagerPatrolPoint point,float facing)
    {Owner=owner;Point=point;Facing=facing;}
}

// Derive from this and add CreateAssetMenu to implement new actions without editing the controller.
public abstract class RampagerPatrolAction : ScriptableObject
{
    public virtual void Enter(RampagerPatrolContext context){}
    public abstract void Tick(RampagerPatrolContext context,float dt);
    public virtual void Exit(RampagerPatrolContext context,bool interrupted){}
}
