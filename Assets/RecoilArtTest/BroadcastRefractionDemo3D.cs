using UnityEngine;

// Presentation-only driver for the existing 3D interior art scene; no game-world or AI dependency.
[AddComponentMenu("Recoil/Art Test/Broadcast Refraction Demo 3D")]
public sealed class BroadcastRefractionDemo3D : MonoBehaviour
{
    public BroadcastRefractionEffect effect;
    public bool automaticSequence=true;
    [Min(.1f)] public float warningDuration=3,waveDuration=2.6f,restDuration=3;
    int commandsRemaining;
    bool running;
    float nextEvent,nextAutomatic,manualAlertUntil;
    public bool RedAlertActive {get{return effect && (running || effect.Clock<manualAlertUntil);}}
    GUIStyle label;
    string current="READY";
    void Start(){nextAutomatic=effect?effect.Clock+.75f:.75f;}
    public void BeginBroadcast()
    {
        if(!effect)return;
        effect.ClearPulses();effect.EmitPulse(1,true,warningDuration);
        running=true;manualAlertUntil=0;commandsRemaining=3;current="WARNING";nextEvent=effect.Clock+warningDuration;
    }
    void ManualPulse(int direction,bool red)
    {
        effect.ClearPulses();effect.EmitPulse(direction,red,red?warningDuration:waveDuration);
        running=false;manualAlertUntil=effect.Clock+(red?warningDuration:waveDuration);nextAutomatic=effect.Clock+8;current=red?"WARNING":direction>0?"EXPANDING / RIGHT":"CONTRACTING / LEFT";
    }
    void Update()
    {
        if(!effect)return;
        if(Input.GetKeyDown(KeyCode.B))BeginBroadcast();
        if(Input.GetKeyDown(KeyCode.Alpha1))ManualPulse(1,false);
        if(Input.GetKeyDown(KeyCode.Alpha2))ManualPulse(-1,false);
        if(Input.GetKeyDown(KeyCode.Alpha3))ManualPulse(1,true);
        if(Input.GetKeyDown(KeyCode.V))effect.effectEnabled=!effect.effectEnabled;
        if(Input.GetKeyDown(KeyCode.Alpha0)){effect.ClearPulses();running=false;manualAlertUntil=0;nextAutomatic=effect.Clock+8;current="CLEARED";}
        if(Input.GetKeyDown(KeyCode.F))automaticSequence=!automaticSequence;
        if(running && effect.Clock>=nextEvent)
        {
            if(commandsRemaining>0)
            {
                int direction=Random.value>.5f?1:-1;
                effect.EmitPulse(direction,false,waveDuration);commandsRemaining--;nextEvent=effect.Clock+waveDuration;
                current=direction>0?"EXPANDING / RIGHT":"CONTRACTING / LEFT";
            }
            else{running=false;current="REST";nextAutomatic=effect.Clock+restDuration;}
        }
        if(!running && automaticSequence && effect.Clock>=nextAutomatic)BeginBroadcast();
    }
    void OnGUI()
    {
        if(label==null)label=new GUIStyle(GUI.skin.label){fontSize=13,wordWrap=true};
        GUI.Box(new Rect(12,12,Mathf.Min(Screen.width-24,800),88),GUIContent.none);
        GUI.Label(new Rect(24,17,770,24),"3D AIR REFRACTION   "+current+"   ["+(effect && effect.effectEnabled?"ON":"OFF")+"]",label);
        GUI.Label(new Rect(24,42,770,24),"B Broadcast   1 Expand   2 Contract   3 Warning   V Compare   F Auto: "+(automaticSequence?"ON":"OFF"),label);
        GUI.Label(new Rect(24,66,770,24),"A / D Pan camera   Mouse wheel Zoom   Home Reset view   0 Clear pulses",label);
    }
}
