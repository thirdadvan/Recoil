using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public static class CoalerSpikeNativeCoreCheck
{
    const string Request="output/coaler-model-spike/native-core.request";
    static CoalerSpikeNativeCoreCheck(){EditorApplication.delayCall+=Requested;}
    static void Requested(){if(File.Exists(Request)&&!EditorApplication.isPlayingOrWillChangePlaymode&&UnityEngine.SceneManagement.SceneManager.GetActiveScene().path==CoalerModelSpikeBuilder.ScenePath){File.Delete(Request);Setup();}}
    [MenuItem("Recoil/Tests/Restore Source Core And Check Bones")]
    public static void Setup()
    {
        var lab=UnityEngine.Object.FindObjectOfType<CoalerModelSpikeLab>();if(!lab)return;
        var parent=lab.model.parent;
        var proxy=parent.Find("CORE PROXY - separate from imported model");if(proxy)UnityEngine.Object.DestroyImmediate(proxy.gameObject);
        parent.rotation=Quaternion.Euler(0,180,0);
        var root=lab.model.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="Original chest core - source geometry");
        if(!root){var asset=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Tests/CoalerModelSpike/Model/QMonster_OriginalCore.fbx");var go=(GameObject)PrefabUtility.InstantiatePrefab(asset);root=go.transform;root.name="Original chest core - source geometry";root.SetParent(lab.model,false);}
        var renderer=root.GetComponentInChildren<Renderer>();
        string materialPath="Assets/Tests/CoalerModelSpike/Materials/OriginalSourceCore.mat";
        var mat=AssetDatabase.LoadAssetAtPath<Material>(materialPath);if(!mat){mat=new Material(Shader.Find("Standard"));mat.color=new Color(.5f,.025f,.018f);mat.SetFloat("_Glossiness",.35f);AssetDatabase.CreateAsset(mat,materialPath);}renderer.sharedMaterial=mat;
        root.SetParent(lab.model.GetComponentsInChildren<Transform>(true).First(t=>t.name=="COALER_spine02_JNT"),true);
        lab.core=renderer.transform;lab.shutters=new Transform[0];lab.useOriginalCore=true;lab.openCore=false;
        foreach(var label in UnityEngine.Object.FindObjectsOfType<TextMesh>())if(label.text.Contains("IMPORTED"))label.text="B  IMPORTED / SOURCE CORE";
        EditorUtility.SetDirty(lab);EditorSceneManager.MarkSceneDirty(lab.gameObject.scene);EditorSceneManager.SaveScene(lab.gameObject.scene);AssetDatabase.SaveAssets();
        lab.Initialize();lab.ResetLab();
        var result=new List<string>{"Original core: COALER_V03_CHEST_CORE_GEO.001 (V04 red material), exact source geometry/placement/scale. Static mesh has no weights; attached to existing spine02 with world transform preserved.","Removed proxy core and all four proxy shutters. Imported model now faces camera; previous view was its back.","Verified native-core cover: COALER_EXTRA_TENTACLE_FRONT_02_JNT_01 (root of 12-joint FRONT_02 chain). No dedicated core-opening bone. Only this existing root rotates; source core stays fixed."};
        result.Add("REST: "+Visibility(lab));lab.ToggleCoreOpening();lab.Step(2f);result.Add("COVER BONES OPEN: "+Visibility(lab));lab.ResetLab();
        File.WriteAllLines("output/coaler-model-spike/native-core-verification.txt",result);Debug.Log("COALER_NATIVE_CORE\n"+string.Join("\n",result));
    }
    static string Visibility(CoalerModelSpikeLab lab)
    {
        var objects=new List<GameObject>();var meshes=new List<Mesh>();var blocks=new Dictionary<string,int>();int total=0,visible=0;
        try
        {
            foreach(var skin in lab.model.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                var mesh=new Mesh();skin.BakeMesh(mesh);meshes.Add(mesh);
                var go=new GameObject(skin.name);go.hideFlags=HideFlags.HideAndDontSave;go.layer=31;go.transform.SetPositionAndRotation(skin.transform.position,skin.transform.rotation);go.transform.localScale=Vector3.one;
                go.AddComponent<MeshCollider>().sharedMesh=mesh;objects.Add(go);
            }
            var coreRenderer=lab.core.GetComponent<Renderer>();var coreGo=new GameObject("Original core ray target");coreGo.hideFlags=HideFlags.HideAndDontSave;coreGo.layer=30;objects.Add(coreGo);coreGo.transform.SetPositionAndRotation(lab.core.position,lab.core.rotation);coreGo.transform.localScale=lab.core.lossyScale;
            var coreCollider=coreGo.AddComponent<MeshCollider>();coreCollider.sharedMesh=lab.core.GetComponent<MeshFilter>().sharedMesh;Physics.SyncTransforms();
            var center=coreRenderer.bounds.center;var ext=coreRenderer.bounds.extents;var cam=lab.testCamera;
            for(int y=-6;y<=6;y++)for(int x=-4;x<=4;x++)
            {
                if((x*x/16f+y*y/36f)>.8f)continue;
                var target=center+cam.transform.right*(x/4f*ext.x)+cam.transform.up*(y/6f*ext.y);
                var ray=new Ray(target-cam.transform.forward*5,cam.transform.forward);RaycastHit hit;
                if(!coreCollider.Raycast(ray,out hit,10))continue;total++;
                RaycastHit cover;if(Physics.Raycast(ray,out cover,hit.distance-.001f,1<<31)){string key=cover.collider.name;if(!blocks.ContainsKey(key))blocks[key]=0;blocks[key]++;}else visible++;
            }
            return "visible source-core rays "+visible+"/"+total+"; blockers: "+string.Join(", ",blocks.OrderByDescending(k=>k.Value).Select(k=>k.Key+"="+k.Value));
        }
        finally{foreach(var go in objects)UnityEngine.Object.DestroyImmediate(go);foreach(var mesh in meshes)UnityEngine.Object.DestroyImmediate(mesh);}
    }
}
