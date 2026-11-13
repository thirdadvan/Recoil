using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class FactoryGateOccupationModels
{
    const string Folder = "Assets/Recoil3D/GateOccupation/";
    static Transform Root(Scene scene) { return scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Single(t=>t.name=="Right factory monumental gate and abandoned occupation camp - editable"); }
    public static string Install()
    {
        var scene=SceneManager.GetActiveScene(); var root=Root(scene);var log=new System.Text.StringBuilder();
        string[] files={"Battered_workers_double_locker","Discarded_enamel_cooking_pot","Destroyed_workers_tarp_shelter_bedding","Overturned_battered_factory_tool_cabinet"};
        string[] anchors={"Meshy locker anchor","Meshy pot anchor","Meshy shelter anchor","Meshy tool cabinet anchor"};
        float[] sizes={1.85f,.65f,3.5f,1.1f};
        Vector3[] rotations={new Vector3(0,18,87),new Vector3(0,49,12),new Vector3(0,-9,0),new Vector3(0,-19,71)};
        for(int i=0;i<files.Length;i++)
        {
            var anchor=root.GetComponentsInChildren<Transform>(true).Single(t=>t.name==anchors[i]);
            if(anchor.childCount>0){log.AppendLine(files[i]+" already placed");continue;}
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"Meshy/"+files[i]+"-game.glb");if(!source)throw new Exception("Missing optimized model "+files[i]);
            var go=(GameObject)PrefabUtility.InstantiatePrefab(source,scene);go.name="Meshy - "+files[i];go.transform.SetParent(anchor,false);
            PrefabUtility.UnpackPrefabInstance(go,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
            var rs=go.GetComponentsInChildren<Renderer>(true);var b=rs[0].bounds;foreach(var renderer in rs)b.Encapsulate(renderer.bounds);
            go.transform.localScale*=sizes[i]/Mathf.Max(b.size.x,b.size.y,b.size.z);go.transform.localRotation=Quaternion.Euler(rotations[i]);
            b=rs[0].bounds;foreach(var renderer in rs)b.Encapsulate(renderer.bounds);
            go.transform.position+=new Vector3(anchor.position.x-b.center.x,.035f-b.min.y,anchor.position.z-b.center.z);
            foreach(var renderer in rs)
            {
                string path=Folder+files[i]+" textured.mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(!mat){mat=new Material(Shader.Find("Recoil/3D/Weathered Surface"));mat.mainTexture=renderer.sharedMaterial.GetTexture("baseColorTexture");AssetDatabase.CreateAsset(mat,path);}
                mat.color=new Color(.75f,.77f,.72f);mat.SetFloat("_HazeDensity",0);mat.SetFloat("_Weathering",.35f);mat.SetFloat("_Glossiness",.1f);renderer.sharedMaterial=mat;EditorUtility.SetDirty(mat);
            }
            log.AppendLine(files[i]+" placed; "+go.GetComponentsInChildren<MeshFilter>().Sum(f=>f.sharedMesh.triangles.Length/3)+" triangles");
        }
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);return log.ToString();
    }
    public static string Validate()
    {
        var scene=SceneManager.GetActiveScene();var root=Root(scene);var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
        var wall=all.Single(t=>t.name=="Factory right perimeter wall");
        if(Mathf.Abs(root.position.x-wall.position.x)>.001f)throw new Exception("Boundary collision misaligned");
        if(root.GetComponentsInChildren<Collider>(true).Any(c=>!c.transform.IsChildOf(wall))||root.GetComponentsInChildren<Collider2D>(true).Any(c=>!c.transform.IsChildOf(wall)))throw new Exception("New decoration collision");
        if(root.GetComponentsInChildren<Light>(true).Length!=0)throw new Exception("Unexpected live camp or gate light");
        var surface=root.GetComponentsInChildren<Renderer>().Single(r=>r.name=="Worn foreground vehicle road");
        float lamps=all.Where(t=>t.name.StartsWith("Yard street light ")).Average(t=>t.position.z);
        if(surface.bounds.max.z>=lamps)throw new Exception("Road is not in front of lamps");
        var camp=root.GetComponentsInChildren<Transform>().Single(t=>t.name=="Abandoned ransacked roadside workers camp");
        if(camp.position.z<6)throw new Exception("Camp intrudes on drain area");
        foreach(string name in new[]{"Meshy locker anchor","Meshy pot anchor","Meshy shelter anchor","Meshy tool cabinet anchor"})if(root.GetComponentsInChildren<Transform>().Single(t=>t.name==name).childCount!=1)throw new Exception("Missing or duplicate detailed prop "+name);
        foreach(var prop in root.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("Meshy - ")))
        {var rs=prop.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);if(b.min.y<-.01f||b.min.y>.12f)throw new Exception("Prop not grounded: "+prop.name+" "+b);}
        Physics2D.SyncTransforms();var hit=Physics2D.RaycastAll(new Vector2(root.position.x-2,1.3f),Vector2.right,3).FirstOrDefault(h=>h.collider.name=="Hard perimeter wall");
        if(!hit.collider||Mathf.Abs(hit.point.x-(root.position.x-.1f))>.02f)throw new Exception("Closed boundary ray test failed");
        string report="PASS: four Meshy props present, grounded and optimized; gate X="+root.position.x+"; closed boundary ray hit="+hit.point+"; road Z="+surface.bounds.min.z+".."+surface.bounds.max.z+" camera-side of lamps Z="+lamps+"; camp Z="+camp.position.z+" clear of drain and sewer return; no new decoration colliders or lights. Runtime Meshy triangles="+root.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("Meshy - ")).Sum(t=>t.GetComponentsInChildren<MeshFilter>().Sum(f=>f.sharedMesh.triangles.Length/3));
        File.AppendAllText("output/gate-occupation/verification.txt","\n"+report);return report;
    }
    public static void Capture()
    {
        Shot("Overview",new Vector3(131,12,-27),new Vector3(147,2,-1),false);
        Shot("GateDetail",new Vector3(133,7,-18),new Vector3(151,3,-6.4f),false);
        Shot("CampDetail",new Vector3(140,5,-3),new Vector3(147,1,6),false);
        Shot("CampInspection",new Vector3(140,6,-3),new Vector3(147,1,6),true);
        Shot("GateInspection",new Vector3(133,10,-21),new Vector3(151,3,-6.4f),true);
    }
    static void Shot(string name,Vector3 position,Vector3 target,bool inspection)
    {
        Directory.CreateDirectory("Captures/GateOccupation");var cameraObject=new GameObject("Temporary gate capture camera");GameObject lightObject=null;
        try
        {
            var cam=cameraObject.AddComponent<Camera>();cam.CopyFrom(Camera.main);cam.enabled=false;cam.fieldOfView=50;cam.transform.position=position;cam.transform.LookAt(target);
            if(inspection){lightObject=new GameObject("Temporary detail inspection light");var light=lightObject.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.4f;light.color=new Color(1,.94f,.85f);light.shadows=LightShadows.None;light.transform.rotation=Quaternion.Euler(48,-25,0);}
            var rt=RenderTexture.GetTemporary(1600,1000,24,RenderTextureFormat.ARGBHalf);var previous=RenderTexture.active;Texture2D raw=null,image=null;
            try
            {
                cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;raw=new Texture2D(1600,1000,TextureFormat.RGBAFloat,false,true);raw.ReadPixels(new Rect(0,0,1600,1000),0,0);raw.Apply();var pixels=raw.GetPixels();
                if(QualitySettings.activeColorSpace==ColorSpace.Linear)for(int i=0;i<pixels.Length;i++)pixels[i]=pixels[i].gamma;
                image=new Texture2D(1600,1000,TextureFormat.RGB24,false,true);image.SetPixels(pixels);image.Apply();File.WriteAllBytes("Captures/GateOccupation/"+name+".png",image.EncodeToPNG());
            }
            finally{cam.targetTexture=null;RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);if(raw)Object.DestroyImmediate(raw);if(image)Object.DestroyImmediate(image);}
        }
        finally{Object.DestroyImmediate(cameraObject);if(lightObject)Object.DestroyImmediate(lightObject);}
    }
}
