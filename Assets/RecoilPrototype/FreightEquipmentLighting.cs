using UnityEngine;

// Equipment illumination has its own supply; bypassing the room lighting circuit must not switch it off.
[ExecuteAlways,DefaultExecutionOrder(-550),DisallowMultipleComponent,RequireComponent(typeof(KBKStackerCrane))]
[AddComponentMenu("Recoil/Freight Equipment Lighting")]
public sealed class FreightEquipmentLighting : MonoBehaviour
{
    [System.Serializable]
    public sealed class Lamp
    {
        public RecoilLight2D illumination;
        public Light realtimeLight;
        public SpriteRenderer lens;
        public bool statusIndicator;
        public Color onColor=new Color(1,.87f,.58f);
        public Color offColor=new Color(.16f,.18f,.15f);
    }
    public Lamp[] lamps=new Lamp[0];
    KBKStackerCrane crane;
    bool applied,lastPower,lastBusy;
    public bool Powered {get{return isActiveAndEnabled && Crane && Crane.HasPower;}}
    KBKStackerCrane Crane {get{if(!crane)crane=GetComponent<KBKStackerCrane>();return crane;}}
    public void RefreshLighting(bool force=false)
    {
        bool power=Powered,busy=power && Crane.Busy;
        if(!force && applied && power==lastPower && busy==lastBusy)return;
        applied=true;lastPower=power;lastBusy=busy;
        if(lamps==null)return;
        foreach(var lamp in lamps)
        {
            if(lamp==null)continue;
            Color color=lamp.statusIndicator?RecoilPrototype.Hex(busy?0xd7a64b:0x53db79):lamp.onColor;
            if(lamp.illumination)lamp.illumination.enabled=power;
            if(lamp.realtimeLight){lamp.realtimeLight.enabled=power;lamp.realtimeLight.color=color;}
            if(lamp.lens)lamp.lens.color=power?color:lamp.offColor;
        }
    }
    void OnEnable(){RefreshLighting(true);}
    void Update(){RefreshLighting();}
    void OnValidate(){applied=false;}
    void OnDisable(){RefreshLighting(true);}
}
