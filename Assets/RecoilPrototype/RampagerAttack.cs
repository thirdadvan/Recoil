using UnityEngine;

[DisallowMultipleComponent,AddComponentMenu("Recoil/Rampager Attack")]
public sealed class RampagerAttack : MonoBehaviour
{
    public enum AttackPhase { Ready, Windup, Recovery }
    [Min(.01f),Tooltip("这只怪物两次出手之间的冷却时间，单位秒。")]
    public float attackInterval=1.5f;
    [Min(.01f)] public float contactRadius=.78f;
    [Min(0)] public float windupDuration=.2f,recoveryDuration=.4f;
    public AttackPhase Phase {get;private set;}
    public bool MovementLocked {get{return isActiveAndEnabled && Phase!=AttackPhase.Ready;}}
    public float PhaseRemaining {get{return Mathf.Max(0,remaining);}}
    RecoilPrototype world;RampagerPerception perception;
    RecoilPlayerController strikePlayer;EscortChild strikeChild;
    float nextAttackAt,remaining;
    public float CooldownRemaining {get{return world?Mathf.Max(0,nextAttackAt-world.Clock):0;}}
    public void Initialize(RecoilPrototype context,RampagerPerception senses)
    {world=context;perception=senses;ResetAttack();}
    public void ResetAttack(){nextAttackAt=0;CancelAttack();}
    public void CancelAttack(){Phase=AttackPhase.Ready;remaining=0;strikePlayer=null;strikeChild=null;}
    // Start a swing and commit to its target; range and cover are checked again at impact.
    public bool TryAttack(bool allowed)
    {
        if(!isActiveAndEnabled || !allowed || !world || !perception || Phase!=AttackPhase.Ready || world.Player.Health<=0 || world.Clock<nextAttackAt)return false;
        bool hitPlayer=perception.CanTouchPlayer(contactRadius),hitChild=perception.CanTouchChild(contactRadius);
        if(!hitPlayer&&!hitChild)return false;
        bool child=hitChild&&(!hitPlayer||perception.ChildIsCloser);
        strikeChild=child?world.Child:null;strikePlayer=child?null:world.Player;
        Phase=AttackPhase.Windup;remaining=Mathf.Max(0,windupDuration);
        if(remaining<=0)Impact();return true;
    }
    public void Simulate(float dt,bool allowed)
    {
        if(!isActiveAndEnabled || !allowed){CancelAttack();return;}
        if(dt<=0)return;
        if(Phase==AttackPhase.Ready){TryAttack(true);return;}
        remaining-=dt;
        if(remaining>.000001f)return;
        if(Phase==AttackPhase.Windup){float overshoot=Mathf.Min(0,remaining);Impact();remaining=Mathf.Max(0,remaining+overshoot);}
        else CancelAttack();
    }
    void Impact()
    {
        nextAttackAt=world.Clock+Mathf.Max(.01f,attackInterval);
        if(strikeChild && strikeChild==world.Child && strikeChild.Alive && perception.CanTouchChild(contactRadius))strikeChild.TakeHit();
        else if(strikePlayer && strikePlayer==world.Player && perception.CanTouchPlayer(contactRadius))world.CatchPlayer();
        strikePlayer=null;strikeChild=null;Phase=AttackPhase.Recovery;remaining=Mathf.Max(0,recoveryDuration);
    }
    void OnDisable(){CancelAttack();}
}
