using UnityEngine;

// One continuous, frame-rate-independent speed budget shared by all nearby grabbers.
[DisallowMultipleComponent,AddComponentMenu("Recoil/Player Grab Pressure")]
public sealed class PlayerGrabPressure : MonoBehaviour
{
    [Tooltip("每 0.5 秒扣除的初始速度比例：一、二、三、四只及以上。")]
    public Vector4 lossPerHalfSecond=new Vector4(.16f,.288f,.352f,.4f);
    [Range(0,1)] public float maximumDarkness=.93f;
    [Min(0),Tooltip("实际移动每米恢复初始速度的比例；0.3 表示每 0.1 米恢复 3%。") ]
    public float recoveryPerMetre=.3f;
    public int GrabberCount {get;private set;}
    // The budget may fall below the movement floor; only physical movement is clamped.
    public float SpeedBudgetMultiplier {get;private set;}=1;
    public float SpeedMultiplier {get{return Mathf.Max(MinimumSpeedMultiplier,SpeedBudgetMultiplier);}}
    public float Pressure {get{return 1-SpeedBudgetMultiplier;}}
    public const float MinimumSpeedMultiplier=.2f;
    float movementModeSpeed;
    float minimumSpeedElapsed,minimumSpeedFrameTime;
    const float MinimumSpeedDeathDelay=1.5f;
    // Preserve the absolute speed reduction when changing between walk and stealth.
    public void SetMovementMode(float speed)
    {
        speed=Mathf.Max(.001f,speed);
        if(movementModeSpeed>0 && Mathf.Abs(speed-movementModeSpeed)>.000001f)
            SpeedBudgetMultiplier=Mathf.Clamp01((movementModeSpeed*SpeedMultiplier+speed-movementModeSpeed)/speed);
        movementModeSpeed=speed;
    }
    RecoilPrototype world;
    public void Initialize(RecoilPrototype context){world=context;ResetPressure();}
    public static float RateForCount(int count,Vector4 losses)
    {return count<=0?0:Mathf.Clamp01(count==1?losses.x:count==2?losses.y:count==3?losses.z:losses.w)*2;}
    public void ResetPressure()
    {
        GrabberCount=0;SpeedBudgetMultiplier=1;movementModeSpeed=0;
        minimumSpeedElapsed=minimumSpeedFrameTime=0;
        if(world)foreach(var enemy in world.Rampagers)if(enemy){var grab=enemy.GetComponent<RampagerGrab>();if(grab)grab.Release();}
    }
    public void Simulate(float dt)
    {
        if(!world || !world.Playing || dt<=0)return;
        GrabberCount=0;
        foreach(var enemy in world.Rampagers)if(enemy)
        {var grab=enemy.GetComponent<RampagerGrab>();if(grab && grab.Evaluate())GrabberCount++;}
        AdvanceBudget(dt,GrabberCount);

    }
    // Called after collision resolution so blocked input cannot earn recovery.
    // Includes gun recoil and vertical/depth movement, but never reset/teleport distance.
    public void CompleteMovement(float distance)
    {
        RecoverDistance(distance);
        // Resolve recovery first: escaping or moving above the floor cancels the countdown.
        if(GrabberCount>0 && SpeedBudgetMultiplier<=MinimumSpeedMultiplier+.000001f)
            minimumSpeedElapsed+=minimumSpeedFrameTime;
        else minimumSpeedElapsed=0;
        minimumSpeedFrameTime=0;
        if(world && world.Playing && minimumSpeedElapsed>=MinimumSpeedDeathDelay-.000001f)
            world.DragPlayerToDeath();
    }
    public void RecoverDistance(float distance)
    {
        SpeedBudgetMultiplier=Mathf.Clamp01(SpeedBudgetMultiplier+Mathf.Max(0,distance)*Mathf.Max(0,recoveryPerMetre));
    }
    public void AdvanceBudget(float dt,int count)
    {
        GrabberCount=Mathf.Max(0,count);
        dt=Mathf.Max(0,dt);
        float rate=RateForCount(GrabberCount,lossPerHalfSecond);
        // Count only the part of this frame with a budget at or below 20%.
        float timeToMinimum=rate>0?Mathf.Max(0,SpeedBudgetMultiplier-MinimumSpeedMultiplier)/rate:float.PositiveInfinity;
        minimumSpeedFrameTime=GrabberCount>0?Mathf.Max(0,dt-timeToMinimum):0;
        SpeedBudgetMultiplier=Mathf.Max(0,SpeedBudgetMultiplier-rate*dt);
    }
    void OnGUI()
    {
        if(!world || Pressure<=0 || (!world.Playing && !world.ConversationKeepsAudioPlaying))return;
        var oldMatrix=GUI.matrix;var oldColour=GUI.color;int oldDepth=GUI.depth;
        GUI.matrix=Matrix4x4.identity;GUI.depth=100;GUI.color=new Color(0,0,0,Pressure*maximumDarkness);
        GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);
        GUI.matrix=oldMatrix;GUI.color=oldColour;GUI.depth=oldDepth;
    }
    void OnDisable(){ResetPressure();}
}
