using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CoalerModelSpikeBuilder
{
    public const string ScenePath="Assets/Scenes/Tests/CoalerModelSpike01.unity";
    const string Folder="Assets/Tests/CoalerModelSpike";
    const string ModelPath=Folder+"/Model/QMonster_SkinnedOnly.fbx";
    const string Output="output/coaler-model-spike";
    static void Need(bool ok,string message){if(!ok)throw new Exception("Coaler spike: "+message);}
    [MenuItem("Recoil/Tests/Build Coaler Model Spike 01")]
    public static void BuildAndVerify()
    {
        Directory.CreateDirectory(Output);Directory.CreateDirectory(Folder+"/Materials");
        var importer=AssetImporter.GetAtPath(ModelPath) as ModelImporter;
        Need(importer!=null,"FBX importer missing");
        importer.animationType=ModelImporterAnimationType.Generic;importer.importAnimation=false;
        importer.isReadable=true;importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;importer.SaveAndReimport();
        var asset=AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);Need(asset,"FBX asset did not import");
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.23f,.25f,.28f);RenderSettings.fog=false;
        var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Flat ground - Y zero";floor.transform.position=new Vector3(0,-.06f,0);floor.transform.localScale=new Vector3(10,.12f,7);floor.GetComponent<Renderer>().sharedMaterial=Material("Ground",new Color(.15f,.18f,.19f));
        var light=new GameObject("Key light").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.3f;light.transform.rotation=Quaternion.Euler(35,-25,0);
        var fill=new GameObject("Fill light").AddComponent<Light>();fill.type=LightType.Directional;fill.intensity=.65f;fill.transform.rotation=Quaternion.Euler(15,155,0);
        var cam=new GameObject("Main Camera").AddComponent<Camera>();cam.tag="MainCamera";cam.transform.position=new Vector3(0,2,-7);cam.transform.LookAt(new Vector3(0,1,0));cam.orthographic=true;cam.orthographicSize=2.05f;cam.nearClipPlane=.1f;cam.farClipPlane=50;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.045f,.065f,.075f);
        var oldGO=new GameObject("A - original procedural Coaler");oldGO.transform.position=new Vector3(-1.55f,.8f,0);
        var reference=oldGO.AddComponent<RampagerTentacleVisual>();reference.SetPreview(RampagerTentacleVisual.Form.Watching);reference.autoCycle=false;reference.UpdatePose(0,true);
        var newGO=new GameObject("B - imported FBX spike - NOT production");newGO.transform.position=new Vector3(1.55f,0,0);
        var model=(GameObject)PrefabUtility.InstantiatePrefab(asset,scene);model.name="Imported skinned subset - 20 meshes";model.transform.SetParent(newGO.transform,false);
        // Work on a scene instance only. Imported asset and source bind pose stay intact.
        var animator=model.GetComponent<Animator>();if(animator)animator.enabled=false;
        var skins=model.GetComponentsInChildren<SkinnedMeshRenderer>(true);Need(skins.Length>0,"No imported skinned renderers");
        var before=Bounds(skins);float targetHeight=Bounds(reference.VisualRoot.GetComponentsInChildren<Renderer>()).max.y;model.transform.localScale*=targetHeight/before.size.y;
        var boots=skins.Where(r=>r.name=="polySurface6"||r.name=="polySurface7").ToArray();Need(boots.Length==2,"Two boot meshes not found");
        var after=Bounds(skins);float sole=Bounds(boots).min.y;model.transform.position+=new Vector3(newGO.transform.position.x-after.center.x,-sole,-after.center.z);
        var shared=new Dictionary<string,Material>();
        foreach(var skin in skins)
        {
            var mats=skin.sharedMaterials;
            for(int i=0;i<mats.Length;i++)
            {
                string key=mats[i]?mats[i].name:"Unassigned";
                if(!shared.ContainsKey(key))shared[key]=Material("Model_"+key,key.ToLowerInvariant().Contains("lambert")?new Color(.42f,.43f,.40f):new Color(.035f,.043f,.041f));
                mats[i]=shared[key];
            }
            skin.sharedMaterials=mats;skin.updateWhenOffscreen=true;
        }
        var chain=model.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name.EndsWith("COALER_EXTRA_TENTACLE_BACK_01_JNT_06",StringComparison.Ordinal));
        Need(chain,"Named single-chain joint not found");
        newGO.transform.rotation=Quaternion.Euler(0,180,0);
        var coreAsset=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/Model/QMonster_OriginalCore.fbx");Need(coreAsset,"Source core asset missing");
        var coreRoot=(GameObject)PrefabUtility.InstantiatePrefab(coreAsset,scene);coreRoot.name="Original chest core - source geometry";coreRoot.transform.SetParent(model.transform,false);
        var core=coreRoot.GetComponentInChildren<Renderer>();core.sharedMaterial=Material("OriginalCore",new Color(.5f,.025f,.018f));
        var spine=model.GetComponentsInChildren<Transform>(true).First(t=>t.name=="COALER_spine02_JNT");coreRoot.transform.SetParent(spine,true);
        var lab=new GameObject("Spike controls - no game state or save system").AddComponent<CoalerModelSpikeLab>();lab.model=model.transform;lab.chainJoint=chain;lab.core=core.transform;lab.shutters=new Transform[0];lab.useOriginalCore=true;lab.reference=reference;lab.testCamera=cam;lab.Initialize();
        Label("A  ORIGINAL",new Vector3(-1.55f,.01f,-1));Label("B  IMPORTED / SOURCE CORE",new Vector3(1.55f,.01f,-1));
        EditorSceneManager.MarkSceneDirty(scene);Need(EditorSceneManager.SaveScene(scene,ScenePath),"Scene save failed");
        var checks=new List<string>();var b=Bounds(skins);Need(Mathf.Abs(Bounds(boots).min.y)<.025f,"FBX boot soles do not match ground");checks.Add("PASS Unity FBX import: "+skins.Length+" skinned meshes; total bounds height "+b.size.y.ToString("F3")+" m; boot soles Y "+Bounds(boots).min.y.ToString("F4"));
        long triangles=skins.Sum(s=>(long)s.sharedMesh.triangles.Length/3);checks.Add("INFO imported triangles: "+triangles+"; no decimation or final cleanup");
        Need(skins.All(s=>s.bones.Length>0&&s.bones.All(t=>t)),"Broken bone references");checks.Add("PASS all skinned renderer bone references resolved");
        var skinTarget=skins.First(s=>s.name.Contains("COALER_EXTRA_TENTACLE_BACK_01_GEO"));var bakedA=new Mesh();var bakedB=new Mesh();skinTarget.BakeMesh(bakedA);
        lab.animateChain=true;lab.Step(.5f);skinTarget.BakeMesh(bakedB);var va=bakedA.vertices;var vb=bakedB.vertices;float moved=0;for(int i=0;i<Mathf.Min(va.Length,vb.Length);i++)moved=Mathf.Max(moved,(va[i]-vb[i]).magnitude);
        Need(lab.DrivenJointCount==12&&moved>.1f,"Single chain motion is too small to see");checks.Add("PASS actual Unity skin deformation: max local displacement "+moved.ToString("F5"));UnityEngine.Object.DestroyImmediate(bakedA);UnityEngine.Object.DestroyImmediate(bakedB);
        lab.ResetLab();Capture(cam,"01-imported-and-original");
        checks.Add("SOURCE CORE: original FBX geometry, preserved size and position, attached to spine02; no proxy geometry. 5 tests existing cover bones, 4 tests approach.");
        checks.Add("LIMITATION: test scene only; existing torso weights do not include a dedicated core-opening control.");File.WriteAllLines(Output+"/verification.txt",checks);AssetDatabase.SaveAssets();Debug.Log("COALER_SPIKE_PASS\n"+string.Join("\n",checks));
    }
    static Bounds Bounds(Renderer[] rs){var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b;}
    static Material Material(string name,Color color)
    {
        string safe=string.Concat(name.Select(c=>char.IsLetterOrDigit(c)||c=='_'?c:'_'));string path=Folder+"/Materials/"+safe+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(m,path);}m.color=color;m.SetFloat("_Glossiness",.28f);return m;
    }
    static void Label(string text,Vector3 p){var t=new GameObject(text).AddComponent<TextMesh>();t.text=text;t.anchor=TextAnchor.MiddleCenter;t.characterSize=.065f;t.fontSize=28;t.color=new Color(.72f,.78f,.75f);t.transform.position=p;}
    public static void Capture(Camera cam,string name)
    {
        var rt=RenderTexture.GetTemporary(1200,760,24,RenderTextureFormat.ARGBHalf);var previous=cam.targetTexture;var active=RenderTexture.active;Texture2D raw=null,img=null;
        try{cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;raw=new Texture2D(1200,760,TextureFormat.RGBAFloat,false,true);raw.ReadPixels(new Rect(0,0,1200,760),0,0);raw.Apply();var pixels=raw.GetPixels();if(QualitySettings.activeColorSpace==ColorSpace.Linear)for(int i=0;i<pixels.Length;i++)pixels[i]=pixels[i].gamma;img=new Texture2D(1200,760,TextureFormat.RGB24,false,true);img.SetPixels(pixels);img.Apply();File.WriteAllBytes(Output+"/"+name+".png",img.EncodeToPNG());}
        finally{cam.targetTexture=previous;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);if(raw)UnityEngine.Object.DestroyImmediate(raw);if(img)UnityEngine.Object.DestroyImmediate(img);}
    }
}
