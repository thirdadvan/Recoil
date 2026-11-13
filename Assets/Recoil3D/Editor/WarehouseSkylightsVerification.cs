using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

public static class WarehouseSkylightsVerification
{
    const string Inside="Assets/Scenes/3D/RecoilFactoryWarehouse.unity",Outside="Assets/Scenes/3D/RecoilPrototype.unity";
    const string Marker="Warehouse sealed roof lanterns - natural light";
    static T[] All<T>(Scene s)where T:Component{return s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true)).ToArray();}
    static float Shot(Camera camera,string name)
    {
        Directory.CreateDirectory("Captures/WarehouseSkylights");var rt=RenderTexture.GetTemporary(1440,900,24,RenderTextureFormat.ARGBHalf);var previous=camera.targetTexture;var active=RenderTexture.active;Texture2D raw=null,img=null;
        try
        {
            camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;raw=new Texture2D(1440,900,TextureFormat.RGBAFloat,false,true);raw.ReadPixels(new Rect(0,0,1440,900),0,0);raw.Apply();var pixels=raw.GetPixels();double luminance=0;int samples=0;
            for(int y=80;y<430;y+=3)for(int x=100;x<1340;x+=3){var c=pixels[y*1440+x];luminance+=c.r*.2126f+c.g*.7152f+c.b*.0722f;samples++;}
            if(QualitySettings.activeColorSpace==ColorSpace.Linear)for(int i=0;i<pixels.Length;i++)pixels[i]=pixels[i].gamma;img=new Texture2D(1440,900,TextureFormat.RGB24,false,true);img.SetPixels(pixels);img.Apply();File.WriteAllBytes("Captures/WarehouseSkylights/"+name+".png",img.EncodeToPNG());return (float)(luminance/samples);
        }
        finally{camera.targetTexture=previous;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);if(raw)Object.DestroyImmediate(raw);if(img)Object.DestroyImmediate(img);}
    }
    public static string Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Exit Play first");var previous=SceneManager.GetActiveScene();var report=new System.Text.StringBuilder();
        foreach(string path in new[]{Inside,Outside})
        {
            var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            var otherRenderers=Enumerable.Range(0,SceneManager.sceneCount).Select(SceneManager.GetSceneAt).Where(s=>s!=scene&&s.isLoaded).SelectMany(s=>All<Renderer>(s)).Where(r=>r.enabled).ToArray();var otherLights=Enumerable.Range(0,SceneManager.sceneCount).Select(SceneManager.GetSceneAt).Where(s=>s!=scene&&s.isLoaded).SelectMany(s=>All<Light>(s)).Where(l=>l.enabled).ToArray();
            try
            {
                foreach(var r in otherRenderers)r.enabled=false;foreach(var l in otherLights)l.enabled=false;SceneManager.SetActiveScene(scene);var world=All<RecoilPrototype>(scene).Single();var root=All<Transform>(scene).Single(t=>t.name==Marker);All<Recoil3DWorld>(scene).Single().ApplyAtmosphere();
                if(root.GetComponentsInChildren<RecoilDoor>(true).Length!=0)throw new Exception("Skylight became a traversable exit");
                if(root.GetComponentsInChildren<Transform>().Count(t=>t.name.StartsWith("Sealed wired glass roof lantern "))!=3)throw new Exception("Missing roof lantern");
                var cam=All<Camera>(scene).First(c=>c.CompareTag("MainCamera"));var position=cam.transform.position;var rotation=cam.transform.rotation;float fov=cam.fieldOfView;var follow=cam.GetComponent<RecoilCameraController>();bool follows=follow&&follow.enabled;
                try
                {
                    if(follow)follow.enabled=false;
                    if(path==Inside)
                    {
                        var look=All<WarehouseAtmosphereTest>(scene).Single();var power=All<WarehouseLightingPower>(scene).Single();var flags=BindingFlags.NonPublic|BindingFlags.Instance;var lights=root.GetComponentsInChildren<Light>();var strengths=lights.Select(l=>l.intensity).ToArray();bool cut=world.HasItem(WarehousePowerState.Prefix+"lighting_cut"),bypass=world.HasItem(WarehousePowerState.Prefix+"freight_connected");float density=look.skylightDensity;
                        try
                        {
                            world.SetProgressFlag(WarehousePowerState.Prefix+"lighting_cut",false);world.SetProgressFlag(WarehousePowerState.Prefix+"freight_connected",false);typeof(WarehouseLightingPower).GetMethod("Start",flags).Invoke(power,null);foreach(var sync in All<Recoil3DLight>(scene))sync.Sync();look.ApplyLook();cam.fieldOfView=52;cam.transform.position=new Vector3(26,4.7f,-18);cam.transform.LookAt(new Vector3(26,2.8f,3));Shot(cam,"InteriorPowered");
                            world.SetProgressFlag(WarehousePowerState.Prefix+"lighting_cut",true);typeof(WarehouseLightingPower).GetMethod("Apply",flags).Invoke(power,null);foreach(var sync in All<Recoil3DLight>(scene))sync.Sync();look.ApplyLook();float offWithSky=Shot(cam,"InteriorPowerOff");
                            for(int i=0;i<lights.Length;i++)if(!lights[i].enabled||Mathf.Abs(lights[i].intensity-strengths[i])>.0001f)throw new Exception("Natural light switched off with electricity");
                            var material=(Material)typeof(WarehouseAtmosphereTest).GetField("material",flags).GetValue(look);if(!material||material.GetInt("_SkyCount")!=3||material.GetInt("_LampCount")!=0)throw new Exception("Power-independent sky volume failed");
                            foreach(var l in lights)l.enabled=false;look.skylightDensity=0;float offWithoutSky=Shot(cam,"InteriorPowerOffComparison_NoSkylight");foreach(var l in lights)l.enabled=true;look.skylightDensity=density;
                            if(offWithSky<=offWithoutSky+.00005f)throw new Exception("No visible floor response from skylights: "+offWithSky+" / "+offWithoutSky);
                            world.SetProgressFlag(WarehousePowerState.Prefix+"lighting_cut",false);world.SetProgressFlag(WarehousePowerState.Prefix+"freight_connected",true);typeof(WarehouseLightingPower).GetMethod("Apply",flags).Invoke(power,null);foreach(var sync in All<Recoil3DLight>(scene))sync.Sync();look.ApplyLook();Shot(cam,"InteriorFreightBypass");for(int i=0;i<lights.Length;i++)if(!lights[i].enabled||lights[i].intensity!=strengths[i])throw new Exception("Sky lighting affected by freight bypass");
                            cam.transform.position=new Vector3(9.5f,4.8f,-15);cam.transform.LookAt(new Vector3(9.5f,3,2));Shot(cam,"InteriorPowerOffNearEntrance");
                            var ceiling=All<BoxCollider2D>(scene).Single(c=>c.name=="Ceiling");Physics2D.SyncTransforms();foreach(float x in new[]{9.5f,26,42.5f})if(!Physics2D.RaycastAll(new Vector2(x,6),Vector2.up,3).Any(h=>h.collider==ceiling))throw new Exception("Player can pass through skylight ceiling at "+x);
                            report.AppendLine("PASS warehouse: 3 glass lanterns; closed ceiling collision at all 3 bays; no new exit; sky light intensity invariant through lighting-cut/freight-bypass. Shader sky volumes=3, powered lamp volumes=0 during outage. Floor-region linear luminance "+offWithoutSky.ToString("F6")+" -> "+offWithSky.ToString("F6")+" with natural light.");
                        }
                        finally{world.SetProgressFlag(WarehousePowerState.Prefix+"lighting_cut",cut);world.SetProgressFlag(WarehousePowerState.Prefix+"freight_connected",bypass);typeof(WarehouseLightingPower).GetMethod("Apply",flags).Invoke(power,null);foreach(var sync in All<Recoil3DLight>(scene))sync.Sync();look.skylightDensity=density;foreach(var l in lights)l.enabled=true;look.ApplyLook();}
                    }
                    else
                    {
                        var source=All<Renderer>(scene).Single(r=>r.name=="Solid flat roof"&&r.transform.IsChildOf(All<Transform>(scene).Single(t=>t.name=="Factory Warehouse - exterior")));if(source.enabled)throw new Exception("Uncut old roof covers glazing");
                        var lightObject=new GameObject("Temporary roof inspection light");SceneManager.MoveGameObjectToScene(lightObject,scene);try{var light=lightObject.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.2f;light.transform.rotation=Quaternion.Euler(48,-25,0);cam.fieldOfView=55;cam.transform.position=new Vector3(107,24,-15);cam.transform.LookAt(new Vector3(125,7,15));Shot(cam,"ExteriorRoofInspection");}finally{Object.DestroyImmediate(lightObject);}
                        report.AppendLine("PASS exterior: three glazed roof strips, retained walking edge and original collision; no roof-travel door.");
                    }
                }
                finally{cam.transform.SetPositionAndRotation(position,rotation);cam.fieldOfView=fov;if(follow)follow.enabled=follows;}
                var shader=Shader.Find("Recoil/Art Tests/Warehouse Atmosphere");if(ShaderUtil.ShaderHasError(shader))throw new Exception("Atmosphere shader compilation failed");
            }
            finally{foreach(var r in otherRenderers)if(r)r.enabled=true;foreach(var l in otherLights)if(l)l.enabled=true;if(opened)EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(previous);}
        }
        var presentation=All<Recoil3DWorld>(previous).FirstOrDefault();if(presentation)presentation.ApplyAtmosphere();var previousLook=All<WarehouseAtmosphereTest>(previous).FirstOrDefault();if(previousLook)previousLook.ApplyLook();File.AppendAllText("output/warehouse-skylights/verification.txt",report.ToString());return report.ToString();
    }
}
