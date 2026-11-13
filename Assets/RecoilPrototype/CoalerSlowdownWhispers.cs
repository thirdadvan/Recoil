using UnityEngine;

// A personal audio layer driven by actual Coaler movement loss, independent of the music bus.
[DefaultExecutionOrder(1100),DisallowMultipleComponent,AddComponentMenu("Recoil/Audio/Coaler Slowdown Whispers")]
public sealed class CoalerSlowdownWhispers : MonoBehaviour
{
    public const float VolumeLimit=.50f;
    [Range(0,VolumeLimit)] public float maximumVolume=VolumeLimit;
    [Min(.02f)] public float fadeIn=.20f,fadeOut=1.5f;
    public AudioClip clip;
    AudioSource voice;RecoilPrototype world;RecoilPlayerController player;
    public AudioSource Voice {get{return voice;}}
    public float TargetVolume {get;private set;}
    public static float VolumeForSpeed(float speedMultiplier,float maximum=VolumeLimit)
    {return Mathf.Clamp(maximum,0,VolumeLimit)*Mathf.InverseLerp(1,PlayerGrabPressure.MinimumSpeedMultiplier,speedMultiplier);}
    public void Initialize(RecoilPrototype context,RecoilPlayerController actor)
    {
        world=context;player=actor;
        if(!clip)clip=Resources.Load<AudioClip>("Audio/CoalerWhispers");
        if(!voice){var child=new GameObject("Coaler slowdown whispers - personal audio");child.transform.SetParent(transform,false);voice=child.AddComponent<AudioSource>();}
        voice.playOnAwake=false;voice.loop=true;voice.spatialBlend=0;voice.dopplerLevel=0;voice.priority=80;voice.clip=clip;ResetAudio();
    }
    void LateUpdate()
    {
        if(!world||!player||!voice)return;
        if(player.Health<=0){ResetAudio();return;}
        if(AudioListener.pause||!world.Playing){if(voice.isPlaying)voice.Pause();return;}
        Advance(player.GrabPressure?player.GrabPressure.SpeedMultiplier:1,Time.deltaTime);
    }
    public void Advance(float speedMultiplier,float dt)
    {
        if(!voice||dt<=0)return;
        TargetVolume=VolumeForSpeed(speedMultiplier,maximumVolume);
        float duration=TargetVolume>voice.volume?fadeIn:fadeOut;
        voice.volume=Mathf.Clamp(Mathf.MoveTowards(voice.volume,TargetVolume,VolumeLimit*dt/Mathf.Max(.02f,duration)),0,VolumeLimit);
        if(voice.volume>.0001f&&voice.clip&&!voice.isPlaying){if(voice.time>0)voice.UnPause();else voice.Play();}
        if(TargetVolume==0&&voice.volume<=.0001f){voice.Stop();voice.volume=0;}
    }
    public void ResetAudio(){TargetVolume=0;if(voice){voice.Stop();voice.volume=0;}}
    void OnDisable(){ResetAudio();}
}
