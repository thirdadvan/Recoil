using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class Recoil3DTextPlacement
{
    public static string Apply()
    {
        var original=SceneManager.GetActiveScene();int count=0;
        foreach(var entry in EditorBuildSettings.scenes.Where(e=>e.enabled && e.path.StartsWith(Recoil3DScene.Folder)))
        {
            var scene=SceneManager.GetSceneByPath(entry.path);bool added=!scene.isLoaded;if(added)scene=EditorSceneManager.OpenScene(entry.path,OpenSceneMode.Additive);
            try
            {
                foreach(var text in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<TextMesh>(true)))
                {
                    var parent=text.transform.parent;if(!parent)continue;
                    var nearest=parent.GetComponentsInChildren<Recoil3DSpriteVisual>(true).Where(v=>v.meshRenderer && !v.suppressed).OrderBy(v=>((Vector2)v.transform.position-(Vector2)text.transform.position).sqrMagnitude).FirstOrDefault();
                    var p=text.transform.position;p.z=nearest && Vector2.Distance(nearest.transform.position,p)<4?nearest.meshRenderer.bounds.min.z-.03f:-.35f;text.transform.position=p;count++;
                }
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            }
            finally{if(added)EditorSceneManager.CloseScene(scene,true);}
        }
        SceneManager.SetActiveScene(original);return count+" world-space labels placed in front of their 3D surfaces.";
    }
}
