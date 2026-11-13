using UnityEngine;

[DisallowMultipleComponent,AddComponentMenu("Recoil/3D/Fuel Pump Display")]
public sealed class Recoil3DFuelPumpDisplay : MonoBehaviour
{
    public FuelPumpInteraction source;
    public GameObject readyDisplay,failedDisplay;
    [Tooltip("这台加油机在故事开始前就已经损坏，独立于全站故障事件。")]
    public bool startsBroken;
    [Tooltip("仅关闭屏幕，不改变油枪互动和故障剧情。")]
    public bool screenBlack;
    bool initialized,failed,wasBroken,wasBlack;
    public void Sync()
    {
        bool value=startsBroken || source && source.Completed;
        if(initialized && value==failed && wasBroken==startsBroken && wasBlack==screenBlack)return;
        failed=value;wasBroken=startsBroken;wasBlack=screenBlack;initialized=true;
        if(readyDisplay)readyDisplay.SetActive(!screenBlack && !failed);
        if(failedDisplay)failedDisplay.SetActive(!screenBlack && !startsBroken && failed);
    }
    void OnEnable(){initialized=false;Sync();}
    void LateUpdate(){Sync();}
}
