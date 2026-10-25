using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class Recoil3DSkylightFrameLightPass
{
    public static string Apply()
    {
        var scene=EditorSceneManager.GetActiveScene();if(EditorApplication.isPlayingOrWillChangePlaymode||scene.path!="Assets/Scenes/3D/RecoilServiceStore.unity")throw new Exception("Open store outside Play.");
        var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
        var well=all.First(t=>t.name=="Skylight curb and shallow light well");
        string path="Assets/Recoil3D/Materials/Skylight inner frame illuminated.mat";
        var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!mat){mat=new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/Recoil3D/Materials/Store skylight oxidised frame.mat"));AssetDatabase.CreateAsset(mat,path);}
        mat.color=new Color(.28f,.34f,.35f);mat.SetColor("_EmissionColor",new Color(.035f,.055f,.065f));mat.SetFloat("_Metallic",.12f);EditorUtility.SetDirty(mat);
        foreach(var renderer in well.GetComponentsInChildren<MeshRenderer>())renderer.sharedMaterial=mat;
        // Keep the inner side faces clear of the ceiling slab faces: overlapping coplanar
        // geometry produced the jagged dark patch on the left reveal.
        foreach(Transform side in well)if(side.name=="Vertical light well lining")
        {var p=side.localPosition;p.x=p.x<0?-1.545f:1.545f;side.localPosition=p;}
        var face=well.Find("Illuminated rear ceiling fascia");
        if(!face)
        {
            var obj=GameObject.CreatePrimitive(PrimitiveType.Cube);obj.name="Illuminated rear ceiling fascia";UnityEngine.Object.DestroyImmediate(obj.GetComponent<Collider>());face=obj.transform;face.SetParent(well,false);
        }
        face.localPosition=new Vector3(0,0,1.294f);face.localScale=new Vector3(3,.35f,.012f);face.GetComponent<Renderer>().sharedMaterial=mat;
        var lightTransform=well.Find("Light washing inner frame - no floor reach");if(!lightTransform){lightTransform=new GameObject("Light washing inner frame - no floor reach").transform;lightTransform.SetParent(well,false);lightTransform.gameObject.AddComponent<Light>();}
        lightTransform.localPosition=new Vector3(0,.15f,-.65f);var light=lightTransform.GetComponent<Light>();light.type=LightType.Point;light.range=3.1f;light.intensity=1.5f;light.color=new Color(.62f,.77f,.84f);light.renderMode=LightRenderMode.ForcePixel;light.shadows=LightShadows.None;
        EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssets();return "Illuminated inner frame, side reveals and rear horizontal fascia only; local lamp cannot reach floor.";
    }
}
