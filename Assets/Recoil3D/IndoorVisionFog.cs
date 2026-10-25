using UnityEngine;

// Camera-only darkness. Does not change AI exposure, physics, lights or the underlying 3D render.
[ExecuteAlways,RequireComponent(typeof(Camera)),DisallowMultipleComponent]
[AddComponentMenu("Recoil/Effects/Indoor Vision Fog")]
public sealed class IndoorVisionFog : MonoBehaviour
{
    public Shader fogShader;
    public RecoilPrototype world;
    [Header("Player visibility")]
    public bool effectEnabled=true;
    [Min(.1f),Tooltip("玩家周围保持原画面亮度的半径，单位米。")]
    public float clearRadius=3.4f;
    [Min(.1f),Tooltip("可见区边缘逐渐变暗的宽度。")]
    public float featherWidth=2.2f;
    [Range(0,1),Tooltip("完全离开可见区时保留多少原画面亮度。")]
    public float distantBrightness=.055f;
    [Range(.1f,1),Tooltip("纵深方向对距离的影响，避免近处背景墙过早全黑。")]
    public float depthDistanceWeight=.55f;
    [Header("Environmental lights")]
    [Range(0,8),Tooltip("保留场景真实灯光照亮区域的程度，不包括玩家视觉灯。")]
    public float lightRevealStrength=2;
    [Min(.1f)] public float lightSelectionInterval=.25f;
    [Header("Preview")]
    public bool previewInEditMode=true;
    public bool showMaskOnly;
    public int SelectedLightCount {get;private set;}
    const int Capacity=12;
    readonly Light[] selected=new Light[Capacity];readonly float[] scores=new float[Capacity];
    readonly Vector4[] positions=new Vector4[Capacity],directions=new Vector4[Capacity],data=new Vector4[Capacity];
    Light[] candidates;float refreshAt,selectAt;int candidateScene;
    Light playerLight;Camera lens;Material material;
    void OnEnable()
    {
        lens=GetComponent<Camera>();lens.depthTextureMode|=DepthTextureMode.Depth;
        if(!world)world=GetComponentInParent<RecoilPrototype>();refreshAt=selectAt=-1;
    }
    void SelectLights()
    {
        float now=Time.realtimeSinceStartup;
        if(candidates==null||now>=refreshAt||candidateScene!=gameObject.scene.handle)
        {
            candidates=world.GetComponentsInChildren<Light>(true);candidateScene=gameObject.scene.handle;refreshAt=now+1;
            var presentation=world.GetComponent<Recoil3DWorld>();playerLight=presentation?presentation.playerVisualLight:null;
        }
        if(now<selectAt)return;selectAt=now+Mathf.Max(.1f,lightSelectionInterval);SelectedLightCount=0;
        Vector3 observer=world.Player.transform.position;
        foreach(var light in candidates)
        {
            if(!light||light==playerLight||!light.gameObject.activeInHierarchy||light.type==LightType.Directional||light.type==LightType.Area||light.range<=0)continue;
            float range=light.range;
            // Keep flickering fixtures eligible, but favour currently active lights when filling the budget.
            float score=range*range*Mathf.Max(.05f,light.intensity)/(1+(light.transform.position-observer).sqrMagnitude)*(light.enabled?1:.2f);
            int at=SelectedLightCount;while(at>0&&score>scores[at-1])at--;
            if(at>=Capacity)continue;
            int last=Mathf.Min(SelectedLightCount,Capacity-1);for(int i=last;i>at;i--){selected[i]=selected[i-1];scores[i]=scores[i-1];}
            selected[at]=light;scores[at]=score;SelectedLightCount=Mathf.Min(Capacity,SelectedLightCount+1);
        }
    }
    void OnRenderImage(RenderTexture source,RenderTexture destination)
    {
        if(!effectEnabled||!world||!world.Player||(!Application.isPlaying&&!previewInEditMode)||!fogShader||!fogShader.isSupported)
        {Graphics.Blit(source,destination);return;}
        if(!lens)lens=GetComponent<Camera>();
        if(!material)material=new Material(fogShader){hideFlags=HideFlags.HideAndDontSave};
        SelectLights();int count=0;
        for(int i=0;i<SelectedLightCount;i++)
        {
            var light=selected[i];if(!light||!light.enabled||!light.gameObject.activeInHierarchy||light.intensity<=0)continue;
            Vector3 p=light.transform.position,d=light.transform.forward;
            positions[count]=new Vector4(p.x,p.y,p.z,light.range);
            directions[count]=new Vector4(d.x,d.y,d.z,Mathf.Cos(light.spotAngle*.5f*Mathf.Deg2Rad));
            data[count]=new Vector4(light.intensity,light.type==LightType.Spot?1:0,0,0);count++;
        }
        var observer=world.Player.transform.position+Vector3.up*.2f;
        material.SetVector("_Observer",new Vector4(observer.x,observer.y,observer.z,depthDistanceWeight));
        material.SetVector("_Vision",new Vector4(Mathf.Max(.1f,clearRadius),Mathf.Max(.1f,featherWidth),distantBrightness,lightRevealStrength));
        material.SetMatrix("_InverseViewProjection",(GL.GetGPUProjectionMatrix(lens.projectionMatrix,false)*lens.worldToCameraMatrix).inverse);
        material.SetInt("_LightCount",count);material.SetVectorArray("_LightPosition",positions);material.SetVectorArray("_LightDirection",directions);material.SetVectorArray("_LightData",data);
        material.SetFloat("_PreviewMask",showMaskOnly?1:0);Graphics.Blit(source,destination,material);
    }
    void OnDisable()
    {
        if(material){if(Application.isPlaying)Destroy(material);else DestroyImmediate(material);}material=null;candidates=null;
    }
}
