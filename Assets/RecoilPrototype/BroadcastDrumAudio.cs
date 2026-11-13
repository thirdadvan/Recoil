using UnityEngine;

// Presentation-only arrival cue. Never emits an AI sound stimulus or changes music pitch.
[DisallowMultipleComponent,AddComponentMenu("Recoil/Audio/Broadcast Arrival Drum")]
public sealed class BroadcastDrumAudio : MonoBehaviour
{
    [Range(0,1)] public float volume=.82f;
    public AudioClip drumClip;
    static AudioClip fallback;
    AudioSource source;
    RecoilPrototype world;
    bool paused;
    public void Play()
    {
        if(!source){source=gameObject.AddComponent<AudioSource>();source.playOnAwake=false;source.loop=false;source.spatialBlend=0;source.dopplerLevel=0;source.priority=24;}
        if(!world)world=GetComponentInParent<RecoilPrototype>();
        if(!fallback&&!drumClip)fallback=BuildClip();
        source.pitch=1;source.PlayOneShot(drumClip?drumClip:fallback,volume);
    }
    public void Stop(){if(source)source.Stop();paused=false;}
    void Update()
    {
        if(!source||!world)return;
        if(!world.Playing&&!paused){source.Pause();paused=true;}
        else if(world.Playing&&paused){source.UnPause();paused=false;}
    }
    public static AudioClip BuildClip()
    {
        const int rate=44100;const float length=2.8f;
        var dry=new float[(int)(rate*length)];var samples=new float[dry.Length];
        var noise=new System.Random(41582);float phase=0,filtered=0;
        for(int i=0;i<dry.Length;i++) {
            float t=i/(float)rate;
            phase+=2*Mathf.PI*(37+25*Mathf.Exp(-t*24))/rate;
            filtered=Mathf.Lerp(filtered,(float)noise.NextDouble()*2-1,.16f);
            float attack=1-Mathf.Exp(-t*380);
            float body=Mathf.Sin(phase)*Mathf.Exp(-t*5.5f)*.64f;
            float modes=(Mathf.Sin(phase*1.49f)*.22f+Mathf.Sin(phase*2.03f)*.12f)*Mathf.Exp(-t*7);
            float strike=filtered*Mathf.Exp(-t*70)*.27f;
            dry[i]=(body+modes+strike)*attack;
        }
        // Dense, quiet room reflections leave a distinct attack for a .4s double beat.
        var delays=new int[16];var gains=new float[16];
        for(int tap=1;tap<=16;tap++){delays[tap-1]=(int)(rate*(.071f*tap+.007f*(tap%3)));gains[tap-1]=.085f*Mathf.Exp(-tap*.14f)*(tap%2==0?1:-.65f);}
        for(int i=0;i<samples.Length;i++) {
            float value=dry[i];
            for(int tap=0;tap<delays.Length;tap++) {
                if(i>=delays[tap])value+=dry[i-delays[tap]]*gains[tap];
            }
            samples[i]=Mathf.Clamp(value,-.95f,.95f);
        }
        var clip=AudioClip.Create("Broadcast - deep resonant drum arrival",samples.Length,1,rate,false);clip.SetData(samples,0);return clip;
    }
    void OnDisable(){Stop();}
}
