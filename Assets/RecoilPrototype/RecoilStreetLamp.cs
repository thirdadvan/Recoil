using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways,DefaultExecutionOrder(-100),DisallowMultipleComponent,AddComponentMenu("Recoil/Street Lamp")]
public sealed class RecoilStreetLamp : MonoBehaviour
{
    static readonly List<RecoilStreetLamp> active=new List<RecoilStreetLamp>();
    public static IEnumerable<RecoilStreetLamp> Active {get{return active;}}
    public static int Revision {get;private set;}
    [Tooltip("可互动的发光灯头，光锥从此处向下延伸。")]
    public Transform lampHead;
    public SpriteRenderer headRenderer;
    [Min(.1f)] public float coneLength=4.8f, coneHalfWidth=2;
    [Min(.1f)] public float interactionRange=1.2f;
    public bool initiallyOn=true;
    [Header("Unstable street lamp")]
    [Tooltip("启用后，每盏路灯独立循环：亮起、随机快速闪两下、熄暗。互动关闭后不再闪烁。")]
    public bool unstableStreetLamp;
    [Tooltip("每次持续亮起的随机时长范围，单位秒。")]
    public Vector2 brightDuration=new Vector2(.5f,1.5f);
    [Tooltip("每次持续熄暗的随机时长范围，单位秒。")]
    public Vector2 darkDuration=new Vector2(2.5f,3.5f);
    [SerializeField] bool isOn=true;
    bool flickerLit=true,wasUnstable;
    int flickerPhase;
    float phaseRemaining;
    RecoilPrototype world;
    public bool IsOn {get{return isOn && isActiveAndEnabled && (!Application.isPlaying || !unstableStreetLamp || flickerLit);}}
    public Vector2 Origin {get{return lampHead?lampHead.position:transform.position;}}
    public Vector2 Direction {get{return lampHead?-(Vector2)lampHead.up:-(Vector2)transform.up;}}
    public bool BelongsTo(RecoilPrototype world){return transform.IsChildOf(world.transform);}
    void OnEnable(){if(!active.Contains(this))active.Add(this);world=GetComponentInParent<RecoilPrototype>();RestartFlicker();Revision++;SyncHead();}
    void OnDisable(){active.Remove(this);Revision++;}
    void Update()
    {
        if(!Application.isPlaying)return;
        if(wasUnstable!=unstableStreetLamp){RestartFlicker();Revision++;SyncHead();}
        if(!isOn || !unstableStreetLamp || (world && !world.Playing))return;
        phaseRemaining-=Time.deltaTime;
        while(phaseRemaining<=0)
        {
            // Bright, gap, flash, gap, flash, long dark. Each lamp owns its timer.
            flickerPhase=(flickerPhase+1)%6;
            phaseRemaining+=PhaseDuration(flickerPhase);
            bool lit=flickerPhase==0 || flickerPhase==2 || flickerPhase==4;
            if(flickerLit!=lit){flickerLit=lit;Revision++;SyncHead();}
        }
    }
    float PhaseDuration(int phase)
    {
        if(phase==0)return RandomDuration(brightDuration);
        if(phase==5)return RandomDuration(darkDuration);
        return phase==2 || phase==4?Random.Range(.06f,.12f):Random.Range(.08f,.2f);
    }
    static float RandomDuration(Vector2 range){return Random.Range(Mathf.Max(.01f,range.x),Mathf.Max(.01f,Mathf.Max(range.x,range.y)));}
    void RestartFlicker(){wasUnstable=unstableStreetLamp;flickerLit=true;flickerPhase=0;phaseRemaining=Application.isPlaying?PhaseDuration(0):0;}
    public bool Contains(Vector2 point)
    {
        if(!IsOn)return false;
        Vector2 delta=point-Origin,dir=Direction;
        float depth=Vector2.Dot(delta,dir),side=Mathf.Abs(delta.x*dir.y-delta.y*dir.x);
        return depth>=0 && depth<=coneLength && side<=coneHalfWidth*depth/Mathf.Max(.1f,coneLength);
    }
    public bool InRange(RecoilPrototype world)
    {return isOn && isActiveAndEnabled && BelongsTo(world) && Vector2.Distance(world.Player.Position,Origin)<=interactionRange && world.Clear(world.Player.Position,Origin);}
    public bool Interact(RecoilPrototype world)
    {
        if(!InRange(world))return false;
        world.InterruptInteraction();world.Player.NotifyForbiddenInteraction();
        isOn=false;Revision++;SyncHead();world.ShowInteractionMessage("Street lamp switched off.");return true;
    }
    public void ResetLamp(){isOn=initiallyOn;RestartFlicker();Revision++;SyncHead();}
    void SyncHead(){if(headRenderer)headRenderer.color=IsOn?new Color(1,.89f,.55f,1):new Color(.22f,.25f,.27f,1);}
    void OnValidate(){coneLength=Mathf.Max(.1f,coneLength);coneHalfWidth=Mathf.Max(.1f,coneHalfWidth);brightDuration=new Vector2(Mathf.Max(.01f,brightDuration.x),Mathf.Max(.01f,Mathf.Max(brightDuration.x,brightDuration.y)));darkDuration=new Vector2(Mathf.Max(.01f,darkDuration.x),Mathf.Max(.01f,Mathf.Max(darkDuration.x,darkDuration.y)));RestartFlicker();Revision++;SyncHead();}
    void OnDrawGizmosSelected()
    {
        Vector2 dir=Direction,side=new Vector2(-dir.y,dir.x),end=Origin+dir*coneLength;
        Gizmos.color=new Color(1,.85f,.3f,.8f);Gizmos.DrawLine(Origin,end+side*coneHalfWidth);Gizmos.DrawLine(Origin,end-side*coneHalfWidth);Gizmos.DrawLine(end-side*coneHalfWidth,end+side*coneHalfWidth);Gizmos.DrawWireSphere(Origin,interactionRange);
    }
}
