using UnityEngine;

/// <summary>Art-test camera only; never attaches to the playable factory.</summary>
[ExecuteAlways,RequireComponent(typeof(Camera))]
public sealed class FactoryArtCamera : MonoBehaviour
{
    public float travelSpeed=5;
    public Vector2 horizontalLimits=new Vector2(3,27);
    public Vector2 fieldOfViewLimits=new Vector2(27,55);
    public float defaultFieldOfView=33;
    public Material filmGrade;
    Vector3 initialPosition;
    Camera lens;
    void Awake(){initialPosition=transform.position;lens=GetComponent<Camera>();}
    void OnRenderImage(RenderTexture source,RenderTexture destination)
    {Graphics.Blit(source,destination,filmGrade);}
    void Update()
    {
        if(!Application.isPlaying)return;
        if(!lens)lens=GetComponent<Camera>();
        float move=(Input.GetKey(KeyCode.D)||Input.GetKey(KeyCode.RightArrow)?1:0)-(Input.GetKey(KeyCode.A)||Input.GetKey(KeyCode.LeftArrow)?1:0);
        Vector3 p=transform.position;p.x=Mathf.Clamp(p.x+move*travelSpeed*Time.deltaTime,horizontalLimits.x,horizontalLimits.y);transform.position=p;
        lens.fieldOfView=Mathf.Clamp(lens.fieldOfView-Input.mouseScrollDelta.y*2,fieldOfViewLimits.x,fieldOfViewLimits.y);
        if(Input.GetKeyDown(KeyCode.Home)){transform.position=initialPosition;lens.fieldOfView=defaultFieldOfView;}
    }
}
