using UnityEngine;
using UnityEngine.Rendering;

// Presentation only. The existing coordinator, senses, navigation and damage remain authoritative.
[ExecuteAlways,DefaultExecutionOrder(850),DisallowMultipleComponent]
public sealed class RampagerTentacleVisual : MonoBehaviour
{
    public enum Form { Dormant, Awakening, Roaming, Watching, Approaching, Enraged, Fusion, Idle }
    [HideInInspector] public bool followAI=true; // Retained for existing scene serialization.
    public bool autoCycle;
    [Tooltip("门外投影专用：使用相同 Coaler 模型，只投射阴影，由堵门动画驱动。")]
    public bool shadowOnly;
    [HideInInspector] public float doorwayReach;
    [HideInInspector] public int doorwayLeadingArm;
    public void AdvanceDoorway(float reach,int leading,float dt)
    {
        doorwayReach=reach;doorwayLeadingArm=leading;clock+=Mathf.Max(0,dt);
        if(!root)Build();
        if(clock>=nextTick){UpdatePose(dt,false);nextTick=clock+1f/Mathf.Max(10,animationRate);}
    }
    public Form previewForm=Form.Roaming;
    [Range(0,1)] public float fusionProgress=.7f;
    [Min(.1f)] public float transitionSeconds=1.1f,cycleSeconds=5;
    [Range(10,40)] public int animationRate=24;
    [Header("Small-step locomotion")]
    [Min(.2f)] public float stepCycleDistance=.48f;
    [Range(.01f,.12f)] public float footLift=.055f;
    [Header("Exposed core light (visual only)")]
    [Min(0)] public float coreLightIntensity=2.2f;
    [Min(.1f)] public float coreLightRange=1.05f;
    [Min(0)] public float coreEmissionStrength=2.4f;
    [Range(0,100)] public float previewSoundEnergy=100;
    [Range(0,1)] public float tentacleGlowRelativeToCore=.65f;
    [Header("Idle tentacle sway")]
    [Min(.1f),Tooltip("进入待机后，松散触须完全展开需要的秒数。") ]
    public float idleUnfurlSeconds=1.75f;
    [Range(0,6),Tooltip("待机触须晃动幅度倍率，1 为原来的轻微晃动。") ]
    public float idleSwayAmplitude=3;
    [Header("Tentacle sweep preview")]
    [Range(-1,1)] public int previewSweepSide;
    [Range(0,1)] public float previewSweepProgress;
    public int variationSeed=317;
    public Material tentacleMaterial,trouserMaterial,patchMaterial,bootMaterial,coreMaterial,badgeMaterial;
    public Transform previewTarget;
    public Form CurrentForm {get;private set;}
    public float OpenAmount {get{return opening;}}
    public float IdleUnfurlAmount {get{return Mathf.SmoothStep(0,1,idleUnfurl);}}
    public float AngerAmount {get{return tension;}}
    public Transform VisualRoot {get{return root;}}
    public int VertexCount {get{return vertices==null?0:vertices.Length;}}
    const int Rings=15,Sides=6;
    struct Strand {public int group,index;public float random,radius;}
    Strand[] strands;
    Transform root,legs,leftLeg,rightLeg,core,badge;
    Transform leftKnee,rightKnee,leftFoot,rightFoot;
    Mesh mesh;MeshRenderer tubes;
    Vector3[] vertices,normals;Color[] vertexColours;
    readonly Vector3[] curve=new Vector3[4];
    readonly float[] cos=new float[Sides],sin=new float[Sides];
    RampagerPatrolRoute patrol;int leftSweepIndex=-1,rightSweepIndex=-1;float leftSweep,rightSweep;
    RampagerController ai;RampagerMovement motor;RampagerGrab grab;RampagerAttack attack;
    Recoil3DActorVisual legacy;RecoilPrototype world;
    float standing=1,opening,extension,tension,fusion,stride,clock,nextTick,bodyYaw,headYaw,slump,fusionReach,idleUnfurl;
    Vector3 previous,fusionTargetLocal;
    Vector3[] strandFrameRights;
    float sampledSpeed,gaitStrength,glowEnergy,roar;
    bool RuntimeDriven {get{return Application.isPlaying && ai;}}
    Material[] temporaryMaterials;
    MaterialPropertyBlock properties;
    MaterialPropertyBlock coreProperties;
    Renderer coreRenderer;
    Light coreLight;
    Renderer[] renderers;Color[] rendererColours;float previousAlpha=-1;

    void OnEnable(){Build();}
    static void Release(Object value){if(!value)return;if(Application.isPlaying)Destroy(value);else DestroyImmediate(value);}
    Transform Group(string name,Transform parent)
    {var g=new GameObject(name);g.hideFlags=HideFlags.DontSave;g.layer=parent.gameObject.layer;g.transform.SetParent(parent,false);return g.transform;}
    Material Fallback(string name,Color c,float gloss)
    {var m=new Material(Shader.Find("Recoil/3D/Weathered Surface")?Shader.Find("Recoil/3D/Weathered Surface"):Shader.Find("Standard"));m.name=name;m.hideFlags=HideFlags.DontSave;m.color=c;m.SetFloat("_Glossiness",gloss);return m;}
    Transform Shape(string name,Transform parent,Vector3 p,Vector3 size,Material mat,PrimitiveType type=PrimitiveType.Cube)
    {
        var g=GameObject.CreatePrimitive(type);g.name=name;g.hideFlags=HideFlags.DontSave;g.layer=parent.gameObject.layer;g.transform.SetParent(parent,false);
        var collider=g.GetComponent<Collider>();if(collider){collider.enabled=false;Release(collider);}
        g.transform.localPosition=p;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=mat;return g.transform;
    }
    public void Build()
    {
        Cleanup();patrol=GetComponent<RampagerPatrolRoute>();ai=GetComponent<RampagerController>();motor=GetComponent<RampagerMovement>();grab=GetComponent<RampagerGrab>();attack=GetComponent<RampagerAttack>();
        legacy=GetComponent<Recoil3DActorVisual>();world=GetComponentInParent<RecoilPrototype>();
        if(legacy && legacy.model)legacy.model.gameObject.SetActive(false);
        temporaryMaterials=new Material[6];
        var mats=new[]{tentacleMaterial,trouserMaterial,patchMaterial,bootMaterial,coreMaterial,badgeMaterial};
        var colours=new[]{new Color(.025f,.027f,.024f),new Color(.86f,.31f,.035f),new Color(.12f,.115f,.095f),new Color(.045f,.04f,.032f),new Color(.25f,.012f,.018f),new Color(.64f,.58f,.43f)};
        for(int i=0;i<6;i++)if(!mats[i])mats[i]=temporaryMaterials[i]=Fallback("Tentacle prototype "+i,colours[i],i==0?.52f:.15f);
        if(temporaryMaterials[4]){mats[4].EnableKeyword("_EMISSION");mats[4].SetColor("_EmissionColor",new Color(.35f,.008f,.014f));}
        root=Group("Tentacle worker - procedural visual",transform);
        legs=Group("Human legs - narrow tired stance",root);
        Shape("Worn orange waistband",legs,new Vector3(0,.78f,0),new Vector3(.35f,.16f,.26f),mats[1],PrimitiveType.Sphere);
        leftLeg=MakeLeg("Left leg - long arm side",-.105f,mats);rightLeg=MakeLeg("Right leg - short arm side",.105f,mats);
        core=Shape("Small suspended core - empty cavity around it",root,new Vector3(.025f,1.15f,-.045f),new Vector3(.12f,.19f,.12f),mats[4],PrimitiveType.Sphere);
        coreRenderer=core.GetComponent<Renderer>();coreProperties=new MaterialPropertyBlock();
        // The light belongs to the core and follows every pose; no gameplay exposure registration.
        var glow=Group("Red core - local chest illumination",core);
        coreLight=glow.gameObject.AddComponent<Light>();coreLight.type=LightType.Point;
        coreLight.color=new Color(1,.025f,.035f);coreLight.range=coreLightRange;
        coreLight.shadows=LightShadows.None;coreLight.renderMode=LightRenderMode.ForcePixel;
        coreLight.cullingMask=1<<gameObject.layer;coreLight.enabled=false;
        badge=Group("Crooked worker identification - follows left opening",root);
        Shape("Identity card",badge,Vector3.zero,new Vector3(.085f,.12f,.012f),mats[5]);
        Shape("Faded portrait",badge,new Vector3(-.016f,.012f,-.008f),new Vector3(.024f,.036f,.004f),mats[2]);
        Shape("Faded name",badge,new Vector3(.014f,-.028f,-.008f),new Vector3(.036f,.008f,.004f),mats[2]);
        var list=new System.Collections.Generic.List<Strand>();
        int[] counts={8,7,12,8,8,6,7,3,12};
        var rng=new System.Random(variationSeed);
        for(int g=0;g<counts.Length;g++)for(int i=0;i<counts[g];i++)list.Add(new Strand{group=g,index=i,random=(float)rng.NextDouble(),radius=g==7?.006f:g==8?.029f:Mathf.Lerp(.018f,.045f,(float)rng.NextDouble())*(g==5?1.25f:1)});
        strands=list.ToArray();
        float longestLeft=-1,longestRight=-1;leftSweepIndex=rightSweepIndex=-1;
        foreach(var s in strands)if(s.group==6)
        {
            if(s.index%2==0&&s.random>longestLeft){longestLeft=s.random;leftSweepIndex=s.index;}
            if(s.index%2==1&&s.random>longestRight){longestRight=s.random;rightSweepIndex=s.index;}
        }
        strandFrameRights=new Vector3[strands.Length];vertices=new Vector3[strands.Length*Rings*Sides];normals=new Vector3[vertices.Length];vertexColours=new Color[vertices.Length];
        int[] indices=new int[strands.Length*(Rings-1)*Sides*6];int n=0;
        for(int t=0;t<strands.Length;t++)for(int r=0;r<Rings-1;r++)for(int s=0;s<Sides;s++)
        {int a=(t*Rings+r)*Sides+s,b=(t*Rings+r)*Sides+(s+1)%Sides,c=a+Sides,d=b+Sides;indices[n++]=a;indices[n++]=c;indices[n++]=b;indices[n++]=b;indices[n++]=c;indices[n++]=d;}
        for(int s=0;s<Sides;s++){cos[s]=Mathf.Cos(s*Mathf.PI*2/Sides);sin[s]=Mathf.Sin(s*Mathf.PI*2/Sides);}
        mesh=new Mesh{name="Dynamic combined curved tentacles",hideFlags=HideFlags.DontSave};mesh.MarkDynamic();mesh.vertices=vertices;mesh.triangles=indices;
        var skin=Group("Back head torso shutters arms loose strands and fusion",root);skin.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;tubes=skin.gameObject.AddComponent<MeshRenderer>();tubes.sharedMaterial=mats[0];tubes.shadowCastingMode=ShadowCastingMode.On;
        properties=new MaterialPropertyBlock();previous=transform.position;
        renderers=root.GetComponentsInChildren<Renderer>(true);rendererColours=new Color[renderers.Length];
        for(int i=0;i<renderers.Length;i++)rendererColours[i]=renderers[i].sharedMaterial.color;
        previousAlpha=-1;
        CurrentForm=followAI && ai?(ai.initialState==RampagerController.Behaviour.Sleeping?Form.Dormant:Form.Roaming):previewForm;
        standing=CurrentForm==Form.Dormant?0:1;opening=CurrentForm==Form.Watching||CurrentForm==Form.Approaching||CurrentForm==Form.Fusion?1:0;
        extension=CurrentForm==Form.Approaching||CurrentForm==Form.Fusion?1:0;tension=CurrentForm==Form.Enraged?1:0;fusion=CurrentForm==Form.Fusion?fusionProgress:0;
        fusionReach=0;glowEnergy=roar=0;idleUnfurl=0;
        UpdatePose(0,true);
        nextTick=(GetInstanceID()&31)/(32f*animationRate);
    }
    Transform MakeLeg(string name,float x,Material[] m)
    {
        var hip=Group(name,legs);hip.localPosition=new Vector3(x,.72f,0);
        Shape("Upper orange trouser",hip,new Vector3(0,-.17f,0),new Vector3(.18f,.23f,.20f),m[1],PrimitiveType.Capsule);
        var knee=Group("Relaxed knee",hip);knee.localPosition=new Vector3(0,-.32f,-.015f);knee.localRotation=Quaternion.Euler(-7,0,0);
        Shape("Lower orange trouser",knee,new Vector3(0,-.14f,.015f),new Vector3(.16f,.20f,.18f),m[1],PrimitiveType.Capsule);
        Shape("Dark reinforced knee",knee,new Vector3(0,.005f,-.092f),new Vector3(.135f,.13f,.025f),m[2]);
        Shape("Side cargo pocket",hip,new Vector3(Mathf.Sign(x)*.088f,-.18f,.005f),new Vector3(.025f,.13f,.135f),m[2]);
        var foot=Group("Ankle - planted safety boot",knee);foot.localPosition=new Vector3(0,-.32f,0);
        Shape("Safety boot",foot,new Vector3(0,0,-.045f),new Vector3(.19f,.15f,.31f),m[3]);
        Shape("Thick boot sole",foot,new Vector3(0,-.067f,-.045f),new Vector3(.20f,.035f,.32f),m[3]);
        if(x<0){leftKnee=knee;leftFoot=foot;}else{rightKnee=knee;rightFoot=foot;}
        return hip;
    }
    Form DesiredForm()
    {
        if(!RuntimeDriven)
        {
            if(autoCycle)return (Form)(Mathf.FloorToInt(clock/Mathf.Max(.1f,cycleSeconds))%8);
            if(ai && followAI)return ai.initialState==RampagerController.Behaviour.Sleeping?Form.Dormant:ai.HasPatrolRoute?Form.Roaming:Form.Idle;
            return previewForm;
        }
        if(ai.Waking)return Form.Awakening;
        if(ai.State==RampagerController.Behaviour.Sleeping)return Form.Dormant;
        if(ai.PreparingAttack)return Form.Watching;
        if(ai.State==RampagerController.Behaviour.Attack)return Form.Enraged;
        if(grab && grab.IsGrabbing)return Form.Fusion;
        if(ai.WatchingPlayer)return sampledSpeed>(CurrentForm==Form.Approaching?.04f:.12f)?Form.Approaching:Form.Watching;
        return sampledSpeed>(CurrentForm==Form.Idle?.08f:.04f)?Form.Roaming:Form.Idle;
    }
    void LateUpdate()
    {
        if(!Application.isPlaying||shadowOnly)return;
        if(!root){Build();return;}
        float dt=world?world.FrameDelta:Time.deltaTime;if(dt<=0)return;
        clock+=dt;
        sampledSpeed=(transform.position-previous).magnitude/Mathf.Max(.0001f,dt);
        bool changed=DesiredForm()!=CurrentForm;
        bool inView=!RuntimeDriven || !tubes || tubes.isVisible;
        if(inView && nextTick-clock>.05f)nextTick=clock;
        bool striking=RuntimeDriven && attack && attack.Phase!=RampagerAttack.AttackPhase.Ready;
        bool deform=changed || striking || clock>=nextTick;
        // Unseen monsters keep their behaviour fully active; only mesh deformation updates less often.
        if(deform)nextTick=clock+(inView?1f/animationRate:.25f);
        // Rotation, legs and state blending run every frame; the expensive mesh has its own budget.
        AdvancePose(dt,false,deform);previous=transform.position;
    }
    public void PreviewTick(float seconds)
    {if(!root)Build();clock+=Mathf.Max(0,seconds);UpdatePose(seconds,false);}
    public void SetPreview(Form form){followAI=false;autoCycle=false;previewForm=form;}
    public void UpdatePose(float dt,bool snap)
    {sampledSpeed=dt>0?(transform.position-previous).magnitude/dt:0;AdvancePose(dt,snap,true);previous=transform.position;}
    void AdvancePose(float dt,bool snap,bool deform)
    {
        if(!root || !mesh)return;
        Form previousForm=CurrentForm;
        CurrentForm=DesiredForm();float response=CurrentForm==Form.Enraged?.16f:transitionSeconds;
        float k=snap?1:1-Mathf.Exp(-Mathf.Max(0,dt)*4/Mathf.Max(.1f,response));
        float targetStand=CurrentForm==Form.Dormant?0:CurrentForm==Form.Awakening?(RuntimeDriven?Mathf.Clamp01(1-ai.WakeRemaining/1.75f):Mathf.Clamp01((clock%cycleSeconds)/Mathf.Max(.1f,cycleSeconds*.8f))):1;
        standing=Mathf.Lerp(standing,targetStand,k);
        slump=Mathf.Lerp(slump,CurrentForm==Form.Idle?1:0,k);
        // A finite-duration unfurl, separate from the faster tired-torso blend.
        // Runtime spawn starts folded; explicit editor pose snapshots stay immediate.
        if(CurrentForm==Form.Idle)
        {
            if(previousForm!=Form.Idle)idleUnfurl=0;
            idleUnfurl=snap&&!Application.isPlaying?1:Mathf.MoveTowards(idleUnfurl,1,Mathf.Max(0,dt)/Mathf.Max(.1f,idleUnfurlSeconds));
        }
        else idleUnfurl=snap?0:Mathf.Lerp(idleUnfurl,0,k);

        bool sweeping=CurrentForm==Form.Idle||CurrentForm==Form.Roaming;
        int sweepSide=RuntimeDriven?(patrol?patrol.TentacleSweepSide:0):previewSweepSide;
        float sweepProgress=RuntimeDriven?(patrol?patrol.TentacleSweepProgress:0):previewSweepProgress;
        float sweep=sweeping?Mathf.Sin(Mathf.Clamp01(sweepProgress)*Mathf.PI):0;
        leftSweep=sweepSide<0?sweep:0;rightSweep=sweepSide>0?sweep:0;
        roar=RuntimeDriven?ai.SearchRoarAmount:0;
        float anger=RuntimeDriven && (ai.WatchingPlayer || ai.PreparingAttack)?(ai.PreparingAttack?ai.AttackChargeProgress:0):CurrentForm==Form.Enraged?1-roar:0;
        bool open=roar>.01f||CurrentForm==Form.Watching||CurrentForm==Form.Approaching||CurrentForm==Form.Fusion;
        opening=Mathf.Lerp(opening,open?1:0,k);extension=Mathf.Lerp(extension,CurrentForm==Form.Approaching||CurrentForm==Form.Fusion?1:roar,k);
        // The countdown IS the transformation: no second slow blend after anger reaches one.
        if(RuntimeDriven && (ai.WatchingPlayer || ai.PreparingAttack)){opening=Mathf.Min(opening,1-anger);extension=Mathf.Min(extension,1-anger);tension=anger;}
        else tension=snap?anger:Mathf.Lerp(tension,anger,k*.9f);
        glowEnergy=RuntimeDriven?ai.TentacleGlowEnergy:CurrentForm==Form.Enraged?1:CurrentForm==Form.Watching||CurrentForm==Form.Approaching||CurrentForm==Form.Fusion?Mathf.Clamp01(previewSoundEnergy*.01f):0;
        float targetFusion=CurrentForm==Form.Fusion?(RuntimeDriven && world && world.Player && world.Player.GrabPressure?world.Player.GrabPressure.Pressure:fusionProgress):0;
        fusion=Mathf.Lerp(fusion,targetFusion*(1-anger),k);
        fusionReach=Mathf.Lerp(fusionReach,CurrentForm==Form.Fusion?1:0,k);
        if(RuntimeDriven && ai.WatchingPlayer)fusion=Mathf.Min(fusion,1-anger);
        float scale=motor?motor.bodyScale:1.3f;Vector3 lossy=transform.lossyScale;
        root.localScale=new Vector3(scale/Mathf.Max(.001f,Mathf.Abs(lossy.x)),scale/Mathf.Max(.001f,Mathf.Abs(lossy.y)),scale/Mathf.Max(.001f,Mathf.Abs(lossy.z)));
        root.position=transform.position+Vector3.down*.8f;
        float desiredYaw=motor && RuntimeDriven?motor.VisualYaw:transform.eulerAngles.y;
        float desiredHeadYaw=desiredYaw;
        if(ai && Application.isPlaying && (ai.Noticing || ai.WatchingPlayer || ai.State==RampagerController.Behaviour.Attack))
        {Vector3 look=ai.AttentionWorldPosition-transform.position;if(look.sqrMagnitude>.001f)desiredHeadYaw=Mathf.Atan2(-look.x,-look.z)*Mathf.Rad2Deg;}
        headYaw=Mathf.MoveTowardsAngle(headYaw,desiredHeadYaw,360*dt);bodyYaw=snap?desiredYaw:Mathf.MoveTowardsAngle(bodyYaw,desiredYaw,(motor?motor.turnSpeed:150)*dt);
        root.rotation=Quaternion.Euler(transform.eulerAngles.x,bodyYaw,transform.eulerAngles.z);
        float moved=(transform.position-previous).magnitude;
        if(RuntimeDriven){if(sampledSpeed>.04f)stride+=Mathf.Min(.15f,moved)*Mathf.PI*2/Mathf.Max(stepCycleDistance,sampledSpeed/1.8f);}else if(CurrentForm==Form.Roaming||CurrentForm==Form.Approaching)stride+=dt*5;
        bool canStep=CurrentForm!=Form.Dormant && CurrentForm!=Form.Awakening && !(motor && motor.Airborne);
        float gaitTarget=canStep?(RuntimeDriven?Mathf.InverseLerp(.04f,.18f,sampledSpeed):CurrentForm==Form.Roaming||CurrentForm==Form.Approaching?1:0):0;
        gaitStrength=Mathf.MoveTowards(gaitStrength,gaitTarget,dt*8);float gait=gaitStrength;
        float legStand=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.05f,.8f,standing));
        legs.localPosition=Vector3.Lerp(new Vector3(.08f,.045f,.68f),Vector3.zero,legStand);legs.localRotation=Quaternion.Slerp(Quaternion.Euler(84,0,-11),Quaternion.identity,legStand);
        AnimateLeg(leftLeg,leftKnee,leftFoot,stride,gait,legStand,true);
        AnimateLeg(rightLeg,rightKnee,rightFoot,stride+Mathf.PI,gait,legStand,false);
        Vector3 target=previewTarget?root.InverseTransformPoint(previewTarget.position):world && world.Player?root.InverseTransformPoint(world.Player.WorldPosition):new Vector3(.95f,.8f,-.25f);
        target.y=Mathf.Clamp(target.y,.65f,1.6f);
        fusionTargetLocal=target;
        if(deform)
        {
            for(int i=0;i<strands.Length;i++){Controls(strands[i],target);WriteTube(i,strands[i]);}
            mesh.vertices=vertices;mesh.normals=normals;mesh.colors=vertexColours;mesh.RecalculateBounds();
        }
        core.localPosition=new Vector3(.025f,1.145f+Mathf.Sin(clock*1.2f)*.007f,-.045f);core.localScale=new Vector3(.12f,.19f,.12f)*(1+Mathf.Sin(clock*2)*.035f);
        core.gameObject.SetActive(CurrentForm!=Form.Enraged && standing>.86f && opening>.42f && tension<.7f);
        badge.localPosition=Vector3.Lerp(new Vector3(-.4f,.075f,-.26f),new Vector3(-.13f-opening*.24f,1.27f,-.21f),standing);
        Vector3 badgePos=badge.localPosition;if(badgePos.y>.8f){float height=badgePos.y-.8f;badgePos.y-=height*.30f*slump;badgePos.z-=height*.15f*slump;badge.localPosition=badgePos;}
        badge.localRotation=Quaternion.Euler(standing<.3f?84:10,0,-19-opening*12+Mathf.Sin(clock)*2);
        ApplyVisibility();
        // Per-instance properties: a watching monster must not recolour another monster's core.
        bool glowing=CurrentForm==Form.Watching || CurrentForm==Form.Approaching || CurrentForm==Form.Fusion;
        coreRenderer.GetPropertyBlock(coreProperties);
        Color coreColour=glowing?new Color(.28f,.012f,.018f):new Color(.008f,.008f,.008f);
        coreColour.a=previousAlpha>=0?previousAlpha:1;
        coreProperties.SetColor("_Color",coreColour);
        float visibleAlpha=previousAlpha>=0?Mathf.Clamp01(previousAlpha):1;
        coreProperties.SetColor("_EmissionColor",glowing?new Color(1,.012f,.025f)*coreEmissionStrength*visibleAlpha:Color.black);
        coreLight.enabled=!shadowOnly && glowing && core.gameObject.activeInHierarchy && visibleAlpha>.01f;
        coreLight.range=coreLightRange;
        coreLight.intensity=coreLightIntensity*visibleAlpha;
        coreRenderer.SetPropertyBlock(coreProperties);
        // Audible response in combat: a brief material pulse only beyond physical striking reach.
        float heardFlash=RuntimeDriven && ai.State==RampagerController.Behaviour.Attack && !ai.WaveActive && world && world.Player &&
            Vector3.Distance(transform.position,world.Player.WorldPosition)>ai.contactRadius?ai.SoundResponse:0;
        tubes.GetPropertyBlock(properties);properties.SetColor("_EmissionColor",new Color(.35f,.055f,.025f)*heardFlash);properties.SetFloat("_TentacleEnergyStrength",Mathf.Min(.5f,coreEmissionStrength*tentacleGlowRelativeToCore));tubes.SetPropertyBlock(properties);
        if(shadowOnly)foreach(var renderer in renderers)if(renderer)renderer.shadowCastingMode=ShadowCastingMode.ShadowsOnly;
    }
    void AnimateLeg(Transform hip,Transform knee,Transform foot,float phase,float strength,float upright,bool left)
    {
        if(!hip || !knee || !foot)return;
        float halfStep=Mathf.Lerp(.10f,.15f,RuntimeDriven?Mathf.InverseLerp(.2f,3,sampledSpeed):.25f);
        float z=-Mathf.Cos(phase)*halfStep*strength;
        float lift=Mathf.Max(0,-Mathf.Sin(phase))*footLift*strength;
        Vector2 target=new Vector2(z,.102f+lift-hip.localPosition.y);
        const float thigh=.32f,shin=.32f;
        float distance=Mathf.Clamp(target.magnitude,.12f,thigh+shin-.001f);
        float direction=Mathf.Atan2(-target.x,-target.y)*Mathf.Rad2Deg;
        float hipOffset=Mathf.Acos(Mathf.Clamp((thigh*thigh+distance*distance-shin*shin)/(2*thigh*distance),-1,1))*Mathf.Rad2Deg;
        float bend=180-Mathf.Acos(Mathf.Clamp((thigh*thigh+shin*shin-distance*distance)/(2*thigh*shin),-1,1))*Mathf.Rad2Deg;
        float hipAngle=Mathf.Lerp(left?3:0,direction+hipOffset,upright),kneeAngle=Mathf.Lerp(-7,-bend,upright);
        hip.localRotation=Quaternion.Euler(hipAngle,0,left?2:-2);
        knee.localPosition=Vector3.Lerp(new Vector3(0,-.32f,-.015f),new Vector3(0,-.32f,0),upright);
        knee.localRotation=Quaternion.Euler(kneeAngle,0,0);
        foot.localRotation=Quaternion.Euler(-(hipAngle+kneeAngle)*upright,0,0);
    }
    void Controls(Strand s,Vector3 target)
    {
        float r=s.random,a=r*Mathf.PI*2,i=s.index,phase=clock*(.55f+r*.4f)+i*1.7f;
        Vector3 p0,p1,p2,p3;
        float leftOpen=Mathf.SmoothStep(0,1,Mathf.Clamp01(opening*1.25f)),rightOpen=Mathf.SmoothStep(0,1,Mathf.Clamp01((opening-.12f)/.88f));
        float sway=Mathf.Sin(phase)*(s.group==6?.004f:s.group==4||s.group==5?.003f:0)*(1-tension);
        if(s.group==0) // Uneven raised back: a rib-like volume, no solid chest underneath.
        {float x=Mathf.Sin(i*2.4f)*.19f;p0=new Vector3(x*.65f,.8f,.055f);p1=new Vector3(x*1.25f,1.26f+r*.16f,.34f);p2=new Vector3(x*.9f,1.76f+r*.15f,.28f);p3=new Vector3(x*.5f+.025f,1.55f,-.065f);}
        else if(s.group==1)
        {float x=Mathf.Cos(a)*.13f;p0=new Vector3(-.02f+x,1.49f,.03f);p1=new Vector3(x+.055f,1.94f-r*.05f,-.20f);p2=new Vector3(-x*.6f+.045f,1.70f,-.49f);p3=new Vector3(x*.45f+.07f,1.43f,-.36f);float turn=Mathf.Clamp(Mathf.DeltaAngle(bodyYaw,headYaw),-50,50);var q=Quaternion.Euler(0,turn*.6f,-9);var center=new Vector3(.04f,1.57f,-.20f);p0=center+q*(p0-center);p1=center+q*(p1-center);p2=center+q*(p2-center);p3=center+q*(p3-center);
            // Tense upper fibres arch behind the body, with their tips pointing forwards (-Z).
            p1+=new Vector3(0,.10f,-.06f)*roar;p2+=new Vector3(Mathf.Cos(a)*.13f,.09f,-.10f)*roar;
            float quiver=Mathf.Sin(clock*(12+r*4)+i*2.1f)*.012f*tension;
            p1=Vector3.Lerp(p1,center+new Vector3(Mathf.Cos(a)*.18f,.12f,.35f),tension);
            p2=Vector3.Lerp(p2,center+new Vector3(Mathf.Cos(a)*.23f,.13f,.55f+Mathf.Sin(a)*.08f),tension);
            p3=Vector3.Lerp(p3,center+new Vector3(Mathf.Cos(a)*(.27f+r*.1f)+quiver,.10f,-.45f-r*.1f),tension);}
        else if(s.group==2 || s.group==3)
        {float side=(int)i%2==0?-1:1;float spread=side<0?leftOpen:rightOpen;float level=(int)i/2f/(s.group==2?5:3);float y=.88f+level*.59f;
            p0=new Vector3(side*.14f,.82f,.04f);p1=new Vector3(side*(.33f+spread*.12f),y-.02f,.12f);p2=new Vector3(side*(.28f+spread*.18f),1.53f+(side<0?.08f:0),-.06f);p3=new Vector3(side*.12f,1.53f,.075f);
            if(s.group==3){p0=new Vector3(-.22f,1.51f-i*.075f+r*.04f,-.16f);p1=new Vector3(-.11f,1.41f-i*.04f,-.23f);p2=new Vector3(.12f,1.01f+i*.045f,-.20f);p3=new Vector3(.24f,.9f+i*.07f,-.14f);
                p0=Vector3.Lerp(p0,new Vector3(side*.21f,1.49f+r*.07f,-.10f),spread);p1=Vector3.Lerp(p1,new Vector3(side*(.37f+r*.06f),1.43f,-.18f),spread);p2=Vector3.Lerp(p2,new Vector3(side*(.43f+r*.04f),1.02f,-.17f),spread);p3=Vector3.Lerp(p3,new Vector3(side*.16f,.83f,-.09f),spread);}}
        else if(s.group==4 || s.group==5)
        {bool longArm=s.group==4;float side=longArm?-1:1;float j=i/(longArm?7:5f);Vector3 jitter=new Vector3(Mathf.Sin(a)*.052f,Mathf.Cos(a)*.022f,Mathf.Cos(a)*.065f);
            p0=new Vector3(side*.26f,longArm?1.56f:1.42f,.045f)+jitter;
            p1=new Vector3(side*(longArm?.59f:.48f),longArm?1.08f:1.13f,-.075f)+jitter;
            p2=new Vector3(side*(longArm?.34f:.39f),longArm?.55f:.85f,.035f)+jitter;
            p3=new Vector3(side*(longArm?.49f:.41f),longArm?.06f:.66f,-.15f)+jitter;
            var reach=new Vector3(side*(.5f+(j-.5f)*.35f),.92f+(j-.5f)*.6f,-.58f-r*.38f);
            p1=Vector3.Lerp(p1,p0+new Vector3(side*.14f,-.20f,-.12f),extension);p2=Vector3.Lerp(p2,reach+new Vector3(side*.12f,-.15f,.15f),extension);p3=Vector3.Lerp(p3,reach,extension);
            Vector3 drawnBack=new Vector3(side*(longArm?.63f:.45f),p0.y-.04f,-.42f-r*.15f);
            p1=Vector3.Lerp(p1,new Vector3(side*(longArm?.58f:.49f),p0.y+.05f,.55f)+jitter,tension);
            p2=Vector3.Lerp(p2,new Vector3(side*(longArm?.78f:.60f),p0.y-.03f,.80f)+jitter*.4f,tension);
            p3=Vector3.Lerp(p3,drawnBack+jitter*.25f,tension);
            float contactReach=Mathf.Max(fusion,fusionReach);
            p2=Vector3.Lerp(p2,(p0+target)*.5f+new Vector3(side*.19f,0,-.16f),contactReach);p3=Vector3.Lerp(p3,target+new Vector3(Mathf.Cos(a)*.2f,(j-.5f)*.55f,Mathf.Sin(a)*.19f),contactReach);
            ApplyStrike(p0,side,jitter,ref p1,ref p2,ref p3);}
        else if(s.group==6)
        {float side=s.index%2==0?-1:1;p0=new Vector3(side*.19f,1.42f-r*.35f,.18f);p1=new Vector3(side*.41f,1-r*.3f,.26f);p2=new Vector3(side*(.44f+r*.23f),.2f,.1f);p3=new Vector3(side*(.39f+r*.29f),.035f,-.12f-r*.2f);
            // Open the two lower hanging bundles with the reaching arms, maintaining asymmetry.
            float lowerOpen=extension*opening;
            p1+=new Vector3(side*.13f,.025f,-.04f)*lowerOpen;
            p2+=new Vector3(side*(side<0?.45f:.37f),.10f,-.12f)*lowerOpen;
            p3+=new Vector3(side*(side<0?.58f:.48f),.12f,-.16f)*lowerOpen;
            // Lower bundles use the same backward arch and forward-pointing tips as upper bundles.
            Vector3 jitter=new Vector3(Mathf.Sin(a)*.035f,Mathf.Cos(a)*.025f,Mathf.Sin(a)*.035f);
            p1=Vector3.Lerp(p1,new Vector3(side*(side<0?.64f:.56f),p0.y-.08f,.55f)+jitter,tension);
            p2=Vector3.Lerp(p2,new Vector3(side*(side<0?.98f:.85f),p0.y-.16f,.80f)+jitter,tension);
            p3=Vector3.Lerp(p3,new Vector3(side*(side<0?.72f:.61f),p0.y-.14f,-.42f-r*.16f)+jitter*.5f,tension);
            ApplyStrike(p0,side,jitter,ref p1,ref p2,ref p3);}
        else if(s.group==7)
        {p0=new Vector3((r-.5f)*.22f,1.46f,.07f);p1=new Vector3((r-.5f)*.13f,1.34f,.03f);p2=new Vector3(.02f,1.26f,.01f);p3=new Vector3(.025f,1.2f,0);}
        else // Irregular strands bridge the two torsos, then grow around and over the target.
        {float side=i%2==0?-1:1;float level=(i/2f)/5;float progress=Mathf.Clamp01(fusion*1.5f-level*.35f);
            p0=new Vector3(side*.24f,1.31f-level*.36f,-.12f);Vector3 end=target+new Vector3(Mathf.Cos(a)*.23f,(level-.5f)*.83f,Mathf.Sin(a)*.23f);
            p1=Vector3.Lerp(p0,end+new Vector3(side*.33f,.16f,-.24f),progress);p2=Vector3.Lerp(p0,end+new Vector3(-side*.28f,-.13f,-.25f),progress);p3=Vector3.Lerp(p0,end,progress);}
        // Idle fibres sag below their roots, then curl outwards and upwards at the tips.
        float unfurl=IdleUnfurlAmount;
        if(unfurl>.001f&&(s.group==4||s.group==5||s.group==6))
        {
            float side=s.group==4?-1:s.group==5?1:s.index%2==0?-1:1;
            float wave=Mathf.Sin(clock*(.65f+r*.35f)+i*1.73f)*.0675f*idleSwayAmplitude;
            Vector3 outward=new Vector3(side*(.45f+.55f*Mathf.Abs(Mathf.Cos(a))),0,Mathf.Sin(a)*.85f).normalized;
            p1=Vector3.Lerp(p1,p0+Vector3.down*(.48f+r*.22f)+outward*.10f,unfurl);
            p2=Vector3.Lerp(p2,p0+Vector3.down*(.35f+r*.18f)+outward*(.57f+r*.28f),unfurl);
            p3=Vector3.Lerp(p3,p0+Vector3.up*(.18f+r*.40f+wave*.6f)+outward*(.78f+r*.42f)+new Vector3(wave,0,Mathf.Sin(phase*.9f)*.045f*idleSwayAmplitude),unfurl);
        }
        if(s.group==6)
        {
            float sweep=s.index==leftSweepIndex?leftSweep:s.index==rightSweepIndex?rightSweep:0;
            if(sweep>.0001f)
            {
                float side=s.index==leftSweepIndex?-1:1;
                Vector3 outward=root.InverseTransformVector(Vector3.right*side).normalized;
                Vector3 tip=p0+outward*(1.25f+r*.25f)+new Vector3(0,-.55f,-.15f);
                p1=Vector3.Lerp(p1,p0+Vector3.up*.20f+outward*.30f,sweep);
                p2=Vector3.Lerp(p2,tip+Vector3.up*.28f-outward*.20f,sweep);
                p3=Vector3.Lerp(p3,tip,sweep);
            }
        }
        if(shadowOnly&&(s.group==4||s.group==5||s.group==6))
        {
            float side=s.group==4?-1:s.group==5?1:s.index%2==0?-1:1;
            bool leading=side<0?doorwayLeadingArm==0:doorwayLeadingArm==1;
            float reach=Mathf.Clamp01(doorwayReach*(leading?1:.38f));
            p1=Vector3.Lerp(p1,p0+new Vector3(side*.18f,.08f,-.20f),reach);
            p2=Vector3.Lerp(p2,p0+new Vector3(side*(.34f+r*.12f),.02f,-.80f),reach);
            p3=Vector3.Lerp(p3,p0+new Vector3(side*(.24f+r*.18f),-.08f+r*.20f,-1.20f-r*.18f),reach);
        }
        float groupStand=s.group==1?Mathf.SmoothStep(0,1,Mathf.InverseLerp(.55f,1,standing)):s.group==4?Mathf.Clamp01(standing*1.35f):s.group==5?Mathf.SmoothStep(0,1,Mathf.InverseLerp(.25f,1,standing)):standing;
        Vector3 ground0=new Vector3(Mathf.Sin(a)*.3f,.06f,Mathf.Cos(a)*.24f);
        Vector3 ground3=new Vector3(Mathf.Sin(a+i)*(.55f+r*.65f),.04f,Mathf.Cos(a+i)*(.45f+r*.65f));
        curve[0]=Vector3.Lerp(ground0,p0,groupStand);
        curve[1]=Vector3.Lerp(ground0+new Vector3(Mathf.Cos(a)*.65f,.05f,Mathf.Sin(a)*.5f),p1,groupStand);
        curve[2]=Vector3.Lerp(ground3+new Vector3(-Mathf.Cos(a)*.35f,.025f,Mathf.Sin(a)*.2f),p2,groupStand);
        curve[3]=Vector3.Lerp(ground3,p3,groupStand);
        // Keep the tired torso slumped while the loose idle fibres remain lifted.
        if(slump>.001f&&s.group!=4&&s.group!=5&&s.group!=6)for(int c=0;c<4;c++)
        {float height=Mathf.Max(0,curve[c].y-.80f);curve[c].y-=height*.30f*slump;curve[c].z-=height*.15f*slump;curve[c].x+=Mathf.Sin(i*2.3f)*height*.08f*slump;}
        float flutter=(standing>.1f?1:.15f)*(1-tension*.65f);curve[1].x+=sway*flutter;curve[2].z+=sway*flutter;
        if(tension>.1f && attack && attack.Phase==RampagerAttack.AttackPhase.Windup)
        {float twitch=Mathf.Sin(attack.PhaseProgress*Mathf.PI)*.003f*tension;curve[1].x+=twitch;}
    }
    // Upper and lower attack bundles share windup, impact and recovery timing.
    void ApplyStrike(Vector3 anchor,float side,Vector3 jitter,ref Vector3 p1,ref Vector3 p2,ref Vector3 p3)
    {
        if(!attack || !RuntimeDriven || attack.Phase==RampagerAttack.AttackPhase.Ready)return;
        float progress=attack.PhaseProgress;
        bool windup=attack.Phase==RampagerAttack.AttackPhase.Windup;
        float strike=windup?Mathf.SmoothStep(0,1,Mathf.InverseLerp(.55f,1,progress)):1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.15f,1,progress));
        if(windup){float pull=Mathf.Sin(progress*Mathf.PI)*.18f;p2.z+=pull;p3.z+=pull;}
        var hit=root.InverseTransformPoint(attack.StrikePosition);
        p1=Vector3.Lerp(p1,Vector3.Lerp(anchor,hit,.25f)+new Vector3(side*.12f,.08f,.12f),strike*.8f);
        p2=Vector3.Lerp(p2,Vector3.Lerp(anchor,hit,.68f)+new Vector3(side*.07f,.03f,.06f),strike);
        p3=Vector3.Lerp(p3,hit+jitter*.12f,strike);
    }
    void WriteTube(int index,Strand s)
    {
        float radius=s.radius;if(s.group==8)radius*=Mathf.SmoothStep(0,1,fusion)*Mathf.Lerp(1,2.8f,fusion);if(s.group==7)radius*=standing;
        int glowGroup=s.group==6?0:s.group==4||s.group==5?1+Mathf.Min(2,Mathf.FloorToInt(s.index/(s.group==4?8f:6f)*3)):s.group==8?Mathf.Clamp(s.index/3,0,3):-1;
        float intensity=glowGroup<0?0:Mathf.Clamp01(glowEnergy*4-glowGroup);
        Color glow=new Color(1,.012f,.025f)*intensity;
        Vector3 previousRight=Vector3.zero;
        for(int r=0;r<Rings;r++)
        {
            float t=r/(float)(Rings-1);float u=1-t;
            Vector3 center=u*u*u*curve[0]+3*u*u*t*curve[1]+3*u*t*t*curve[2]+t*t*t*curve[3];
            if(s.group==8)
            {
                float level=(s.index/2f)/5,angle=s.random*Mathf.PI*2+t*(3.5f+s.random*7);
                float growth=Mathf.Clamp01(fusion*1.5f-level*.35f);
                Vector3 shell=fusionTargetLocal+new Vector3(Mathf.Cos(angle)*(.20f+s.random*.05f),(level-.5f)*1.1f+Mathf.Sin(t*(5+s.random*7)+s.random*5)*.17f,Mathf.Sin(angle)*.23f);
                center=Vector3.Lerp(curve[0],shell,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.03f,.55f,t))*growth);
            }
            // Different pitches and offsets avoid parallel rods and a uniform woven pattern.
            float ripple=Mathf.Sin(t*Mathf.PI)*(.014f+.018f*s.random)*(1-tension*.8f);
            center+=new Vector3(Mathf.Sin(t*13+s.index*2.1f)*ripple,Mathf.Sin(t*8+s.random*9)*ripple*.3f,Mathf.Cos(t*11+s.index)*ripple);
            center+=new Vector3(Mathf.Sin(t*11+s.random*8),.08f*Mathf.Sin(t*8),Mathf.Cos(t*10+s.index))*(.12f*(1-standing)*Mathf.Sin(t*Mathf.PI));
            Vector3 tangent=3*u*u*(curve[1]-curve[0])+6*u*t*(curve[2]-curve[1])+3*t*t*(curve[3]-curve[2]);
            if(tangent.sqrMagnitude<.000001f)tangent=Vector3.up;tangent.Normalize();
            Vector3 reference=r==0?strandFrameRights[index]:previousRight;
            if(reference.sqrMagnitude<.1f)reference=Vector3.right;
            Vector3 right=Vector3.ProjectOnPlane(reference,tangent).normalized;
            if(right.sqrMagnitude<.1f)right=Vector3.ProjectOnPlane(Vector3.forward,tangent).normalized;
            if(r==0)strandFrameRights[index]=right;
            previousRight=right;Vector3 up=Vector3.Cross(right,tangent);
            float taper=Mathf.Lerp(1,.23f,t*t);if(s.group==4||s.group==5||s.group==6)taper=Mathf.Lerp(taper,Mathf.Lerp(1,.015f,t*t*t),tension);
            float rad=radius*taper*(1+.12f*Mathf.Sin(t*12+s.random*8));
            for(int side=0;side<Sides;side++){int v=(index*Rings+r)*Sides+side;Vector3 normal=right*cos[side]+up*sin[side];vertices[v]=center+normal*rad;normals[v]=normal;vertexColours[v]=glow;}
        }
    }
    void ApplyVisibility()
    {
        if(!RuntimeDriven || !legacy)return;
        var effect=GetComponent<RampagerVisualEffects>();var source=effect && effect.VisualBody?effect.VisualBody:legacy.visibilitySource;
        if(!source)return;source.forceRenderingOff=true;root.gameObject.SetActive(source.enabled);
        // Preserve the existing darkness visibility/fades. Material properties stay shared.
        if(Mathf.Abs(previousAlpha-source.color.a)>.001f)
        {
            previousAlpha=source.color.a;
            for(int i=0;i<renderers.Length;i++)if(renderers[i]){Color c=rendererColours[i];c.a=source.color.a;properties.Clear();properties.SetColor("_Color",c);renderers[i].SetPropertyBlock(properties);}
        }
    }
    void Cleanup()
    {if(root)Release(root.gameObject);if(mesh)Release(mesh);if(temporaryMaterials!=null)foreach(var m in temporaryMaterials)Release(m);root=null;mesh=null;}
    void OnDisable(){Cleanup();if(legacy && legacy.model)legacy.model.gameObject.SetActive(true);}
}
