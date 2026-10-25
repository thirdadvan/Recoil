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
    public Vector2 skylightWidthDepth=new Vector2(3,2.6f);
    [Min(.1f)] public float skylightHeight=4.65f;
    [Range(0,.5f)] public float skylightSpread=.10f;
    [Range(.05f,1)] public float skylightFeather=.32f;
    [Range(0,.1f)] public float skylightDensity=.018f;
    public Color skylightColour=new Color(.46f,.60f,.68f);
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
    void OnEnable(){powerResolved=false;lens=GetComponent<Camera>();lens.depthTextureMode|=DepthTextureMode.Depth;}
    void Update(){if(showTestControls && Application.isPlaying && Input.GetKeyDown(KeyCode.F6))testLook=!testLook;}
    void LateUpdate()
    {
        if(!manageSceneLighting)return;
        RenderSettings.ambientLight=testLook?testAmbient:originalAmbient;
        RenderSettings.fogColor=testLook?testFog:originalFog;
        RenderSettings.fogStartDistance=testLook?testStart:originalStart;
        RenderSettings.fogEndDistance=testLook?testEnd:originalEnd;
        for(int i=0;i<originalLights.Length;i++)if(originalLights[i])originalLights[i].intensity=testLook?testIntensities[i]:originalIntensities[i];
        float supplyMultiplier=VolumeLightingPowered?1:.30f;
        foreach(var l in decorativeLights)if(l)
        {
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
        int count=0;
        for(int i=0;i<3;i++){nearest[i]=null;distances[i]=float.PositiveInfinity;}
        if(VolumeLightingPowered&&lamps!=null)foreach(var l in lamps)
        {
            if(!l||!l.enabled||!l.gameObject.activeInHierarchy||l.intensity<=0)continue;
            float d=(l.transform.position-lens.transform.position).sqrMagnitude;
            for(int i=0;i<3;i++)if(d<distances[i]){for(int j=2;j>i;j--){distances[j]=distances[j-1];nearest[j]=nearest[j-1];}distances[i]=d;nearest[i]=l;break;}
        }
        foreach(var l in nearest)
        {
            if(!l)continue;
            var p=l.transform.position;var d=l.transform.forward;
            pos[count]=new Vector4(p.x,p.y,p.z,l.range);
            dir[count]=new Vector4(d.x,d.y,d.z,Mathf.Cos(l.spotAngle*.5f*Mathf.Deg2Rad));
            colour[count]=(Vector4)(l.color*(l.intensity*scattering));count++;
        }
        material.SetVectorArray("_LampPos",pos);material.SetVectorArray("_LampDir",dir);material.SetVectorArray("_LampColour",colour);
        material.SetInt("_LampCount",count);
        bool sky=skylightOpening&&skylightOpening.gameObject.activeInHierarchy;
        material.SetFloat("_SkyEnabled",sky?1:0);
        if(sky)
        {
            material.SetVector("_SkyTop",skylightOpening.position);
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
    void OnGUI()
    {
        if(!Application.isPlaying||!showTestControls)return;
        GUI.Label(new Rect(18,100,550,26),"WAREHOUSE LIGHTING TEST   |   F6: "+(testLook?"TEST LOOK":"ORIGINAL LOOK"));
    }
    void OnDisable(){foreach(var pair in decorativeBaseIntensities)if(pair.Key)pair.Key.intensity=pair.Value;decorativeBaseIntensities.Clear();if(material){if(Application.isPlaying)Destroy(material);else DestroyImmediate(material);}}
}
