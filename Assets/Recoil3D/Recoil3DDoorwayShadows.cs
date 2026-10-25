using UnityEngine;

// Hidden shadow casters stay at the entrance and reach/pound at the unseen door.
// Presentation only: no monster AI, travel or collision is added inside the store.
[DisallowMultipleComponent,AddComponentMenu("Recoil/3D/Pounding Doorway Shadows")]
public sealed class Recoil3DDoorwayShadows : MonoBehaviour
{
    public Transform[] silhouettes=new Transform[0];
    [Min(.5f)] public float strikeCycle=3;
    [Min(0)] public float stagger=.85f;
    [Range(30,140)] public float reachAngle=105;
    [Min(.1f)] public float shoulderHeightAboveArmCentre=.65f;
    public float Elapsed {get;private set;}
    sealed class Pose
    {
        public RampagerTentacleVisual coaler;
        public Transform root;public Vector3 position;public Quaternion rootRotation;
        public Transform[] arms;public Vector3[] positions;public Quaternion[] rotations;
    }
    Pose[] poses;RecoilPrototype world;
    public void RebuildPoses(){poses=null;Initialize();}
    public void Initialize()
    {
        if(poses!=null)return;
        world=GetComponentInParent<RecoilPrototype>();poses=new Pose[silhouettes.Length];
        for(int i=0;i<poses.Length;i++)
        {
            var root=silhouettes[i];if(!root)continue;
            var arms=new System.Collections.Generic.List<Transform>();
            foreach(var arm in root.GetComponentsInChildren<Transform>(true))if(arm.name=="Limp arm shadow")arms.Add(arm);
            var pose=new Pose{coaler=root.GetComponentInChildren<RampagerTentacleVisual>(true),root=root,position=root.localPosition,rootRotation=root.localRotation,arms=arms.ToArray()};
            pose.positions=new Vector3[pose.arms.Length];pose.rotations=new Quaternion[pose.arms.Length];
            for(int j=0;j<pose.arms.Length;j++){pose.positions[j]=pose.arms[j].localPosition;pose.rotations[j]=pose.arms[j].localRotation;}
            poses[i]=pose;
        }
    }
    void Start(){Initialize();if(world)world.RunReset+=ResetShadows;ResetShadows();}
    public void ResetShadows()
    {
        Initialize();Elapsed=0;
        foreach(var pose in poses)if(pose!=null&&pose.root)
        {
            pose.root.gameObject.SetActive(true);pose.root.localPosition=pose.position;pose.root.localRotation=pose.rootRotation;
            if(pose.coaler){pose.coaler.doorwayReach=0;pose.coaler.doorwayLeadingArm=0;pose.coaler.UpdatePose(0,true);}
            for(int j=0;j<pose.arms.Length;j++)if(pose.arms[j]){pose.arms[j].localPosition=pose.positions[j];pose.arms[j].localRotation=pose.rotations[j];}
        }
    }
    void Update(){if(world&&world.Playing)Advance(world.FrameDelta);}
    static float Reach(float phase)
    {
        if(phase<.30f)return Mathf.SmoothStep(0,1,phase/.30f);
        if(phase<.48f)return Mathf.Lerp(1,.45f,Mathf.SmoothStep(0,1,(phase-.30f)/.18f));
        if(phase<.56f)return Mathf.Lerp(.45f,1.08f,Mathf.SmoothStep(0,1,(phase-.48f)/.08f));
        if(phase<.66f)return 1.08f;
        return Mathf.Lerp(1.08f,0,Mathf.SmoothStep(0,1,(phase-.66f)/.34f));
    }
    public void Advance(float dt)
    {
        Initialize();Elapsed+=Mathf.Max(0,dt);
        for(int i=0;i<poses.Length;i++)
        {
            var pose=poses[i];if(pose==null||!pose.root)continue;
            float time=Mathf.Max(0,Elapsed-i*stagger),cycle=Mathf.Max(.5f,strikeCycle+i*.35f);
            float phase=(time%cycle)/cycle,reach=Reach(phase);int leading=(int)(time/cycle)%2;
            pose.root.gameObject.SetActive(true);pose.root.localPosition=pose.position;
            pose.root.localRotation=pose.rootRotation*Quaternion.Euler(reach*5.5f,0,Mathf.Sin(time*1.5f+i)*.7f);
            if(pose.coaler)pose.coaler.AdvanceDoorway(reach,leading,dt);
            for(int j=0;j<pose.arms.Length;j++)if(pose.arms[j])
            {
                float amount=j==leading?reach:reach*.38f;
                var pivot=pose.positions[j]+Vector3.up*shoulderHeightAboveArmCentre;
                var rotation=Quaternion.Euler(-reachAngle*amount,0,0);
                pose.arms[j].localPosition=pivot+rotation*(pose.positions[j]-pivot);
                pose.arms[j].localRotation=rotation*pose.rotations[j];
            }
        }
    }
    void OnDestroy(){if(world)world.RunReset-=ResetShadows;}
}
