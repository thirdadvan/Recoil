using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

public static class Recoil3DCameraWindowPass
{
    const string Marker="Camera and boarded window revision";
    static Material wood,glow;
    static Texture2D fullCookie,halfCookie;
    static Material Material(string name,Color colour,Color emission)
    {
        var path="Assets/Recoil3D/Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){m=new Material(Shader.Find("Recoil/3D/Weathered Surface"));AssetDatabase.CreateAsset(m,path);}
        m.color=colour;m.SetColor("_EmissionColor",emission);m.SetFloat("_HazeDensity",0);m.SetFloat("_Weathering",.75f);m.enableInstancing=true;EditorUtility.SetDirty(m);return m;
    }
    static Texture2D Cookie(bool half)
    {
        string path="Assets/Recoil3D/Materials/"+(half?"Half boarded window cookie":"Boarded window slit cookie")+".asset";
        var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(path);if(tex)return tex;
        tex=new Texture2D(64,128,TextureFormat.Alpha8,false,true);tex.name=half?"Half-open window light mask":"Board gaps light mask";tex.wrapMode=TextureWrapMode.Clamp;tex.filterMode=FilterMode.Bilinear;
        var pixels=new Color[64*128];for(int y=0;y<128;y++)for(int x=0;x<64;x++)
        {
            float gap=y%18<2?1:0;float a=half&&x<31?1:gap;
            if(x<2||x>61||y<2||y>125)a=0;pixels[y*64+x]=new Color(1,1,1,a);
        }
        tex.SetPixels(pixels);tex.Apply();AssetDatabase.CreateAsset(tex,path);return tex;
    }
    static Transform Group(string name,Transform parent,Vector3 worldPosition)
    {var t=new GameObject(name).transform;t.SetParent(parent,false);t.position=worldPosition;return t;}
    static GameObject Part(string name,Transform parent,Vector3 pos,Vector3 size,Material material)
    {var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=pos;g.transform.localScale=size;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());g.GetComponent<MeshRenderer>().sharedMaterial=material;g.isStatic=true;return g;}
    static bool WindowGlass(MeshRenderer r)
    {return r.enabled&&r.gameObject.activeInHierarchy&&(r.name=="High wire glass window"||r.name=="Dirty illuminated glass"||r.name=="3D mesh - Window recess");}
    static void BoardWindow(MeshRenderer glass,bool half)
    {
        var b=glass.bounds;if(b.size.x<.2f||b.size.y<.2f)return;
        // A world-sized child of the existing window; moving the window carries its boards/light with it.
        var t=Group(half?"Boarded window - one half exposed":"Boarded window - narrow light gaps",glass.transform,b.center);
        var scale=t.parent.lossyScale;t.localScale=new Vector3(1/Mathf.Abs(scale.x),1/Mathf.Abs(scale.y),1/Mathf.Abs(scale.z));t.rotation=Quaternion.identity;
        float width=b.size.x,height=b.size.y,front=-b.size.z*.5f;
        Part("Light behind the boards",t,new Vector3(0,0,front-.015f),new Vector3(width,height,.015f),glow);
        float boardWidth=half?width*.5f:width;float x=half?width*.25f:0;
        int rows=Mathf.Max(4,Mathf.CeilToInt(height/.25f));float pitch=height/rows;float gap=Mathf.Min(.04f,pitch*.14f);
        for(int i=0;i<rows;i++)
        {
            float y=-height*.5f+pitch*(i+.5f);float slight=(i%3-1)*.7f;
            var board=Part("Weathered timber board "+(i+1),t,new Vector3(x,y,front-.09f),new Vector3(boardWidth+.035f,pitch-gap,.10f),wood);board.transform.localRotation=Quaternion.Euler(0,0,slight);
            foreach(float sign in new[]{-1f,1f})Part("Rusty nail",t,new Vector3(x+sign*(boardWidth*.5f-.07f),y,front-.145f),new Vector3(.025f,.025f,.008f),AssetDatabase.LoadAssetAtPath<Material>("Assets/Recoil3D/Materials/Flaking oxidised metal.mat"));
        }
        // The original unmasked lamp is replaced by a spot cookie, so the covered opening does not cast a broad pool.
        if(glass.transform.parent && glass.transform.parent.name=="Isolated lit window")
            foreach(var l in glass.transform.parent.GetComponentsInChildren<Light>(true))l.enabled=false;
        var light=Group("Window light - projects gaps",t,Vector3.zero).gameObject.AddComponent<Light>();light.transform.localPosition=new Vector3(0,.1f,front-.25f);
        light.type=LightType.Spot;light.transform.localRotation=Quaternion.LookRotation(new Vector3(0,-.28f,-1),Vector3.up);light.spotAngle=48;light.range=18;
        light.intensity=half?.8f:.35f;light.color=new Color(.58f,.68f,.72f);light.cookie=half?halfCookie:fullCookie;light.shadows=LightShadows.None;
        // If the original lamp shared the same parent, it may have been disabled above.
        light.enabled=true;
    }
    static void AttachStoreDetails(Scene scene)
    {
        var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
        var facadeRoot=all.First(t=>t.name=="Store facade");var facade=facadeRoot.GetComponentsInChildren<Recoil3DSpriteVisual>(true).First(v=>v.source&&v.source.enabled&&!v.suppressed&&v.meshRenderer.bounds.size.x>5);float face=facade.meshRenderer.bounds.min.z;
        var store=facadeRoot.parent;var group=Group("Wall-mounted windows and poster",store,new Vector3(0,0,face));
        foreach(var v in store.GetComponentsInChildren<Recoil3DSpriteVisual>(true).Where(v=>v.source&&(v.source.name=="Shop window"||v.source.name=="Window crossbar"||v.GetComponent<ReadableProp>())).ToArray())
        {
            var pos=v.transform.position;pos.z=face-(v.source.name=="Window crossbar"?.09f:.04f);v.transform.SetParent(group,true);v.transform.position=pos;
            foreach(var layer in v.GetComponentsInChildren<Recoil3DSpriteVisual>(true)){layer.depth=v.GetComponent<ReadableProp>()?.018f:v.source.name=="Window crossbar"?.06f:.035f;layer.depthOffset=0;layer.Sync();}
        }
    }
    [MenuItem("Recoil/3D/Apply Low Camera And Boarded Windows")]
    public static string Apply()
    {
        if(EditorApplication.isPlaying)throw new Exception("Exit Play first.");
        for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new Exception("Save scene edits first.");
        wood=Material("Window weathered timber",new Color(.19f,.17f,.125f),Color.black);glow=Material("Window gap light",new Color(.17f,.2f,.22f),new Color(.2f,.27f,.3f));fullCookie=Cookie(false);halfCookie=Cookie(true);
        var previous=SceneManager.GetActiveScene();int scenes=0,windows=0;
        foreach(var entry in EditorBuildSettings.scenes.Where(e=>e.enabled&&e.path.StartsWith(Recoil3DScene.Folder)))
        {
            var s=SceneManager.GetSceneByPath(entry.path);bool added=!s.isLoaded;if(added)s=EditorSceneManager.OpenScene(entry.path,OpenSceneMode.Additive);SceneManager.SetActiveScene(s);
            try
            {
                var all=s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();var w=all.Select(t=>t.GetComponent<RecoilPrototype>()).First(t=>t);
                if(w.transform.Find(Marker))continue;
                bool outside=entry.path.EndsWith("/RecoilPrototype.unity");var cam=all.Select(t=>t.GetComponent<Camera>()).First(c=>c);var f=cam.GetComponent<RecoilCameraController>();
                f.use3DComposition=true;f.pitch3D=-2.5f;f.panY3D=outside?-.55f:-.45f;f.frameOnGameplayPlane=true;
                if(outside){f.cameraDepth=-19.5f;f.viewSize=6.7f;var fx=cam.GetComponent<BroadcastRefractionEffect>();if(fx)fx.perspectiveReferenceDistance=19.5f;AttachStoreDetails(s);}
                else if(!entry.path.EndsWith("/RecoilFactorySewer.unity"))
                {
                    var glass=all.Select(t=>t.GetComponent<MeshRenderer>()).Where(r=>r&&WindowGlass(r)).OrderBy(r=>r.name=="3D mesh - Window recess"?0:1).ThenBy(r=>Mathf.Abs(r.bounds.center.x-w.Player.transform.position.x)).ToArray();
                    for(int i=0;i<glass.Length;i++){BoardWindow(glass[i],i==0);windows++;}
                }
                bool mouse=f.mouseLookEnabled;f.mouseLookEnabled=false;f.SnapToTarget();f.mouseLookEnabled=mouse;
                Group(Marker,w.transform,w.transform.position);EditorSceneManager.MarkSceneDirty(s);EditorSceneManager.SaveScene(s);scenes++;
            }
            finally{if(added)EditorSceneManager.CloseScene(s,true);}
        }
        AssetDatabase.SaveAssets();SceneManager.SetActiveScene(previous);return "Adjusted "+scenes+" scenes and boarded "+windows+" windows.";
    }
    // Captures the saved follow-camera composition, without changing its pitch to the old downward angle.
    public static string CaptureCurrent(string file)
    {
        var cam=Camera.main;if(!cam)throw new Exception("No MainCamera");var old=cam.targetTexture;var oldAspect=cam.aspect;var rt=new RenderTexture(1600,900,24);var tex=new Texture2D(1600,900,TextureFormat.RGB24,false);var active=RenderTexture.active;
        try{cam.aspect=16f/9;cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1600,900),0,0);tex.Apply();Directory.CreateDirectory("Captures/CameraWindowPass");File.WriteAllBytes("Captures/CameraWindowPass/"+file,tex.EncodeToPNG());return file;}
        finally{cam.targetTexture=old;cam.aspect=oldAspect;RenderTexture.active=active;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);}
    }
}
