using UnityEngine;

[ExecuteAlways,DefaultExecutionOrder(1100),DisallowMultipleComponent]
[RequireComponent(typeof(Camera)),AddComponentMenu("Recoil/Camera Controller")]
public sealed class RecoilCameraController : MonoBehaviour
{
    public Transform target;
    [Min(.1f),Tooltip("正交镜头可见区域的半高度。数值越大，镜头越远，看到的范围越大。")]
    public float viewSize=6.7f;
    [Header("3D perspective presentation")] public bool perspective3D;
    [Tooltip("将跟随构图定位在角色所在 Z=0 平面，补偿透视镜头俯角。只影响3D镜头。 ")] public bool frameOnGameplayPlane;
    [Tooltip("启用3D构图参数；只影响透视场景。俯仰和下移在跟随计算中持续保留。 ")] public bool use3DComposition;
    [Range(-20,20),Tooltip("负值仰视（Pitch up），正值俯视。 ")] public float pitch3D;
    [Tooltip("构图中心的垂直平移，负值为Pan down，单位米。 ")] public float panY3D;
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
    [Tooltip("开启后限制镜头中心的高度；横向限制使用下方的室内边墙设置。") ]
    public bool limitVertical=true;
    public float minimumY=4.8f,maximumY=7;
    [Header("Indoor foot framing")]
    [Tooltip("保留脚底以下的画幅，避免鼠标观察或跟随延迟将地面推出屏幕；室外高度构图不受影响。")]
    public bool preserveFootFraming;
    [Range(0,.35f),Tooltip("脚底以下至少保留的屏幕高度比例，例如 0.1 表示10%。")]
    public float minimumFootScreenMargin=.1f;
    [Header("Indoor horizontal walls")]
    [Tooltip("让完整画幅保持在左右墙内，包括鼠标偏移和跟随平滑。") ] public bool limitHorizontal;
    public BoxCollider2D leftBoundary,rightBoundary;
    [Tooltip("房间比当前画幅窄时自动收近镜头。") ] public bool fitNarrowRooms=true;
    [Tooltip("限制画幅所在的最远操作面；当前平台和楼梯操作面为 Z=0.7。") ] public float horizontalBoundaryPlaneZ=.7f;
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
        if(perspective3D && use3DComposition){var angle=Quaternion.Euler(pitch3D,transform.eulerAngles.y,0);if(Quaternion.Angle(transform.rotation,angle)>.001f)transform.rotation=angle;}
        cameraComponent.orthographic=!perspective3D;float size=Mathf.Max(.1f,viewSize);
        if(Application.isPlaying && target){float feetY=TargetFeetY();size=ViewSizeAtHeight(feetY);}
        float wallSize=MaximumWallViewSize();size=Mathf.Min(size,wallSize);
        float blend=snap || heightZoomSpeed<=0?1:1-Mathf.Exp(-heightZoomSpeed*Mathf.Max(0,dt));
        cameraComponent.orthographicSize=Mathf.Min(wallSize,Mathf.Lerp(cameraComponent.orthographicSize,size,blend));
        if(perspective3D)cameraComponent.fieldOfView=2*Mathf.Atan(cameraComponent.orthographicSize/Mathf.Max(1,Mathf.Abs(cameraDepth)))*Mathf.Rad2Deg;
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
    Vector3 PerspectivePlaneShift
    {get{if(!perspective3D || !frameOnGameplayPlane || Mathf.Abs(transform.forward.z)<.01f)return Vector3.zero;var shift=-transform.forward*(-cameraDepth/transform.forward.z);shift.z=0;return shift;}}
    Vector3 TargetPosition(bool mouseLook=false)
    {
        Vector3 follow=FollowPosition;
        Vector3 position=follow+(Vector3)followOffset;
        float halfHeight=cameraComponent?cameraComponent.orthographicSize:Mathf.Max(.1f,viewSize);
        position.y=HeightFramingY(follow.y,TargetFeetY(),halfHeight);
        if(perspective3D && use3DComposition)position.y+=panY3D;
        if(mouseLook)position+=(Vector3)MouseOffset(Input.mousePosition);
        // Mouse look may reveal more above, but cannot push the player past the height framing cap.
        if(outdoorHeightZoomEnabled && HeightProgress(TargetFeetY())>0)
            position.y=Mathf.Max(position.y,follow.y+(1-2*maximumHeightPlayerViewportY)*halfHeight);
        position.y=ConstrainFootFraming(position.y,FramingFeetY,halfHeight);
        position.z=cameraDepth;return ConstrainHorizontal(position+PerspectivePlaneShift);
    }
    bool WallLimits(out float left,out float right)
    {
        left=right=0;if(!limitHorizontal||!leftBoundary||!rightBoundary)return false;
        left=leftBoundary.bounds.max.x;right=rightBoundary.bounds.min.x;return right>left+.01f;
    }
    float cachedWallWidth=-1,cachedWallAspect,cachedWallDepth,cachedWallPlane,cachedWallUpper,cachedWallSize;
    Quaternion cachedWallRotation;
    bool cachedWallPerspective;
    float ProjectedHorizontalSpan(float size,float aspect)
    {
        float min=float.PositiveInfinity,max=float.NegativeInfinity;
        for(int i=0;i<8;i++)
        {
            float distance=(i<4?0:horizontalBoundaryPlaneZ)-cameraDepth;
            float x=(i%2==0?-1:1)*size*aspect,y=((i%4)/2==0?-1:1)*size;
            Vector3 origin=Vector3.zero,direction;
            if(perspective3D)direction=transform.rotation*new Vector3(x/Mathf.Max(1,Mathf.Abs(cameraDepth)),y/Mathf.Max(1,Mathf.Abs(cameraDepth)),1);
            else{origin=transform.rotation*new Vector3(x,y,0);direction=transform.forward;}
            if(direction.z<=.00001f)return float.PositiveInfinity;
            float projected=origin.x+(distance-origin.z)*direction.x/direction.z;
            min=Mathf.Min(min,projected);max=Mathf.Max(max,projected);
        }
        return max-min;
    }
    float MaximumWallViewSize()
    {
        float left,right;if(!fitNarrowRooms||!WallLimits(out left,out right)||!cameraComponent)return float.PositiveInfinity;
        float width=right-left,aspect=Mathf.Max(.1f,cameraComponent.aspect);
        float upper=Mathf.Max(.1f,viewSize)*(outdoorHeightZoomEnabled?Mathf.Max(1,maximumHeightZoomMultiplier):1);
        // Cache this calculation; only room width, projection, rotation or window size can change it.
        if(cachedWallWidth==width&&cachedWallAspect==aspect&&cachedWallDepth==cameraDepth&&cachedWallPlane==horizontalBoundaryPlaneZ&&cachedWallUpper==upper&&cachedWallRotation==transform.rotation&&cachedWallPerspective==perspective3D)return cachedWallSize;
        cachedWallWidth=width;cachedWallAspect=aspect;cachedWallDepth=cameraDepth;cachedWallPlane=horizontalBoundaryPlaneZ;cachedWallUpper=upper;cachedWallRotation=transform.rotation;cachedWallPerspective=perspective3D;
        if(ProjectedHorizontalSpan(upper,aspect)<=width)return cachedWallSize=float.PositiveInfinity;
        // Solve against all four projected corners, including the authored camera yaw and pitch.
        float low=0,high=upper;
        for(int i=0;i<20;i++){float mid=(low+high)*.5f;if(ProjectedHorizontalSpan(mid,aspect)<=width)low=mid;else high=mid;}
        return cachedWallSize=Mathf.Max(.01f,low*.99995f);
    }
    public Vector3 ConstrainHorizontal(Vector3 position)
    {
        float left,right;if(!WallLimits(out left,out right)||!cameraComponent)return position;
        float min=float.PositiveInfinity,max=float.NegativeInfinity;
        for(int i=0;i<8;i++)
        {
            var ray=cameraComponent.ViewportPointToRay(new Vector3(i%2,(i%4)/2,0));
            ray.origin+=position-transform.position;
            if(Mathf.Abs(ray.direction.z)<.00001f)continue;
            float distance=((i<4?0:horizontalBoundaryPlaneZ)-ray.origin.z)/ray.direction.z;
            if(distance<0)continue;
            float offset=ray.GetPoint(distance).x-position.x;min=Mathf.Min(min,offset);max=Mathf.Max(max,offset);
        }
        if(float.IsInfinity(min)||float.IsInfinity(max))return position;
        float low=left-min,high=right-max;
        position.x=low<=high?Mathf.Clamp(position.x,low,high):(left+right-min-max)*.5f;
        return position;
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
        float planeShiftY=PerspectivePlaneShift.y;
        next.y=ConstrainFootFraming(next.y-planeShiftY,FramingFeetY,cameraComponent.orthographicSize)+planeShiftY;
        transform.position=ConstrainHorizontal(next);
    }
    void OnValidate(){viewSize=Mathf.Max(.1f,viewSize);followSpeed=Mathf.Max(0,followSpeed);maximumHeightZoomMultiplier=Mathf.Max(1,maximumHeightZoomMultiplier);heightZoomMaximumY=Mathf.Max(heightZoomStartY+.01f,heightZoomMaximumY);heightZoomSpeed=Mathf.Max(0,heightZoomSpeed);maximumHeightPlayerViewportY=Mathf.Clamp(maximumHeightPlayerViewportY,.5f,.9f);minimumFootScreenMargin=Mathf.Clamp(minimumFootScreenMargin,0,.35f);maximumMouseOffset=Mathf.Max(0,maximumMouseOffset);maximumDownwardMouseOffset=Mathf.Max(0,maximumDownwardMouseOffset);mouseLookSensitivity=Mathf.Clamp(mouseLookSensitivity,0,.9f);ApplySize();}
}
