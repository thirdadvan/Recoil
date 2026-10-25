using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class RecoilMacBuild
{
    public const string Folder="Builds/Recoil-Mac";
    public const string Status="Builds/mac-build-status.txt";
    [MenuItem("Recoil/Build Mac Universal")]
    public static void Build()
    {
        Directory.CreateDirectory("Builds");File.WriteAllText(Status,"RUNNING");
        try
        {
            if(EditorApplication.isPlaying)throw new Exception("Exit Play mode before building.");
            if(!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone,BuildTarget.StandaloneOSX))throw new Exception("Mac build support is missing.");
            var scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray();
            if(scenes.Length==0 || scenes.Any(s=>!File.Exists(s)))throw new Exception("Build scene list is empty or contains missing scenes.");
            if(!EditorSceneManager.SaveOpenScenes())throw new Exception("Could not save open scenes.");
            Directory.CreateDirectory(Folder);
            var engine=Path.GetFullPath("Builds/BuildTools/MacStandaloneSupport");
            if(!Directory.Exists(engine+"/Variations"))throw new Exception("Local Mac build tool extraction is incomplete.");
            string previousEngine=BuildPipeline.GetPlaybackEngineDirectory(BuildTarget.StandaloneOSX,BuildOptions.None);
            int previousArchitecture=PlayerSettings.GetArchitecture(BuildTargetGroup.Standalone);
            string previousMacArchitecture=EditorUserBuildSettings.GetPlatformSettings("OSXUniversal","Architecture");
            int width=PlayerSettings.defaultScreenWidth,height=PlayerSettings.defaultScreenHeight;
            var mode=PlayerSettings.fullScreenMode;bool resizable=PlayerSettings.resizableWindow;
            BuildReport report;
            try
            {
                SetEngine(engine);EnsureMacModule(engine);
                PlayerSettings.SetArchitecture(BuildTargetGroup.Standalone,2);
                EditorUserBuildSettings.SetPlatformSettings("OSXUniversal","Architecture","x64ARM64");
                PlayerSettings.defaultScreenWidth=1280;PlayerSettings.defaultScreenHeight=720;
                PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.resizableWindow=true;
                EditorUserBuildSettings.buildScriptsOnly=false;
                report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=scenes,locationPathName=Path.GetFullPath(Folder+"/Recoil.app"),target=BuildTarget.StandaloneOSX,options=BuildOptions.CleanBuildCache});
            }
            finally{SetEngine(previousEngine);PlayerSettings.SetArchitecture(BuildTargetGroup.Standalone,previousArchitecture);EditorUserBuildSettings.SetPlatformSettings("OSXUniversal","Architecture",previousMacArchitecture);PlayerSettings.defaultScreenWidth=width;PlayerSettings.defaultScreenHeight=height;PlayerSettings.fullScreenMode=mode;PlayerSettings.resizableWindow=resizable;}
            if(report.summary.result!=BuildResult.Succeeded || report.summary.totalErrors>0)throw new Exception("Mac build: "+report.summary.result+"; errors="+report.summary.totalErrors);
            if(!Directory.Exists(Folder+"/Recoil.app") || !Directory.Exists(Folder+"/Recoil.app/Contents/MacOS") || !File.Exists(Folder+"/Recoil.app/Contents/Resources/Data/globalgamemanagers") || !File.Exists(Folder+"/Recoil.app/Contents/Resources/Data/level0"))throw new Exception("Build output is incomplete.");
            if(File.Exists("Documents/操作说明-Mac.txt"))File.Copy("Documents/操作说明-Mac.txt",Folder+"/Controls.txt",true);
            File.WriteAllText(Status,"SUCCEEDED\n"+report.summary.outputPath+"\nScenes: "+scenes.Length+"\nBytes: "+report.summary.totalSize+"\nWarnings: "+report.summary.totalWarnings);
            Debug.Log("RECOIL_MAC_BUILD_SUCCEEDED: "+report.summary.outputPath);
        }
        catch(Exception error){File.WriteAllText(Status,"FAILED\n"+error);Debug.LogException(error);}
    }
    static void EnsureMacModule(string engine)
    {
        var flags=System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic;
        var manager=typeof(Editor).Assembly.GetType("UnityEditor.Modules.ModuleManager");
        var modules=(System.Collections.IDictionary)manager.GetProperty("platformSupportModules",flags).GetValue(null,null);
        if(modules.Contains("OSXStandalone"))return;
        var assembly=System.Reflection.Assembly.LoadFrom(Path.Combine(engine,"UnityEditor.OSXStandalone.Extensions.dll"));
        var type=assembly.GetType("UnityEditor.OSXStandalone.TargetExtension",true);
        var module=Activator.CreateInstance(type,true);
        type.GetMethod("OnLoad").Invoke(module,null);
        modules.Add("OSXStandalone",module);
    }
    static void SetEngine(string path)
    {
        var method=typeof(BuildPipeline).GetMethod("SetPlaybackEngineDirectory",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic,null,new[]{typeof(BuildTargetGroup),typeof(BuildTarget),typeof(BuildOptions),typeof(string)},null);
        if(method==null)throw new Exception("Cannot locate playback engine override.");
        method.Invoke(null,new object[]{BuildTargetGroup.Standalone,BuildTarget.StandaloneOSX,BuildOptions.None,path});
    }
    public static string Schedule()
    {EditorApplication.delayCall+=Build;return "Mac Universal build scheduled; status: "+Status;}
}
