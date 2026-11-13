using UnityEngine;
[ExecuteAlways, RequireComponent(typeof(Camera))]
public sealed class PS1Presentation : MonoBehaviour
{
    public Shader shader;
    [Range(160,640)] public int horizontalResolution=320;
    [Range(8,64)] public int colorSteps=32;
    [Range(0,1)] public float ditherStrength=.65f;
    Material material;
    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if(!shader || !shader.isSupported){Graphics.Blit(source,destination);return;}
        if(!material)material=new Material(shader){hideFlags=HideFlags.HideAndDontSave};
        int height=Mathf.Max(1,Mathf.RoundToInt(horizontalResolution*(float)source.height/source.width));
        var low=RenderTexture.GetTemporary(horizontalResolution,height,0,RenderTextureFormat.ARGB32);
        low.filterMode=FilterMode.Point;
        material.SetFloat("_Steps",colorSteps);material.SetFloat("_Dither",ditherStrength);
        Graphics.Blit(source,low,material);Graphics.Blit(low,destination);RenderTexture.ReleaseTemporary(low);
    }
    void OnDisable(){if(material)DestroyImmediate(material);}
}
