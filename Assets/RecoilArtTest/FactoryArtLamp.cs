using UnityEngine;

/// <summary>Visual emergency lamp, separate from gameplay sound-wave lighting.</summary>
public sealed class FactoryArtLamp : MonoBehaviour
{
    public Light source;
    public Renderer luminousSurface;
    [Min(0)] public float peakIntensity=2;
    [Min(.1f)] public float period=4;
    public float phase;
    MaterialPropertyBlock properties;
    void Awake(){properties=new MaterialPropertyBlock();}
    void Update()
    {
        float t=Mathf.Repeat(Time.time+phase,period)/period;
        float envelope=t<.22f?Mathf.Pow(Mathf.Sin(t/.22f*Mathf.PI),2):.025f;
        if(source)source.intensity=peakIntensity*envelope;
        if(luminousSurface){properties.SetColor("_EmissionColor",new Color(1,.035f,.01f)*(envelope*3+.1f));luminousSurface.SetPropertyBlock(properties);}
    }
}
