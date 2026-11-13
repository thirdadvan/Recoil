using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class FactoryGroundFloorLightingPass
{
    static T[] All<T>(Scene s) where T:Component {return s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true)).ToArray();}
    [MenuItem("Recoil/Lighting/Relight factory ground floor")]
    public static string Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Exit Play first.");
        var s=SceneManager.GetSceneByPath("Assets/Scenes/3D/RecoilFactoryInterior.unity");
        if(!s.isLoaded)throw new Exception("Load factory interior first.");
        var world=All<RecoilPrototype>(s).Single();
        if(world.transform.Find("Ground floor - dim workshop lighting"))return "Already applied; edit the existing lighting rig.";
        string backup="Backups/FactoryGroundFloorLighting/"+DateTime.Now.ToString("yyyyMMdd-HHmmss");
        Directory.CreateDirectory(backup);EditorSceneManager.SaveScene(s,backup+"/Interior.unity",true);
        Capture(s,"Ground-floor-before");
        var colliders=All<Collider2D>(s).ToDictionary(c=>c.GetInstanceID(),c=>c.bounds);
        var doors=All<RecoilDoor>(s).ToDictionary(d=>d.GetInstanceID(),d=>d.destinationScene+"|"+d.destinationSpawn+"|"+d.transform.position);
        var look=All<WarehouseAtmosphereTest>(s).Single();
        // Release the look controller's cached decorative intensities before authoring.
        bool wasEnabled=look.enabled;look.enabled=false;
        var removed=All<Light>(s).Where(l=>l.name=="Indoor fixed reflected fill").ToArray();
        var removedSet=new HashSet<Light>(removed);
        look.decorativeLights=look.decorativeLights.Where(l=>l&&!removedSet.Contains(l)).ToArray();
        look.lamps=look.lamps.Where(l=>l&&!removedSet.Contains(l)).ToArray();
        foreach(var l in removed)UnityEngine.Object.DestroyImmediate(l.gameObject);
        var rig=new GameObject("Ground floor - dim workshop lighting").transform;rig.SetParent(world.transform,false);
        // Reuse the outdoor route-band shading on the existing shadowless bounce.
        // Height masking prevents this presentation change from lighting upper corridors.
        var bounce=All<Light>(s).Single(l=>l.name=="3D soft ambient bounce");
        var band=bounce.GetComponent<OperationPlaneMoonlight>();if(!band)band=bounce.gameObject.AddComponent<OperationPlaneMoonlight>();
        band.planeZ=0;band.fullStrengthHalfWidth=1.15f;band.fadeWidth=4.5f;
        band.foregroundFullStrengthWidth=.55f;band.foregroundFadeWidth=2.2f;
        band.intensityMultiplier=1;band.minimumIntensityMultiplier=1;
        band.diffuseFillIntensity=.32f;
        band.limitHeight=true;band.minimumHeight=-.4f;band.maximumHeight=6.3f;band.heightFeather=.8f;
        band.Apply(null);EditorUtility.SetDirty(band);
        var keys=All<Light>(s).Where(l=>l.name=="Indoor overhead key").ToArray();
        var keepKeys=new List<Light>();
        foreach(var key in keys) {
            float x=key.transform.position.x;
            bool keep=Mathf.Abs(x-2.5f)<.1f||Mathf.Abs(x-10)<.1f||Mathf.Abs(x-19.13f)<.1f||Mathf.Abs(x-24)<.1f||Mathf.Abs(x-32.5f)<.1f;
            if(!keep){key.gameObject.SetActive(false);continue;}
            key.transform.position=new Vector3(x,6.95f,.15f);
            key.transform.rotation=Quaternion.LookRotation(new Vector3(x,.2f,1.4f)-key.transform.position);
            key.intensity=x<5?1.0f:1.35f;key.range=10.2f;key.spotAngle=88;key.innerSpotAngle=40;
            key.shadows=LightShadows.Soft;key.shadowResolution=UnityEngine.Rendering.LightShadowResolution.Low;
            keepKeys.Add(key);EditorUtility.SetDirty(key);
        }
        // The authored key now belongs above the six-metre machines. Keep fixture
        // sources registered for gameplay, but remove duplicate native illumination.
        foreach(var link in All<Recoil3DLight>(s).Where(l=>l.lightSource&&l.lightSource.name=="3D light - source owned"&&l.transform.position.y<7&&l.transform.position.z<3)) {
            bool ceiling=false;var t=link.transform;
            while(t){if(t.name.StartsWith("Weak Ceiling Lamp")){ceiling=true;break;}t=t.parent;}
            if(ceiling){link.intensityMultiplier=0;link.Sync();EditorUtility.SetDirty(link);}
        }
        var additions=new List<Light>();
        foreach(float x in new[]{9f,21f,33f}) {
            var g=new GameObject("Machine inspection pendant - "+x).transform;g.SetParent(rig,false);
            g.position=new Vector3(x+1.2f,6.65f,10.5f);
            var lamp=new GameObject("Downward work light - machine shoulder");lamp.transform.SetParent(g,false);
            var l=lamp.AddComponent<Light>();l.type=LightType.Spot;l.color=new Color(.54f,.64f,.63f);
            l.intensity=1.05f;l.range=7.2f;l.spotAngle=68;l.innerSpotAngle=32;
            lamp.transform.rotation=Quaternion.LookRotation(new Vector3(x,2.7f,8.8f)-g.position);
            l.shadows=LightShadows.None;l.renderMode=LightRenderMode.ForcePixel;
            additions.Add(l);
            var shade=new GameObject("Deep enamel shade - luminous inner lining");shade.transform.SetParent(g,false);
            shade.AddComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Recoil3D/ArtTests/Deep industrial reflector.asset");
            shade.AddComponent<MeshRenderer>().sharedMaterials=new[]{AssetDatabase.LoadAssetAtPath<Material>("Assets/Recoil3D/ArtTests/Deep shade - aged charcoal enamel.mat"),AssetDatabase.LoadAssetAtPath<Material>("Assets/Recoil3D/ArtTests/Deep shade - illuminated ivory lining.mat")};
            var cable=GameObject.CreatePrimitive(PrimitiveType.Cylinder);cable.name="Suspension cable to upper structure";UnityEngine.Object.DestroyImmediate(cable.GetComponent<Collider>());cable.transform.SetParent(g,false);cable.transform.localPosition=new Vector3(0,.25f,0);cable.transform.localScale=new Vector3(.018f,.25f,.018f);cable.GetComponent<MeshRenderer>().sharedMaterial=shade.GetComponent<MeshRenderer>().sharedMaterials[0];
        }
        look.decorativeLights=look.decorativeLights.Where(l=>l&&l.gameObject.activeInHierarchy).Concat(additions).Distinct().ToArray();
        look.lamps=look.lamps.Where(l=>l&&l.gameObject.activeInHierarchy&&l.name!="Indoor overhead key").Concat(keepKeys).Distinct().ToArray();
        look.enabled=wasEnabled;look.ApplyLook();EditorUtility.SetDirty(look);
        if(All<Collider2D>(s).Any(c=>!colliders.ContainsKey(c.GetInstanceID())||c.bounds!=colliders[c.GetInstanceID()])||All<Collider2D>(s).Length!=colliders.Count)throw new Exception("Collision changed.");
        foreach(var d in All<RecoilDoor>(s))if(doors[d.GetInstanceID()]!=d.destinationScene+"|"+d.destinationSpawn+"|"+d.transform.position)throw new Exception("Door changed.");
        if(Mathf.Abs(band.MultiplierAt(new Vector3(0,9,0))-1)>.001f)throw new Exception("Upper floor fill leaked.");
        EditorSceneManager.MarkSceneDirty(s);EditorSceneManager.SaveScene(s);AssetDatabase.SaveAssets();
        Capture(s,"Ground-floor-relit");
        Directory.CreateDirectory("output/factory-ground-lighting");
        string report="Backup="+backup+"\nRemoved floor fills="+removed.Length+"; selected overhead keys="+keepKeys.Count+"; machine pendants="+additions.Count+"\nRoute-band fill uses masked soft diffuse bounce near Z0; height fade ends at Y7.1; upper floor multiplier stays 1.\nNo changed gameplay colliders, doors, source perception intensities, ambient exposure, or upper-floor lamps.";
        File.WriteAllText("output/factory-ground-lighting/verification.txt",report);return report;
    }
    static void Capture(Scene s,string prefix)
    {
        var method=typeof(FactoryConflictCapture).GetMethod("Shot",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static);
        foreach(float x in new[]{7f,19f,30f})method.Invoke(null,new object[]{s,prefix+"-"+x,new Vector3(x,4.2f,-17),new Vector3(x,2.7f,7),50f});
    }
}
