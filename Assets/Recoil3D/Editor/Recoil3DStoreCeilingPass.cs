using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

// Authored geometry only: the existing door owns opening state and scene travel.
public static class Recoil3DStoreCeilingPass
{
    static Material plaster,metal,dark,glass;
    static Transform Group(string name,Transform parent,Vector3 position)
    {var t=new GameObject(name).transform;t.SetParent(parent,false);t.position=position;t.rotation=Quaternion.identity;var s=parent.lossyScale;t.localScale=new Vector3(1/s.x,1/s.y,1/s.z);return t;}
    static GameObject Part(string name,Transform parent,Vector3 p,Vector3 size,Material mat)
    {var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=p;g.transform.localScale=size;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());g.GetComponent<MeshRenderer>().sharedMaterial=mat;return g;}
    static Material Mat(string name,Color color,string template)
    {
        string path="Assets/Recoil3D/Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){m=new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/Recoil3D/Materials/"+template+".mat"));AssetDatabase.CreateAsset(m,path);}
        m.color=color;EditorUtility.SetDirty(m);return m;
    }
    static void Slab(Transform root,string name,float left,float right,float front,float back)
    {
        Part(name,root,new Vector3((left+right)/2,4.7f,(front+back)/2),new Vector3(right-left,.35f,back-front),plaster);
        // Sparse panel seams and stains keep the low ceiling readable without filling the room with light.
        for(float x=left+.8f;x<right-.15f;x+=1.25f)Part("Recessed ceiling panel seam",root,new Vector3(x,4.519f,(front+back)/2),new Vector3(.014f,.008f,back-front),dark);
        for(float z=front+.8f;z<back-.1f;z+=1.25f)Part("Ceiling panel joint",root,new Vector3((left+right)/2,4.518f,z),new Vector3(right-left,.009f,.012f),dark);
    }
    static void Lid(Transform source,bool opened)
    {
        var root=Group(opened?"Raised skylight cover - real depth":"Closed skylight cover - fits opening",source,new Vector3(9.5f,4.9f,0));
        root.rotation=Quaternion.Euler(0,0,opened?-78:0);
        Part("Dirty wired glazing",root,new Vector3(-1.5f,0,0),new Vector3(2.85f,.075f,2.4f),glass);
        foreach(float x in new[]{-3f,0f})Part("Lid side rail",root,new Vector3(x,0,0),new Vector3(.10f,.12f,2.5f),metal);
        foreach(float z in new[]{-1.25f,1.25f})Part("Lid end rail",root,new Vector3(-1.5f,0,z),new Vector3(3.1f,.12f,.10f),metal);
        Part("Glazing centre brace",root,new Vector3(-1.5f,.045f,0),new Vector3(.045f,.04f,2.4f),metal);
        Part("Inside pull handle",root,new Vector3(-2.7f,-.12f,0),new Vector3(.18f,.08f,.38f),metal);
    }
    public static string Apply()
    {
        var scene=SceneManager.GetActiveScene();if(EditorApplication.isPlayingOrWillChangePlaymode||scene.path!="Assets/Scenes/3D/RecoilServiceStore.unity")throw new Exception("Open the 3D service store outside Play mode.");
        var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
        var world=all.Select(t=>t.GetComponent<RecoilPrototype>()).First(w=>w);
        if(world.transform.Find("Service store - rebuilt ceiling and skylight"))return "Already rebuilt.";
        plaster=Mat("Store ceiling stained plaster",new Color(.19f,.205f,.19f),"Pitted concrete");metal=Mat("Store skylight oxidised frame",new Color(.18f,.20f,.19f),"Flaking oxidised metal");dark=Mat("Store ceiling damp seams",new Color(.075f,.085f,.077f),"Pitted concrete");glass=Mat("Store skylight dirty glass",new Color(.12f,.17f,.17f),"Pitted concrete");
        foreach(var visual in all.Select(t=>t.GetComponent<Recoil3DSpriteVisual>()).Where(v=>v&&(v.name=="Ceiling left"||v.name=="Ceiling right"||v.name=="Closed skylight"||v.name=="Open skylight lid")))
        {visual.suppressed=true;visual.Sync();EditorUtility.SetDirty(visual);}
        // Remove generic migrated slabs and girders which incorrectly bridge or continue the opening as a slit.
        foreach(var t in all.Where(t=>t.name=="Roof girder"||t.name=="Upper floor extends into office depth"||t.name=="Ceiling service main"||t.name=="Small cable tray"))t.gameObject.SetActive(false);
        var root=Group("Service store - rebuilt ceiling and skylight",world.transform,Vector3.zero);
        Slab(root,"Ceiling - left of opening",0,6.5f,-1.3f,9.6f);Slab(root,"Ceiling - right of opening",9.5f,16,-1.3f,9.6f);
        // The camera-facing wall and matching roof strip are cut away, keeping the hatch visible.
        Slab(root,"Ceiling - rear of opening",6.5f,9.5f,1.3f,9.6f);
        foreach(float x in new[]{2f,5.5f,10.5f,14f})Part("Exposed ceiling support",root,new Vector3(x,4.43f,4.15f),new Vector3(.09f,.16f,10.9f),metal);
        var hatch=all.Select(t=>t.GetComponent<RecoilDoor>()).First(d=>d&&d.name=="Central skylight");
        var opening=Group("Skylight curb and shallow light well",hatch.transform,new Vector3(8,4.7f,0));
        foreach(float x in new[]{-1.56f,1.56f})
        {
            Part("Vertical light well lining",opening,new Vector3(x,.13f,0),new Vector3(.12f,.57f,2.72f),plaster);
            Part("Oxidised side reveal",opening,new Vector3(x,-.2f,0),new Vector3(.15f,.06f,2.72f),metal);
        }
        // Cut away the camera-facing curb: it otherwise projects across the playable exit.
        foreach(float z in new[]{1.36f})
        {
            Part("Light well end",opening,new Vector3(0,.23f,z),new Vector3(3.24f,.20f,.12f),plaster);
            Part("End reveal trim",opening,new Vector3(0,-.2f,z),new Vector3(3.24f,.06f,.15f),metal);
        }
        foreach(float z in new[]{-.86f,.86f})Part("Skylight hinge",opening,new Vector3(1.54f,.22f,z),new Vector3(.18f,.10f,.24f),metal);
        Lid(hatch.closedVisual,false);Lid(hatch.openVisual,true);
        // Keep the exit's existing XY collision, item requirements and auto-travel behaviour.
        var centralLamp=all.First(t=>t.name=="Store ceiling lamp 1");centralLamp.position=new Vector3(10.8f,4.2f,1.8f);
        var lightRoot=Group("Skylight daylight - child of opening",opening,new Vector3(8,5.25f,0));var light=lightRoot.gameObject.AddComponent<Light>();light.type=LightType.Spot;light.transform.rotation=Quaternion.Euler(90,0,0);light.range=2.8f;light.spotAngle=80;light.intensity=2f;light.color=new Color(.52f,.64f,.69f);light.shadows=LightShadows.None;light.renderMode=LightRenderMode.ForcePixel;
        var rim=Group("Soft sky bounce on inner curb",opening,new Vector3(8,4.35f,-.25f)).gameObject.AddComponent<Light>();rim.type=LightType.Point;rim.range=1.7f;rim.intensity=2.5f;rim.color=new Color(.48f,.62f,.68f);rim.renderMode=LightRenderMode.ForcePixel;
        var sky=Mat("Store skylight faint sky",new Color(.10f,.15f,.17f),"Pitted concrete");sky.SetColor("_EmissionColor",new Color(.16f,.22f,.26f));
        var outside=Group("Sky glimpse above open hatch",hatch.openVisual,new Vector3(8,5.75f,1.5f));Part("Muted sky beyond roof",outside,Vector3.zero,new Vector3(2.9f,1.55f,.025f),sky);
        EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssets();return "Four ceiling sections surround a real 3m by 2.6m opening at the gameplay plane; raised framed cover, well lining, moved centre fixture.";
    }
}
