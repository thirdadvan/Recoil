using UnityEngine;

// A short synthesized growl shared by all Coalers; no sound-wave stimulus is emitted by playback.
[DisallowMultipleComponent,AddComponentMenu("Recoil/Coaler Roar Audio")]
public sealed class CoalerRoarAudio : MonoBehaviour
{
    [Range(0,1)] public float volume=.6f;
    static AudioClip clip;
    AudioSource source;RampagerController owner;int playedSequence;
    void Update()
    {
        if(!owner)owner=GetComponent<RampagerController>();
        if(!owner)return;
        if(!owner.SearchingForPlayer){if(source && source.isPlaying)source.Stop();playedSequence=owner.RoarSequence;return;}
        if(owner.RoarSequence==playedSequence)return;
        playedSequence=owner.RoarSequence;
        if(!source){source=gameObject.AddComponent<AudioSource>();source.playOnAwake=false;source.spatialBlend=1;source.minDistance=4;source.maxDistance=35;source.dopplerLevel=0;source.rolloffMode=AudioRolloffMode.Logarithmic;}
        if(!clip)clip=BuildClip();source.PlayOneShot(clip,volume);
    }
    static AudioClip BuildClip()
    {
        const int rate=22050;int count=(int)(rate*.9f);var samples=new float[count];var random=new System.Random(2707);float low=0;
        for(int i=0;i<count;i++)
        {
            float t=i/(float)rate,p=t/.9f,envelope=Mathf.Pow(Mathf.Sin(p*Mathf.PI),.7f);
            low=Mathf.Lerp(low,(float)random.NextDouble()*2-1,.13f);
            float rasp=Mathf.Sin(t*2*Mathf.PI*(58+12*Mathf.Sin(t*9)))+.38f*Mathf.Sin(t*2*Mathf.PI*117);
            samples[i]=Mathf.Clamp((rasp*.24f+low*.55f)*envelope,-.8f,.8f);
        }
        var value=AudioClip.Create("Coaler - frustrated search growl",count,1,rate,false);value.SetData(samples,0);return value;
    }
    void OnDisable(){if(source)source.Stop();}
}
