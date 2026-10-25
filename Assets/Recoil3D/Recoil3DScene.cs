using UnityEngine;
using UnityEngine.SceneManagement;

// Explicit scene identity: original 2D assets remain playable without remapping.
[DisallowMultipleComponent,AddComponentMenu("Recoil/3D/Scene Identity")]
public sealed class Recoil3DScene : MonoBehaviour
{
    public string originalScene;
    public const string Folder="Assets/Scenes/3D/";
    public static string ConvertPath(string path)
    {return path.StartsWith("Assets/Scenes/") && !path.StartsWith(Folder)?Folder+path.Substring("Assets/Scenes/".Length):path;}
    public static bool Is3D {get{return SceneManager.GetActiveScene().path.StartsWith(Folder);}}
}
