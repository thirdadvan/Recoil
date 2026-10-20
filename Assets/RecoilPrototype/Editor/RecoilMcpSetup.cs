using System.IO;
using UnityEditor;
using UnityEngine;
using MCPForUnity.Editor.Services;
using MCPForUnity.Editor.Services.Transport;

[InitializeOnLoad]
public static class RecoilMcpSetup
{
    static RecoilMcpSetup(){EditorApplication.delayCall+=CheckRequest;}
    static void CheckRequest()
    {
        const string request="Library/RecoilMCP/connect.request";
        if(!File.Exists(request))return;
        File.Delete(request);
        Connect();
    }
    [MenuItem("Recoil/Unity MCP/Connect Local")]
    public static async void Connect()
    {
        string root=Directory.GetParent(Application.dataPath).FullName;
        EditorPrefs.SetString("MCPForUnity.UvxPath",Path.Combine(root,"Tools/UnityMCP/uvx"));
        EditorPrefs.SetBool("MCPForUnity.UseHttpTransport",true);
        EditorPrefs.SetString("MCPForUnity.HttpTransportScope","local");
        EditorPrefs.SetString("MCPForUnity.HttpUrl","http://127.0.0.1:8080");
        EditorPrefs.SetBool("MCPForUnity.AutoStartOnLoad",true);
        EditorPrefs.SetBool("MCPForUnity.TelemetryDisabled",true);
        MCPServiceLocator.Paths.SetUvxPathOverride(Path.Combine(root,"Tools/UnityMCP/uvx"));
        EditorConfigurationCache.Instance.Refresh();
        bool connected=await MCPServiceLocator.TransportManager.StartAsync(TransportMode.Http);
        Debug.Log(connected?"RECOIL_MCP_CONNECTED":"RECOIL_MCP: Start the local server in Window > MCP for Unity, then connect.");
    }
}
