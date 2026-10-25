using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways,DefaultExecutionOrder(1500),RequireComponent(typeof(Camera))]
public sealed class WarehouseShadowBudget : MonoBehaviour
{
    public Light[] lights=new Light[0];
    public LightShadows[] originalShadows=new LightShadows[0];
    public LightShadowResolution[] originalResolution=new LightShadowResolution[0];
    public Light doorway;
    WarehouseAtmosphereTest study;
    void OnEnable(){study=GetComponent<WarehouseAtmosphereTest>();}
    void LateUpdate(){Apply();}
    public void Apply()
    {
        if(!study)study=GetComponent<WarehouseAtmosphereTest>();
        if(!study||!study.testLook){Restore();return;}
        foreach(var l in lights)if(l)
        {
            // Disabling a shadow while its light still illuminates visible shelves
            // reveals previously occluded surfaces. Keep the authored keys stable;
            // Unity culls offscreen lights, and reduced map resolution saves cost.
            bool entry=l==doorway;
            bool shadow=entry||l.name.Contains("overhead key");
            l.shadows=shadow?LightShadows.Soft:LightShadows.None;
            l.shadowResolution=entry?LightShadowResolution.Medium:LightShadowResolution.Low;
        }
    }
    void Restore()
    {
        for(int i=0;i<lights.Length;i++)if(lights[i])
        {
            if(i<originalShadows.Length)lights[i].shadows=originalShadows[i];
            if(i<originalResolution.Length)lights[i].shadowResolution=originalResolution[i];
        }
    }
    void OnDisable(){Restore();}
}
