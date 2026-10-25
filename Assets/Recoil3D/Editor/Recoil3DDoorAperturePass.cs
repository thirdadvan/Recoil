using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;

// A complete unseen foreground wall shapes the light; the wall itself is cut away for the camera.
public static class Recoil3DDoorAperturePass
{
    static void Wall(Transform root,string name,Vector3 position,Vector3 size,Material material)
    {
        var t=root.Find(name);
        if(!t){var obj=GameObject.CreatePrimitive(PrimitiveType.Cube);obj.name=name;UnityEngine.Object.DestroyImmediate(obj.GetComponent<Collider>());t=obj.transform;t.SetParent(root,false);}
        t.localPosition=position;t.localScale=size;var renderer=t.GetComponent<Renderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.ShadowsOnly;renderer.receiveShadows=false;
    }
    public static string Apply()
    {
        var scene=EditorSceneManager.GetActiveScene();if(EditorApplication.isPlayingOrWillChangePlaymode||scene.path!="Assets/Scenes/3D/RecoilServiceStore.unity")throw new Exception("Open store outside Play.");
        var root=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).First(t=>t.name=="Foreground entrance - light and two restless shadows");
        foreach(Transform part in root)if(part.name=="Foreground door jamb"||part.name=="Foreground door header"||part.name=="Worn entrance threshold")part.gameObject.SetActive(false);
        var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/Recoil3D/Materials/Store front entrance steel.mat");
        Wall(root,"Unseen front wall - left of doorway",new Vector3(-8.125f,2.5f,0),new Vector3(15,5,.24f),material);
        Wall(root,"Unseen front wall - right of doorway",new Vector3(8.125f,2.5f,0),new Vector3(15,5,.24f),material);
        Wall(root,"Unseen front wall - above doorway",new Vector3(0,4.1f,0),new Vector3(1.25f,3,.24f),material);
        var silhouettes=root.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("Unseen monster shadow ")).OrderBy(t=>t.name).ToArray();
        for(int i=0;i<silhouettes.Length;i++){var p=silhouettes[i].localPosition;p.x=(i-(silhouettes.Length-1)*.5f)*.62f;silhouettes[i].localPosition=p;}
        var lamp=root.GetComponentsInChildren<Light>(true).First(l=>l.name=="Outside entrance light - projects into room");lamp.range=9.4f;lamp.intensity=2.1f;lamp.shadowBias=.008f;
        var mask=(Texture2D)lamp.cookie;var pixels=new Color[mask.width*mask.height];
        for(int y=0;y<mask.height;y++)for(int x=0;x<mask.width;x++)
        {
            float side=Mathf.SmoothStep(0,1,Mathf.InverseLerp(46,49,x))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(79,82,x)));
            float height=(y>10&&y<117)?1:0;pixels[y*mask.width+x]=new Color(1,1,1,side*height);
        }
        mask.SetPixels(pixels);mask.Apply();EditorUtility.SetDirty(mask);EditorUtility.SetDirty(lamp);EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssets();return "Whole foreground wall with 1.25m doorway; narrow incident beam and short 9.4m light reach.";
    }
}
