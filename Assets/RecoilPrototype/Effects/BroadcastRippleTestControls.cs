using UnityEngine;

// Only attached to the dedicated test scene.
[DisallowMultipleComponent,AddComponentMenu("Recoil/Tests/Broadcast Ripple Controls")]
public sealed class BroadcastRippleTestControls : MonoBehaviour
{
    public RecoilPrototype world;
    public BroadcastRefractionEffect effect;
    public SoundWaveManager broadcast;
    public GameObject referenceLights;
    GUIStyle label;
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
    void OnGUI()
    {
        if(!world || !world.Playing)return;
        if(label==null)label=new GUIStyle(GUI.skin.label){fontSize=13,wordWrap=true};
        GUI.Box(new Rect(12,12,Mathf.Min(Screen.width-24,650),66),GUIContent.none);
        GUI.Label(new Rect(24,18,620,25),"REFRACTION TEST   B Broadcast   1 Expand   2 Contract   3 Warning",label);
        GUI.Label(new Rect(24,41,620,25),"V Refraction: "+(effect.effectEnabled?"ON":"OFF")+"   H Reference lights   0 Clear pulses",label);
    }
}
