using UnityEngine;

// Keep the depth-tested text material in sync when Unity resizes a dynamic font atlas.
[ExecuteAlways,DisallowMultipleComponent,RequireComponent(typeof(TextMesh))]
public sealed class Recoil3DDepthText : MonoBehaviour
{
    TextMesh text;Renderer target;MaterialPropertyBlock properties;Texture atlas;
    void OnEnable(){text=GetComponent<TextMesh>();target=GetComponent<Renderer>();atlas=null;SyncAtlas();}
    void OnWillRenderObject(){SyncAtlas();}
    void SyncAtlas()
    {
        if(!text || !target || !text.font || !text.font.material)return;
        var current=text.font.material.mainTexture;if(!current || current==atlas)return;
        if(properties==null)properties=new MaterialPropertyBlock();
        target.GetPropertyBlock(properties);properties.SetTexture("_MainTex",current);target.SetPropertyBlock(properties);atlas=current;
    }
}
