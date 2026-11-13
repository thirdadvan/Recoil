using UnityEngine;

// A bounded, interruptible operation. Only player controls stop; enemies keep running.
[DefaultExecutionOrder(-650),DisallowMultipleComponent]
public sealed class RecoilPropOperation : MonoBehaviour
{
    public bool Active {get;private set;}
    public float Elapsed {get;private set;}
    public float Duration {get;private set;}
    public bool KeyInserted {get{return presentation&&presentation.PropOperationActive?presentation.PropKeyInserted:Elapsed>=Duration*.35f;}}
    bool terminalSession,terminalExiting;float queryRemaining,exitRemaining;
    System.Action queryCompleted;
    public bool TerminalSession {get{return Active&&terminalSession;}}
    public bool TerminalExiting {get{return TerminalSession&&terminalExiting;}}
    public bool BlocksConversation {get{return Active&&(!terminalSession||terminalExiting||queryRemaining>0);}}
    RecoilPrototype world;DawnPlayerAnimation presentation;
    System.Action<float> progress;System.Action completed,cancelled;
    public void Initialize(RecoilPrototype owner){world=owner;}
    public bool Begin(float duration,Vector3 target,bool typing,System.Action<float> onProgress,System.Action onComplete,System.Action onCancel=null)
    {
        if(Active||!world||!world.Player||world.Player.Health<=0)return false;
        Duration=Mathf.Max(.05f,duration);Elapsed=0;progress=onProgress;completed=onComplete;cancelled=onCancel;Active=true;
        world.Player.StopForConversation();world.Player.NotifyForbiddenInteraction(Duration);
        presentation=world.Player.GetComponentInChildren<DawnPlayerAnimation>();
        if(presentation)presentation.BeginPropOperation(target,typing,Duration);
        if(progress!=null)progress(0);return true;
    }
    public bool BeginTerminalSession(Vector3 target)
    {
        if(!Begin(1,target,true,null,null))return false;
        terminalSession=true;terminalExiting=false;queryRemaining=exitRemaining=0;
        if(presentation)presentation.HoldTypingOperation();return true;
    }
    public bool BeginTerminalQuery(float duration,System.Action done)
    {
        if(!TerminalSession||terminalExiting||queryRemaining>0)return false;
        queryRemaining=Mathf.Max(.05f,duration);queryCompleted=done;return true;
    }
    public bool EndTerminalSession(System.Action done=null)
    {
        if(!TerminalSession||terminalExiting)return false;
        terminalExiting=true;queryRemaining=0;queryCompleted=null;exitRemaining=.55f;completed=done;
        if(presentation)presentation.BeginTypingReturn(exitRemaining);return true;
    }
    public void Simulate(float dt)
    {
        if(!Active||dt<=0)return;
        if(!world||!world.Player||world.Player.Health<=0){Cancel();return;}
        if(terminalSession){
            Elapsed+=dt;if(presentation)presentation.SetPropOperationTime(Elapsed);
            if(terminalExiting){exitRemaining-=dt;if(exitRemaining<=.00001f){var done=completed;Clear();if(done!=null)done();}}
            else if(queryRemaining>0){queryRemaining=Mathf.Max(0,queryRemaining-dt);if(queryRemaining<=.00001f){queryRemaining=0;var done=queryCompleted;queryCompleted=null;if(done!=null)done();}}
            return;
        }
        Elapsed=Mathf.Min(Duration,Elapsed+dt);
        if(presentation)presentation.SetPropOperationTime(Elapsed);
        if(progress!=null)progress(Elapsed/Duration);
        if(Elapsed>=Duration){var done=completed;Clear();if(done!=null)done();}
    }
    void Update(){if(Active){if(!world.Player||world.Player.Health<=0){Cancel();return;}if(world.Playing)Simulate(Time.deltaTime);}}
    void Clear(){Active=false;terminalSession=terminalExiting=false;queryRemaining=exitRemaining=0;queryCompleted=null;progress=null;completed=null;cancelled=null;if(presentation)presentation.EndPropOperation();presentation=null;}
    public void Cancel(){if(!Active)return;var cancel=cancelled;Clear();if(cancel!=null)cancel();}
    void OnDisable(){Cancel();}
}
