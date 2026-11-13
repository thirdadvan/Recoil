using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Read-only measurements. Opt-in request lets diagnostics run without a live editor connector.
[InitializeOnLoad]
public static class CharacterGroundingInspection
{
    const string Folder="output/character-grounding";
    static CharacterGroundingInspection(){EditorApplication.update+=Poll;}
    static void Poll()
    {
        string request=Folder+"/inspect.request";
        if(!File.Exists(request)||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        File.Delete(request);
        try{Inspect();}catch(Exception e){Directory.CreateDirectory(Folder);File.WriteAllText(Folder+"/error.txt",e.ToString());}
    }
    [MenuItem("Recoil/Diagnostics/Inspect warehouse character grounding")]
    public static void Inspect()
    {
        Directory.CreateDirectory(Folder);var report=new StringBuilder();
        report.AppendLine("Runtime="+Application.isPlaying);
        var previous=SceneManager.GetActiveScene();
        foreach(string path in new[]{"Assets/Scenes/3D/RecoilWarehouseStoreroom.unity","Assets/Scenes/3D/RecoilFactoryWarehouse.unity"})
        {
            var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;
            if(opened&&Application.isPlaying){report.AppendLine(path+": not loaded; runtime scene was not changed.");continue;}
            if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            try
            {
                report.AppendLine("SCENE "+path);
                var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
                var floor=all.Select(t=>t.GetComponent<BoxCollider2D>()).Where(c=>c&&!c.isTrigger&&c.enabled&&c.bounds.size.x>3&&c.bounds.size.y<2).ToArray();
                foreach(var c in floor)report.AppendLine("Collider "+c.name+" top="+c.bounds.max.y+" X="+c.bounds.min.x+".."+c.bounds.max.x);
                foreach(var t in all.Where(t=>t.name.IndexOf("floor",StringComparison.OrdinalIgnoreCase)>=0))
                foreach(var r in t.GetComponents<MeshRenderer>().Where(r=>r.enabled))report.AppendLine("Floor mesh "+t.name+" Y="+r.bounds.min.y+".."+r.bounds.max.y+" Z="+r.bounds.min.z+".."+r.bounds.max.z);
                foreach(var p in all.Select(t=>t.GetComponent<RecoilPlayerController>()).Where(p=>p))Measure(p.transform,p.BodyHeight,floor,report);
                foreach(var child in all.Select(t=>t.GetComponent<EscortChild>()).Where(c=>c))Measure(child.transform,child.bodyHeight,floor,report);
            }
            finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
        }
        if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);
        File.WriteAllText(Folder+"/inspection.txt",report.ToString());
    }
    static void Measure(Transform root,float height,BoxCollider2D[] floors,StringBuilder report)
    {
        float feet=root.position.y-height*.5f;
        report.AppendLine("Actor "+root.name+" root="+root.position+" height="+height+" controllerFeet="+feet);
        var driver=root.GetComponentInChildren<DawnPlayerAnimation>(true);
        if(driver)report.AppendLine("Dawn modelHeightOffset="+driver.modelHeightOffset+" pivotLocal="+driver.facingRoot.localPosition);
        foreach(var animator in root.GetComponentsInChildren<Animator>(true))
        {
            report.AppendLine("Model "+animator.name+" local="+animator.transform.localPosition+" scale="+animator.transform.lossyScale);
            if(Application.isPlaying){BoundsNow(animator,feet,report,"current live pose");continue;}
            var pivotBefore=driver&&driver.facingRoot?driver.facingRoot.localPosition:Vector3.zero;
            var ts=animator.GetComponentsInChildren<Transform>(true);var ps=ts.Select(t=>t.localPosition).ToArray();var qs=ts.Select(t=>t.localRotation).ToArray();var ss=ts.Select(t=>t.localScale).ToArray();
            try
            {
                animator.Rebind();animator.Update(0);BoundsNow(animator,feet,report,"initial pose");
                if(driver&&driver.facingRoot){driver.facingRoot.localPosition=driver.player.transform.InverseTransformVector(Vector3.up*driver.modelHeightOffset);BoundsNow(animator,feet,report,"runtime fixed-height idle");}
                if(animator.runtimeAnimatorController!=null)
                foreach(var clip in animator.runtimeAnimatorController.animationClips.Distinct().Where(c=>c.name.IndexOf("Idle",StringComparison.OrdinalIgnoreCase)>=0||c.name.IndexOf("Crying",StringComparison.OrdinalIgnoreCase)>=0||c.name.IndexOf("Walk",StringComparison.OrdinalIgnoreCase)>=0))
                {
                    animator.Rebind();animator.Update(0);
                    float min=float.PositiveInfinity,max=float.NegativeInfinity;
                    for(int sample=0;sample<24;sample++){clip.SampleAnimation(animator.gameObject,clip.length*sample/24f);float gap=FactoryManagerMeshyIntegration.SkinBounds(animator.gameObject).min.y-feet;min=Mathf.Min(min,gap);max=Mathf.Max(max,gap);}
                    report.AppendLine("  "+clip.name+" full cycle sole/controller gap="+min.ToString("F4")+".."+max.ToString("F4"));
                }
            }
            finally{for(int i=0;i<ts.Length;i++){ts[i].localPosition=ps[i];ts[i].localRotation=qs[i];ts[i].localScale=ss[i];}if(driver&&driver.facingRoot)driver.facingRoot.localPosition=pivotBefore;}
        }
    }
    static void BoundsNow(Animator a,float feet,StringBuilder report,string pose)
    {
        float low=FactoryManagerMeshyIntegration.SkinBounds(a.gameObject).min.y;
        string bones="";
        if(a.isHuman){var l=a.GetBoneTransform(HumanBodyBones.LeftFoot);var r=a.GetBoneTransform(HumanBodyBones.RightFoot);bones=" footBones="+(l?l.position.y.ToString("F4"):"-")+"/"+(r?r.position.y.ToString("F4"):"-");}
        report.AppendLine("  "+pose+" soleY="+low.ToString("F4")+" visual/controller gap="+(low-feet).ToString("F4")+bones);
    }
}
