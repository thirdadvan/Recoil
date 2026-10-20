using UnityEngine;

// World-space darkness with actor light, environmental point lights and directional lamps.
[ExecuteAlways, DefaultExecutionOrder(1000)]
public sealed class RecoilLighting2D : MonoBehaviour
{
    [SerializeField] Transform player;
    [SerializeField] Transform factoryArea;
    [SerializeField] SpriteRenderer darkness;
    [Range(0,1)] public float outdoorAmbient=.28f;
    [Range(0,1), Tooltip("遮罩中无灯区域保留的环境亮度比例。0 为全黑，1 为原亮度；不影响玩家受光系数，玩家周围、路灯和其他灯光仍可照亮画面。")]
    public float unlitAmbientScale=.2f;
    [Min(.1f)] public float playerLightRadius=5.4f;
    [Range(0,1)] public float playerLightIntensity=.48f;
    [Header("Swarm red flicker (visual and gameplay)")]
    [Range(0,1), Tooltip("声波阶段暗区红色闪光的最高亮度。")]
    public float swarmFlickerIntensity=.45f;
    [Min(.1f), Tooltip("一次亮起、熄灭的完整周期，单位秒。")]
    public float swarmFlickerPeriod=1.4f;
    [Min(.05f), Tooltip("每次警灯渐入渐出的亮起时长，单位秒。")]
    public float swarmFlashDuration=.45f;
    const int PointLightCapacity=32;
    readonly Vector4[] lights=new Vector4[PointLightCapacity];
    readonly Vector4[] industrialShapes=new Vector4[PointLightCapacity],industrialDirections=new Vector4[PointLightCapacity];
    readonly RecoilLight2D[] selectedLights=new RecoilLight2D[PointLightCapacity-1];
    readonly float[] selectedScores=new float[PointLightCapacity-1];
    int selectedCount,selectedFrame=-1,selectedRevision=-1;
    Camera viewCamera;
    readonly Vector4[] cones=new Vector4[16],coneDirections=new Vector4[16];
    Material runtimeMaterial;
    RecoilSightOcclusion sightShadows;
    bool alarmWasActive;
    float alarmStartedAt;
    RecoilPrototype world;
    public bool HasIndoorArea {get{return factoryArea && factoryArea.gameObject.activeInHierarchy;}}
    public float SwarmAmbientBrightness
    {
        get
        {
            if(!world)world=GetComponentInParent<RecoilPrototype>();
            bool alarm=Application.isPlaying && world && world.SoundWaves && world.SoundWaves.RedAlert;
            if(alarm && !alarmWasActive)alarmStartedAt=world.Clock;
            alarmWasActive=alarm;
            if(!alarm)return 0;
            float period=Mathf.Max(.1f,swarmFlickerPeriod),phase=Mathf.Repeat(world.Clock-alarmStartedAt,period);
            float duration=Mathf.Min(period,Mathf.Max(.05f,swarmFlashDuration));
            float pulse=Mathf.SmoothStep(0,1,Mathf.InverseLerp(0,duration*.25f,phase))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(duration*.35f,duration,phase)));
            return swarmFlickerIntensity*pulse;
        }
    }
    public void Configure(Transform actor,Transform factory,SpriteRenderer overlay)
    {player=actor;factoryArea=factory;darkness=overlay;Refresh();}
    void SelectLights(bool force=false)
    {
        if(!force && selectedFrame==Time.frameCount && selectedRevision==RecoilLight2D.Revision)return;
        selectedFrame=Time.frameCount;selectedRevision=RecoilLight2D.Revision;selectedCount=0;
        if(!viewCamera)viewCamera=Camera.main;
        Vector2 center=viewCamera?(Vector2)viewCamera.transform.position:player?(Vector2)player.position:Vector2.zero;
        foreach(var lamp in RecoilLight2D.Active)
        {
            if(!lamp || !lamp.isActiveAndEnabled || lamp.gameObject.scene!=gameObject.scene)continue;
            float score=((Vector2)lamp.transform.position-center).sqrMagnitude-lamp.InfluenceRadius*lamp.InfluenceRadius;
            int index=selectedCount;
            if(index==selectedLights.Length){if(score>=selectedScores[index-1])continue;index--;}
            else selectedCount++;
            while(index>0 && score<selectedScores[index-1])
            {selectedLights[index]=selectedLights[index-1];selectedScores[index]=selectedScores[index-1];index--;}
            selectedLights[index]=lamp;selectedScores[index]=score;
        }
    }
    public float EnvironmentBrightnessAt(Vector2 position)
    {bool inCone;return EnvironmentBrightnessAt(position,out inCone);}
    public float EnvironmentBrightnessAt(Vector2 position,out bool inCone)
    {return BrightnessAt(position,1,out inCone);}
    float BrightnessAt(Vector2 position,float ambientScale)
    {
        bool inCone;return BrightnessAt(position,ambientScale,out inCone);
    }
    float BrightnessAt(Vector2 position,float ambientScale,out bool inCone)
    {
        if(!world)world=GetComponentInParent<RecoilPrototype>();
        inCone=world && world.InStreetLampCone(position);
        if(inCone)return 1;
        Vector2 center=factoryArea?(Vector2)factoryArea.position:Vector2.zero,size=factoryArea?(Vector2)factoryArea.lossyScale:Vector2.zero;
        bool indoors=HasIndoorArea && Mathf.Abs(position.x-center.x)<=Mathf.Abs(size.x)/2 && Mathf.Abs(position.y-center.y)<=Mathf.Abs(size.y)/2;
        float brightness=indoors?SwarmAmbientBrightness:outdoorAmbient*ambientScale;
        SelectLights();
        for(int i=0;i<selectedCount;i++)
        {
            var lamp=selectedLights[i];
            brightness=Mathf.Max(brightness,lamp.BrightnessAt(position));
        }
        return brightness;
    }
    public float VisibilityAt(Vector2 position)
    {if(world && !world.Clear(player.position,position))return 0;return Mathf.Max(BrightnessAt(position,unlitAmbientScale),Falloff(position,player.position,playerLightRadius,playerLightIntensity));}
    static float Falloff(Vector2 point,Vector2 origin,float radius,float intensity)
    {return intensity*(1-Mathf.SmoothStep(0,1,Mathf.Clamp01(Vector2.Distance(point,origin)/radius)));}
    void LateUpdate(){Refresh();}
    public void Refresh()
    {
        if(!world)world=GetComponentInParent<RecoilPrototype>();
        if(!player || !darkness || !darkness.sharedMaterial)return;
        if(!sightShadows)sightShadows=GetComponent<RecoilSightOcclusion>();
        if(!sightShadows && Application.isPlaying)sightShadows=gameObject.AddComponent<RecoilSightOcclusion>();
        if(sightShadows)sightShadows.Configure(world,player,darkness);
        Material material=darkness.sharedMaterial;
        if(Application.isPlaying)
        {
            if(!runtimeMaterial){runtimeMaterial=new Material(material);darkness.material=runtimeMaterial;}
            material=runtimeMaterial;
        }
        for(int i=0;i<lights.Length;i++){lights[i]=Vector4.zero;industrialShapes[i]=Vector4.zero;industrialDirections[i]=Vector4.zero;}
        SelectLights(true);
        lights[0]=new Vector4(player.position.x,player.position.y,playerLightRadius,playerLightIntensity);
        int slot=1;
        for(int i=0;i<selectedCount;i++)
        {var lamp=selectedLights[i];lights[slot]=new Vector4(lamp.transform.position.x,lamp.transform.position.y,lamp.radius,lamp.intensity);
            var industrial=lamp as RecoilIndustrialLamp;
            if(industrial){industrialShapes[slot]=new Vector4(industrial.coneLength,industrial.coneHalfWidth,industrial.edgeSoftness,industrial.scatterIntensity);Vector2 dir=industrial.Direction;industrialDirections[slot]=new Vector4(dir.x,dir.y,0,0);}slot++;}
        for(int i=0;i<cones.Length;i++){cones[i]=Vector4.zero;coneDirections[i]=Vector4.zero;}
        int coneSlot=0;
        foreach(var lamp in RecoilStreetLamp.Active)if(lamp.IsOn && world && lamp.BelongsTo(world) && coneSlot<cones.Length)
        {Vector2 origin=lamp.Origin,dir=lamp.Direction;cones[coneSlot]=new Vector4(origin.x,origin.y,lamp.coneLength,lamp.coneHalfWidth);coneDirections[coneSlot++]=new Vector4(dir.x,dir.y,0,0);}
        material.SetVectorArray("_ConeLights",cones);material.SetVectorArray("_ConeDirections",coneDirections);material.SetInt("_ConeCount",coneSlot);
        Vector2 center=factoryArea?(Vector2)factoryArea.position:Vector2.zero,size=factoryArea?(Vector2)factoryArea.lossyScale:Vector2.zero;
        size=new Vector2(Mathf.Abs(size.x),Mathf.Abs(size.y));
        material.SetFloat("_FactoryEnabled",HasIndoorArea?1:0);
        material.SetVector("_FactoryMin",center-size/2);material.SetVector("_FactoryMax",center+size/2);
        material.SetFloat("_AlarmAmbient",SwarmAmbientBrightness);
        material.SetFloat("_AlarmRed",alarmWasActive?1:0);
        material.SetFloat("_OutdoorAmbient",outdoorAmbient);material.SetFloat("_UnlitAmbientScale",unlitAmbientScale);material.SetVectorArray("_IndustrialLightShapes",industrialShapes);material.SetVectorArray("_IndustrialLightDirections",industrialDirections);material.SetVectorArray("_PointLights",lights);material.SetInt("_PointCount",slot);
    }
    void OnDrawGizmosSelected()
    {
        if(!HasIndoorArea)return;
        Gizmos.color=new Color(.2f,.5f,1,.7f);Gizmos.DrawWireCube(factoryArea.position,factoryArea.lossyScale);
    }
    void OnDestroy(){if(runtimeMaterial)Destroy(runtimeMaterial);}
}
