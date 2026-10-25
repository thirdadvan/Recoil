using UnityEngine;
[ExecuteAlways,DefaultExecutionOrder(950)]
public sealed class Recoil3DBeacon : MonoBehaviour
{
    public SpriteRenderer source;
    public MeshRenderer lens;
    public Light beacon;
    MaterialPropertyBlock block;
    void LateUpdate()
    {
        if(!source)return;float power=Mathf.Clamp01((source.color.r-.12f)/.88f);
        if(beacon){beacon.intensity=power*2;beacon.enabled=power>.01f;}
        if(lens){if(block==null)block=new MaterialPropertyBlock();block.SetColor("_Color",source.color);block.SetColor("_EmissionColor",source.color*power*2);lens.SetPropertyBlock(block);}
    }
}
