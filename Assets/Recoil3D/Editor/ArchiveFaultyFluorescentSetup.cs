using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;

public static class ArchiveFaultyFluorescentSetup
{
    const string Folder="Assets/Art/Environment/ArchiveRoom/";
    static Material Mat(string name,Color colour,bool emissive)
    {
        var path=Folder+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){m=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(m,path);}m.color=colour;m.SetFloat("_Glossiness",.17f);
        if(emissive){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",Color.black);}EditorUtility.SetDirty(m);return m;
    }
    public static string Apply(string fixtureName="Fluorescent fixture 2 - complete assembly")
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Use Edit mode.");
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(!scene.path.EndsWith("/FactoryArchiveF3.unity")||UnityEngine.SceneManagement.SceneManager.sceneCount!=1)throw new Exception("Use the single archive scene.");
        var fixture=Object.FindObjectsOfType<Transform>().Single(t=>t.name==fixtureName);
        Undo.RegisterFullObjectHierarchyUndo(fixture.gameObject,"Age second archive fluorescent fixture");
        var controller=fixture.GetComponent<FaultyFluorescentFixture>();if(!controller)controller=Undo.AddComponent<FaultyFluorescentFixture>(fixture.gameObject);
        var batch=fixture.Find("Static detail - Fluorescent glass");if(batch)batch.GetComponent<Renderer>().enabled=false;
        var glass=Mat("Faulty fluorescent aged phosphor",new Color(.18f,.19f,.17f),true);var black=Mat("Fluorescent blackened electrodes",new Color(.055f,.051f,.042f),false);var warm=Mat("Fluorescent preheating electrodes",new Color(.25f,.10f,.035f),true);
        var tubes=fixture.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.name=="Working fluorescent tube").OrderBy(r=>r.transform.localPosition.z).ToArray();if(tubes.Length!=2)throw new Exception("Expected two original tubes.");
        var ends=new System.Collections.Generic.List<Renderer>();
        foreach(var tube in tubes){tube.gameObject.SetActive(true);tube.enabled=true;tube.sharedMaterial=glass;var mesh=tube.GetComponent<MeshFilter>().sharedMesh;
            for(int i=0;i<2;i++){
                var sign=i==0?-1:1;
                foreach(bool glow in new[]{false,true}){
                    string name=glow?"Warm electrode "+i:"Blackened tube end "+i;var child=tube.transform.Find(name);if(!child){child=new GameObject(name).transform;child.SetParent(tube.transform,false);child.gameObject.AddComponent<MeshFilter>();child.gameObject.AddComponent<MeshRenderer>();}
                    child.localPosition=new Vector3(0,sign*(glow?.77f:.91f),0);child.localRotation=Quaternion.identity;child.localScale=new Vector3(glow?1.025f:1.02f,glow?.018f:.085f,glow?1.025f:1.02f);
                    child.GetComponent<MeshFilter>().sharedMesh=mesh;var r=child.GetComponent<MeshRenderer>();r.sharedMaterial=glow?warm:black;if(glow)ends.Add(r);
                }
            }
        }
        controller.tubes=tubes;controller.endGlows=ends.ToArray();controller.roomLight=fixture.GetComponentsInChildren<Light>(true).Single();controller.roomLight.gameObject.SetActive(true);controller.gameplayLight=fixture.GetComponent<RecoilLight2D>();controller.roomIntensity=FaultyFluorescentFixture.MaximumIntensity;controller.gameplayIntensity=.28f;controller.randomSeed=fixtureName.Contains("3 -")?19783:19782;controller.roomLight.range=7;controller.roomLight.spotAngle=100;controller.roomLight.innerSpotAngle=65;controller.roomLight.shadows=LightShadows.Soft;
        ArchiveFluorescentSurfaceLighting.Configure(controller);
        EditorUtility.SetDirty(controller);EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssets();if(!EditorSceneManager.SaveScene(scene))throw new Exception("Save failed.");
        return "Saved "+fixtureName+": two independent glow-starter failure cycles, blackened ends, orange preheat, irregular brief ignition and dropout; native lighting and gameplay illumination synchronized. Other lamps unchanged.";
    }
}
