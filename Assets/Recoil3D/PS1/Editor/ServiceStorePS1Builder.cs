using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class ServiceStorePS1Builder {
[MenuItem("Recoil/Art Tests/Create Service Store PS1 Example")]
public static void Build(){
var scene=EditorSceneManager.OpenScene("Assets/Scenes/3D/RecoilServiceStore.unity",OpenSceneMode.Additive);
EditorSceneManager.SaveScene(scene,"Assets/Scenes/3D/RecoilServiceStore_PS1.unity");
foreach(var root in scene.GetRootGameObjects())foreach(var camera in root.GetComponentsInChildren<Camera>(true)){
var effect=camera.gameObject.AddComponent<PS1Presentation>();effect.shader=Shader.Find("Recoil/PS1 Presentation");camera.allowMSAA=false;camera.allowHDR=false;
}
EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
EditorSceneManager.SetActiveScene(scene);
}
}
