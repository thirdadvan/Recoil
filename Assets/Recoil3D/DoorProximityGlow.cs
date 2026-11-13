using UnityEngine;

// A scene-anchored rectangular light volume. Gameplay light and interaction stay on the door.
[ExecuteAlways, DefaultExecutionOrder(1800), DisallowMultipleComponent]
[AddComponentMenu("Recoil/Effects/Door Proximity Glow")]
public sealed class DoorProximityGlow : MonoBehaviour
{
    public RecoilDoor door;
    public RecoilPrototype world;
    [HideInInspector] public Light[] doorwayLights = new Light[0];
    [HideInInspector] public Renderer halo;
    [HideInInspector] public float brightnessMultiplier = 1;
    [HideInInspector] public Renderer[] reflectiveSurfaces = new Renderer[0];
    [HideInInspector] public Renderer[] surfaceSources = new Renderer[0];
    public bool volumeEnabled = true;
    [Header("3D trapezoid beam")]
    public bool useTrapezoidMesh = true;
    [Tooltip("向前景延伸光束，使起始截面位于游戏镜头后方。不会移动实际门洞或改变室内梯形。") ]
    [Min(0)] public float foregroundExtension = 32f;
    [Range(.02f,.5f)] public float beamEdgeFeather = .18f;
    [Min(.02f)] public float beamEndFeather = .3f;
    [Min(0)] public float beamBrightness = 1.8f;
    [Tooltip("柔光使用这盏门口灯的颜色和投影遮罩，从门洞向室内展开。留空时从本门的灯具查找。")]
    public Light volumeLight;
    public Vector3 volumeOrigin;
    public Vector2 apertureHalfSize = new Vector2(.65f,1.15f);
    [Min(.1f)] public float volumeLength=3.5f;
    [Range(0,.4f)] public float volumeSpread=.12f;
    [Range(0,.1f)] public float restingDensity=.006f, activeDensity=.026f;
    public float VolumeDensity {get{return isActiveAndEnabled&&volumeEnabled?Mathf.Lerp(restingDensity,activeDensity,Blend):0;}}
    [Min(.05f)] public float fadeIn = .22f, fadeOut = .4f;
    [Range(0,1)] public float haloStrength = .3f;
    public Color haloTint = new Color(.48f,.65f,.70f);
    [Range(0,1)] public float editorPreview;
    public float Blend { get; private set; }
    MaterialPropertyBlock properties;
    GameObject beamObject;
    Mesh beamMesh;
    MeshRenderer beamRenderer;
    Material beamMaterial;
    Camera beamCamera;
    WarehouseAtmosphereTest beamLook;
    readonly Vector3[] beamVertices = new Vector3[12];
    static readonly int[] BeamTriangles = BuildBeamTriangles();
    static int[] BuildBeamTriangles()
    {
        var ts=new System.Collections.Generic.List<int>{0,1,2,0,2,3,8,10,9,8,11,10};
        for(int ring=0;ring<2;ring++)for(int j=0;j<4;j++){
            int a=ring*4+j,b=ring*4+(j+1)%4,c=a+4,d=b+4;
            ts.AddRange(new[]{a,c,d,a,d,b});
        }
        return ts.ToArray();
    }
    float previousForegroundLength;

    Vector3 previousOrigin;
    Vector2 previousNear,previousFar,previousOffset;
    float previousLength;
    Matrix4x4 previousTransform;
    bool meshBaked;
    public bool MeshBeamActive {get{return beamRenderer&&beamRenderer.enabled&&useTrapezoidMesh;}}
    Light resolvedVolumeLight;
    bool searchedVolumeLight;
    Renderer apertureLeft,apertureRight,apertureHeader;
    bool searchedAperture;
    public void GetVolumeAperture(out Vector3 origin,out Vector2 halfSize)
    {
        origin=volumeOrigin;halfSize=apertureHalfSize;
        if(!searchedAperture)
        {
            searchedAperture=true;
            foreach(var r in GetComponentsInChildren<Renderer>(true))
            {
                if(r.name=="Unseen front wall - left of doorway")apertureLeft=r;
                else if(r.name=="Unseen front wall - right of doorway")apertureRight=r;
                else if(r.name=="Unseen front wall - above doorway")apertureHeader=r;
            }
        }
        if(!apertureLeft||!apertureRight||!apertureHeader)return;
        var left=apertureLeft.bounds;var right=apertureRight.bounds;var header=apertureHeader.bounds;
        float width=right.min.x-left.max.x;
        float bottom=Mathf.Min(left.min.y,right.min.y),height=header.min.y-bottom;
        if(width<=0||height<=0)return;
        // Use the inner face of the same shadow-only wall that clips the real light.
        // Shadow-only casters may be compressed vertically; use the authored door opening.
        halfSize=apertureHalfSize;
        origin=new Vector3((left.max.x+right.min.x)*.5f,volumeOrigin.y,Mathf.Max(left.max.z,right.max.z));
    }
    public Light VolumeLight
    {
        get
        {
            if(volumeLight)return volumeLight;
            if(resolvedVolumeLight)return resolvedVolumeLight;
            if(searchedVolumeLight)return null;
            searchedVolumeLight=true;Resolve();
            foreach(var light in GetComponentsInChildren<Light>(true))
                if(light.type==LightType.Spot){resolvedVolumeLight=light;return light;}
            foreach(var light in doorwayLights)
                if(light&&light.type==LightType.Spot){resolvedVolumeLight=light;return light;}
            // Some entrances keep their exterior fixture beside the door hierarchy.
            float closest=float.PositiveInfinity;
            foreach(var root in gameObject.scene.GetRootGameObjects())
            foreach(var light in root.GetComponentsInChildren<Light>(true))
            {
                if(light.type!=LightType.Spot)continue;
                string name=light.name.ToLowerInvariant();
                if(!name.Contains("doorway")&&!name.Contains("entrance"))continue;
                Vector3 delta=volumeOrigin-light.transform.position;
                float distance=delta.sqrMagnitude;
                if(distance>=closest||distance>light.range*light.range||Vector3.Dot(delta,light.transform.forward)<=0)continue;
                closest=distance;resolvedVolumeLight=light;
            }
            return resolvedVolumeLight;
        }
    }
    void OnEnable(){searchedVolumeLight=false;resolvedVolumeLight=null;searchedAperture=false;Camera.onPreCull+=PrepareCameraDepth;}
    void OnValidate(){searchedVolumeLight=false;resolvedVolumeLight=null;searchedAperture=false;}
    void Resolve(){if(!door)door=GetComponent<RecoilDoor>();if(!world)world=GetComponentInParent<RecoilPrototype>();}
    public bool PlayerInActivationRange(){Resolve();return world&&world.Playing&&door&&door.InRange(world)&&world.NearbyDoor==door;}
    public void ApplyBlend(float amount)
    {
        Resolve();Blend=Mathf.Clamp01(amount);
        UpdateBeam();
    }
    // The doorway is the smallest section. Expand into the room above the floor.
    // Camera position is deliberately absent: only perspective changes as the camera moves.
    public bool GetTrapezoid(out Vector3 origin,out Vector2 nearHalf,out Vector2 farHalf,out Vector2 farOffset,out float length)
    {
        GetVolumeAperture(out origin,out nearHalf);farHalf=nearHalf;farOffset=Vector2.zero;length=0;
        var light=VolumeLight;if(!light||light.type!=LightType.Spot)return false;
        Vector3 delta=origin-light.transform.position;
        if(delta.z<=.05f||delta.sqrMagnitude<.001f||Vector3.Dot(delta,light.transform.forward)<=0)return false;
        float distance=delta.magnitude;
        float travel=Mathf.Min(Mathf.Max(.1f,volumeLength),Mathf.Max(0,light.range-distance));
        length=travel*delta.z/distance;if(length<=.01f)return false;
        float scale=(delta.z+length)/delta.z;
        farHalf=nearHalf*scale;
        // Keep the lower edge at the doorway floor, rather than burying the wide end.
        farOffset=new Vector2(delta.x*(scale-1),farHalf.y-nearHalf.y);
        return nearHalf.x>0&&nearHalf.y>0;
    }
    void PrepareCameraDepth(Camera camera)
    {
        if(MeshBeamActive&&(camera.gameObject.scene==gameObject.scene||camera.cameraType==CameraType.SceneView))
            camera.depthTextureMode|=DepthTextureMode.Depth;
    }
    void UpdateBeam()
    {
        var light=VolumeLight;
        bool active=useTrapezoidMesh&&VolumeDensity>0&&light&&light.enabled&&light.gameObject.activeInHierarchy&&light.intensity>0;
        if(active&&!beamCamera)
        {
            beamCamera=world?world.ViewCamera:null;
            if(!beamCamera&&world)beamCamera=world.GetComponentInChildren<Camera>();
            if(beamCamera)beamLook=beamCamera.GetComponent<WarehouseAtmosphereTest>();
        }
        if(beamLook&&!beamLook.testLook)active=false;
        Vector3 origin;Vector2 nearHalf,farHalf,offset;float length;
        if(!active||!GetTrapezoid(out origin,out nearHalf,out farHalf,out offset,out length))
        {if(beamRenderer)beamRenderer.enabled=false;return;}
        if(!beamMaterial)
        {
            var shader=Shader.Find("Recoil/3D/Trapezoid Door Beam");if(!shader||!shader.isSupported)return;
            beamMaterial=new Material(shader){name="Door trapezoid beam",hideFlags=HideFlags.HideAndDontSave};
        }
        if(!beamObject)
        {
            beamObject=new GameObject("Door beam - 3D trapezoid"){hideFlags=HideFlags.HideAndDontSave};
            beamObject.transform.SetParent(transform,false);
            beamMesh=new Mesh{name="Door beam frustum",hideFlags=HideFlags.HideAndDontSave};beamMesh.MarkDynamic();
            beamObject.AddComponent<MeshFilter>().sharedMesh=beamMesh;
            beamRenderer=beamObject.AddComponent<MeshRenderer>();beamRenderer.sharedMaterial=beamMaterial;
            beamRenderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;beamRenderer.receiveShadows=false;
            beamRenderer.lightProbeUsage=UnityEngine.Rendering.LightProbeUsage.Off;
            beamRenderer.reflectionProbeUsage=UnityEngine.Rendering.ReflectionProbeUsage.Off;
            meshBaked=false;
        }
        // Keep the front cap behind the gameplay camera, even after FOV/zoom edits.
        float foregroundLength=foregroundExtension;
        if(foregroundLength>0&&beamCamera)foregroundLength=Mathf.Max(foregroundLength,origin.z-beamCamera.transform.position.z+1f);
        var matrix=transform.localToWorldMatrix;
        if(!meshBaked||origin!=previousOrigin||nearHalf!=previousNear||farHalf!=previousFar||offset!=previousOffset||length!=previousLength||matrix!=previousTransform||foregroundLength!=previousForegroundLength)
        {
            for(int i=0;i<4;i++)
            {
                float x=i<2?-1:1,y=i==0||i==3?-1:1;
                beamVertices[i]=transform.InverseTransformPoint(origin+new Vector3(x*nearHalf.x,y*nearHalf.y,-foregroundLength));
                beamVertices[i+4]=transform.InverseTransformPoint(origin+new Vector3(x*nearHalf.x,y*nearHalf.y,0));
                beamVertices[i+8]=transform.InverseTransformPoint(origin+new Vector3(offset.x+x*farHalf.x,offset.y+y*farHalf.y,length));
            }
            beamMesh.vertices=beamVertices;beamMesh.triangles=BeamTriangles;beamMesh.RecalculateBounds();
            previousOrigin=origin;previousNear=nearHalf;previousFar=farHalf;previousOffset=offset;previousLength=length;previousTransform=matrix;previousForegroundLength=foregroundLength;meshBaked=true;
        }
        if(properties==null)properties=new MaterialPropertyBlock();
        properties.SetVector("_BeamOrigin",origin);
        properties.SetFloat("_ForegroundLength",foregroundLength);
        properties.SetVector("_NearFarHalf",new Vector4(nearHalf.x,nearHalf.y,farHalf.x,farHalf.y));
        properties.SetVector("_FarOffsetLength",new Vector4(offset.x,offset.y,length,beamEndFeather));
        properties.SetVector("_BeamSettings",new Vector4(VolumeDensity,beamEdgeFeather,beamBrightness,light.intensity));
        properties.SetColor("_BeamColour",light.color);
        properties.SetVector("_DoorBeamLightPositionRange",new Vector4(light.transform.position.x,light.transform.position.y,light.transform.position.z,light.range));
        float halfAngle=light.spotAngle*.5f*Mathf.Deg2Rad;
        properties.SetVector("_SpotShape",new Vector4(Mathf.Tan(halfAngle),Mathf.Cos(halfAngle),Mathf.Cos(light.innerSpotAngle*.5f*Mathf.Deg2Rad),0));
        properties.SetMatrix("_WorldToLight",Matrix4x4.TRS(light.transform.position,light.transform.rotation,Vector3.one).inverse);
        properties.SetTexture("_Cookie",light.cookie?light.cookie:Texture2D.whiteTexture);
        beamRenderer.SetPropertyBlock(properties);beamRenderer.enabled=true;
        if(beamCamera)beamCamera.depthTextureMode|=DepthTextureMode.Depth;
    }
    void LateUpdate(){if(!Application.isPlaying){ApplyBlend(editorPreview);return;}float target=PlayerInActivationRange()?1:0;float dt=world?world.FrameDelta:Time.deltaTime;ApplyBlend(Mathf.MoveTowards(Blend,target,dt/Mathf.Max(.05f,target>Blend?fadeIn:fadeOut)));}
    void OnDisable()
    {
        Camera.onPreCull-=PrepareCameraDepth;Blend=0;
        if(beamObject){beamObject.SetActive(false);ReleaseBeamObject(beamObject);}
        if(beamMesh)ReleaseBeamObject(beamMesh);if(beamMaterial)ReleaseBeamObject(beamMaterial);
        beamObject=null;beamMesh=null;beamMaterial=null;beamRenderer=null;meshBaked=false;
    }
    static void ReleaseBeamObject(Object value){if(Application.isPlaying)Destroy(value);else DestroyImmediate(value);}
}
