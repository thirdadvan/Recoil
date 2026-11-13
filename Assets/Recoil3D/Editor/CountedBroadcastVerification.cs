using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class CountedBroadcastVerification
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Check(bool ok,string message){if(!ok)throw new Exception("Counted broadcast: "+message);}
    public static string Run()
    {
        var original=SceneManager.GetActiveScene();
        var source=SceneManager.GetSceneByPath("Assets/Scenes/3D/RecoilFactoryInterior.unity");
        Check(source.isLoaded,"load the factory interior");
        var preview=EditorSceneManager.NewPreviewScene();var report=new StringBuilder();
        try {
            foreach(var root in source.GetRootGameObjects()){var copy=UnityEngine.Object.Instantiate(root);SceneManager.MoveGameObjectToScene(copy,preview);}
            var all=preview.GetRootGameObjects();
            var world=all.SelectMany(g=>g.GetComponentsInChildren<RecoilPrototype>(true)).Single();
            var cam=all.SelectMany(g=>g.GetComponentsInChildren<Camera>(true)).First(c=>c.CompareTag("MainCamera"));
            typeof(RecoilPrototype).GetField("cam",Private).SetValue(world,cam);
            typeof(RecoilPrototype).GetMethod("BindScene",Private).Invoke(world,null);
            typeof(RecoilPrototype).GetMethod("ResetRun",Private).Invoke(world,new object[]{true});
            typeof(RecoilPrototype).GetField("started",Private).SetValue(world,true);
            var waves=world.GetComponentInChildren<SoundWaveManager>(true);
            var effect=cam.GetComponent<BroadcastRefractionEffect>();
            // Camera is a separate scene root; Instantiate cannot remap references
            // between roots cloned in separate calls.
            effect.broadcast=waves;
            typeof(BroadcastRefractionEffect).GetField("context",Private).SetValue(effect,world);
            waves.Initialize(world);
            Check(effect&&effect.broadcast==waves,"shared camera/manager references");
            world.Player.ResetController(new Vector2(16,world.Player.BodyHeight*.5f));
            cam.transform.SetPositionAndRotation(new Vector3(16,4,-18),Quaternion.Euler(3,0,0));
            foreach(float fov in new[]{40f,58f}) {
                cam.fieldOfView=fov;var flight=effect.MeasureFlight(waves.DirectionVisualDuration);
                Check(flight.centreArrival>0&&flight.centreArrival<flight.duration,"centre arrival within packet duration");
                Check(flight.RadiusAt(flight.centreArrival-.01f)<flight.RadiusAt(flight.centreArrival+.01f),"both commands must propagate outward");
                foreach(int direction in new[]{-1,1}) {
                    waves.ResetManager();effect.ClearPulses();waves.BeginCommand(direction);
                    int count=direction<0?1:2;
                    float expected=flight.centreArrival+(count-1)*waves.doublePulseSpacing;
                    Check(waves.CuePulseCount==count&&effect.ActivePulseCount==count,"one/two packet count");
                    Check(Mathf.Abs(waves.CommandMoveDelay-expected)<.001f,"manager follows measured last packet arrival");
                    float elapsed=0;bool moved=false;
                    float playerArrival=flight.ArrivalAt(effect.DistanceToWorld(flight,world.Player.WorldPosition));
                    for(int i=0;i<600;i++) {
                        waves.Simulate(.005f);elapsed+=.005f;
                        if(elapsed<expected-.006f)Check(waves.MoveStep<=.000001f,"early monster movement");
                        if(elapsed<playerArrival-.006f)Check(waves.PlayedDrumCount==0,"drum before player crossing");
                        if(waves.MoveStep>0)moved=true;
                        if(elapsed>Mathf.Max(expected+.2f,playerArrival+(count-1)*waves.doublePulseSpacing+.2f))break;
                    }
                    Check(moved,"movement never begins");
                    Check(waves.PlayedDrumCount==count,"missing/duplicate arrival drum");
                    report.AppendLine("FOV "+fov+", "+(direction<0?"LEFT":"RIGHT")+": "+count+" outward packets; first centre="+flight.centreArrival.ToString("F3")+"s; move="+expected.ToString("F3")+"s; first player beat="+playerArrival.ToString("F3")+"s; drum count="+waves.PlayedDrumCount+"; no early move/beat.");
                }
            }
            waves.ResetManager();effect.ClearPulses();
            Check(waves.StartBroadcastAt(Vector2.zero),"start complete sequence");
            waves.Simulate(waves.warningDuration);
            Check(waves.Phase==SoundWaveManager.BroadcastPhase.Commands&&waves.CueIndex==1,"warning transition");
            float interval=waves.WaveInterval;
            Check(Mathf.Abs(waves.Remaining-interval)<.001f,"sequence used stale previous command delay");
            waves.Simulate(interval-.01f);Check(waves.CueIndex==1,"next command before move/hold completed");
            waves.Simulate(.02f);Check(waves.CueIndex==2,"next counted command missing");
            report.AppendLine("Full broadcast sequence: warning, measured arrival, move, hold and next-command scheduling passed.");
            var clip=BroadcastDrumAudio.BuildClip();var samples=new float[clip.samples];clip.GetData(samples,0);
            Check(samples.Max(v=>Mathf.Abs(v))>.2f&&samples.Max(v=>Mathf.Abs(v))<1,"drum silence or clipping");
            UnityEngine.Object.DestroyImmediate(clip);
            report.AppendLine("Synthesized deep drum: nonzero output, no clipped samples. Direct-impact/normal AI rules unchanged.");
            Directory.CreateDirectory("output/counted-broadcast");File.WriteAllText("output/counted-broadcast/verification.txt",report.ToString());
            return report.ToString();
        }
        finally {
            foreach(var root in preview.GetRootGameObjects())UnityEngine.Object.DestroyImmediate(root);
            EditorSceneManager.ClosePreviewScene(preview);
            if(original.IsValid()&&original.isLoaded)SceneManager.SetActiveScene(original);
        }
    }
}
