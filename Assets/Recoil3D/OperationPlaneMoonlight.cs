using UnityEngine;
using UnityEngine.SceneManagement;

[ExecuteAlways,DisallowMultipleComponent,RequireComponent(typeof(Light))]
[AddComponentMenu("Recoil/Lighting/Operation Plane Moonlight")]
public sealed class OperationPlaneMoonlight : MonoBehaviour
{
    public bool effectEnabled=true;
    [Tooltip("玩家默认地面操作面的世界 Z 坐标。")]
    public float planeZ=0;
    [Min(0),Tooltip("此纵深范围内保持完整增强，覆盖地面与后方平台操作面。")]
    public float fullStrengthHalfWidth=1.8f;
    [Min(.01f),Tooltip("超出完整增强区后，平滑恢复原月光强度的距离。")]
    public float fadeWidth=4.5f;
    [Header("Foreground falloff (toward camera)")]
    [Min(0),Tooltip("操作面向镜头方向保留完整增强的距离。")]
    public float foregroundFullStrengthWidth=.6f;
    [Min(.01f),Tooltip("前景从完整增强回落到基础月光的过渡距离。")]
    public float foregroundFadeWidth=1.6f;
    [Range(1,4),Tooltip("操作面附近的月光强度倍率。")]
    public float intensityMultiplier=1.75f;
    [Range(0,1),Tooltip("衰减后的最低月光倍率，相对于 Directional Light 的基础强度。")]
    public float minimumIntensityMultiplier=.6f;
    [Header("Optional floor height limit")]
    public bool limitHeight;
    public float minimumHeight=-1;
    public float maximumHeight=6.4f;
    [Min(.01f)] public float heightFeather=.8f;
    [Header("Optional soft diffuse fill")]
    [Range(0,1)] public float diffuseFillIntensity;
    public Color diffuseFillColour=new Color(.52f,.62f,.68f);
    Light moon;
    static readonly int Fill=Shader.PropertyToID("_RecoilRoutePlaneDiffuseFill");
    static readonly int Band=Shader.PropertyToID("_RecoilRouteMoonBand"),Foreground=Shader.PropertyToID("_RecoilRouteMoonForeground"),Direction=Shader.PropertyToID("_RecoilRouteMoonDirection"),Multiplier=Shader.PropertyToID("_RecoilRouteMoonMultiplier"),Minimum=Shader.PropertyToID("_RecoilRouteMoonMinimum"),Height=Shader.PropertyToID("_RecoilRouteMoonHeight");
    void OnEnable(){moon=GetComponent<Light>();Camera.onPreCull+=Prepare;Apply(null);}
    void OnDisable(){Camera.onPreCull-=Prepare;Shader.SetGlobalVector(Band,Vector4.zero);}
    void OnValidate(){moon=GetComponent<Light>();Apply(null);}
    void Prepare(Camera camera){Apply(camera);}
    public void Apply(Camera camera)
    {
        if(!moon)moon=GetComponent<Light>();
        bool correctScene=camera&&camera.cameraType!=CameraType.SceneView?camera.gameObject.scene==gameObject.scene:SceneManager.GetActiveScene()==gameObject.scene;
        bool active=effectEnabled&&isActiveAndEnabled&&moon&&moon.enabled&&moon.type==LightType.Directional&&gameObject.activeInHierarchy&&correctScene;
        Shader.SetGlobalVector(Band,new Vector4(active?1:0,planeZ,fullStrengthHalfWidth,fadeWidth));
        Shader.SetGlobalVector(Foreground,new Vector4(foregroundFullStrengthWidth,foregroundFadeWidth,0,0));
        Shader.SetGlobalVector(Direction,-transform.forward);
        Shader.SetGlobalFloat(Multiplier,intensityMultiplier);
        Shader.SetGlobalFloat(Minimum,minimumIntensityMultiplier);
        Shader.SetGlobalVector(Height,new Vector4(limitHeight?1:0,minimumHeight,maximumHeight,Mathf.Max(.01f,heightFeather)));
        Shader.SetGlobalVector(Fill,(Vector4)(diffuseFillColour*Mathf.Max(0,diffuseFillIntensity)));
    }
    public float MultiplierAt(float z)
    {
        if(!effectEnabled)return 1;
        bool foreground=z<planeZ;
        float width=foreground?foregroundFullStrengthWidth:fullStrengthHalfWidth;
        float fade=foreground?foregroundFadeWidth:fadeWidth;
        float t=Mathf.Clamp01((Mathf.Abs(z-planeZ)-width)/Mathf.Max(.01f,fade));
        return Mathf.Lerp(intensityMultiplier,minimumIntensityMultiplier,t*t*(3-2*t));
    }
    public float MultiplierAt(Vector3 position)
    {
        float multiplier=MultiplierAt(position.z);
        if(!effectEnabled || !limitHeight)return multiplier;
        float low=Mathf.SmoothStep(0,1,Mathf.InverseLerp(minimumHeight-heightFeather,minimumHeight,position.y));
        float high=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(maximumHeight,maximumHeight+heightFeather,position.y));
        return Mathf.Lerp(minimumIntensityMultiplier,multiplier,low*high);
    }
}
