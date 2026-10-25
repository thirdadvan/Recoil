using UnityEngine;

[DefaultExecutionOrder(-850),DisallowMultipleComponent]
public sealed class Recoil3DWorld : MonoBehaviour
{
    public RecoilPrototype world;
    public Light playerVisualLight;
    public Light[] emergencyLights;
    [Header("Local pools of light; no depth emission fill")]
    public Color ambientColour=new Color(.045f,.052f,.058f);
    public Color fogColour=new Color(.025f,.032f,.037f);
    public float fogDensity=.009f;
    public FogMode fogMode=FogMode.Linear;
    [Min(0),Tooltip("雾开始遮蔽的相机距离。角色与相机之间保留清晰区域。 ")] public float fogStartDistance=19;
    [Min(1),Tooltip("此距离之后完全隐入雾中，单位米。 ")] public float fogEndDistance=48;
    public void ApplyAtmosphere()
    {RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=ambientColour;RenderSettings.fog=true;RenderSettings.fogMode=fogMode;RenderSettings.fogColor=fogColour;RenderSettings.fogDensity=fogDensity;RenderSettings.fogStartDistance=Mathf.Max(0,fogStartDistance);RenderSettings.fogEndDistance=Mathf.Max(fogStartDistance+1,fogEndDistance);}
    void OnValidate(){if(!Application.isPlaying && gameObject.scene.IsValid() && gameObject.scene==UnityEngine.SceneManagement.SceneManager.GetActiveScene())ApplyAtmosphere();}
    void Awake()
    {
        if(!world)world=GetComponent<RecoilPrototype>();
        ApplyAtmosphere();
        foreach(var overlay in GetComponentsInChildren<RecoilSightOcclusion>(true))overlay.enabled=false;
        foreach(var lighting in GetComponentsInChildren<RecoilLighting2D>(true))lighting.enabled=false;
        if(world && world.ViewCamera)world.ViewCamera.depthTextureMode|=DepthTextureMode.Depth;
    }
    void LateUpdate()
    {
        if(!world)return;
        if(playerVisualLight && world.Player)playerVisualLight.transform.position=world.Player.WorldPosition+new Vector3(0,.5f,-1.8f);
        bool red=world.SoundWaves && world.SoundWaves.RedAlert;
        if(emergencyLights!=null)for(int i=0;i<emergencyLights.Length;i++)if(emergencyLights[i])
        {emergencyLights[i].enabled=red;emergencyLights[i].intensity=1.3f*Mathf.Pow(Mathf.Max(0,Mathf.Sin(world.Clock*5+i*.7f)),3);}
    }
}
