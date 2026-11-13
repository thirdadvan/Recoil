using UnityEngine;

[DefaultExecutionOrder(850),DisallowMultipleComponent]
public sealed class WarehouseChildAnimation : MonoBehaviour
{
    public enum RestAction { Crying, Eating }
    public RestAction restAction=RestAction.Crying;
    public Animator animator;
    public EscortChild escort;
    public GameObject food;
    [Min(.01f)] public float blendTime=.18f;
    public bool manuallyDriven;
    [Min(.1f),Tooltip("实际移动达到此速度时，以 1 倍速度播放小步行走动画。数值越小，步频越快。")]
    public float walkSpeedAtOneX=.65f;
    [Min(1),Tooltip("快跑时允许更高步频，减少原先 1.9 倍上限造成的滑步。")]
    public float maximumWalkPlayback=2f;
    [Min(.1f)] public float jogSpeedAtOneX=2.1f;
    Vector3 previous;float eatingUntil=-1;string current="";
    public bool TurningForHandhold {get;private set;}
    public string CurrentAction {get{return current;}}
    void Awake(){if(!animator)animator=GetComponent<Animator>();if(!escort)escort=GetComponentInParent<EscortChild>();if(escort)foreach(var sprite in escort.GetComponentsInChildren<SpriteRenderer>(true))sprite.forceRenderingOff=true;previous=escort?escort.transform.position:transform.position;}
    void OnEnable(){current="";previous=escort?escort.transform.position:transform.position;}
    public void StartEating(float seconds=0){restAction=RestAction.Eating;eatingUntil=seconds>0?Time.time+seconds:-1;}
    public void StopEating(){restAction=RestAction.Crying;eatingUntil=-1;}
    void Update(){if(!manuallyDriven)Advance(Time.deltaTime);}
    public void Advance(float dt)
    {
        if(!animator||dt<=0)return;
        if(eatingUntil>0&&Time.time>=eatingUntil)StopEating();
        var position=escort?escort.transform.position:transform.position;
        var delta=position-previous;previous=position;
        bool following=escort&&escort.Following;
        float travelSpeed=new Vector2(delta.x,delta.y).magnitude/Mathf.Max(.0001f,dt);
        bool running=following&&!escort.WalkingStairs&&travelSpeed>1.7f&&animator.HasState(0,Animator.StringToHash("FollowRun"));
        string state=following?(delta.sqrMagnitude>.00001f?(running?"FollowRun":"FollowWalk"):"FollowIdle"):(restAction==RestAction.Eating?"Eating":"Crying");
        if(state!=current){animator.CrossFadeInFixedTime(state,blendTime);current=state;}
        // A fixed model placement; only playback cadence follows actual travelling speed.
        animator.speed=state=="FollowRun"?Mathf.Clamp(travelSpeed/Mathf.Max(.1f,jogSpeedAtOneX),.7f,1.6f):state=="FollowWalk"?Mathf.Clamp(travelSpeed/Mathf.Max(.1f,walkSpeedAtOneX),.65f,Mathf.Max(1,maximumWalkPlayback)):1;
        if(food)food.SetActive(state=="Eating");
        float requestedYaw=following?(Mathf.Abs(delta.x)>.0001f?(delta.x>0?90:270):transform.localEulerAngles.y):180;
        AdvanceFacing(requestedYaw,dt,following);
    }
    public void AdvanceFacing(float requestedYaw,float dt,bool holding)
    {
        var target=Quaternion.Euler(0,requestedYaw,0);
        TurningForHandhold=holding&&Quaternion.Angle(transform.localRotation,target)>20f;
        // Recompute the shortest path from the current pose, including mid-turn reversals.
        transform.localRotation=Quaternion.RotateTowards(transform.localRotation,target,240*dt);
    }

}
