using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent,AddComponentMenu("Recoil/Story/Service Store Clerk Encounter")]
public sealed class ServiceStoreEncounter : MonoBehaviour
{
    public RecoilPrototype world;
    [FormerlySerializedAs("hiddenMonster")] public RampagerController monster;
    public NpcDialogueSequence introduction,reassurance;
    [Tooltip("吧台前沿的位置。") ] public Transform approachPoint;
    [Min(.1f),Tooltip("进入此距离且怪物出现在镜头内时触发安抚台词，单位为主角身高。") ]
    public float reassuranceDistanceInBodies=5;
    [Min(0)] public float sleepBeforeFade=2;
    [Min(.01f)] public float fadeDuration=1.25f;
    public const string CompletedFlag="store_clerk_reassured";
    public string CompletionFlag {get{return CompletedFlag;}}
    public string stealthHint="Use A/D to stealth walk. Hold SHIFT to run.";
    enum Phase { Waiting,Introduction,Approach,Attacking,Reassurance,Complete }
    Phase phase;
    SpriteRenderer[] monsterRenderers;
    bool[] rendererDefaults;float[] originalAlpha;
    bool fading;float fadeElapsed;
    void Start()
    {
        if(!world)world=GetComponentInParent<RecoilPrototype>();
        if(!world || !monster || !introduction || !reassurance || !approachPoint)
        {Debug.LogError("Store encounter is missing scene references.",this);enabled=false;return;}
        monsterRenderers=monster.GetComponentsInChildren<SpriteRenderer>(true);
        rendererDefaults=new bool[monsterRenderers.Length];originalAlpha=new float[monsterRenderers.Length];
        for(int i=0;i<monsterRenderers.Length;i++){rendererDefaults[i]=monsterRenderers[i].enabled;originalAlpha[i]=monsterRenderers[i].color.a;}
        world.RunReset+=ResetEncounter;ResetEncounter();
    }
    public void ResetEncounter()
    {
        if(!monster)return;
        fading=false;fadeElapsed=0;monster.enabled=true;monster.ResetController();SetMonsterVisible(true);
        phase=world.HasItem(CompletedFlag)?Phase.Complete:Phase.Waiting;
        if(phase==Phase.Complete){monster.SleepForStory();monster.enabled=false;SetMonsterVisible(false);}
    }
    void SetMonsterVisible(bool visible)
    {
        if(monsterRenderers==null)return;
        for(int i=0;i<monsterRenderers.Length;i++)if(monsterRenderers[i])
        {monsterRenderers[i].enabled=visible && rendererDefaults[i];var color=monsterRenderers[i].color;color.a=originalAlpha[i];monsterRenderers[i].color=color;}
        monster.HideStatusLabel=!visible;
    }
    bool ReassuranceInReach()
    {
        if(Mathf.Abs(world.Player.Position.x-approachPoint.position.x)>world.Player.StandingHeight*reassuranceDistanceInBodies || !world.ViewCamera)return false;
        var screen=world.ViewCamera.WorldToViewportPoint(monster.Position+Vector2.up*.45f);
        return screen.z>0 && screen.x>=0 && screen.x<=1 && screen.y>=0 && screen.y<=1;
    }
    void Update()
    {
        if(!world || !world.Playing)return;
        if(phase==Phase.Waiting)
        {
            monster.WatchForStory();phase=Phase.Introduction;
            if(!world.BeginConversation(introduction,()=>{phase=Phase.Approach;world.ShowDialogue(stealthHint,8);}))phase=Phase.Waiting;
        }
        else if(phase==Phase.Approach)
        {
            if(monster.State==RampagerController.Behaviour.Attack){phase=Phase.Attacking;return;}
            if(ReassuranceInReach())
            {
                phase=Phase.Reassurance;
                world.BeginConversation(reassurance,()=>{phase=Phase.Complete;world.RememberProgress(CompletedFlag);},line=>
                {
                    monster.SleepForStory();monster.enabled=false;monster.HideStatusLabel=true;
                    fading=true;fadeElapsed=0;
                    var feedback=world.Player.GetComponent<PlayerGazeFeedback>();if(feedback)feedback.ResetFeedback();
                });
            }
        }
    }
    void LateUpdate()
    {
        if(phase==Phase.Approach && monster.State==RampagerController.Behaviour.Attack)phase=Phase.Attacking;
        // Apply after the world's SyncVisuals, which refreshes the monster's body/eye colours.
        // Unscaled time lets the visible sleep/fade finish while dialogue pauses gameplay.
        AdvanceFade(Time.unscaledDeltaTime);
    }
    void AdvanceFade(float dt)
    {
        if(!fading)return;
        fadeElapsed+=Mathf.Max(0,dt);
        float progress=Mathf.Clamp01((fadeElapsed-sleepBeforeFade)/Mathf.Max(.01f,fadeDuration));
        float alpha=1-Mathf.SmoothStep(0,1,progress);
        for(int i=0;i<monsterRenderers.Length;i++)if(monsterRenderers[i])
        {var color=monsterRenderers[i].color;color.a=originalAlpha[i]*alpha;monsterRenderers[i].color=color;}
        if(progress>=1){fading=false;SetMonsterVisible(false);}
    }
    void OnDestroy(){if(world)world.RunReset-=ResetEncounter;}
    void OnDrawGizmosSelected()
    {if(approachPoint){Gizmos.color=Color.cyan;Gizmos.DrawWireSphere(approachPoint.position,(world && world.Player?world.Player.StandingHeight:1.65f)*reassuranceDistanceInBodies);}}
}
