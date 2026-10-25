using UnityEngine;

[RequireComponent(typeof(KBKStackerCrane),typeof(FreightNoiseEmitter))]
public sealed class FreightMotorAudio : MonoBehaviour
{
    [Range(0,1)] public float volume=.48f;
    public AudioClip motorClip;
    KBKStackerCrane crane;FreightNoiseEmitter emitter;RecoilPrototype world;AudioSource source;static AudioClip fallback;
    void Awake(){crane=GetComponent<KBKStackerCrane>();emitter=GetComponent<FreightNoiseEmitter>();world=GetComponentInParent<RecoilPrototype>();}
    void Update()
    {
        bool running=world&&world.Playing&&crane.HasPower&&crane.Busy;
        if(!source&&!running)return;
        if(!source)
        {
            var g=new GameObject("Spatial motor sound - follows active drive");g.transform.SetParent(transform,false);source=g.AddComponent<AudioSource>();
            source.playOnAwake=false;source.loop=true;source.spatialBlend=1;source.dopplerLevel=0;source.minDistance=3;source.maxDistance=35;source.volume=0;
            source.clip=motorClip?motorClip:Loop();
        }
        source.transform.position=emitter.ActiveOrigin.position;
        source.maxDistance=35*emitter.soundRadiusMultiplier;
        source.pitch=crane.Phase==KBKStackerCrane.MotionPhase.Travelling?.88f:crane.Phase==KBKStackerCrane.MotionPhase.Lowering?.95f:1.06f;
        if(running&&!source.isPlaying)source.Play();
        source.volume=Mathf.MoveTowards(source.volume,running?volume:0,Time.deltaTime*.8f);
        if(!running&&source.volume<=.001f)source.Stop();
    }
    static AudioClip Loop()
    {
        if(fallback)return fallback;const int rate=22050,count=rate*2;var samples=new float[count];var random=new System.Random(48);float filtered=0;
        for(int i=0;i<count;i++)
        {
            float t=(float)i/rate;filtered=Mathf.Lerp(filtered,(float)random.NextDouble()*2-1,.035f);
            float hum=Mathf.Sin(t*Mathf.PI*2*50)*.25f+Mathf.Sin(t*Mathf.PI*2*100)*.12f;
            float chain=Mathf.Sin(t*Mathf.PI*2*340)*(.035f+.035f*Mathf.Sin(t*Mathf.PI*2*8));samples[i]=hum+chain+filtered*.3f;
        }
        fallback=AudioClip.Create("Freight motor and chain - temporary synthesized loop",count,1,rate,false);fallback.SetData(samples,0);return fallback;
    }
    void OnDisable(){if(source)source.Stop();}
}
