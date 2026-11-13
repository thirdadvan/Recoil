using UnityEditor;
using UnityEngine;
public static class RecoilDebugModeMenu
{
    const string Menu="Recoil/Debug/Start in current scene (ignore saves)";
    [MenuItem(Menu)]
    static void Toggle()
    {
        bool enabled=!EditorPrefs.GetBool(GameManager.DebugScenePreference,true);
        EditorPrefs.SetBool(GameManager.DebugScenePreference,enabled);
        if(GameManager.Instance)GameManager.Instance.debugStartInCurrentScene=enabled;
        Debug.Log("Recoil current-scene debug mode: "+enabled);
    }
    [MenuItem(Menu,true)]
    static bool Validate(){UnityEditor.Menu.SetChecked(Menu,EditorPrefs.GetBool(GameManager.DebugScenePreference,true));return true;}
}
