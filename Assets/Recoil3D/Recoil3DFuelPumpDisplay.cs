using UnityEngine;

[DisallowMultipleComponent,AddComponentMenu("Recoil/3D/Fuel Pump Display")]
public sealed class Recoil3DFuelPumpDisplay : MonoBehaviour
{
    public FuelPumpInteraction source;
    public GameObject readyDisplay,failedDisplay;
    [Tooltip("这台加油机在故事开始前就已经损坏，独立于全站故障事件。")]
    public bool startsBroken;
    bool initialized,failed,wasBroken;
    public void Sync()
    {
        bool value=startsBroken || source && source.Completed;
        if(initialized && value==failed && wasBroken==startsBroken)return;
        failed=value;wasBroken=startsBroken;initialized=true;
        if(readyDisplay)readyDisplay.SetActive(!failed);
        if(failedDisplay)failedDisplay.SetActive(!startsBroken && failed);
    }
    void OnEnable(){initialized=false;Sync();}
    void LateUpdate(){Sync();}
}
