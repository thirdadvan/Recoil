using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class WarehousePowerPresentationVerification
{
    [MenuItem("Recoil/Tests/Verify Warehouse Power Presentation")]
    public static void Verify()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Exit Play before checking presentation power.");
        if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        string previous=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
        try
        {
            foreach(string path in new[]{"Assets/Scenes/3D/RecoilFactoryWarehouse.unity",WarehouseInsideLightingTestBuilder.Destination})
            {
                var scene=EditorSceneManager.OpenScene(path);var roots=scene.GetRootGameObjects();
                var look=roots.SelectMany(g=>g.GetComponentsInChildren<WarehouseAtmosphereTest>(true)).Single();
                var power=roots.SelectMany(g=>g.GetComponentsInChildren<WarehouseLightingPower>(true)).Single();var world=power.GetComponent<RecoilPrototype>();
                bool cut=world.HasItem(WarehousePowerState.Prefix+"lighting_cut"),bypass=world.HasItem(WarehousePowerState.Prefix+"freight_connected");
                var lights=look.decorativeLights.Where(l=>l).ToArray();var camera=look.GetComponent<Camera>();
                try
                {
                    world.SetProgressFlag(WarehousePowerState.Prefix+"lighting_cut",false);world.SetProgressFlag(WarehousePowerState.Prefix+"freight_connected",false);look.ApplyLook();
                    var baseline=lights.Select(l=>l.intensity).ToArray();if(!look.VolumeLightingPowered||baseline.Length==0)throw new Exception("Missing powered profile");
                    WarehousePathLightingPass.Capture(camera,"WarehousePower_"+scene.name+"_On");
                    if(LampCount(look)<=0)throw new Exception("Powered warehouse has no shader cones");
                    foreach(bool freightBypass in new[]{false,true})
                    {
                        world.SetProgressFlag(WarehousePowerState.Prefix+"lighting_cut",!freightBypass);world.SetProgressFlag(WarehousePowerState.Prefix+"freight_connected",freightBypass);
                        for(int frame=0;frame<5;frame++)look.ApplyLook();
                        if(look.VolumeLightingPowered)throw new Exception("Shader cones still powered");
                        for(int i=0;i<lights.Length;i++)if(Mathf.Abs(lights[i].intensity-baseline[i]*.3f)>.00001f)throw new Exception("Light failed 70% reduction: "+lights[i].name);
                        WarehousePathLightingPass.Capture(camera,"WarehousePower_"+scene.name+(freightBypass?"_Bypass":"_Off"));
                        if(LampCount(look)!=0)throw new Exception("Shader submitted lights while supply was off");
                    }
                    world.SetProgressFlag(WarehousePowerState.Prefix+"lighting_cut",false);world.SetProgressFlag(WarehousePowerState.Prefix+"freight_connected",false);look.ApplyLook();
                    for(int i=0;i<lights.Length;i++)if(Mathf.Abs(lights[i].intensity-baseline[i])>.00001f)throw new Exception("Light failed restoration");
                    Debug.Log("WAREHOUSE_POWER_PRESENTATION_PASS: "+scene.name+"; unplugged/bypass shader count=0; lights=30%; repeated frames stable; reconnect=100%");
                }
                finally{world.SetProgressFlag(WarehousePowerState.Prefix+"lighting_cut",cut);world.SetProgressFlag(WarehousePowerState.Prefix+"freight_connected",bypass);look.ApplyLook();}
            }
        }
        finally{if(!string.IsNullOrEmpty(previous))EditorSceneManager.OpenScene(previous);}
    }
    static int LampCount(WarehouseAtmosphereTest look)
    {var material=(Material)typeof(WarehouseAtmosphereTest).GetField("material",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(look);if(!material)throw new Exception("Shader failed to render");return material.GetInt("_LampCount");}
}
