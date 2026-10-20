using UnityEngine;

[DisallowMultipleComponent, AddComponentMenu("Recoil/Foreground Occluder")]
public sealed class RecoilForegroundOccluder : MonoBehaviour
{
    public Transform player;
    public SpriteRenderer[] foregroundRenderers;
    [Range(0,1)] public float coveredAlpha=.3f;
    [Min(.01f)] public float fadeTime=.2f;
    [Tooltip("门下方的遮挡区域，相对该物体原点。")]
    public Vector2 overlapCenter=new Vector2(0,1.3f);
    public Vector2 overlapSize=new Vector2(2,2.6f);
    Color[] originalColors;
    float visibility=1;

    void Awake()
    {
        if(!player){var controller=FindObjectOfType<RecoilPlayerController>();if(controller)player=controller.transform;}
        if(foregroundRenderers==null || foregroundRenderers.Length==0)foregroundRenderers=GetComponentsInChildren<SpriteRenderer>();
        originalColors=new Color[foregroundRenderers.Length];
        for(int i=0;i<foregroundRenderers.Length;i++)if(foregroundRenderers[i])originalColors[i]=foregroundRenderers[i].color;
    }
    void LateUpdate()
    {
        bool covered=false;
        if(player)
        {
            var bounds=new Bounds(transform.TransformPoint(overlapCenter),new Vector3(overlapSize.x*Mathf.Abs(transform.lossyScale.x),overlapSize.y*Mathf.Abs(transform.lossyScale.y),100));
            foreach(var sprite in player.GetComponentsInChildren<SpriteRenderer>())
                if(sprite.enabled && sprite.gameObject.activeInHierarchy && bounds.Intersects(sprite.bounds)){covered=true;break;}
        }
        visibility=Mathf.MoveTowards(visibility,covered?coveredAlpha:1,Time.deltaTime/Mathf.Max(.01f,fadeTime));
        for(int i=0;i<foregroundRenderers.Length;i++)if(foregroundRenderers[i])
        {var color=originalColors[i];color.a*=visibility;foregroundRenderers[i].color=color;}
    }
    void OnDisable()
    {
        if(originalColors==null)return;
        for(int i=0;i<foregroundRenderers.Length;i++)if(foregroundRenderers[i])foregroundRenderers[i].color=originalColors[i];
        visibility=1;
    }
    void OnDrawGizmosSelected()
    {Gizmos.color=new Color(1,.6f,.15f,.8f);var previous=Gizmos.matrix;Gizmos.matrix=transform.localToWorldMatrix;Gizmos.DrawWireCube(overlapCenter,overlapSize);Gizmos.matrix=previous;}
}
