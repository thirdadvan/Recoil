using UnityEngine;

// Only attached to the dedicated test scene.
[DisallowMultipleComponent,AddComponentMenu("Recoil/Tests/Broadcast Ripple Controls")]
public sealed class BroadcastRippleTestControls : MonoBehaviour
{
    public RecoilPrototype world;
    public BroadcastRefractionEffect effect;
    public SoundWaveManager broadcast;
    public GameObject referenceLights;
    void Update()
    {
        if(!world || !world.Playing || !effect)return;
        if(Input.GetKeyDown(KeyCode.Alpha1))effect.EmitPulse(1);
        if(Input.GetKeyDown(KeyCode.Alpha2))effect.EmitPulse(-1);
        if(Input.GetKeyDown(KeyCode.Alpha3))effect.EmitPulse(1,true,3);
        if(Input.GetKeyDown(KeyCode.V))effect.effectEnabled=!effect.effectEnabled;
        if(Input.GetKeyDown(KeyCode.C) && broadcast)broadcast.drawBroadcastCircles=!broadcast.drawBroadcastCircles;
        if(Input.GetKeyDown(KeyCode.H) && referenceLights)referenceLights.SetActive(!referenceLights.activeSelf);
        if(Input.GetKeyDown(KeyCode.Alpha0))effect.ClearPulses();
    }
}
