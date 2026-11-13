using UnityEngine;
using System.Collections.Generic;

// Presentation-only experiment: never changes gameplay lights, exposure or AI.
[ExecuteAlways,DefaultExecutionOrder(1300),RequireComponent(typeof(Camera))]
public sealed class WarehouseAtmosphereTest : MonoBehaviour
{
    public Shader effectShader;
    public bool testLook=true;
    [Tooltip("室外仅使用体积光，保留场景本身的环境光和雾设置。") ]
    public bool manageSceneLighting=true;
    public bool showTestControls=true;
    public Light[] lamps;
    public Light[] originalLights;
    public float[] originalIntensities,testIntensities;
    public Color originalAmbient,testAmbient=new Color(.018f,.023f,.027f);
    public Color originalFog,testFog=new Color(.023f,.032f,.037f);
    public float originalStart,originalEnd,testStart=22,testEnd=68;
    public Light[] decorativeLights;
    public GameObject contactShadows;
    public bool useAuthoredPathLighting;
    public bool originalVisionFogEnabled=true;
    [Range(0,1)] public float scattering=.16f;
    [Range(0,1)] public float softGlow=.12f;
    [Range(2,4)] public int volumeResolutionDivider=2;
    [Range(8,32)] public int volumeSteps=24;
    [Header("Rectangular skylight — independent of powered pendant lights")]
    public Transform skylightOpening;
    [Tooltip("Additional fixed roof apertures; share the primary aperture's dimensions and daylight profile.")]
    public Transform[] additionalSkylightOpenings=new Transform[0];
    readonly Vector4[] skyTops=new Vector4[4];
    public Vector2 skylightWidthDepth=new Vector2(3,2.6f);
    [Min(.1f)] public float skylightHeight=4.65f;
    [Range(0,.5f)] public float skylightSpread=.10f;
    [Range(.05f,1)] public float skylightFeather=.32f;
    [Range(0,.1f)] public float skylightDensity=.018f;
    public Color skylightColour=new Color(.46f,.60f,.68f);
    readonly Vector4[] doorOrigin=new Vector4[2],doorShape=new Vector4[2],doorColour=new Vector4[2];
    readonly Vector4[] doorLightPosition=new Vector4[2],doorLightShape=new Vector4[2];
    readonly Matrix4x4[] doorWorldToLight=new Matrix4x4[2];
    DoorProximityGlow[] doorVolumes;
    void SetDoorVolumes(){
        if(doorVolumes==null||!Application.isPlaying){var list=new List<DoorProximityGlow>();foreach(var root in gameObject.scene.GetRootGameObjects())list.AddRange(root.GetComponentsInChildren<DoorProximityGlow>(true));doorVolumes=list.ToArray();}
        DoorProximityGlow first=null,second=null;float a=float.PositiveInfinity,b=float.PositiveInfinity;
        foreach(var g in doorVolumes){if(!g||g.MeshBeamActive||g.VolumeDensity<=0||!g.gameObject.activeInHierarchy)continue;var light=g.VolumeLight;if(!light||!light.enabled||!light.gameObject.activeInHierarchy||light.type!=LightType.Spot||light.intensity<=0)continue;float distance=(g.volumeOrigin-lens.transform.position).sqrMagnitude;if(distance<a){second=first;b=a;first=g;a=distance;}else if(distance<b){second=g;b=distance;}}
        int count=0;for(int i=0;i<2;i++)
        {
            var g=i==0?first:second;if(!g)continue;var light=g.VolumeLight;
            Vector3 opening;Vector2 halfSize;g.GetVolumeAperture(out opening,out halfSize);
            doorOrigin[count]=new Vector4(opening.x,opening.y,opening.z,g.volumeLength);
            doorShape[count]=new Vector4(halfSize.x,halfSize.y,0,g.VolumeDensity);
            doorColour[count]=light.color;
            var p=light.transform.position;
            doorLightPosition[count]=new Vector4(p.x,p.y,p.z,light.range);
            float halfAngle=light.spotAngle*.5f*Mathf.Deg2Rad;
            doorLightShape[count]=new Vector4(Mathf.Tan(halfAngle),Mathf.Cos(halfAngle),Mathf.Cos(light.innerSpotAngle*.5f*Mathf.Deg2Rad),light.intensity);
            // Light projection ignores parent scaling, just as the real spot light does.
            doorWorldToLight[count]=Matrix4x4.TRS(p,light.transform.rotation,Vector3.one).inverse;
            material.SetTexture(count==0?"_DoorCookie0":"_DoorCookie1",light.cookie?light.cookie:Texture2D.whiteTexture);
            count++;
        }
        material.SetInt("_DoorCount",count);material.SetVectorArray("_DoorOrigin",doorOrigin);material.SetVectorArray("_DoorShape",doorShape);material.SetVectorArray("_DoorColour",doorColour);
        material.SetVectorArray("_DoorLightPosition",doorLightPosition);material.SetVectorArray("_DoorLightShape",doorLightShape);material.SetMatrixArray("_DoorWorldToLight",doorWorldToLight);
    }
    Material material;Camera lens;
    WarehouseLightingPower warehousePower;bool powerResolved;
    readonly Dictionary<Light,float> decorativeBaseIntensities=new Dictionary<Light,float>();
    public bool VolumeLightingPowered
    {
        get
        {
            if(!powerResolved)
            {
                warehousePower=GetComponentInParent<WarehouseLightingPower>();
                if(!warehousePower&&gameObject.scene.IsValid())foreach(var root in gameObject.scene.GetRootGameObjects())
                {warehousePower=root.GetComponentInChildren<WarehouseLightingPower>(true);if(warehousePower)break;}
                powerResolved=true;
            }
            return !warehousePower||warehousePower.LightingPowered;
        }
    }
    readonly Light[] nearest=new Light[3];
    readonly float[] distances=new float[3];
    readonly Vector4[] pos=new Vector4[3],dir=new Vector4[3],colour=new Vector4[3];
    readonly Vector4[] volumeProfiles=new Vector4[3],shelterExtents=new Vector4[6];
    readonly Matrix4x4[] shelterTransforms=new Matrix4x4[6];
    readonly Dictionary<Light,VolumetricLightProfile> fixtureProfiles=new Dictionary<Light,VolumetricLightProfile>();
    void OnEnable(){doorVolumes=null;powerResolved=false;lens=GetComponent<Camera>();lens.depthTextureMode|=DepthTextureMode.Depth;}
    void Update(){if(showTestControls && Application.isPlaying && Input.GetKeyDown(KeyCode.F6))testLook=!testLook;}
    void LateUpdate()
    {
        if(!manageSceneLighting)return;
        RenderSettings.ambientLight=testLook?testAmbient:originalAmbient;
        RenderSettings.fogColor=testLook?testFog:originalFog;
        RenderSettings.fogStartDistance=testLook?testStart:originalStart;
        RenderSettings.fogEndDistance=testLook?testEnd:originalEnd;
        for(int i=0;i<originalLights.Length;i++)if(originalLights[i]&&!originalLights[i].GetComponentInParent<FaultyFluorescentFixture>())originalLights[i].intensity=testLook?testIntensities[i]:originalIntensities[i];
        float supplyMultiplier=VolumeLightingPowered?1:.30f;
        foreach(var l in decorativeLights)if(l)
        {
            // Animated fixtures own their surface illumination; never restore a cached steady intensity.
            if(l.GetComponentInParent<FaultyFluorescentFixture>())continue;
            if(!decorativeBaseIntensities.ContainsKey(l))decorativeBaseIntensities[l]=l.intensity;
            l.enabled=testLook;l.intensity=decorativeBaseIntensities[l]*supplyMultiplier;
        }
        if(useAuthoredPathLighting)
        {
            var vision=GetComponent<IndoorVisionFog>();
            if(vision)vision.effectEnabled=testLook?false:originalVisionFogEnabled;
        }
        if(contactShadows && contactShadows.activeSelf!=testLook)contactShadows.SetActive(testLook);
    }
    public void ApplyLook(){LateUpdate();}
    void OnRenderImage(RenderTexture source,RenderTexture destination)
    {
        if(!testLook||!effectShader||!effectShader.isSupported){Graphics.Blit(source,destination);return;}
        if(!material)material=new Material(effectShader){hideFlags=HideFlags.HideAndDontSave};
        SetDoorVolumes();
        int count=0;
        for(int i=0;i<3;i++){nearest[i]=null;distances[i]=float.PositiveInfinity;}
        if(VolumeLightingPowered&&lamps!=null)foreach(var l in lamps)
        {
            if(!l||!l.enabled||!l.gameObject.activeInHierarchy||l.intensity<=0)continue;
            float d=(l.transform.position-lens.transform.position).sqrMagnitude;
            for(int i=0;i<3;i++)if(d<distances[i]){for(int j=2;j>i;j--){distances[j]=distances[j-1];nearest[j]=nearest[j-1];}distances[i]=d;nearest[i]=l;break;}
        }
        for(int i=0;i<6;i++){shelterExtents[i]=Vector4.zero;shelterTransforms[i]=Matrix4x4.identity;}
        foreach(var l in nearest)
        {
            if(!l)continue;
            var p=l.transform.position;var d=l.transform.forward;
            pos[count]=new Vector4(p.x,p.y,p.z,l.range);
            dir[count]=new Vector4(d.x,d.y,d.z,Mathf.Cos(l.spotAngle*.5f*Mathf.Deg2Rad));
            colour[count]=(Vector4)(l.color*(l.intensity*scattering));
            VolumetricLightProfile profile;
            if(!fixtureProfiles.TryGetValue(l,out profile)||!Application.isPlaying)
            {profile=l.GetComponent<VolumetricLightProfile>();fixtureProfiles[l]=profile;}
            volumeProfiles[count]=profile&&profile.enabled?new Vector4(profile.scatteringMultiplier,profile.maximumVisibleHeight,Mathf.Max(.05f,profile.heightFeather),profile.fadeAboveHeight?1:0):new Vector4(1,0,1,0);
            if(profile&&profile.enabled&&profile.shelters!=null)for(int i=0;i<Mathf.Min(2,profile.shelters.Length);i++)
            {
                var shelter=profile.shelters[i];if(!shelter||!shelter.enabled||!shelter.gameObject.activeInHierarchy)continue;
                int slot=count*2+i;var half=shelter.size*.5f;
                shelterTransforms[slot]=Matrix4x4.Translate(-shelter.center)*shelter.transform.worldToLocalMatrix;
                shelterExtents[slot]=new Vector4(half.x,half.y,half.z,1);
            }
            count++;
        }
        material.SetVectorArray("_LampPos",pos);material.SetVectorArray("_LampDir",dir);material.SetVectorArray("_LampColour",colour);
        material.SetVectorArray("_LampVolumeProfile",volumeProfiles);
        material.SetMatrixArray("_VolumeShelterWorldToLocal",shelterTransforms);
        material.SetVectorArray("_VolumeShelterHalfSize",shelterExtents);
        material.SetInt("_LampCount",count);
        int skyCount=0;
        if(skylightOpening&&skylightOpening.gameObject.activeInHierarchy)skyTops[skyCount++]=skylightOpening.position;
        if(additionalSkylightOpenings!=null)foreach(var opening in additionalSkylightOpenings)
            if(opening&&opening!=skylightOpening&&opening.gameObject.activeInHierarchy&&skyCount<skyTops.Length)skyTops[skyCount++]=opening.position;
        bool sky=skyCount>0;
        material.SetInt("_SkyCount",skyCount);material.SetVectorArray("_SkyTops",skyTops);
        material.SetFloat("_SkyEnabled",sky?1:0);
        if(sky)
        {
            material.SetVector("_SkyTop",skyTops[0]);
            material.SetVector("_SkyShape",new Vector4(skylightWidthDepth.x*.5f,skylightWidthDepth.y*.5f,skylightHeight,skylightSpread));
            material.SetVector("_SkyColour",(Vector4)(skylightColour*skylightDensity));
            material.SetFloat("_SkyFeather",skylightFeather);
        }
        // Viewport rays stay in camera-image coordinates. Do not mix an RT-flipped GPU
        // projection with a separately flipped depth UV (Metal inverted the cone).
        material.SetVector("_Ray00",lens.ViewportToWorldPoint(new Vector3(0,0,1))-lens.transform.position);
        material.SetVector("_Ray10",lens.ViewportToWorldPoint(new Vector3(1,0,1))-lens.transform.position);
        material.SetVector("_Ray01",lens.ViewportToWorldPoint(new Vector3(0,1,1))-lens.transform.position);
        material.SetVector("_Ray11",lens.ViewportToWorldPoint(new Vector3(1,1,1))-lens.transform.position);
        material.SetVector("_CameraPosition",lens.transform.position);
        material.SetFloat("_Glow",softGlow);
        material.SetInt("_VolumeSteps",volumeSteps);
        var tight=RenderTexture.GetTemporary(Mathf.Max(1,source.width/volumeResolutionDivider),Mathf.Max(1,source.height/volumeResolutionDivider),0,source.format);
        var broad=RenderTexture.GetTemporary(Mathf.Max(1,source.width/4),Mathf.Max(1,source.height/4),0,source.format);
        var temp=RenderTexture.GetTemporary(broad.width,broad.height,0,source.format);
        try{
            Graphics.Blit(source,tight,material,0);
            Graphics.Blit(tight,broad,material,1);
            Graphics.Blit(broad,temp,material,1);
            Graphics.Blit(temp,broad,material,1);
            material.SetTexture("_Scattering",tight);material.SetTexture("_BroadGlow",broad);
            Graphics.Blit(source,destination,material,2);
        }finally{RenderTexture.ReleaseTemporary(tight);RenderTexture.ReleaseTemporary(broad);RenderTexture.ReleaseTemporary(temp);}
    }
    void OnDisable(){foreach(var pair in decorativeBaseIntensities)if(pair.Key)pair.Key.intensity=pair.Value;decorativeBaseIntensities.Clear();fixtureProfiles.Clear();if(material){if(Application.isPlaying)Destroy(material);else DestroyImmediate(material);}}
}
