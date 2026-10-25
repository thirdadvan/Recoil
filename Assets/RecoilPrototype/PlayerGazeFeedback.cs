using UnityEngine;

// Private bodily feedback: never feeds the player-noise/monster hearing system.
[DefaultExecutionOrder(150),DisallowMultipleComponent,AddComponentMenu("Recoil/Player Gaze Feedback")]
public sealed class PlayerGazeFeedback : MonoBehaviour
{
    [Min(.1f)] public float beatsPerSecond=1;
    [Min(.1f)] public float attackBeatsPerSecond=3;
    [Min(.01f)] public float fadeOutDuration=2;
    [Range(0,1)] public float maximumVolume=.7f;
    [Range(0,1)] public float grabbedMaximumVolume=1f;
    public bool heartbeatAudio=true;
    public Vector2 chestOffset=new Vector2(0,.12f);
    [Min(.01f)] public float heartSize=.3f;
    public int WatcherCount {get;private set;}
    public int AttackWatcherCount {get;private set;}
    public float CurrentBeatRate {get;private set;}
    public float Visibility {get;private set;}
    float beatPhase;bool attackFeedback;
    public float Pressure {get;private set;}
    RecoilPrototype world;
    SpriteRenderer heart;
    Texture2D texture;Sprite sprite;
    AudioSource audioSource;AudioClip heartbeat;
    public void Initialize(RecoilPrototype context)
    {
        world=context;
        if(heart)return;
        texture=new Texture2D(64,64,TextureFormat.RGBA32,false);texture.filterMode=FilterMode.Bilinear;
        var colors=new Color32[64*64];
        for(int y=0;y<64;y++)for(int x=0;x<64;x++)
        {
            float px=(x-31.5f)/25f,py=(y-29f)/25f;
            float a=px*px+py*py-1;bool inside=a*a*a-px*px*py*py*py<=0;
            colors[y*64+x]=inside?new Color32(255,255,255,255):new Color32(0,0,0,0);
        }
        texture.SetPixels32(colors);texture.Apply(false,true);sprite=Sprite.Create(texture,new Rect(0,0,64,64),new Vector2(.5f,.5f),64);
        var visual=new GameObject("Heart - gaze feedback");visual.transform.SetParent(transform,false);
        heart=visual.AddComponent<SpriteRenderer>();heart.sprite=sprite;heart.sortingOrder=170;heart.enabled=false;
        audioSource=gameObject.AddComponent<AudioSource>();audioSource.playOnAwake=false;audioSource.loop=true;audioSource.spatialBlend=0;audioSource.volume=0;audioSource.priority=0;audioSource.dopplerLevel=0;
        BuildHeartbeatClip();
    }
    void BuildHeartbeatClip()
    {
        if(!audioSource)return;
        audioSource.Stop();if(heartbeat)Release(heartbeat);
        const int sampleRate=24000;var samples=new float[sampleRate];
        for(int i=0;i<samples.Length;i++)
        {
            float t=i/(float)sampleRate;
            float first=Thump(t-.05f,72),second=Thump(t-.24f,86)*.7f;
            samples[i]=(first+second)+Thump(t-.05f,144)*.55f+Thump(t-.24f,172)*.38f;
        }
        float peak=0;foreach(float sample in samples)peak=Mathf.Max(peak,Mathf.Abs(sample));
        if(peak>0)for(int i=0;i<samples.Length;i++)samples[i]=samples[i]/peak*.95f;
        heartbeat=AudioClip.Create("Private heartbeat - fixed rhythm",sampleRate,1,sampleRate,false);heartbeat.SetData(samples,0);audioSource.clip=heartbeat;
    }
    static float Thump(float t,float frequency)
    {if(t<0 || t>.18f)return 0;return Mathf.Sin(t*frequency*Mathf.PI*2)*Mathf.Exp(-t*28)*Mathf.Clamp01(t/.008f);}
    public void ResetFeedback()
    {WatcherCount=AttackWatcherCount=0;Pressure=Visibility=beatPhase=0;attackFeedback=false;CurrentBeatRate=beatsPerSecond;if(heart)heart.enabled=false;if(audioSource){audioSource.Stop();audioSource.volume=0;}}
    void LateUpdate(){if(world)Simulate(world.ConversationActive?Mathf.Min(Time.unscaledDeltaTime,.05f):world.FrameDelta);}
    public void Simulate(float dt)
    {
        if(!world || !heart)return;
        if(!world.Playing && !world.ConversationKeepsAudioPlaying){ResetFeedback();return;}
        WatcherCount=AttackWatcherCount=0;float sum=0,visible=0;
        foreach(var enemy in world.Rampagers)if(enemy && enemy.isActiveAndEnabled && enemy.PlayerUnderGaze)
        {
            WatcherCount++;if(enemy.State==RampagerController.Behaviour.Attack)AttackWatcherCount++;
            visible=Mathf.Max(visible,enemy.GazeFeedbackStrength);float near=1-Mathf.Clamp01(Vector3.Distance(enemy.transform.position,world.Player.WorldPosition)/Mathf.Max(.1f,enemy.SightRange));
            sum+=Mathf.Lerp(.25f,1,near);
        }
        if(WatcherCount>0){Pressure=1-Mathf.Exp(-sum*.85f);Visibility=visible;attackFeedback=AttackWatcherCount>0;}
        else Visibility=Mathf.MoveTowards(Visibility,0,Mathf.Max(0,dt)/Mathf.Max(.01f,fadeOutDuration));
        var grab=world.Player.GrabPressure;float grip=grab?grab.Pressure:0;
        if(grip>0){Visibility=Mathf.Max(Visibility,grip);Pressure=Mathf.Max(Pressure,grip);}
        heart.enabled=Visibility>0;
        CurrentBeatRate=Mathf.Max(.1f,attackFeedback?attackBeatsPerSecond:beatsPerSecond);
        beatPhase=Mathf.Repeat(beatPhase+Mathf.Max(0,dt)*CurrentBeatRate,1);float phase=beatPhase;
        float beat=Mathf.Exp(-Mathf.Pow((phase-.08f)/.065f,2))+.55f*Mathf.Exp(-Mathf.Pow((phase-.27f)/.07f,2));
        heart.transform.position=world.Player.WorldPosition+(Vector3)chestOffset;
        float size=heartSize*(attackFeedback?1.3f:1)*(1+beat*.18f);Vector3 parentScale=transform.lossyScale;
        heart.transform.localScale=new Vector3(size/Mathf.Max(.001f,Mathf.Abs(parentScale.x)),size/Mathf.Max(.001f,Mathf.Abs(parentScale.y)),1);
        Color color=new Color(1,.12f,.12f);
        color.a=Mathf.Lerp(.15f,.95f,Pressure)*Visibility;heart.color=color;
        // Sound cadence is fixed; only loudness changes. Attack keeps its existing faster heart icon.
        audioSource.pitch=Mathf.Max(.1f,beatsPerSecond);
        float audible=WatcherCount>0 || (grab && grab.GrabberCount>0)?1:Visibility;
        audioSource.volume=heartbeatAudio?Mathf.Lerp(maximumVolume,grabbedMaximumVolume,grip)*Mathf.Lerp(.7f,1,Pressure)*audible:0;
        if(heartbeatAudio && audible>0 && !audioSource.isPlaying){audioSource.time=phase;audioSource.Play();}
        else if((audible<=0 || !heartbeatAudio) && audioSource.isPlaying)audioSource.Stop();
    }
    void OnEnable(){AudioSettings.OnAudioConfigurationChanged+=RestoreAudio;}
    void RestoreAudio(bool deviceWasChanged)
    {
        // Unity clears runtime-created PCM clips when its audio device resets.
        // Recreate the clip as well as restarting the source after a device change.
        if(audioSource)BuildHeartbeatClip();
    }
    void OnDisable(){AudioSettings.OnAudioConfigurationChanged-=RestoreAudio;ResetFeedback();}
    void Release(Object item){if(!item)return;if(Application.isPlaying)Destroy(item);else DestroyImmediate(item);}
    void OnDestroy(){Release(sprite);Release(texture);Release(heartbeat);}
}
