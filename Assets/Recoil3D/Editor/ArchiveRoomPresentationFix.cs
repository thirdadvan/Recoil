using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;

public static class ArchiveRoomPresentationFix
{
    const string Folder="Assets/Art/Environment/ArchiveRoom/";
    public static Material TerminalTextMaterial(Font font)
    {
        const string path=Folder+"Archive terminal depth tested text.mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!material){material=new Material(Shader.Find("Recoil/3D/World Text"));AssetDatabase.CreateAsset(material,path);}
        material.shader=Shader.Find("Recoil/3D/World Text");material.mainTexture=font.material.mainTexture;material.color=Color.white;
        material.renderQueue=3000;EditorUtility.SetDirty(material);return material;
    }
    public static string Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Use Edit mode.");
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(!scene.path.EndsWith("/FactoryArchiveF3.unity")||UnityEngine.SceneManagement.SceneManager.sceneCount!=1)throw new Exception("Use the single archive scene.");
        var terminal=Object.FindObjectOfType<ArchiveTerminalConversation>();if(!terminal||!terminal.screenText)throw new Exception("Terminal text missing.");
        var renderer=terminal.screenText.GetComponent<MeshRenderer>();Undo.RecordObject(renderer,"Depth test terminal text");renderer.sharedMaterial=TerminalTextMaterial(terminal.screenText.font);renderer.sortingOrder=0;EditorUtility.SetDirty(renderer);
        var finishes=Object.FindObjectsOfType<Transform>().Single(t=>t.name=="Room finishes - plaster wainscot windows and radiator");
        Undo.RegisterFullObjectHierarchyUndo(finishes.gameObject,"Extend archive side wall finishes beyond camera");
        int verticesChanged=0;
        foreach(var filter in finishes.GetComponentsInChildren<MeshFilter>()){
            if(!filter.name.StartsWith("Static detail - ")||!filter.sharedMesh)continue;
            var source=filter.sharedMesh;var vertices=source.vertices;int changed=0;
            // Only the camera-facing endpoints of the two original side wainscots/skirtings.
            for(int i=0;i<vertices.Length;i++){
                var p=vertices[i];if(Mathf.Abs(p.z+1.7f)<.002f && (p.x<.08f||p.x>13.92f) && p.y<1.12f){p.z=-32.5f;vertices[i]=p;changed++;}
            }
            if(changed==0)continue;
            string path=Folder+"Continuous side finishes - "+filter.name.Replace("Static detail - ","")+".asset";
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(!mesh){mesh=Object.Instantiate(source);mesh.name="Continuous archive side wall finish";AssetDatabase.CreateAsset(mesh,path);}
            else EditorUtility.CopySerialized(source,mesh);
            mesh.vertices=vertices;mesh.RecalculateBounds();filter.sharedMesh=mesh;EditorUtility.SetDirty(mesh);EditorUtility.SetDirty(filter);verticesChanged+=changed;
        }
        foreach(var part in finishes.GetComponentsInChildren<Transform>())if(part.name=="Side lower wall"||part.name=="Side skirting"){
            var at=part.localPosition;at.z=(-32.5f+6.7f)*.5f;part.localPosition=at;var scale=part.localScale;scale.z=39.2f;part.localScale=scale;
        }
        EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssets();if(!EditorSceneManager.SaveScene(scene))throw new Exception("Save failed.");
        return "Saved archive: continuous side wall finishes to Z -32.5, extended "+verticesChanged+" mesh endpoints; depth-tested terminal text. No new scene or duplicate wall.";
    }
}
