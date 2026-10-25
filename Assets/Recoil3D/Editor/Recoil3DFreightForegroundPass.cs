using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Recoil3DFreightForegroundPass
{
    const string Marker="Foreground mast assembly - keep gameplay fork in place";
    static bool MastPart(string name)
    {return name=="Guided steel mast"||name=="Mast cross brace"||name=="Polished mast guide"||name=="Lift chain dark channel"||name=="Chain link"||name=="Trolley housing"||name=="Motor and chain hoist"||name=="Trolley face inset"||name=="KBK"||name=="Motor cooling vent";}
    static bool CarriagePart(string name)
    {return name=="Fork carriage"||name=="Carriage service cover"||name=="Carriage roller bolt"||name=="FREIGHT";}
    static Transform Group(string name,Transform parent)
    {var t=new GameObject(name).transform;t.SetParent(parent,false);return t;}
    static void Beam(string name,Transform parent,Vector3 world,Vector3 size,Material material)
    {
        var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());
        g.transform.SetParent(parent,false);g.transform.position=world;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=material;
    }
    public static string Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Exit Play before editing Freight.");
        var original=SceneManager.GetActiveScene();const string path="Assets/Scenes/3D/RecoilFactoryWarehouse.unity";
        var scene=SceneManager.GetSceneByPath(path);bool added=!scene.isLoaded;if(added)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
        try
        {
            var crane=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<KBKStackerCrane>(true)).First();
            if(crane.travelRig.Find(Marker))return "Freight foreground pass already applied.";
            Directory.CreateDirectory("Backups/ForegroundDoorsAndFreight/20261022");EditorSceneManager.SaveScene(scene,"Backups/ForegroundDoorsAndFreight/20261022/Warehouse-before.unity",true);
            var area=crane.forkPlatform.Area;var fork=crane.forkPlatform.transform.position;var start=crane.startPoint.position;var destination=crane.destinationPoint.position;
            var mast=Group(Marker,crane.travelRig);var carriage=Group("Foreground moving carriage - fork depth unchanged",crane.liftCarriage);
            foreach(var part in crane.travelRig.GetComponentsInChildren<Transform>(true).ToArray())
            {
                if(part.name=="Trolley wheel"){var wheel=part.position;wheel.z=3;part.position=wheel;continue;}
                bool moving=CarriagePart(part.name);if(!moving&&!MastPart(part.name))continue;
                var p=part.position;p.z=-1.4f;part.SetParent(moving?carriage:mast,true);part.position=p;
            }
            var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/Recoil3D/Materials/Worn industrial steel.mat");
            float mastX=mast.GetComponentsInChildren<Transform>().Where(t=>t.name=="Guided steel mast").Average(t=>t.position.x);
            Beam("Carriage depth linkage - under deck",carriage,new Vector3(mastX,fork.y-.11f,-.35f),new Vector3(.48f,.18f,2.1f),material);
            Beam("Trolley depth connection to overhead rail",mast,new Vector3(mastX,crane.travelRig.position.y+6.5f,.8f),new Vector3(.48f,.18f,4.4f),material);
            Physics2D.SyncTransforms();
            if(crane.forkPlatform.Area!=area||crane.forkPlatform.transform.position!=fork||crane.startPoint.position!=start||crane.destinationPoint.position!=destination)throw new Exception("Freight functional geometry changed.");
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);return "Freight mast and moving carriage now z=-1.4; fork, collision and travel endpoints unchanged.";
        }
        finally{SceneManager.SetActiveScene(original);if(added)EditorSceneManager.CloseScene(scene,true);}
    }
}
