using System;
using UnityEngine;

// Persistent music and wind buses. Scene bindings never restart a shared track.
[DisallowMultipleComponent,AddComponentMenu("Recoil/Global Background Music")]
public sealed class RecoilBackgroundMusic : MonoBehaviour
{
    public const float MusicFadeSeconds=.8f;
    public const float BattleFadeSeconds=2f;
    public const float SlowdownOnsetSeconds=.5f;
    public const float InitialSlowPlaybackSpeed=.9f;
    public const float MinimumPlaybackSpeed=.65f;
    static RecoilBackgroundMusic instance;
    [Range(0,1)] public float volume=.35f;
    [Range(0,1)] public float windVolume=.35f;
    readonly PlaybackSlowdown playbackSlowdown=new PlaybackSlowdown();
    RecoilPlayerController player;
    RecoilPrototype world;
    string windKey="Wind";
    sealed class Track
    {
        public string key;
        public AudioSource source;
        public float gain,target,startGain,fadeElapsed,resumeTime;
        public int previousSample;
    }
    Track[] tracks;
    bool restorePending;
    float nextHealthCheck,nextDeviceReset;
    double previousDsp;

    void OnEnable(){AudioSettings.OnAudioConfigurationChanged+=AudioConfigurationChanged;}
    void OnDisable(){AudioSettings.OnAudioConfigurationChanged-=AudioConfigurationChanged;}
    void AudioConfigurationChanged(bool deviceWasChanged){restorePending=true;}

    public static void Bind(RecoilPlayerController currentPlayer,RecoilPrototype currentWorld)
    {
        if(!Application.isPlaying)return;
        if(!instance)
        {
            var root=new GameObject("Global Background Music");
            instance=root.AddComponent<RecoilBackgroundMusic>();
        }
        if(instance.player!=currentPlayer)instance.playbackSlowdown.Reset();
        instance.player=currentPlayer;instance.world=currentWorld;
    }
    void Awake()
    {
        if(instance && instance!=this){Destroy(gameObject);return;}
        instance=this;DontDestroyOnLoad(gameObject);
        string[] keys={"FactoryKids","Outdoor","Warehouse","Memory","Battle","Wind","Indoor_Wind"};
        tracks=new Track[keys.Length];
        for(int i=0;i<keys.Length;i++)
        {
            var source=gameObject.AddComponent<AudioSource>();
            source.playOnAwake=false;source.loop=true;source.spatialBlend=0;
            source.dopplerLevel=0;source.priority=128;source.volume=0;
            source.clip=Resources.Load<AudioClip>("Audio/"+keys[i]);
            if(!source.clip)Debug.LogWarning("Background audio clip is missing: "+keys[i],this);
            tracks[i]=new Track{key=keys[i],source=source};
        }
        previousDsp=AudioSettings.dspTime;nextHealthCheck=Time.unscaledTime+2;
    }
    public static float PlaybackSpeed(float movementMultiplier,float minimumPitch=MinimumPlaybackSpeed)
    {
        if(movementMultiplier>=1)return 1;
        float t=Mathf.InverseLerp(PlayerGrabPressure.MinimumSpeedMultiplier,1,movementMultiplier);
        return Mathf.Lerp(minimumPitch,InitialSlowPlaybackSpeed,t);
    }
    // The first half-second is a distinct cue; sustained pressure then follows movement speed.
    public sealed class PlaybackSlowdown
    {
        bool slowed;
        float onsetElapsed,onsetStartPitch;
        public float Pitch {get;private set;}=1;
        public void Reset(){slowed=false;onsetElapsed=0;Pitch=1;}
        public float Advance(float movementMultiplier,float dt)
        {
            dt=Mathf.Max(0,dt);
            if(movementMultiplier>=1)
            {
                slowed=false;onsetElapsed=0;
                Pitch=Mathf.MoveTowards(Pitch,1,dt*(1-InitialSlowPlaybackSpeed)/SlowdownOnsetSeconds);
                return Pitch;
            }
            if(!slowed)
            {
                slowed=true;onsetElapsed=0;onsetStartPitch=Pitch;
            }
            // Re-grabbing during recovery must never speed the music up to 90%.
            if(onsetStartPitch>InitialSlowPlaybackSpeed && onsetElapsed<SlowdownOnsetSeconds)
            {
                float step=Mathf.Min(dt,SlowdownOnsetSeconds-onsetElapsed);
                onsetElapsed+=step;dt-=step;
                Pitch=Mathf.Lerp(onsetStartPitch,InitialSlowPlaybackSpeed,onsetElapsed/SlowdownOnsetSeconds);
            }
            float target=PlaybackSpeed(movementMultiplier);
            Pitch=Mathf.MoveTowards(Pitch,target,dt*(InitialSlowPlaybackSpeed-MinimumPlaybackSpeed)/SlowdownOnsetSeconds);
            return Pitch;
        }
    }
    public static string ResolveMusicKey(string sceneName,bool enraged,bool returningWithChild)
    {
        if(string.IsNullOrEmpty(sceneName))return null;
        // All memory scenes share the score, including future memories.
        if(sceneName.IndexOf("Memory",StringComparison.OrdinalIgnoreCase)>=0)return "Memory";
        if(enraged)return "Battle";
        if(sceneName=="RecoilPrototype" || sceneName=="Outdoor")return "Outdoor";
        if(sceneName.IndexOf("Warehouse",StringComparison.OrdinalIgnoreCase)>=0 ||
            sceneName.IndexOf("Factory",StringComparison.OrdinalIgnoreCase)>=0)return "Warehouse";
        bool residential=sceneName.IndexOf("Residential",StringComparison.OrdinalIgnoreCase)>=0;
        // The quiet arrival belongs to the dormitory, not the outdoor return route.
        if(residential && returningWithChild)return null;
        return residential?"FactoryKids":null;
    }
    public static string ResolveWindKey(string sceneName)
    {
        // The yard is the outdoor hub; factory, store, dormitory and memories are indoors.
        return string.IsNullOrEmpty(sceneName) || sceneName=="RecoilPrototype" || sceneName=="Outdoor"?"Wind":"Indoor_Wind";
    }
    string DesiredMusic()
    {
        if(!world)return null;
        bool enraged=false;
        foreach(var enemy in world.Rampagers)
            if(enemy && enemy.isActiveAndEnabled && (enemy.State==RampagerController.Behaviour.Attack || enemy.PreparingAttack))
            {enraged=true;break;}
        // Preserve the quiet dormitory arrival if the child is temporarily asked to wait.
        bool returning=world.HasItem(RecoilPrototype.WarehouseChildEvacuated) || world.EscortingChild ||
            world.HasItem(EscortChild.EscortFlag) || world.HasItem(RecoilPrototype.WarehouseRescueStarted);
        return ResolveMusicKey(world.gameObject.scene.name,enraged,returning);
    }
    void LateUpdate()
    {
        if(tracks==null)return;
        string desired=DesiredMusic();
        // Keep the previous ambience during the gap between scene bindings.
        if(world)windKey=ResolveWindKey(world.gameObject.scene.name);
        var grab=player?player.GrabPressure:null;
        float pitch=playbackSlowdown.Advance(grab?grab.SpeedMultiplier:1,AudioListener.pause?0:Time.deltaTime);
        foreach(var track in tracks)
        {
            bool wind=track.key=="Wind" || track.key=="Indoor_Wind";
            float target=(wind?track.key==windKey:track.key==desired)?1:0;
            if(track.target!=target)
            {
                track.startGain=track.gain;track.target=target;track.fadeElapsed=0;
                if(target>0 && track.source.clip && !track.source.isPlaying)track.source.Play();
            }
            if(!AudioListener.pause)track.fadeElapsed+=Time.unscaledDeltaTime;
            float duration=track.key=="Battle"?BattleFadeSeconds:MusicFadeSeconds;
            track.gain=Mathf.Lerp(track.startGain,track.target,Mathf.Clamp01(track.fadeElapsed/duration));
            track.source.volume=track.gain*(wind?windVolume:volume);
            track.source.pitch=wind?1:pitch;
            if(track.target==0 && track.gain==0 && track.source.isPlaying)
            {track.source.Stop();track.resumeTime=0;track.previousSample=0;}
        }
        if(AudioListener.pause)return;
        if(restorePending)
        {
            restorePending=false;
            foreach(var track in tracks)if(track.target>0 || track.gain>0)ResumeTrack(track);
        }
        if(Time.unscaledTime<nextHealthCheck)return;
        nextHealthCheck=Time.unscaledTime+2;
        double dsp=AudioSettings.dspTime;
        // Recover from an output device lost after an Editor reload.
        if(dsp<=previousDsp+.0001 && Time.unscaledTime>=nextDeviceReset)
        {
            nextDeviceReset=Time.unscaledTime+10;
            AudioSettings.Reset(AudioSettings.GetConfiguration());restorePending=true;
        }
        else foreach(var track in tracks)
        {
            if(!track.source.clip || (track.target==0 && track.gain==0))continue;
            if(!track.source.isPlaying || (track.previousSample>0 && track.source.timeSamples==track.previousSample))ResumeTrack(track);
            if(track.source.time>0)track.resumeTime=track.source.time;
            track.previousSample=track.source.timeSamples;
        }
        previousDsp=AudioSettings.dspTime;
    }
    void ResumeTrack(Track track)
    {
        var source=track.source;if(!source || !source.clip)return;
        source.Stop();source.clip.UnloadAudioData();source.clip.LoadAudioData();
        source.time=Mathf.Clamp(track.resumeTime,0,Mathf.Max(0,source.clip.length-.1f));source.Play();
        track.previousSample=source.timeSamples;
    }
    void OnDestroy(){if(instance==this)instance=null;}
}
