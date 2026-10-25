using UnityEngine;

[ExecuteAlways,DisallowMultipleComponent,AddComponentMenu("Recoil/3D/Street Lamp Lens")]
public sealed class Recoil3DStreetLampVisual : MonoBehaviour
{
    public RecoilStreetLamp lamp;
    public Renderer lens;
    public RecoilLight2D scatterSource;
    public Color litColour=new Color(1,.78f,.43f);
    MaterialPropertyBlock block;bool primed,lastOn;
    void OnEnable(){primed=false;Sync();}
    void LateUpdate(){Sync();}
    public void Sync()
    {
        if(!lamp||!lens)return;bool on=lamp.IsOn;
        if(scatterSource && scatterSource.enabled!=on)scatterSource.enabled=on;
        if(primed&&lastOn==on)return;primed=true;lastOn=on;
        if(block==null)block=new MaterialPropertyBlock();
        block.SetColor("_Color",on?litColour:new Color(.16f,.18f,.18f));
        block.SetColor("_EmissionColor",on?litColour*.65f:Color.black);lens.SetPropertyBlock(block);
    }
}
