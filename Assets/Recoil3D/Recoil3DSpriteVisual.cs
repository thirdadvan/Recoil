using UnityEngine;

// Geometry is baked once and remains editable in 3D. Gameplay state stays on the parent.
[ExecuteAlways,DefaultExecutionOrder(700),DisallowMultipleComponent]
public sealed class Recoil3DSpriteVisual : MonoBehaviour
{
    public SpriteRenderer source;
    public MeshRenderer meshRenderer;
    public float depth=.3f,depthOffset;
    public Color tint=Color.white;
    [Tooltip("开启才按旧精灵尺寸重算模型。默认关闭，保留你在 Scene 中编辑的 3D 形状。")]
    public bool followSpriteGeometry;
    public bool suppressed;
    [SerializeField,HideInInspector] bool geometryBaked;
    [SerializeField,HideInInspector] float appliedDepth,appliedOffset;
    MaterialPropertyBlock block;
    Sprite cachedSprite;Vector2 cachedSize;Color cachedColour,cachedTint;
    bool primed;
    public void Sync()
    {
        if(!source)return;
        source.forceRenderingOff=true;
        if(!meshRenderer)return;
        bool visible=!suppressed && source.enabled && source.sprite;
        if(meshRenderer.enabled!=visible)meshRenderer.enabled=visible;
        if(!source.sprite)return;
        if(!geometryBaked || followSpriteGeometry)
        {
            var size=source.drawMode==SpriteDrawMode.Simple?(Vector2)source.sprite.bounds.size:source.size;
            if(!geometryBaked || cachedSprite!=source.sprite || size!=cachedSize)
            {
                var bounds=source.sprite.bounds;var t=meshRenderer.transform;var p=t.localPosition;p.x=bounds.center.x;p.y=bounds.center.y;t.localPosition=p;
                var scale=t.localScale;scale.x=Mathf.Abs(size.x);scale.y=Mathf.Abs(size.y);t.localScale=scale;
                cachedSprite=source.sprite;cachedSize=size;
            }
        }
        if(!geometryBaked || appliedDepth!=depth)
        {var s=meshRenderer.transform.localScale;s.z=depth/Mathf.Max(.001f,Mathf.Abs(transform.lossyScale.z));meshRenderer.transform.localScale=s;appliedDepth=depth;}
        if(!geometryBaked || appliedOffset!=depthOffset)
        {var p=meshRenderer.transform.localPosition;p.z=depthOffset;meshRenderer.transform.localPosition=p;appliedOffset=depthOffset;}
        geometryBaked=true;
        if(!primed || cachedColour!=source.color || cachedTint!=tint)
        {if(block==null)block=new MaterialPropertyBlock();cachedColour=source.color;cachedTint=tint;block.SetColor("_Color",cachedColour*tint);meshRenderer.SetPropertyBlock(block);}
        primed=true;
    }
    void LateUpdate(){Sync();}
    void OnEnable(){primed=false;Sync();}
    void OnDisable(){if(source)source.forceRenderingOff=false;}
}
