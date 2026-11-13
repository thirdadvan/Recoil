using UnityEngine;
using UnityEngine.SceneManagement;

// Only two bounded Shuriken systems. No per-drop collision, lighting or scene searches.
[DisallowMultipleComponent,DefaultExecutionOrder(1500)]
public sealed class RecoilOutdoorRain : MonoBehaviour
{
    public Camera sceneCamera;
    public ParticleSystem rain,splashes;
    public bool rainEnabled=true,groundImpacts=true;
    [Range(0,2)] public float density=1;
    [Range(100,1000)] public int maximumDrops=1000;
    [Range(0,64)] public int maximumImpacts=64;
    [Min(0)] public float dropsPerSecond=650,impactsPerSecond=32;
    public Vector3 velocity=new Vector3(1.25f,-16.5f,.18f);
    public float groundY=.04f;
    [Tooltip("Simple solid roof footprints; rain below these roof tops is clipped by the material.")]
    public Bounds[] shelters=new Bounds[0];
    readonly Vector4[] roofMin=new Vector4[16],roofMax=new Vector4[16];
    MaterialPropertyBlock propertyBlock;
    Vector3 lastCentre;bool positioned;
    float previousWidth=-1,previousLifetime=-1;
    static readonly int MinId=Shader.PropertyToID("_ShelterMin"),MaxId=Shader.PropertyToID("_ShelterMax"),CountId=Shader.PropertyToID("_ShelterCount"),GroundId=Shader.PropertyToID("_GroundY");

    void OnEnable(){positioned=false;ApplyShelters();}
    void OnDisable(){StopAndClear();}
    public void ApplyShelters()
    {
        if(propertyBlock==null)propertyBlock=new MaterialPropertyBlock();
        int count=Mathf.Min(16,shelters==null?0:shelters.Length);
        for(int i=0;i<count;i++){var lo=shelters[i].min;var hi=shelters[i].max;roofMin[i]=new Vector4(lo.x,lo.y,lo.z,0);roofMax[i]=new Vector4(hi.x,hi.y,hi.z,0);}
        propertyBlock.Clear();propertyBlock.SetInt(CountId,count);propertyBlock.SetVectorArray(MinId,roofMin);propertyBlock.SetVectorArray(MaxId,roofMax);propertyBlock.SetFloat(GroundId,groundY-.015f);
        if(rain)rain.GetComponent<ParticleSystemRenderer>().SetPropertyBlock(propertyBlock);
        if(splashes)splashes.GetComponent<ParticleSystemRenderer>().SetPropertyBlock(propertyBlock);
    }
    static Vector3 OnPlane(Camera camera,float x,float y,float z)
    {var ray=camera.ViewportPointToRay(new Vector3(x,y,0));float distance=Mathf.Abs(ray.direction.z)>.001f?(z-ray.origin.z)/ray.direction.z:20;return ray.GetPoint(Mathf.Clamp(distance,1,100));}
    public void PositionForCamera(Camera camera)
    {
        if(!camera||!rain)return;
        var left=OnPlane(camera,0,.5f,4);var right=OnPlane(camera,1,.5f,4);var top=OnPlane(camera,.5f,1,4);var centre=OnPlane(camera,.5f,.5f,0);
        float width=Mathf.Clamp(Mathf.Abs(right.x-left.x)+5,16,72);
        float nearZ=Mathf.Max(camera.transform.position.z+4,-18),farZ=4.5f;
        float height=Mathf.Max(7,top.y+2.5f);
        var emitter=new Vector3(centre.x,height,(nearZ+farZ)*.5f);
        bool teleport=positioned&&Vector3.Distance(centre,lastCentre)>Mathf.Max(9,width*.45f);
        if(teleport){rain.Clear(true);if(splashes)splashes.Clear(true);}
        rain.transform.position=emitter;
        var shape=rain.shape;shape.scale=new Vector3(width,1.6f,Mathf.Max(4,farZ-nearZ));
        float lifetime=Mathf.Clamp((height-groundY)/Mathf.Max(3,-velocity.y)+.22f,.85f,2.8f);
        var main=rain.main;
        if(Mathf.Abs(previousLifetime-lifetime)>.04f){main.startLifetime=new ParticleSystem.MinMaxCurve(lifetime*.90f,lifetime);previousLifetime=lifetime;}
        main.maxParticles=Mathf.Clamp(maximumDrops,100,1000);
        var emission=rain.emission;emission.rateOverTime=Mathf.Min(Mathf.Max(0,dropsPerSecond*density),main.maxParticles*.90f/lifetime);
        var motion=rain.velocityOverLifetime;motion.x=velocity.x;motion.y=velocity.y;motion.z=velocity.z;
        if(splashes){splashes.transform.position=new Vector3(centre.x,groundY,-2.6f);var splashShape=splashes.shape;if(Mathf.Abs(previousWidth-width)>.1f)splashShape.scale=new Vector3(width,.001f,10.2f);var sm=splashes.main;sm.maxParticles=Mathf.Clamp(maximumImpacts,1,64);var se=splashes.emission;se.rateOverTime=groundImpacts&&maximumImpacts>0?Mathf.Min(impactsPerSecond*density,sm.maxParticles/.45f):0;}
        previousWidth=width;lastCentre=centre;positioned=true;
    }
    void LateUpdate()
    {
        if(!Application.isPlaying)return;
        if(!rainEnabled||density<=0||!sceneCamera||!sceneCamera.enabled||SceneManager.GetActiveScene()!=gameObject.scene){StopAndClear();return;}
        PositionForCamera(sceneCamera);
        if(!rain.isPlaying)rain.Play(true);
        if(splashes){if(groundImpacts&&maximumImpacts>0){if(!splashes.isPlaying)splashes.Play(true);}else if(splashes.isPlaying)splashes.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);}
    }
    public void Preview(Camera camera,float seconds=2.3f)
    {
        StopAndClear();ApplyShelters();PositionForCamera(camera);
        if(rain)rain.Simulate(seconds,true,true,true);
        if(splashes&&groundImpacts)splashes.Simulate(seconds,true,true,true);
    }
    public void StopAndClear()
    {if(rain&&(rain.isPlaying||rain.particleCount>0))rain.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);if(splashes&&(splashes.isPlaying||splashes.particleCount>0))splashes.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);}
}
