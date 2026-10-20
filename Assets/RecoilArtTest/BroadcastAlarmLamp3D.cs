using UnityEngine;

// One physical fixture owns its lamp and luminous surface; no fullscreen red overlay.
[DisallowMultipleComponent,DefaultExecutionOrder(200)]
[AddComponentMenu("Recoil/Art Test/Broadcast Alarm Lamp 3D")]
public sealed class BroadcastAlarmLamp3D : MonoBehaviour
{
    public BroadcastRefractionDemo3D broadcastDemo;
    public Light source;
    public Renderer luminousSurface;
    [Min(0)] public float peakIntensity=5;
    [Min(.1f)] public float period=1.4f;
    [Min(.05f)] public float flashDuration=.45f;
    [Min(.01f)] public float fadeIn=.1f;
    [Range(0,1)] public float fadeOutStart=.4f;
    public float phaseOffset;
    [ColorUsage(false,true)] public Color emissionColor=new Color(4,.08f,.02f);
    MaterialPropertyBlock properties;
    bool wasActive;
    float activatedAt;
    public float Brightness {get;private set;}
    void Update()
    {
        if(!broadcastDemo || !broadcastDemo.effect){Apply(0);return;}
        Refresh(broadcastDemo.effect.Clock,broadcastDemo.RedAlertActive);
    }
    public void Refresh(float clock,bool active)
    {
        if(active && !wasActive)activatedAt=clock;
        wasActive=active;
        if(!active){Apply(0);return;}
        float cycle=Mathf.Max(.1f,period),duration=Mathf.Min(cycle,Mathf.Max(.05f,flashDuration));
        float t=Mathf.Repeat(clock-activatedAt+phaseOffset,cycle);
        float rise=Mathf.SmoothStep(0,1,t/Mathf.Clamp(fadeIn,.01f,duration*.5f));
        float fall=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(duration*Mathf.Clamp01(fadeOutStart),duration,t));
        Apply(t<duration?rise*fall:0);
    }
    void Apply(float envelope)
    {
        Brightness=envelope;
        if(source)source.intensity=peakIntensity*envelope;
        if(luminousSurface)
        {
            if(properties==null)properties=new MaterialPropertyBlock();
            luminousSurface.GetPropertyBlock(properties);properties.SetColor("_EmissionColor",emissionColor*envelope);luminousSurface.SetPropertyBlock(properties);
        }
    }
    void OnDisable(){wasActive=false;Apply(0);}
}
