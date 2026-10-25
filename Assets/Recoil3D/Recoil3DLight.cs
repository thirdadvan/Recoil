using UnityEngine;

[ExecuteAlways,DefaultExecutionOrder(900),DisallowMultipleComponent]
public sealed class Recoil3DLight : MonoBehaviour
{
    public RecoilLight2D source;
    public RecoilStreetLamp streetLamp;
    public Light lightSource;
    public float intensityMultiplier=3.5f;
    public Color workingColour=new Color(.69f,.78f,.73f);
    void LateUpdate(){Sync();}
    public void Sync()
    {
        if(!lightSource)return;
        bool lit=source?source.isActiveAndEnabled && source.gameObject.activeInHierarchy && source.intensity>0:streetLamp && streetLamp.IsOn;
        lightSource.enabled=lit;if(!lit)return;
        lightSource.intensity=source?source.intensity*intensityMultiplier:intensityMultiplier;
        var world=GetComponentInParent<RecoilPrototype>();bool red=Application.isPlaying && world && world.SoundWaves && world.SoundWaves.RedAlert;
        lightSource.color=red?new Color(1,.03f,.01f):workingColour;
    }
}
