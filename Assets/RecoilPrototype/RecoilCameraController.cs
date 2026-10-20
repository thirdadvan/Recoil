using UnityEngine;

[ExecuteAlways,DefaultExecutionOrder(1100),DisallowMultipleComponent]
[RequireComponent(typeof(Camera)),AddComponentMenu("Recoil/Camera Controller")]
public sealed class RecoilCameraController : MonoBehaviour
{
    public Transform target;
    [Min(.1f),Tooltip("正交镜头可见区域的半高度。数值越大，镜头越远，看到的范围越大。")]
    public float viewSize=6.7f;
    [Tooltip("镜头中心相对跟随目标的偏移，单位米。")]
    public Vector2 followOffset=new Vector2(2,2.8f);
    [Min(0),Tooltip("跟随速度；0 表示立即跟随。")]
    public float followSpeed=4;
    [Header("Outdoor height zoom")]
    [Tooltip("仅在室外场景的镜头启用：按玩家脚底高度自动拉远。")] public bool outdoorHeightZoomEnabled;
    public float heightZoomStartY=0,heightZoomMaximumY=11.6f;
    [Min(1)] public float maximumHeightZoomMultiplier=1.4f;
    [Min(0),Tooltip("拉远/拉近平滑速度，0 表示立即变化。")] public float heightZoomSpeed=3;
    [Range(.5f,.9f),Tooltip("最高处人物在画面中的纵向位置：0.7 表示人物上方30%、下方70%。")]
    public float maximumHeightPlayerViewportY=.7f;
    [Header("Mouse look")]
    public bool mouseLookEnabled=true;
    [Min(0),Tooltip("鼠标引起的最大镜头偏移，单位米。0 表示不偏移。")]
    public float maximumMouseOffset=2.5f;
    [Tooltip("单独限制鼠标造成的向下镜头偏移，适合室内场景。") ]
    public bool limitDownwardMouseOffset=false;
    [Min(0),Tooltip("鼠标向下造成的最大镜头偏移，单位米；0 表示不能向下偏移。") ]
    public float maximumDownwardMouseOffset=.5f;
    [Range(0,.9f),Tooltip("鼠标与人物之间的世界距离乘以此系数，再限制到最大偏移。")]
    public float mouseLookSensitivity=.25f;
    public float cameraDepth=-10;
    [Tooltip("开启后限制镜头中心的高度；不限制横向移动。")]
    public bool limitVertical=true;
    public float minimumY=4.8f,maximumY=7;
    [Header("Indoor foot framing")]
    [Tooltip("保留脚底以下的画幅，避免鼠标观察或跟随延迟将地面推出屏幕；室外高度构图不受影响。")]
    public bool preserveFootFraming;
    [Range(0,.35f),Tooltip("脚底以下至少保留的屏幕高度比例，例如 0.1 表示10%。")]
    public float minimumFootScreenMargin=.1f;
    Camera cameraComponent;
    RecoilPrototype world;
    Transform cachedTarget;
    RecoilPlayerController targetPlayer;
    void RefreshTargetCache(){if(cachedTarget!=target){cachedTarget=target;targetPlayer=target?target.GetComponent<RecoilPlayerController>():null;}}
    Vector3 FollowPosition
    {get{RefreshTargetCache();return targetPlayer?(Vector3)targetPlayer.CameraFollowPosition:target.position;}}
    public void Initialize(RecoilPrototype context,Transform defaultTarget)
    {world=context;if(!target)target=defaultTarget;ApplySize();}
    float HeightProgress(float feetY)
    {return outdoorHeightZoomEnabled?Mathf.Clamp01((feetY-heightZoomStartY)/Mathf.Max(.01f,heightZoomMaximumY-heightZoomStartY)):0;}
    float TargetFeetY()
    {RefreshTargetCache();return targetPlayer?targetPlayer.Position.y-targetPlayer.BodyHeight*.5f:target.position.y;}
    public float HeightFramingY(float targetY,float feetY,float halfHeight)
    {
        float y=targetY+followOffset.y;
        if(limitVertical)y=Mathf.Clamp(y,Mathf.Min(minimumY,maximumY),Mathf.Max(minimumY,maximumY));
        if(!outdoorHeightZoomEnabled)return y;
        float framedY=targetY+(1-2*maximumHeightPlayerViewportY)*halfHeight;
        y=Mathf.Lerp(y,framedY,HeightProgress(feetY));
        if(limitVertical)y=Mathf.Clamp(y,Mathf.Min(minimumY,maximumY),Mathf.Max(minimumY,maximumY));
        return Mathf.Max(y,framedY);
    }
    float FramingFeetY
    {get{var follow=FollowPosition;return follow.y-(targetPlayer?targetPlayer.BodyHeight*.5f:0);}}
    public float ConstrainFootFraming(float cameraY,float feetY,float halfHeight)
    {
        if(!preserveFootFraming || outdoorHeightZoomEnabled)return cameraY;
        // Viewport foot position = .5 + (feetY - cameraY) / (2 * halfHeight).
        return Mathf.Min(cameraY,feetY+Mathf.Max(.1f,halfHeight)*(1-2*Mathf.Clamp(minimumFootScreenMargin,0,.35f)));
    }
    public float ViewSizeAtHeight(float feetY)
    {
        float baseSize=Mathf.Max(.1f,viewSize);
        if(!outdoorHeightZoomEnabled)return baseSize;
        float progress=HeightProgress(feetY);
        return baseSize*Mathf.Lerp(1,Mathf.Max(1,maximumHeightZoomMultiplier),progress);
    }
    void ApplySize(float dt=0,bool snap=true)
    {
        if(!cameraComponent)cameraComponent=GetComponent<Camera>();
        if(!cameraComponent)return;
        cameraComponent.orthographic=true;float size=Mathf.Max(.1f,viewSize);
        if(Application.isPlaying && target){float feetY=TargetFeetY();size=ViewSizeAtHeight(feetY);}
        float blend=snap || heightZoomSpeed<=0?1:1-Mathf.Exp(-heightZoomSpeed*Mathf.Max(0,dt));
        cameraComponent.orthographicSize=Mathf.Lerp(cameraComponent.orthographicSize,size,blend);
    }
    Vector2 MouseOffset(Vector3 pointer)
    {
        if(!mouseLookEnabled || maximumMouseOffset<=0 || !cameraComponent || !cameraComponent.pixelRect.Contains(pointer))return Vector2.zero;
        pointer.z=Vector3.Dot(FollowPosition-transform.position,transform.forward);
        // Measure from the settled base framing, not the camera's previous scene position.
        // Otherwise mouse look feeds back into itself and keeps drifting after a snap.
        var worldPointer=cameraComponent.ScreenToWorldPoint(pointer)+TargetPosition()-transform.position;
        var delta=(Vector2)(worldPointer-FollowPosition);
        var offset=Vector2.ClampMagnitude(delta*Mathf.Clamp(mouseLookSensitivity,0,.9f),Mathf.Max(0,maximumMouseOffset));
        if(limitDownwardMouseOffset)offset.y=Mathf.Max(offset.y,-Mathf.Max(0,maximumDownwardMouseOffset));
        return offset;
    }
    Vector3 TargetPosition(bool mouseLook=false)
    {
        Vector3 follow=FollowPosition;
        Vector3 position=follow+(Vector3)followOffset;
        float halfHeight=cameraComponent?cameraComponent.orthographicSize:Mathf.Max(.1f,viewSize);
        position.y=HeightFramingY(follow.y,TargetFeetY(),halfHeight);
        if(mouseLook)position+=(Vector3)MouseOffset(Input.mousePosition);
        // Mouse look may reveal more above, but cannot push the player past the height framing cap.
        if(outdoorHeightZoomEnabled && HeightProgress(TargetFeetY())>0)
            position.y=Mathf.Max(position.y,follow.y+(1-2*maximumHeightPlayerViewportY)*halfHeight);
        position.y=ConstrainFootFraming(position.y,FramingFeetY,halfHeight);
        position.z=cameraDepth;return position;
    }
    public void SnapToTarget(){if(isActiveAndEnabled && target){ApplySize();transform.position=TargetPosition(true);}}
    // Start runs after scene Awake has restored the destination spawn and player state.
    // Standalone scene starts also settle before the first rendered frame.
    void Start(){if(Application.isPlaying)SnapToTarget();}
    void LateUpdate()
    {
        if(!Application.isPlaying){ApplySize();return;}
        if(!target)return;
        if(world && !world.Playing)return;
        float dt=world?world.FrameDelta:Time.deltaTime;
        ApplySize(dt,false);
        float blend=followSpeed<=0?1:1-Mathf.Exp(-followSpeed*dt);
        var next=Vector3.Lerp(transform.position,TargetPosition(true),blend);
        // Apply the safety margin after damping too, especially when landing or descending stairs.
        next.y=ConstrainFootFraming(next.y,FramingFeetY,cameraComponent.orthographicSize);
        transform.position=next;
    }
    void OnValidate(){viewSize=Mathf.Max(.1f,viewSize);followSpeed=Mathf.Max(0,followSpeed);maximumHeightZoomMultiplier=Mathf.Max(1,maximumHeightZoomMultiplier);heightZoomMaximumY=Mathf.Max(heightZoomStartY+.01f,heightZoomMaximumY);heightZoomSpeed=Mathf.Max(0,heightZoomSpeed);maximumHeightPlayerViewportY=Mathf.Clamp(maximumHeightPlayerViewportY,.5f,.9f);minimumFootScreenMargin=Mathf.Clamp(minimumFootScreenMargin,0,.35f);maximumMouseOffset=Mathf.Max(0,maximumMouseOffset);maximumDownwardMouseOffset=Mathf.Max(0,maximumDownwardMouseOffset);mouseLookSensitivity=Mathf.Clamp(mouseLookSensitivity,0,.9f);ApplySize();}
}
