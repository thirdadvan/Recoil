using UnityEngine;

// One 2D music source for the whole game. Scene changes only replace its player binding.
[DisallowMultipleComponent,AddComponentMenu("Recoil/Global Background Music")]
public sealed class RecoilBackgroundMusic : MonoBehaviour
{
    const string MusicResource="Audio/RecoilBackgroundMusic";
    static RecoilBackgroundMusic instance;
    [Range(0,1)] public float volume=.35f;
    [Range(.1f,1)] public float minimumPlaybackSpeed=.7f;
    RecoilPlayerController player;
    AudioSource music;
    bool restorePending;
    float resumeTime,nextHealthCheck,nextDeviceReset;
    double previousDsp;
    int previousSample;

    void OnEnable(){AudioSettings.OnAudioConfigurationChanged+=AudioConfigurationChanged;}
    void OnDisable(){AudioSettings.OnAudioConfigurationChanged-=AudioConfigurationChanged;}
    void AudioConfigurationChanged(bool deviceWasChanged){restorePending=true;}

    public static void Bind(RecoilPlayerController currentPlayer)
    {
        if(!Application.isPlaying)return;
        if(!instance)
        {
            var root=new GameObject("Global Background Music");
            instance=root.AddComponent<RecoilBackgroundMusic>();
        }
        instance.player=currentPlayer;
    }
    void Awake()
    {
        if(instance && instance!=this){Destroy(gameObject);return;}
        instance=this;
        DontDestroyOnLoad(gameObject);
        music=GetComponent<AudioSource>();if(!music)music=gameObject.AddComponent<AudioSource>();
        music.playOnAwake=false;music.loop=true;music.spatialBlend=0;
        music.dopplerLevel=0;music.priority=128;music.volume=volume;
        music.clip=Resources.Load<AudioClip>(MusicResource);
        if(music.clip)music.Play();
        else Debug.LogWarning("Global background music clip is missing.",this);
        previousDsp=AudioSettings.dspTime;nextHealthCheck=Time.unscaledTime+2;
    }
    public static float PlaybackSpeed(float movementMultiplier,float minimumPitch=.7f)
    {
        float t=Mathf.InverseLerp(PlayerGrabPressure.MinimumSpeedMultiplier,1,movementMultiplier);
        return Mathf.Lerp(minimumPitch,1,t);
    }
    void LateUpdate()
    {
        if(!music)return;
        var grab=player?player.GrabPressure:null;
        // Use the allowed speed ratio, not measured velocity: standing still must not slow music.
        music.pitch=PlaybackSpeed(grab?grab.SpeedMultiplier:1,minimumPlaybackSpeed);
        music.volume=volume;
        if(!music.clip || AudioListener.pause)return;
        if(restorePending){restorePending=false;ResumeMusic();}
        if(Time.unscaledTime<nextHealthCheck)return;
        nextHealthCheck=Time.unscaledTime+2;
        double dsp=AudioSettings.dspTime;
        // FMOD can lose its output device after an Editor reload while isPlaying stays true.
        if(dsp<=previousDsp+.0001 && Time.unscaledTime>=nextDeviceReset)
        {
            nextDeviceReset=Time.unscaledTime+10;
            var configuration=AudioSettings.GetConfiguration();
            AudioSettings.Reset(configuration); // Heartbeat also recreates its PCM clip via the device callback.
            restorePending=true;
        }
        else if(!music.isPlaying || music.timeSamples==previousSample)ResumeMusic();
        if(music.time>0)resumeTime=music.time;
        previousSample=music.timeSamples;previousDsp=AudioSettings.dspTime;
    }
    void ResumeMusic()
    {
        if(!music || !music.clip)return;
        // A stream can still report Loaded after losing its FMOD buffer. Reopen the data too.
        music.Stop();music.clip.UnloadAudioData();music.clip.LoadAudioData();
        music.time=Mathf.Clamp(resumeTime,0,Mathf.Max(0,music.clip.length-.1f));music.Play();
        previousSample=music.timeSamples;previousDsp=AudioSettings.dspTime;
    }
    void OnDestroy(){if(instance==this)instance=null;}
}
