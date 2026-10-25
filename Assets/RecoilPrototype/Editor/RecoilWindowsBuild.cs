using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class RecoilWindowsBuild
{
    public const string Folder="Builds/Recoil-Windows";
    public const string Status="Builds/windows-build-status.txt";
    [MenuItem("Recoil/Build Windows x64")]
    public static void Build()
    {
        Directory.CreateDirectory("Builds");File.WriteAllText(Status,"RUNNING");
        try
        {
            if(EditorApplication.isPlaying)throw new Exception("Exit Play mode before building.");
            if(!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone,BuildTarget.StandaloneWindows64))throw new Exception("Windows Mono build support is missing.");
            var scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray();
            if(scenes.Length==0 || scenes.Any(s=>!File.Exists(s)))throw new Exception("Build scene list is empty or contains missing scenes.");
            if(!EditorSceneManager.SaveOpenScenes())throw new Exception("Could not save open scenes.");
            Directory.CreateDirectory(Folder);
            int width=PlayerSettings.defaultScreenWidth,height=PlayerSettings.defaultScreenHeight;
            var mode=PlayerSettings.fullScreenMode;bool resizable=PlayerSettings.resizableWindow;
            BuildReport report;
            try
            {
                PlayerSettings.defaultScreenWidth=1280;PlayerSettings.defaultScreenHeight=720;
                PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.resizableWindow=true;
                EditorUserBuildSettings.buildScriptsOnly=false;
                report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=scenes,locationPathName=Path.GetFullPath(Folder+"/Recoil.exe"),target=BuildTarget.StandaloneWindows64,options=BuildOptions.CleanBuildCache});
            }
            finally{PlayerSettings.defaultScreenWidth=width;PlayerSettings.defaultScreenHeight=height;PlayerSettings.fullScreenMode=mode;PlayerSettings.resizableWindow=resizable;}
            if(report.summary.result!=BuildResult.Succeeded || report.summary.totalErrors>0)throw new Exception("Windows build: "+report.summary.result+"; errors="+report.summary.totalErrors);
            if(!File.Exists(Folder+"/Recoil.exe") || !File.Exists(Folder+"/UnityPlayer.dll") || !Directory.Exists(Folder+"/Recoil_Data") || !File.Exists(Folder+"/Recoil_Data/globalgamemanagers") || !File.Exists(Folder+"/Recoil_Data/level0"))throw new Exception("Build output is incomplete.");
            if(File.Exists("Documents/操作说明-Windows.txt"))File.Copy("Documents/操作说明-Windows.txt",Folder+"/操作说明.txt",true);
            File.WriteAllText(Status,"SUCCEEDED\n"+report.summary.outputPath+"\nScenes: "+scenes.Length+"\nBytes: "+report.summary.totalSize+"\nWarnings: "+report.summary.totalWarnings);
            Debug.Log("RECOIL_WINDOWS_BUILD_SUCCEEDED: "+report.summary.outputPath);
        }
        catch(Exception error){File.WriteAllText(Status,"FAILED\n"+error);Debug.LogException(error);}
    }
    public static string Schedule()
    {EditorApplication.delayCall+=Build;return "Windows x64 build scheduled; status: "+Status;}
}
