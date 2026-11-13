using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;

public static class ArchivePropCollisionPass
{
    const string CollisionName="Navigation collision - solid prop";
    static Bounds LocalGeometryBounds(Transform root)
    {
        var bounds=new Bounds();bool first=true;
        foreach(var filter in root.GetComponentsInChildren<MeshFilter>())
        {
            var renderer=filter.GetComponent<Renderer>();
            if(!filter.sharedMesh||!renderer||!renderer.enabled||!renderer.gameObject.activeInHierarchy)continue;
            var local=filter.sharedMesh.bounds;var matrix=root.worldToLocalMatrix*filter.transform.localToWorldMatrix;
            for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2){
                var corner=matrix.MultiplyPoint3x4(local.center+Vector3.Scale(local.extents,new Vector3(x,y,z)));
                if(first){bounds=new Bounds(corner,Vector3.zero);first=false;}else bounds.Encapsulate(corner);
            }
        }
        if(first)throw new Exception("No active geometry: "+root.name);return bounds;
    }
    public static string Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Apply in Edit mode.");
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(!scene.path.EndsWith("/FactoryArchiveF3.unity")||UnityEngine.SceneManagement.SceneManager.sceneCount!=1)throw new Exception("Open the archive as a single scene.");
        var targets=Object.FindObjectsOfType<Transform>().Where(t=>t.name.StartsWith("Archive rack ")&&t.name.Contains("frame shelves")||t.name.StartsWith("Rear archive rack ")&&t.name.Contains("complete records assembly")||t.name=="Clock model - cabinet hands and pendulum").ToArray();
        if(targets.Length!=8)throw new Exception("Expected seven racks and one clock; found "+targets.Length);
        var report=new List<string>();
        foreach(var target in targets){
            var bounds=LocalGeometryBounds(target);Undo.RegisterFullObjectHierarchyUndo(target.gameObject,"Add archive prop collision");
            var child=target.Find(CollisionName);if(!child){child=new GameObject(CollisionName).transform;child.SetParent(target,false);Undo.RegisterCreatedObjectUndo(child.gameObject,"Add archive prop collision");}
            child.localPosition=Vector3.zero;child.localRotation=Quaternion.identity;child.localScale=Vector3.one;
            var box=child.GetComponent<BoxCollider>();if(!box)box=Undo.AddComponent<BoxCollider>(child.gameObject);
            // Fill the whole occupied footprint so Coaler cannot squeeze between shelf levels.
            box.center=bounds.center;box.size=bounds.size;box.isTrigger=false;box.enabled=true;EditorUtility.SetDirty(box);
            report.Add(target.name+" | size="+box.bounds.size+" | centre="+box.bounds.center);
        }
        EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new Exception("Save failed.");
        System.IO.Directory.CreateDirectory("output/archive-navigation");System.IO.File.WriteAllLines("output/archive-navigation/CollisionSetup.txt",report);
        return "Saved 8 native 3D solid colliders: seven individual racks and one clock. Each collider follows its existing prop parent; shared Coaler navigation consumes them on scene start.";
    }
}
