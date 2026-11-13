using System;
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class DawnVerificationRunner
{
    const string Pending="Dawn.Verification.Pending";
    static IEnumerator steps;static int lastFrame=-1;
    static DawnVerificationRunner()
    {
        // Resume only a check explicitly launched from the menu across a domain reload.
        if(!SessionState.GetBool(Pending,false))return;
        EditorApplication.playModeStateChanged+=Changed;
        if(EditorApplication.isPlaying)EditorApplication.delayCall+=Run;
    }
    [MenuItem("Recoil/Dawn/3 Verify animation blends")]
    public static void Start(){SessionState.SetBool(Pending,true);EditorApplication.playModeStateChanged+=Changed;if(EditorApplication.isPlaying)Run();else EditorApplication.isPlaying=true;}
    static void Changed(PlayModeStateChange state){if(state==PlayModeStateChange.EnteredPlayMode)EditorApplication.delayCall+=Run;}
    static void Run(){EditorApplication.playModeStateChanged-=Changed;if(steps!=null)return;steps=DawnAnimationVerification.VerifyLive();lastFrame=-1;EditorApplication.update+=Tick;}
    static void Tick()
    {
        if(!EditorApplication.isPlaying||Time.frameCount==lastFrame)return;lastFrame=Time.frameCount;
        try{if(steps.MoveNext())return;File.WriteAllText("Captures/DawnAnimation/LiveRunStatus.txt","PASS "+DateTime.UtcNow.ToString("O"));}
        catch(Exception e){Directory.CreateDirectory("Captures/DawnAnimation");File.WriteAllText("Captures/DawnAnimation/LiveRunStatus.txt","FAIL\n"+e);Debug.LogException(e);}
        (steps as IDisposable)?.Dispose();steps=null;SessionState.SetBool(Pending,false);EditorApplication.update-=Tick;EditorApplication.isPlaying=false;
    }
}
