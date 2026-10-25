using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Apply the proven store cutaway entrance to the reverse side of ordinary scene doors.
// Routes, locks, interactions and spawn markers are left authored by the level designer.
public static class Recoil3DForegroundReturnDoorPass
{
    const string Marker="Foreground return doorway - light and cutaway wall";
    const string Store="Assets/Scenes/3D/RecoilServiceStore.unity";
    static bool ReturnDoor(Scene scene,RecoilDoor door)
    {
        if(scene.name=="RecoilFactoryInterior")return string.Equals(door.destinationScene,"Assets/Scenes/3D/RecoilPrototype.unity",StringComparison.Ordinal);
        if(scene.path.Contains("/FactoryRooms/"))return door.name=="RETURN TO CORRIDOR";
        if(scene.name=="RecoilFactoryWarehouse")return door.name=="Leave warehouse";
        if(scene.name=="RecoilWarehouseStoreroom")return door.name=="Return to warehouse";
        return false;
    }
    public static string Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Exit Play before editing door presentation.");
        var original=SceneManager.GetActiveScene();var store=SceneManager.GetSceneByPath(Store);bool openedStore=!store.isLoaded;
        if(openedStore)store=EditorSceneManager.OpenScene(Store,OpenSceneMode.Additive);
        int count=0;var changed=new System.Collections.Generic.List<string>();
        try
        {
            var template=store.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).First(t=>t.name=="Foreground entrance - light and two restless shadows");
            foreach(var entry in EditorBuildSettings.scenes.Where(s=>s.enabled && s.path.StartsWith("Assets/Scenes/3D/")))
            {
                var scene=SceneManager.GetSceneByPath(entry.path);bool opened=!scene.isLoaded;
                if(opened)scene=EditorSceneManager.OpenScene(entry.path,OpenSceneMode.Additive);
                try
                {
                    var doors=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<RecoilDoor>(true)).Where(d=>ReturnDoor(scene,d)&&!d.transform.Find(Marker)).ToArray();
                    if(doors.Length==0)continue;
                    string backup="Backups/ForegroundDoorsAndFreight/20261022";Directory.CreateDirectory(backup);
                    EditorSceneManager.SaveScene(scene,backup+"/"+scene.name+"-before-doors.unity",true);
                    foreach(var door in doors)
                    {
                        float floor=door.transform.position.y-1.25f;float x=door.transform.position.x;
                        foreach(var legacy in door.GetComponentsInChildren<Recoil3DSpriteVisual>(true)){legacy.suppressed=true;legacy.Sync();EditorUtility.SetDirty(legacy);}
                        foreach(var renderer in door.GetComponentsInChildren<Renderer>(true)){renderer.enabled=false;EditorUtility.SetDirty(renderer);}
                        var p=door.transform.position;p.z=-2.2f;door.transform.position=p;
                        var instance=UnityEngine.Object.Instantiate(template.gameObject);instance.name=Marker;
                        SceneManager.MoveGameObjectToScene(instance,scene);instance.transform.SetParent(door.transform,true);instance.transform.position=new Vector3(x,floor,-2.2f);
                        foreach(var movement in instance.GetComponentsInChildren<Recoil3DDoorwayShadows>(true))UnityEngine.Object.DestroyImmediate(movement);
                        foreach(var proxy in instance.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Two pounding exterior silhouettes - shadows only").ToArray())UnityEngine.Object.DestroyImmediate(proxy.gameObject);
                        foreach(var light in instance.GetComponentsInChildren<Light>(true)){light.name="Incident light through foreground return doorway";light.intensity=2.1f;light.range=9.4f;}
                        EditorUtility.SetDirty(door);count++;changed.Add(scene.name+" / "+door.name);
                    }
                    EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
                }
                finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
            }
        }
        finally{SceneManager.SetActiveScene(original);if(openedStore)EditorSceneManager.CloseScene(store,true);}
        return count+" reverse doors updated:\n"+string.Join("\n",changed.ToArray());
    }
}
