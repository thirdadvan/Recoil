using System.Collections.Generic;
using UnityEngine;

// Optional camera presentation. Observes broadcast phase; never drives AI or gameplay timing.
[ExecuteAlways,RequireComponent(typeof(Camera)),DisallowMultipleComponent]
[AddComponentMenu("Recoil/Effects/Broadcast Refraction")]
public sealed class BroadcastRefractionEffect : MonoBehaviour
{
    public Shader refractionShader;
    public SoundWaveManager broadcast;
    [Tooltip("留空时从画面左侧远处传来；指定后使用物体的世界坐标作为声源。")]
    public Transform emitter;
    public bool effectEnabled=true;
    [Min(.1f),Tooltip("透视相机的折射参考平面距离，通常设置为相机到角色所在深度的距离。正交相机忽略。") ]
    public float perspectiveReferenceDistance=18;
    [Header("Refraction — world units")]
    [Range(0,1.5f)] public float strength=.28f;
    [Min(.1f)] public float packetWidth=4f;
    [Min(.1f)] public float wavelength=1.1f;
    [Range(0,1)] public float irregularity=.25f;
    [Range(0,.4f)] public float chromaticAberration=.045f;
    [Range(0,.4f)] public float warningTint=.08f;
    public int ActivePulseCount {get{return pulses.Count;}}
    public float Clock {get{return context?context.Clock:localClock;}}
    sealed class Pulse
    {public Vector3 origin;public float start,end,started,duration;public int direction;public bool warning;}
    const int Capacity=4;
    readonly List<Pulse> pulses=new List<Pulse>(Capacity);
    readonly Vector4[] fronts=new Vector4[Capacity],shapes=new Vector4[Capacity],tints=new Vector4[Capacity];
    Camera lens;
    Material material;
    RecoilPrototype context;
    SoundWaveManager.BroadcastPhase observedPhase;
    int observedCue;
    float localClock;
    void OnEnable()
    {
        lens=GetComponent<Camera>();context=broadcast?broadcast.GetComponentInParent<RecoilPrototype>():null;
        observedPhase=SoundWaveManager.BroadcastPhase.Idle;observedCue=0;ClearPulses();
    }
    void Update()
    {
        if(!Application.isPlaying)return;
        if(!context && broadcast)context=broadcast.GetComponentInParent<RecoilPrototype>();
        if(context && !context.Playing)return;
        if(!context)localClock+=Time.deltaTime;
        for(int i=pulses.Count-1;i>=0;i--)if(Clock-pulses[i].started>=pulses[i].duration)pulses.RemoveAt(i);
        if(!broadcast)return;
        var phase=broadcast.Phase;
        if(phase==SoundWaveManager.BroadcastPhase.Warning && observedPhase!=phase)
            EmitPulse(1,true,broadcast.warningDuration);
        if(phase==SoundWaveManager.BroadcastPhase.Commands && (observedPhase!=phase || observedCue!=broadcast.CueIndex))
        {EmitPulse(broadcast.Direction,false,broadcast.WaveInterval);observedCue=broadcast.CueIndex;}
        if(phase==SoundWaveManager.BroadcastPhase.Idle && observedPhase!=phase){ClearPulses();observedCue=0;}
        observedPhase=phase;
    }
    public void ClearPulses(){pulses.Clear();}
    public void EmitPulse(int direction,bool warning=false,float duration=2.6f)
    {
        if(!lens)lens=GetComponent<Camera>();
        if(!context && broadcast)context=broadcast.GetComponentInParent<RecoilPrototype>();
        Vector2 span=ViewSpan(lens.aspect);
        float depth=lens.orthographic?Mathf.Max(.1f,Mathf.Abs(lens.transform.position.z)):Mathf.Max(.1f,perspectiveReferenceDistance);
        // Keep a world anchor, so panning and FOV changes do not glue the ripple to the screen.
        Vector3 originWorld=emitter?emitter.position:lens.ViewportToWorldPoint(new Vector3(-.7f/lens.aspect,.5f,depth));
        Vector3 viewport=lens.WorldToViewportPoint(originWorld);
        Vector2 origin=new Vector2(viewport.x*span.x,viewport.y*span.y);
        var rect=new Rect(Vector2.zero,span);
        float nearest=Vector2.Distance(origin,new Vector2(Mathf.Clamp(origin.x,rect.xMin,rect.xMax),Mathf.Clamp(origin.y,rect.yMin,rect.yMax)));
        float furthest=0;
        for(int x=0;x<2;x++)for(int y=0;y<2;y++)furthest=Mathf.Max(furthest,Vector2.Distance(origin,new Vector2(x==0?rect.xMin:rect.xMax,y==0?rect.yMin:rect.yMax)));
        if(pulses.Count>=Capacity)pulses.RemoveAt(0);
        pulses.Add(new Pulse{origin=originWorld,start=Mathf.Max(0,nearest-packetWidth*1.5f),end=furthest+packetWidth*1.5f,started=Clock,duration=Mathf.Max(.1f,duration),direction=direction<0?-1:1,warning=warning});
    }
    Vector2 ViewSpan(float aspect)
    {
        float halfHeight=lens.orthographic?lens.orthographicSize:Mathf.Tan(lens.fieldOfView*Mathf.Deg2Rad*.5f)*Mathf.Max(.1f,perspectiveReferenceDistance);
        return new Vector2(halfHeight*2*aspect,halfHeight*2);
    }
    void OnRenderImage(RenderTexture source,RenderTexture destination)
    {
        if(!lens)lens=GetComponent<Camera>();
        if(!effectEnabled || pulses.Count==0 || !refractionShader || !refractionShader.isSupported)
        {Graphics.Blit(source,destination);return;}
        if(!material){material=new Material(refractionShader);material.hideFlags=HideFlags.HideAndDontSave;}
        Vector2 span=ViewSpan(source.width/(float)source.height);
        int count=0;
        foreach(var p in pulses)
        {
            float t=Mathf.Clamp01((Clock-p.started)/p.duration);if(t>=1)continue;
            float fade=Mathf.SmoothStep(0,1,t/.07f)*(1-Mathf.SmoothStep(.84f,1,t));
            float radius=Mathf.Lerp(p.start,p.end,p.direction<0?1-t:t);
            Vector3 viewport=lens.WorldToViewportPoint(p.origin);if(viewport.z<=0)continue;
            fronts[count]=new Vector4(viewport.x*span.x,viewport.y*span.y,radius,fade);
            shapes[count]=new Vector4(packetWidth,wavelength,strength,p.direction);
            tints[count]=p.warning?new Vector4(1,.12f,.08f,warningTint):Vector4.zero;count++;
        }
        if(count==0){Graphics.Blit(source,destination);return;}
        material.SetVector("_WorldRect",new Vector4(0,0,span.x,span.y));
        material.SetVectorArray("_PulseFront",fronts);material.SetVectorArray("_PulseShape",shapes);material.SetVectorArray("_PulseTint",tints);
        material.SetInt("_PulseCount",count);material.SetFloat("_RippleTime",Clock);material.SetFloat("_Irregularity",irregularity);material.SetFloat("_ChromaticAberration",chromaticAberration);
        Graphics.Blit(source,destination,material,0);
    }
    void OnDisable()
    {
        ClearPulses();if(!material)return;
        if(Application.isPlaying)Destroy(material);else DestroyImmediate(material);material=null;
    }
}
