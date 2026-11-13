using UnityEngine;

// Isolated art spike. No RecoilPrototype, saves, AI, navigation or production changes.
public sealed class CoalerModelSpikeLab : MonoBehaviour
{
    public Transform model, chainJoint, core;
    public Transform[] shutters;
    public RampagerTentacleVisual reference;
    public Camera testCamera;
    public bool animateChain;
    public bool useOriginalCore;
    public bool openCore;
    public float CoreOpenBlend {get;private set;}
    public float approachSeconds=2f;
    public bool approaching;
    public float ApproachBlend {get;private set;}
    public int ApproachChains {get{return approachPose==null?0:approachPose.ChainCount;}}
    CoalerSpikeApproachPose approachPose;
    public float foldSeconds=1f, holdSeconds=2.5f, reopenSeconds=.5f;
    Quaternion restJoint;
    Transform[] drivenJoints;
    Quaternion[] restRotations;
    Vector3[] swingAxes;
    SkinnedMeshRenderer drivenRenderer;
    MaterialPropertyBlock highlight;
    float clock, reaction=-1f;
    bool initialized;
    public string Phase {get;private set;}="Rest";
    public float Closure {get;private set;}
    public bool CoreExposed {get{return Closure<.2f;}}
    public int Hits {get;private set;}
    public bool HasJoint {get{return chainJoint;}}
    public int DrivenJointCount {get{return drivenJoints==null?0:drivenJoints.Length;}}

    void Awake(){Initialize();}
    public void Initialize()
    {
        if(initialized&&highlight!=null&&approachPose!=null)return;
        if(chainJoint)restJoint=chainJoint.localRotation;
        var joints=new System.Collections.Generic.List<Transform>();
        if(model)
        {
            var all=model.GetComponentsInChildren<Transform>(true);
            for(int i=1;i<=12;i++)
            {
                string suffix="COALER_EXTRA_TENTACLE_BACK_01_JNT_"+i.ToString("D2");
                foreach(var t in all)if(t.name.EndsWith(suffix,System.StringComparison.Ordinal)){joints.Add(t);break;}
            }
            foreach(var r in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                if(r.name.Contains("COALER_EXTRA_TENTACLE_BACK_01_GEO")){drivenRenderer=r;break;}
        }
        if(joints.Count==0&&chainJoint)joints.Add(chainJoint);
        drivenJoints=joints.ToArray();restRotations=new Quaternion[drivenJoints.Length];swingAxes=new Vector3[drivenJoints.Length];
        Vector3 screenNormal=testCamera?testCamera.transform.forward:Vector3.forward;
        for(int i=0;i<drivenJoints.Length;i++)
        {
            restRotations[i]=drivenJoints[i].localRotation;
            swingAxes[i]=Quaternion.Inverse(drivenJoints[i].rotation)*screenNormal;
        }
        highlight=new MaterialPropertyBlock();
        if(model)approachPose=new CoalerSpikeApproachPose(model,testCamera);
        initialized=true;ResetLab();
    }
    public void ResetLab()
    {
        reaction=-1;clock=0;Hits=0;animateChain=false;Closure=0;Phase="Rest";
        approaching=false;ApproachBlend=0;if(approachPose!=null)approachPose.Apply(0,0);
        openCore=false;CoreOpenBlend=0;
        if(reference){reference.SetPreview(RampagerTentacleVisual.Form.Watching);reference.UpdatePose(0,true);}
        if(chainJoint)chainJoint.localRotation=restJoint;
        ApplyChain();
        ApplyProxy();
    }
    public bool FireRay(Ray ray)
    {
        if(useOriginalCore)return false; // No proxy hit behaviour on the source mesh.
        if(!core||!CoreExposed||reaction>=0)return false;
        float along=Vector3.Dot(core.position-ray.origin,ray.direction);
        if(along<0||(ray.origin+ray.direction*along-core.position).sqrMagnitude>.12f*.12f)return false;
        Hits++;reaction=0;Phase="Closing";return true;
    }
    public void HitCore()
    {
        if(core)FireRay(new Ray(core.position+Vector3.back*4,Vector3.forward));
    }
    public void Step(float dt)
    {
        Initialize();clock+=Mathf.Max(0,dt);
        ApproachBlend=Mathf.MoveTowards(ApproachBlend,approaching?1:0,Mathf.Max(0,dt)/Mathf.Max(.1f,approachSeconds));
        CoreOpenBlend=Mathf.MoveTowards(CoreOpenBlend,openCore?1:0,Mathf.Max(0,dt)/2f);
        if(approachPose!=null)approachPose.Apply(Mathf.SmoothStep(0,1,ApproachBlend),clock,Mathf.SmoothStep(0,1,CoreOpenBlend));
        if(ApproachBlend<=0&&CoreOpenBlend<=0)ApplyChain();
        if(reaction>=0)
        {
            reaction+=Mathf.Max(0,dt);
            if(reaction<foldSeconds){Closure=Mathf.SmoothStep(0,1,reaction/foldSeconds);Phase="Closing";}
            else if(reaction<foldSeconds+holdSeconds){Closure=1;Phase="Covered";}
            else if(reaction<foldSeconds+holdSeconds+reopenSeconds){Closure=1-Mathf.SmoothStep(0,1,(reaction-foldSeconds-holdSeconds)/reopenSeconds);Phase="Reopening";}
            else {reaction=-1;Closure=0;Phase="Open";}
        }
        ApplyProxy();
    }
    void ApplyChain()
    {
        if(drivenJoints!=null)for(int i=0;i<drivenJoints.Length;i++)if(drivenJoints[i])
        {
            float angle=animateChain?Mathf.Sin(clock*1.8f-i*.20f)*14f:0;
            drivenJoints[i].localRotation=restRotations[i]*Quaternion.AngleAxis(angle,swingAxes[i]);
        }
        if(drivenRenderer)
        {
            if(highlight==null)highlight=new MaterialPropertyBlock();
            highlight.Clear();
            if(animateChain)
            {
                highlight.SetColor("_Color",new Color(.82f,.48f,.08f));
                highlight.SetColor("_EmissionColor",new Color(.18f,.07f,.01f));
            }
            drivenRenderer.SetPropertyBlock(highlight);
        }
    }
    void ApplyProxy()
    {
        if(useOriginalCore)return;
        if(shutters!=null)for(int i=0;i<shutters.Length;i++)if(shutters[i])
        {
            float a=i*Mathf.PI*.5f;
            shutters[i].localPosition=new Vector3(Mathf.Cos(a),Mathf.Sin(a),0)*Mathf.Lerp(.29f,.055f,Closure)+Vector3.back*.12f;
            shutters[i].localRotation=Quaternion.Euler(0,0,90+i*90);
        }
        if(core)core.gameObject.SetActive(Closure<.95f);
    }
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Alpha1)){ResetLab();}
        if(Input.GetKeyDown(KeyCode.Alpha2)||Input.GetKeyDown(KeyCode.Keypad2))ToggleChain();
        if(Input.GetKeyDown(KeyCode.Alpha3))HitCore();
        if(Input.GetKeyDown(KeyCode.Alpha4)||Input.GetKeyDown(KeyCode.Keypad4))ToggleApproach();
        if(Input.GetKeyDown(KeyCode.Alpha5)||Input.GetKeyDown(KeyCode.Keypad5))ToggleCoreOpening();
        if(Input.GetMouseButtonDown(0)&&testCamera&&Input.mousePosition.y<Screen.height-145)
            FireRay(testCamera.ScreenPointToRay(Input.mousePosition));
    }
    void LateUpdate(){Step(Time.deltaTime);}
    public void ToggleChain(){Initialize();openCore=false;CoreOpenBlend=0;approaching=false;ApproachBlend=0;if(approachPose!=null)approachPose.Apply(0,0);animateChain=!animateChain;clock=0;ApplyChain();}
    public void ToggleApproach()
    {
        Initialize();openCore=false;animateChain=false;ApplyChain();approaching=!approaching;
        if(reference)reference.SetPreview(approaching?RampagerTentacleVisual.Form.Approaching:RampagerTentacleVisual.Form.Watching);
    }
    public void ToggleCoreOpening(){Initialize();animateChain=false;ApplyChain();approaching=false;openCore=!openCore;}
    void OnDisable(){if(!initialized)return;approaching=false;ApproachBlend=0;if(approachPose!=null)approachPose.Apply(0,0);animateChain=false;ApplyChain();}
    void OnGUI()
    {
        float scale=Mathf.Min(Screen.width/1200f,Screen.height/760f);
        var old=GUI.matrix;GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1));
        GUI.Box(new Rect(14,14,1172,151),GUIContent.none);
        GUI.Label(new Rect(28,22,1100,25),"COALER MODEL SPIKE 01 — original chest core / existing-bone opening test");
        if(GUI.Button(new Rect(28,52,160,30),"1  Rest / Reset"))ResetLab();
        if(GUI.Button(new Rect(198,52,175,30),"2  Tentacle: "+(animateChain?"ON":"OFF")))ToggleChain();
        if(useOriginalCore){if(GUI.Button(new Rect(383,52,160,30),"5  Open / close cover"))ToggleCoreOpening();}
        else if(GUI.Button(new Rect(383,52,160,30),"3  Hit proxy core"))HitCore();
        GUI.Label(new Rect(560,55,600,25),"State: "+Phase+"    Hits: "+Hits+"    Bones: "+DrivenJointCount+"    Sway: "+(animateChain?"ON (gold tentacle)":"OFF"));
        if(GUI.Button(new Rect(28,92,235,30),"4  "+(approaching?"Return to idle":"Idle to approach")))ToggleApproach();
        GUI.Label(new Rect(280,95,870,30),"Pose: "+(ApproachBlend<=0?"IDLE":ApproachBlend>=1?"APPROACH":"TRANSITION")+"  "+Mathf.RoundToInt(ApproachBlend*100)+"%   Chains: "+ApproachChains+"   2s pose only; feet stay in place.");
        GUI.Label(new Rect(28,130,1125,25),useOriginalCore?"Source core mesh restored. 5: FRONT_02_JNT_01 cover; opening "+Mathf.RoundToInt(CoreOpenBlend*100)+"%. No dedicated core-opening bone.":"Legacy proxy test.");
        GUI.matrix=old;
    }
}
