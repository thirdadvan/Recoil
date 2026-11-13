using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;

public static class ArchiveWorkstationRepair
{
    const string Folder="Assets/Art/Environment/ArchiveRoom/";
    static Transform Group(string name,Transform parent,Vector3 position){var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=position;return t;}
    static Transform Part(string name,Transform parent,Vector3 position,Vector3 size,Material material,Mesh mesh=null){var t=Group(name,parent,position);t.localScale=size;t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh?mesh:Resources.GetBuiltinResource<Mesh>("Cube.fbx");t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=material;return t;}
    static void Rod(string name,Transform parent,Vector3 from,Vector3 to,float width,Material material){var t=Part(name,parent,(from+to)*.5f,new Vector3(width,Vector3.Distance(from,to),width),material);t.rotation=parent.rotation*Quaternion.FromToRotation(Vector3.up,to-from);}
    static Material Material(string name,Color color){var path=Folder+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(m,path);}m.color=color;m.SetFloat("_Glossiness",.24f);return m;}
    public static string Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Use Edit mode.");
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(!scene.path.EndsWith("/FactoryArchiveF3.unity")||UnityEngine.SceneManagement.SceneManager.sceneCount!=1)throw new Exception("Use single archive scene.");
        var desk=Object.FindObjectsOfType<Transform>().Single(t=>t.name=="Right computer station - desk terminal keyboard and cables");Undo.RegisterFullObjectHierarchyUndo(desk.gameObject,"Repair archive desk props and lighting");
        var lamp=desk.Find("Archivist desk lamp - complete assembly");if(!lamp)throw new Exception("Lamp missing");
        foreach(var child in lamp.Cast<Transform>().ToArray())Undo.DestroyObjectImmediate(child.gameObject);
        var green=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Environment/ManagerOffice/Grey olive filing steel.mat");var steel=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Environment/ManagerOffice/Painted warm grey steel.mat");var dark=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Environment/ManagerOffice/Bakelite and rubber.mat");var cylinder=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Art/Environment/ManagerOffice/Cylinder12.asset");
        lamp.localPosition=new Vector3(-.91f,.824f,-.24f);lamp.localRotation=Quaternion.identity;lamp.localScale=Vector3.one;
        Part("Solid weighted lamp foot",lamp,new Vector3(0,.014f,0),new Vector3(.095f,.028f,.08f),green,cylinder);
        Rod("Continuous lamp upright",lamp,new Vector3(0,.024f,0),new Vector3(0,.30f,0),.020f,steel);
        Part("Lamp head pivot joint",lamp,new Vector3(0,.30f,0),new Vector3(.044f,.045f,.040f),dark);
        Rod("Lamp head support neck",lamp,new Vector3(0,.30f,0),new Vector3(.025f,.374f,-.055f),.018f,steel);
        var hood=Group("Enamel shade - connected hood and diffuser",lamp,new Vector3(.025f,.388f,-.065f));hood.localRotation=Quaternion.Euler(0,0,-8);
        Part("Enamel top panel",hood,new Vector3(0,.025f,0),new Vector3(.26f,.018f,.16f),green);
        foreach(float x in new[]{-.121f,.121f})Part("Enamel side rim",hood,new Vector3(x,0,0),new Vector3(.018f,.055f,.16f),green);
        foreach(float z in new[]{-.071f,.071f})Part("Enamel end rim",hood,new Vector3(0,0,z),new Vector3(.23f,.055f,.018f),green);
        var diffuser=Material("Desk lamp warm diffuser",new Color(.65f,.54f,.35f));diffuser.EnableKeyword("_EMISSION");diffuser.SetColor("_EmissionColor",new Color(.40f,.25f,.10f));Part("Warm recessed diffuser",hood,new Vector3(0,.005f,0),new Vector3(.218f,.008f,.12f),diffuser);
        var task=Group("Warm workstation pool",lamp,new Vector3(.025f,.34f,-.065f)).gameObject.AddComponent<Light>();task.type=LightType.Spot;task.transform.localRotation=Quaternion.Euler(90,0,0);task.spotAngle=95;task.innerSpotAngle=55;task.intensity=1.2f;task.range=2.2f;task.color=new Color(1,.79f,.54f);task.shadows=LightShadows.Soft;
        var carton=desk.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Tied archive carton 9");var cartonParent=desk.Find("Desk archive carton - move as one");if(!cartonParent)cartonParent=Group("Desk archive carton - move as one",desk,Vector3.zero);carton.SetParent(cartonParent,false);cartonParent.localPosition=new Vector3(.77f,.8275f,.10f);carton.localPosition=Vector3.zero;
        // Rebuild the old desk batch without the lamp or carton, so their parents really move their geometry.
        foreach(var r in desk.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.name.StartsWith("Static detail - ")||r.name.StartsWith("Static desk - ")))r.enabled=false;
        var source=desk.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.GetComponent<MeshFilter>()&&!r.name.StartsWith("Static detail - ")&&!r.name.StartsWith("Static desk - ")&&!r.transform.IsChildOf(lamp)&&!r.transform.IsChildOf(cartonParent)&&!r.GetComponentInParent<ArchiveTerminalConversation>()).ToArray();
        foreach(var material in source.Select(r=>r.sharedMaterial).Distinct()){
            var mesh=new Mesh{name="Archive desk - "+material.name};mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.CombineMeshes(source.Where(r=>r.sharedMaterial==material).Select(r=>new CombineInstance{mesh=r.GetComponent<MeshFilter>().sharedMesh,transform=desk.worldToLocalMatrix*r.localToWorldMatrix}).ToArray(),true,true);
            string path=Folder+"Repaired desk - "+material.name.Replace('/','-')+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(saved){EditorUtility.CopySerialized(mesh,saved);Object.DestroyImmediate(mesh);}else{saved=mesh;AssetDatabase.CreateAsset(saved,path);}
            string name="Static desk - "+material.name;var child=desk.Find(name);if(!child)child=Group(name,desk,Vector3.zero);var mf=child.GetComponent<MeshFilter>();if(!mf)mf=child.gameObject.AddComponent<MeshFilter>();mf.sharedMesh=saved;var renderer=child.GetComponent<MeshRenderer>();if(!renderer)renderer=child.gameObject.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.enabled=true;
        }
        foreach(var r in source)r.enabled=false;foreach(var r in lamp.GetComponentsInChildren<MeshRenderer>())r.enabled=true;foreach(var r in carton.GetComponentsInChildren<MeshRenderer>())r.enabled=true;
        var terminal=Object.FindObjectOfType<ArchiveTerminalConversation>();var screen=terminal.transform.Find("Powered green phosphor screen");
        var spill=terminal.transform.Find("Monitor green light spill").GetComponent<Light>();spill.transform.position=screen.position-screen.forward*.055f;spill.transform.rotation=Quaternion.LookRotation(-screen.forward+Vector3.down*.12f);spill.type=LightType.Spot;spill.spotAngle=105;spill.innerSpotAngle=75;spill.range=3.5f;spill.intensity=2.5f;spill.color=new Color(.30f,.84f,.46f);spill.shadows=LightShadows.Soft;spill.shadowBias=.015f;spill.shadowNormalBias=.025f;
        const string cookiePath=Folder+"CRT rectangular soft light cookie.asset";var cookie=AssetDatabase.LoadAssetAtPath<Texture2D>(cookiePath);if(!cookie){cookie=new Texture2D(64,64,TextureFormat.RGBA32,false,true);cookie.name="CRT rectangular soft aperture";cookie.wrapMode=TextureWrapMode.Clamp;var pixels=new Color[4096];for(int y=0;y<64;y++)for(int x=0;x<64;x++){float u=Mathf.Abs((x+.5f)/32-1),v=Mathf.Abs((y+.5f)/32-1);float a=(1-Mathf.SmoothStep(.65f,.92f,u))*(1-Mathf.SmoothStep(.40f,.66f,v));pixels[y*64+x]=new Color(1,1,1,a);}cookie.SetPixels(pixels);cookie.Apply();AssetDatabase.CreateAsset(cookie,cookiePath);}spill.cookie=cookie;
        var volume=terminal.transform.Find("CRT screen - soft scattered light");if(!volume)volume=Group("CRT screen - soft scattered light",terminal.transform,Vector3.zero);volume.position=screen.position-screen.forward*.004f;volume.rotation=Quaternion.LookRotation(-screen.forward+Vector3.down*.12f);
        var shape=volume.GetComponent<MeshFilter>();if(!shape)shape=volume.gameObject.AddComponent<MeshFilter>();var unit=Resources.GetBuiltinResource<Mesh>("Cube.fbx");const string volumeMeshPath=Folder+"CRT light scattering bounds.asset";var volumeMesh=AssetDatabase.LoadAssetAtPath<Mesh>(volumeMeshPath);if(!volumeMesh){volumeMesh=new Mesh{name="CRT light scattering bounds"};volumeMesh.CombineMeshes(new[]{new CombineInstance{mesh=unit,transform=Matrix4x4.TRS(new Vector3(0,0,1.2f),Quaternion.identity,new Vector3(1.4f,1.25f,2.4f))}});AssetDatabase.CreateAsset(volumeMesh,volumeMeshPath);}shape.sharedMesh=volumeMesh;
        var scattering=volume.GetComponent<MeshRenderer>();if(!scattering)scattering=volume.gameObject.AddComponent<MeshRenderer>();const string volumeMatPath=Folder+"CRT green air scattering.mat";var volumeMat=AssetDatabase.LoadAssetAtPath<Material>(volumeMatPath);if(!volumeMat){volumeMat=new Material(Shader.Find("Recoil/3D/Screen Light Volume"));AssetDatabase.CreateAsset(volumeMat,volumeMatPath);}scattering.sharedMaterial=volumeMat;scattering.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;scattering.receiveShadows=false;
        var effect=volume.GetComponent<ScreenLightScattering>();if(!effect)effect=volume.gameObject.AddComponent<ScreenLightScattering>();effect.screenLight=spill;effect.density=.020f;effect.length=1.9f;effect.spread=.16f;effect.Sync();
        var phosphor=Material("CRT luminous green phosphor",new Color(.012f,.048f,.021f));phosphor.EnableKeyword("_EMISSION");phosphor.SetColor("_EmissionColor",new Color(.025f,.16f,.055f));screen.GetComponent<Renderer>().sharedMaterial=phosphor;
        EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);
        return "Saved connected desk lamp moved 44cm toward camera, separately movable carton with clearance, refreshed desk batch, shadowed rectangular CRT light plus depth-clipped air scattering.";
    }
}
