using System;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class FreightEquipmentLightingValidation
{
    static void Require(bool condition,string message){if(!condition)throw new Exception("Freight lighting: "+message);}
    public static string Validate()
    {
        var scene=EditorSceneManager.NewPreviewScene();var root=new GameObject("Freight lighting validation");SceneManager.MoveGameObjectToScene(root,scene);
        try
        {
            var flags=BindingFlags.Instance|BindingFlags.NonPublic;var world=root.AddComponent<RecoilPrototype>();
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/RecoilPrototype/Prefabs/KBKStackerCrane.prefab");
            var instance=UnityEngine.Object.Instantiate(asset,root.transform);var crane=instance.GetComponent<KBKStackerCrane>();var device=crane.GetComponent<FreightEquipmentLighting>();
            Require(device && device.lamps.Length==3,"prefab has switch, trolley and carriage lights");
            var playerGO=new GameObject("Exposure sampler");playerGO.transform.SetParent(root.transform);var player=playerGO.AddComponent<RecoilPlayerController>();playerGO.AddComponent<PlayerLightExposure>();
            typeof(RecoilPrototype).GetField("player",flags).SetValue(world,player);
            var area=new GameObject("Indoor test area").transform;area.SetParent(root.transform);area.localScale=new Vector3(200,200,1);
            var lighting=root.AddComponent<RecoilLighting2D>();lighting.Configure(player.transform,area,null);typeof(RecoilPrototype).GetField("lighting",flags).SetValue(world,lighting);
            var roomGO=new GameObject("Room circuit fixture");roomGO.transform.SetParent(root.transform);var room=roomGO.AddComponent<RecoilLight2D>();
            var power=root.AddComponent<WarehouseLightingPower>();typeof(WarehouseLightingPower).GetMethod("Start",flags).Invoke(power,null);
            device.RefreshLighting(true);
            foreach(var lamp in device.lamps){Require(!lamp.illumination.enabled && !lamp.realtimeLight.enabled,"unpowered equipment lights disabled");Require(lamp.illumination.transform.parent==lamp.lens.transform,"light attached to emitting object");}
            var state=new WarehousePowerState{LightingCut=true,FreightCut=true,FreightConnected=true};state.Save(world);
            device.RefreshLighting();typeof(WarehouseLightingPower).GetMethod("Update",flags).Invoke(power,null);
            Require(!room.enabled,"bypass extinguishes room lighting");
            foreach(var lamp in device.lamps)Require(lamp.illumination.enabled && lamp.realtimeLight.enabled,"freight lights survive room circuit switch-off");
            var work=device.lamps[2].illumination;player.Position=(Vector2)work.transform.position+Vector2.down;
            float bright=lighting.EnvironmentBrightnessAt(player.Position),exposure=player.Illumination;
            Require(bright>.25f && exposure>.8f,"work light affects actual environment brightness and player exposure");
            Vector3 initial=work.transform.position;crane.ResetMachine();Require(crane.Activate(),"powered crane can activate");crane.Simulate(crane.liftDuration);crane.Simulate(crane.travelDuration*.5f);device.RefreshLighting();
            Require(Mathf.Abs(work.transform.position.x-initial.x)>1,"work light follows moving carriage");
            Require(device.lamps[0].realtimeLight.color==RecoilPrototype.Hex(0xd7a64b),"busy indicator turns amber");
            player.Position=(Vector2)work.transform.position+Vector2.down;float illuminated=player.Illumination;
            state.FreightConnected=false;state.Save(world);device.RefreshLighting();typeof(WarehouseLightingPower).GetMethod("Update",flags).Invoke(power,null);
            Require(lighting.EnvironmentBrightnessAt(player.Position)<.01f && player.Illumination<illuminated-.2f,"disconnect immediately removes illumination and invalidates cached exposure");
            foreach(var lamp in device.lamps)Require(!lamp.illumination.enabled && !lamp.realtimeLight.enabled,"disconnect extinguishes both light systems");
            return "Freight lighting passed: power isolation, native/2D sources, environment brightness, player exposure, moving hierarchy and disconnect.";
        }
        finally{UnityEngine.Object.DestroyImmediate(root);EditorSceneManager.ClosePreviewScene(scene);}
    }
}
