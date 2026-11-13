using UnityEngine;

// Ageing glow-starter fluorescent tubes: preheat, failed strikes, brief sustained light, dropout.
[DefaultExecutionOrder(500),DisallowMultipleComponent,AddComponentMenu("Recoil/Faulty Fluorescent Fixture")]
public sealed class FaultyFluorescentFixture : MonoBehaviour
{
    public Renderer[] tubes=new Renderer[0],endGlows=new Renderer[0];
    public Light roomLight;
    public RecoilLight2D gameplayLight;
    public const float MaximumIntensity=1.2f;
    [Range(0,MaximumIntensity)] public float roomIntensity=MaximumIntensity;
    [Min(0)] public float gameplayIntensity=.22f;
    public Vector2 darkPause=new Vector2(1.4f,3.8f),preheatDuration=new Vector2(.35f,.75f),litDuration=new Vector2(.8f,2.7f);
    public int randomSeed=19782;
    [Min(.1f)] public float timingMultiplier=2.5f;
    [Min(.02f)] public float brightnessTransition=.30f;
    public float LightLevel {get;private set;}
    public int TransitionCount {get;private set;}
    sealed class TubeState{public int phase;public float remaining,age,level,glow;public System.Random random;}
    TubeState[] states;RecoilPrototype world;MaterialPropertyBlock block;
    static readonly int Colour=Shader.PropertyToID("_Color"),Emission=Shader.PropertyToID("_EmissionColor");
    float Pick(TubeState s,Vector2 range){return Mathf.Lerp(range.x,range.y,(float)s.random.NextDouble());}
    float Duration(TubeState s){return BaseDuration(s)*Mathf.Max(.1f,timingMultiplier);}
    float BaseDuration(TubeState s)
    {
        switch(s.phase){case 0:return Pick(s,darkPause);case 1:return Pick(s,preheatDuration);case 2:return .08f+(float)s.random.NextDouble()*.07f;case 3:return .22f+(float)s.random.NextDouble()*.22f;case 4:return .10f+(float)s.random.NextDouble()*.09f;case 5:return Pick(s,preheatDuration);case 6:return Pick(s,litDuration);default:return .12f;}
    }
    public void ResetFlicker()
    {
        states=new TubeState[tubes.Length];TransitionCount=0;
        for(int i=0;i<states.Length;i++){var s=new TubeState{random=new System.Random(randomSeed+i*173),phase=0};s.remaining=Duration(s)+i*.63f*timingMultiplier;states[i]=s;}Apply();
    }
    void Awake(){world=GetComponentInParent<RecoilPrototype>();}
    void OnEnable()
    {
        world=GetComponentInParent<RecoilPrototype>();ResetFlicker();
    }
    void Update()
    {
        if(!Application.isPlaying||Time.timeScale<=0)return;
        if(!world)world=GetComponentInParent<RecoilPrototype>();
        // The start screen is presentation too. Actual gameplay modals/pause still freeze the light.
        if(!world||!world.HasStarted||world.Playing)Simulate(Time.deltaTime);
    }
    public void Simulate(float dt)
    {
        if(dt<=0)return;if(states==null||states.Length!=tubes.Length)ResetFlicker();
        foreach(var s in states){s.age+=dt;s.remaining-=dt;int guard=0;while(s.remaining<=0&&guard++<128){s.phase=(s.phase+1)%8;s.age=0;s.remaining+=Duration(s);TransitionCount++;}}Apply(dt);
    }
    void Tint(Renderer r,Color colour,Color emission)
    {if(!r)return;if(block==null)block=new MaterialPropertyBlock();r.GetPropertyBlock(block);block.SetColor(Colour,colour);block.SetColor(Emission,emission);r.SetPropertyBlock(block);}
    void Apply(float dt=0)
    {
        float sum=0;
        for(int i=0;i<states.Length;i++){
            var s=states[i];float level=s.phase==2?.70f:s.phase==4?.85f:s.phase==6?.72f+Mathf.Sin(s.age*3+i)*.035f:0;
            float preheat=(s.phase==1||s.phase==5)?.42f:0;
            s.level=Mathf.MoveTowards(s.level,level,Mathf.Max(0,dt)/Mathf.Max(.02f,brightnessTransition));
            s.glow=Mathf.MoveTowards(s.glow,preheat,Mathf.Max(0,dt)/Mathf.Max(.02f,brightnessTransition));
            level=s.level;preheat=s.glow;sum+=level;
            Tint(tubes[i],Color.Lerp(new Color(.18f,.19f,.17f),new Color(.76f,.83f,.73f),level),new Color(.72f,.85f,.71f)*level*1.8f);
            for(int end=0;end<2;end++){int at=i*2+end;if(at<endGlows.Length)Tint(endGlows[at],new Color(.25f,.10f,.035f),new Color(1,.19f,.035f)*preheat);}
        }
        LightLevel=states.Length>0?Mathf.Clamp01(sum/states.Length/.75f):0;
        if(roomLight){roomLight.enabled=LightLevel>.001f;roomLight.intensity=Mathf.Clamp(roomIntensity,0,MaximumIntensity)*LightLevel;}
        if(gameplayLight)gameplayLight.intensity=gameplayIntensity*LightLevel;
    }
    void OnDisable()
    {
        if(roomLight){roomLight.enabled=true;roomLight.intensity=Mathf.Clamp(roomIntensity,0,MaximumIntensity);}
        if(gameplayLight)gameplayLight.intensity=gameplayIntensity;
        foreach(var tube in tubes)if(tube)tube.SetPropertyBlock(null);foreach(var glow in endGlows)if(glow)glow.SetPropertyBlock(null);
    }
}
