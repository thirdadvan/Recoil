using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class Recoil3DArtRefinement
{
    static Material Mat(string name){return AssetDatabase.LoadAssetAtPath<Material>("Assets/Recoil3D/Materials/"+name+".mat");}
    static GameObject Part(string name,Transform parent,Vector3 p,Vector3 size,string mat,PrimitiveType primitive=PrimitiveType.Cube)
    {var g=GameObject.CreatePrimitive(primitive);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=p;g.transform.localScale=size;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());g.GetComponent<Renderer>().sharedMaterial=Mat(mat);return g;}
    public static string Apply()
    {
        var original=SceneManager.GetActiveScene();int count=0;
        foreach(var entry in EditorBuildSettings.scenes.Where(e=>e.enabled && e.path.StartsWith(Recoil3DScene.Folder)))
        {
            var s=EditorSceneManager.OpenScene(entry.path,OpenSceneMode.Additive);SceneManager.SetActiveScene(s);
            try
            {
                var w=s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<RecoilPrototype>()).Single();
                var bounce=w.transform.Find("3D soft ambient bounce");if(!bounce){bounce=new GameObject("3D soft ambient bounce").transform;bounce.SetParent(w.transform,false);var l=bounce.gameObject.AddComponent<Light>();l.type=LightType.Directional;l.shadows=LightShadows.None;l.color=new Color(.68f,.76f,.79f);l.intensity=.45f;bounce.rotation=Quaternion.Euler(12,-8,0);}
                // The original flat background must not occlude true depth or fill the frame with opaque sprites.
                foreach(var actor in w.GetComponentsInChildren<Recoil3DActorVisual>(true))foreach(var sprite in actor.GetComponentsInChildren<SpriteRenderer>(true))sprite.forceRenderingOff=true;
                var floors=w.GetComponentsInChildren<BoxCollider2D>(true).Where(c=>!c.isTrigger && c.bounds.size.x>5 && c.bounds.size.y<1.6f).ToArray();
                if(floors.Length>0 && !w.transform.Find("3D continuous foreground floor"))
                {float min=floors.Min(c=>c.bounds.min.x),max=floors.Max(c=>c.bounds.max.x);Part("3D continuous foreground floor",w.transform,new Vector3((min+max)*.5f,-.4f,0),new Vector3(max-min,.7f,65),"Pitted concrete");}
                if(entry.path.EndsWith("RecoilPrototype.unity"))
                {
                    var truck=w.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Truck - soft canvas roof");
                    if(!truck.Find("3D military canvas truck"))
                    {
                        foreach(var bridge in truck.GetComponentsInChildren<Recoil3DSpriteVisual>(true)){bridge.suppressed=true;bridge.Sync();}
                        foreach(var t in truck.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("Truck solid"))){var r=t.GetComponent<Renderer>();if(r)r.enabled=false;}
                        var model=new GameObject("3D military canvas truck").transform;model.SetParent(truck,false);
                        Part("Chassis",model,new Vector3(.2f,.45f,0),new Vector3(7.5f,.28f,2.7f),"Near silhouette");
                        Part("Load bed",model,new Vector3(-.5f,.9f,0),new Vector3(5.8f,.75f,2.55f),"Faded machine enamel");
                        Part("Aged canvas canopy",model,new Vector3(-.5f,1.95f,0),new Vector3(5.65f,1.65f,2.55f),"Faded machine enamel");
                        Part("Cab",model,new Vector3(3.05f,1.25f,0),new Vector3(1.85f,1.8f,2.55f),"Worn industrial steel");
                        for(int side=-1;side<=1;side+=2)
                        {
                            Part("Cab window",model,new Vector3(3.1f,1.73f,side*1.29f),new Vector3(1.15f,.66f,.04f),"Soot covered glass");
                            Part("Cab door handle",model,new Vector3(2.67f,1.16f,side*1.33f),new Vector3(.25f,.07f,.08f),"Flaking oxidised metal");
                            for(int n=0;n<6;n++)Part("Canvas seam and tie",model,new Vector3(-3+n,1.9f,side*1.29f),new Vector3(.035f,1.7f,.025f),"Flaking oxidised metal");
                            foreach(float x in new[]{-2.3f,.7f,3.1f})
                            {
                                var wheel=Part("Rubber tyre",model,new Vector3(x,.43f,side*1.35f),new Vector3(.84f,.16f,.84f),"Near silhouette",PrimitiveType.Cylinder);wheel.transform.localRotation=Quaternion.Euler(90,0,0);
                                var hub=Part("Wheel hub",model,new Vector3(x,.43f,side*1.54f),new Vector3(.31f,.03f,.31f),"Worn industrial steel",PrimitiveType.Cylinder);hub.transform.localRotation=Quaternion.Euler(90,0,0);
                            }
                        }
                        Part("Front grille",model,new Vector3(4.01f,.94f,0),new Vector3(.08f,.65f,1.8f),"Near silhouette");
                        for(int n=0;n<7;n++)Part("Grille rib",model,new Vector3(4.07f,.94f,-.75f+n*.25f),new Vector3(.05f,.55f,.045f),"Worn industrial steel");
                        Part("Headlight",model,new Vector3(4.1f,1.2f,-.91f),new Vector3(.09f,.24f,.23f),"Work light glass");
                    }
                }
                var lamps=w.GetComponentsInChildren<Light>(true);foreach(var l in lamps)if(l.name=="Light - child of fixture" && l.transform.parent && l.transform.parent.name.StartsWith("Rear working")){l.range=11;l.intensity=2;}
                EditorSceneManager.MarkSceneDirty(s);EditorSceneManager.SaveScene(s);count++;
            }
            finally{EditorSceneManager.CloseScene(s,true);}
        }
        SceneManager.SetActiveScene(original);return "Refined ambient bounce, continuous ground and full 3D truck in "+count+" scenes.";
    }
}
