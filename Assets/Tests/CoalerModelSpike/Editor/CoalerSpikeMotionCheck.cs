using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Explicit, test-scene-only diagnostic. Never runs in a production scene.
[InitializeOnLoad]
public static class CoalerSpikeMotionCheck
{
    const string Request="output/coaler-model-spike/check-motion.request";
    static CoalerSpikeMotionCheck(){EditorApplication.delayCall+=Requested;}
    static void Requested()
    {
        if(File.Exists("output/coaler-model-spike/check-approach.request")&&!EditorApplication.isPlayingOrWillChangePlaymode&&UnityEngine.SceneManagement.SceneManager.GetActiveScene().path==CoalerModelSpikeBuilder.ScenePath)
        {File.Delete("output/coaler-model-spike/check-approach.request");CheckApproach();}
        if(!File.Exists(Request)||EditorApplication.isPlayingOrWillChangePlaymode)return;
        if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path!=CoalerModelSpikeBuilder.ScenePath)return;
        File.Delete(Request);Check();
    }
    [MenuItem("Recoil/Tests/Check Coaler Spike Approach")]
    public static void CheckApproach()
    {
        var lab=Object.FindObjectOfType<CoalerModelSpikeLab>();if(!lab)return;
        lab.Initialize();lab.ResetLab();
        var skins=lab.model.GetComponentsInChildren<SkinnedMeshRenderer>();
        var boots=skins.Where(s=>s.name=="polySurface6"||s.name=="polySurface7").ToArray();
        var all=skins.Select(s=>{var m=new Mesh();s.BakeMesh(m);var v=m.vertices.Select(p=>s.transform.position+s.transform.rotation*p).ToArray();Object.DestroyImmediate(m);return v;}).ToArray();
        CoalerModelSpikeBuilder.Capture(lab.testCamera,"06-approach-idle");
        lab.ToggleApproach();lab.Step(1f);float middle=lab.ApproachBlend;
        CoalerModelSpikeBuilder.Capture(lab.testCamera,"07-approach-midpoint");
        lab.Step(1f);float full=lab.ApproachBlend;float moved=0,feet=0;
        for(int s=0;s<skins.Length;s++)
        {
            var m=new Mesh();skins[s].BakeMesh(m);var vertices=m.vertices;
            for(int i=0;i<vertices.Length;i++){float d=Vector3.Distance(all[s][i],skins[s].transform.position+skins[s].transform.rotation*vertices[i]);moved=Mathf.Max(moved,d);if(boots.Contains(skins[s]))feet=Mathf.Max(feet,d);}
            Object.DestroyImmediate(m);
        }
        CoalerModelSpikeBuilder.Capture(lab.testCamera,"08-approach-full");
        lab.ToggleApproach();lab.Step(2f);
        string result=(lab.ApproachChains==8&&Mathf.Abs(middle-.5f)<.001f&&full==1&&lab.ApproachBlend==0&&moved>.1f&&feet<.001f?"PASS":"FAIL")+" chains="+lab.ApproachChains+" midpoint="+middle+" full="+full+" return="+lab.ApproachBlend+" maxVertexMeters="+moved.ToString("F4")+" bootDisplacement="+feet.ToString("F6");
        lab.ResetLab();File.WriteAllText("output/coaler-model-spike/approach-verification.txt",result);Debug.Log("COALER_APPROACH "+result);
    }
    [MenuItem("Recoil/Tests/Check Coaler Spike Motion")]
    public static void Check()
    {
        var lab=Object.FindObjectOfType<CoalerModelSpikeLab>();if(!lab)return;
        lab.Initialize();lab.ResetLab();
        var skin=lab.model.GetComponentsInChildren<SkinnedMeshRenderer>().First(r=>r.name.Contains("COALER_EXTRA_TENTACLE_BACK_01_GEO"));
        var mesh=new Mesh();skin.BakeMesh(mesh);var rest=mesh.vertices;
        float maxWorld=0,maxPixels=0;
        lab.ToggleChain();
        for(int sample=0;sample<12;sample++)
        {
            lab.Step(.29f);skin.BakeMesh(mesh);var now=mesh.vertices;
            for(int i=0;i<now.Length;i++)
            {
                var a=skin.transform.position+skin.transform.rotation*rest[i];var b=skin.transform.position+skin.transform.rotation*now[i];
                maxWorld=Mathf.Max(maxWorld,Vector3.Distance(a,b));
                var sa=lab.testCamera.WorldToViewportPoint(a);var sb=lab.testCamera.WorldToViewportPoint(b);
                maxPixels=Mathf.Max(maxPixels,new Vector2((sa.x-sb.x)*1200,(sa.y-sb.y)*760).magnitude);
            }
        }
        lab.ResetLab();Object.DestroyImmediate(mesh);
        string result=(lab.DrivenJointCount==12&&maxPixels>20?"PASS":"FAIL")+" bones="+lab.DrivenJointCount+" maxWorldMeters="+maxWorld.ToString("F4")+" maxScreenPixelsAt1200x760="+maxPixels.ToString("F1");
        File.WriteAllText("output/coaler-model-spike/motion-verification.txt",result);Debug.Log("COALER_MOTION "+result);
    }
}
