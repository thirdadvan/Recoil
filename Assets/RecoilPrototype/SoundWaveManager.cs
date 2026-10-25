using System.Collections.Generic;
using UnityEngine;

// Building commands affect all scene Rampagers together. Travelling ripples are visual cues.
[DefaultExecutionOrder(-250),DisallowMultipleComponent]
public sealed class SoundWaveManager : MonoBehaviour
{
    public enum BroadcastPhase { Idle, Warning, Commands, Recovery }
    enum WaveKind { AidReport, BuildingBroadcast, DirectionCue, RedWarning }
    [SerializeField] RecoilPrototype world;
    [SerializeField] Transform controlArea, emitter;
    [SerializeField] Material waveMaterial;
    [Tooltip("仅在室内场景启用工厂声波事件；不影响玩家自身声音和怪物求援。")]
    public bool buildingBroadcastsEnabled=true;
    [Tooltip("声波圆线视觉开关；关闭后可由相机折射 Shader 显示，声波对怪物的作用不变。") ]
    public bool drawBroadcastCircles=true;
    [Header("Alternate 3-wave and 4-wave sequences")]
    [Min(1)] public int rounds=3,maxRounds=4;
    public float initialDelay=12,interval=15,warningDuration=3,holdDuration=1.5f,commandDuration=1,recoveryDuration=1.25f;
    [HideInInspector] public float moveDelay=.1f; // Legacy scene data; reaction timing has one owner below.
    [Header("Readable broadcast cues")]
    [Min(.1f)] public float directionWaveDuration=3;
    [UnityEngine.Serialization.FormerlySerializedAs("postWaveMoveDelay")]
    [Min(0),Tooltip("指令波开始后，怪物开始移动前的反应时间；此期间不累计方向违规。") ] public float commandReactionDelay=.5f;
    public float CommandMoveDelay {get{return Mathf.Max(0,commandReactionDelay);}}
    class Wave
    {
        public Vector2 position;public float radius,age,duration;public int direction,id,index,total;
        public WaveKind kind;public LineRenderer line;public LineRenderer[] trails;
    }
    readonly List<Wave> waves=new List<Wave>();
    float remaining,cueDelay,cueHold,cueMove;int nextCount,serial;
    Vector2? triggeredSource;
    public bool HasLocalizedSource {get{return triggeredSource.HasValue;}}
    public Vector2 BroadcastSource {get{return Source;}}
    Vector2 Source {get{return triggeredSource.HasValue?triggeredSource.Value:emitter?(Vector2)emitter.position:Vector2.zero;}}
    public BroadcastPhase Phase {get;private set;}
    public int Round {get;private set;}
    public int TotalWaves {get;private set;}
    public int CueIndex {get;private set;}
    public int Direction {get;private set;}=-1;
    // One per-frame command slice, shared by every monster instead of independent countdowns.
    public float DelayStep {get;private set;}
    public float MoveStep {get;private set;}
    public float HoldStep {get;private set;}
    public float CueStartedAt {get;private set;}
    public float WarningStartedAt {get;private set;}
    public float DirectionVisualDuration {get{return Mathf.Max(.1f,directionWaveDuration);}}
    public float Remaining {get{return Mathf.Max(0,remaining);}}
    public float CueHold {get{return cueDelay>0 || cueMove>0?0:cueHold;}}
    public float CueDelay {get{return cueDelay;}}
    public bool RedAlert {get{return Phase!=BroadcastPhase.Idle;}}
    public float CueMove {get{return cueDelay>0?0:cueMove;}}
    public bool CommandActive {get{return cueDelay<=0 && cueMove>0;}}
    public float WaveInterval {get{return CommandMoveDelay+commandDuration+holdDuration;}}
    public void Configure(RecoilPrototype owner,Transform area,Transform source,Material material){world=owner;controlArea=area;emitter=source;waveMaterial=material;}
    public void Initialize(RecoilPrototype owner){world=owner;world.RegisterSoundWaveManager(this);ResetManager();}
    public void ResetManager()
    {foreach(var w in waves)if(w.line)Destroy(w.line.gameObject);waves.Clear();triggeredSource=null;Phase=BroadcastPhase.Idle;Round=CueIndex=serial=0;TotalWaves=nextCount=rounds;remaining=initialDelay;cueDelay=cueHold=cueMove=0;DelayStep=MoveStep=HoldStep=0;CueStartedAt=WarningStartedAt=0;Direction=0;}
    public bool Contains(Vector2 p)
    {return triggeredSource.HasValue && RedAlert || buildingBroadcastsEnabled && controlArea && Mathf.Abs(p.x-controlArea.position.x)<=Mathf.Abs(controlArea.lossyScale.x)/2 && Mathf.Abs(p.y-controlArea.position.y)<=Mathf.Abs(controlArea.lossyScale.y)/2;}
    public void StartBroadcast()
    {
        if(!buildingBroadcastsEnabled || Phase!=BroadcastPhase.Idle)return;
        BeginBroadcast();
    }
    public bool StartBroadcastAt(Vector2 source)
    {
        if(!world || Phase!=BroadcastPhase.Idle)return false;
        triggeredSource=source;BeginBroadcast();return true;
    }
    void BeginBroadcast()
    {
        TotalWaves=nextCount;nextCount=TotalWaves>=Mathf.Max(rounds,maxRounds)?rounds:TotalWaves+1;
        Round=CueIndex=0;Direction=0;cueDelay=cueMove=0;cueHold=warningDuration;Phase=BroadcastPhase.Warning;remaining=warningDuration;
        WarningStartedAt=world.Clock;DelayStep=MoveStep=HoldStep=0;
        AddWave(Source,100,warningDuration,WaveKind.RedWarning,0);
        foreach(var enemy in world.Rampagers)enemy.ReceiveBuildingWarning(warningDuration);
        world.Say("RED ALERT: Coalers wake and look upward; attacks gain speed. First direction wave in 3 seconds.");
    }
    void Update(){if(world && world.Playing)Simulate(world.FrameDelta);}
    public void EmitAid(Vector2 location,float radius){AddWave(location,radius,.8f,WaveKind.AidReport,-1);}
    Wave AddWave(Vector2 location,float radius,float duration,WaveKind kind,int direction)
    {
        var wave=new Wave{position=location,radius=radius,duration=Mathf.Max(.01f,duration),kind=kind,direction=direction};
        if(drawBroadcastCircles || kind==WaveKind.AidReport)AttachWaveLines(wave);
        waves.Add(wave);return wave;
    }
    void AttachWaveLines(Wave wave)
    {
        var go=new GameObject(wave.kind+" wave");go.transform.SetParent(transform);
        var line=go.AddComponent<LineRenderer>();line.sharedMaterial=waveMaterial?waveMaterial:world.SprayMaterial;
        line.useWorldSpace=true;line.loop=true;line.positionCount=128;
        line.startWidth=line.endWidth=(wave.kind==WaveKind.BuildingBroadcast || wave.kind==WaveKind.RedWarning)?.07f:.045f;line.sortingOrder=150;
        wave.line=line;
        if(wave.kind!=WaveKind.BuildingBroadcast && wave.kind!=WaveKind.RedWarning)return;
        wave.trails=new LineRenderer[2];
        for(int i=0;i<2;i++)
        {
            var trail=new GameObject("Ripple trail").AddComponent<LineRenderer>();trail.transform.SetParent(go.transform,false);
            trail.sharedMaterial=line.sharedMaterial;trail.useWorldSpace=true;trail.loop=true;trail.positionCount=128;
            trail.startWidth=trail.endWidth=.045f;trail.sortingOrder=150;wave.trails[i]=trail;
        }
    }
    void EmitBroadcast()
    {
        var w=AddWave(Source,100,DirectionVisualDuration,WaveKind.BuildingBroadcast,Random.value<.5f?-1:1);
        w.id=++serial;w.index=Round+1;w.total=TotalWaves;
        CueIndex=w.index;Direction=w.direction;cueDelay=CommandMoveDelay;cueHold=holdDuration;cueMove=commandDuration;
        CueStartedAt=world.Clock;DelayStep=MoveStep=HoldStep=0;
        foreach(var enemy in world.Rampagers)if(enemy.ReceiveBuildingWave(w.id,w.index,w.total,w.direction))AddWave(enemy.Position,3,DirectionVisualDuration,WaveKind.DirectionCue,w.direction);
    }
    public void Simulate(float dt)
    {
        float delayUsed=Mathf.Min(dt,cueDelay);cueDelay-=delayUsed;
        float moveUsed=Mathf.Min(dt-delayUsed,cueMove);cueMove-=moveUsed;
        cueHold=Mathf.Max(0,cueHold-(dt-delayUsed-moveUsed));
        DelayStep=delayUsed;MoveStep=moveUsed;HoldStep=Mathf.Max(0,dt-delayUsed-moveUsed);
        if(buildingBroadcastsEnabled || RedAlert)remaining-=dt;
        if((buildingBroadcastsEnabled || RedAlert) && remaining<=.000001f)
        {
            if(Phase==BroadcastPhase.Idle)StartBroadcast();
            else if(Phase==BroadcastPhase.Warning){Phase=BroadcastPhase.Commands;remaining+=WaveInterval;EmitBroadcast();}
            else if(Phase==BroadcastPhase.Commands)
            {
                if(Round+1<TotalWaves){Round++;remaining+=WaveInterval;EmitBroadcast();}
                else{foreach(var enemy in world.Rampagers)enemy.EndSwarm(recoveryDuration);Phase=BroadcastPhase.Recovery;remaining=recoveryDuration;cueDelay=cueHold=cueMove=0;DelayStep=MoveStep=HoldStep=0;}
            }
            else if(Phase==BroadcastPhase.Recovery){Phase=BroadcastPhase.Idle;remaining=interval;triggeredSource=null;}
        }
        for(int i=waves.Count-1;i>=0;i--)
        {
            var w=waves[i];w.age+=dt;float t=Mathf.Clamp01(w.age/w.duration);
            if(t>=1){if(w.line)Destroy(w.line.gameObject);waves.RemoveAt(i);continue;}
            if(!w.line && drawBroadcastCircles)AttachWaveLines(w);
            if(w.line)w.line.enabled=drawBroadcastCircles || w.kind==WaveKind.AidReport;
            if(w.trails!=null)foreach(var trail in w.trails)if(trail)trail.enabled=drawBroadcastCircles;
            Vector2 center=w.position;
            float radius=Mathf.Max(.05f,w.radius*((w.kind==WaveKind.DirectionCue || w.kind==WaveKind.BuildingBroadcast) && w.direction<0?1-t:t));
            if((w.kind==WaveKind.BuildingBroadcast || w.kind==WaveKind.RedWarning) && Camera.main)
            {
                var camera=Camera.main;float offset=camera.orthographicSize*2,width=camera.orthographicSize*camera.aspect*2;
                if(triggeredSource.HasValue)
                {
                    float reach=Vector2.Distance(center,camera.transform.position)+width+offset;
                    radius=Mathf.Max(.05f,reach*(w.kind==WaveKind.BuildingBroadcast && w.direction<0?1-t:t));
                }
                else
                {
                    center=new Vector2(camera.transform.position.x-width/2-offset,camera.transform.position.y);
                    radius=offset+.6f+(w.kind==WaveKind.BuildingBroadcast && w.direction<0?1-t:t)*width;
                }
            }
            Color color=RecoilPrototype.Hex(w.kind==WaveKind.RedWarning?0xff3434:w.kind==WaveKind.AidReport?0xd49cff:w.direction>0?0xffc77d:0x77d7ff,(w.kind==WaveKind.BuildingBroadcast || w.kind==WaveKind.RedWarning)?.9f:.6f*(1-t));
            if(w.line){w.line.startColor=w.line.endColor=color;DrawRing(w.line,center,radius);}
            if(w.trails!=null)for(int n=0;n<w.trails.Length;n++)
            {var trailColor=color;trailColor.a*=n==0?.55f:.3f;w.trails[n].startColor=w.trails[n].endColor=trailColor;DrawRing(w.trails[n],center,Mathf.Max(.05f,radius-(n+1)*.65f));}
        }

    }
    static void DrawRing(LineRenderer line,Vector2 center,float radius)
    {for(int n=0;n<line.positionCount;n++){float a=n*Mathf.PI*2/line.positionCount;line.SetPosition(n,(Vector3)(center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius));}}
    void OnDestroy(){foreach(var w in waves)if(w.line)Destroy(w.line.gameObject);}
    void OnDrawGizmosSelected(){if(controlArea){Gizmos.color=Color.cyan;Gizmos.DrawWireCube(controlArea.position,controlArea.lossyScale);}if(emitter){Gizmos.color=Color.yellow;Gizmos.DrawWireSphere(emitter.position,1);}}
}
