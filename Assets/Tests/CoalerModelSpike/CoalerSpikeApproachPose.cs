using System;
using System.Linq;
using UnityEngine;

// Pose-only experiment on the imported rig. No locomotion or production AI.
public sealed class CoalerSpikeApproachPose
{
    readonly Transform[] bones;
    readonly Quaternion[] rest;
    readonly Transform[][] chains;
    readonly float[] sides;
    readonly Vector3 right,forward;
    public int ChainCount {get{return chains.Length;}}
    public CoalerSpikeApproachPose(Transform model,Camera camera)
    {
        bones=model.GetComponentsInChildren<Transform>(true).Where(t=>t.name.Contains("_JNT")).ToArray();
        rest=bones.Select(t=>t.localRotation).ToArray();
        right=camera?camera.transform.right:Vector3.right;
        forward=camera?Vector3.ProjectOnPlane(camera.transform.forward,Vector3.up).normalized:Vector3.forward;
        string[] names={"FRONT_01","FRONT_02","FRONT_03","BACK_01","BACK_02","BACK_03","SIDE_NEG_X","SIDE_POS_X"};
        chains=names.Select(n=>Enumerable.Range(1,12).Select(i=>Find("COALER_EXTRA_TENTACLE_"+n+"_JNT_"+i.ToString("D2"))).Where(t=>t).ToArray()).Where(c=>c.Length==12).ToArray();
        sides=chains.Select((c,i)=>{float x=Vector3.Dot(c[5].position-model.position,right);return Mathf.Abs(x)>.025f?Mathf.Sign(x):(i%2==0?-1f:1f);}).ToArray();
    }
    Transform Find(string name){return bones.FirstOrDefault(t=>t.name.EndsWith(name,StringComparison.Ordinal));}
    public void Apply(float blend,float clock,float coreOpening=0)
    {
        for(int i=0;i<bones.Length;i++)if(bones[i])bones[i].localRotation=rest[i];
        if(coreOpening>0)
        {
            // Ray tests isolate FRONT_02 as the actual native-core cover.
            var cover=Find("COALER_EXTRA_TENTACLE_FRONT_02_JNT_01");
            if(cover)
            {
                float side=Vector3.Dot(cover.position-Find("COALER_spine02_JNT").position,right)<0?-1:1;
                cover.rotation=Quaternion.AngleAxis(side*65*coreOpening,forward)*cover.rotation;
            }
        }
        if(blend<=0)return;
        var chest=Find("COALER_chest_JNT");
        if(chest)chest.rotation=Quaternion.AngleAxis(-7*blend,right)*chest.rotation;
        for(int c=0;c<chains.Length;c++)for(int j=0;j<11;j++)
        {
            float u=j/10f,side=sides[c];
            float reach=.45f+(c%3)*.12f;
            Vector3 direction=right*(side*Mathf.Lerp(.7f,.25f,u))+Vector3.up*Mathf.Lerp(-.35f,.05f,u)-forward*reach;
            direction+=right*(Mathf.Sin(clock*1.5f-c*.7f-u*2)*.065f*u);
            Aim(chains[c][j],chains[c][j+1],direction,blend);
        }
        foreach(string sideName in new[]{"L","R"})
        {
            var shoulder=Find("COALER_"+sideName+"_shoulder_JNT");var elbow=Find("COALER_"+sideName+"_elbow_JNT");var wrist=Find("COALER_"+sideName+"_wrist_JNT");
            if(!shoulder||!elbow||!wrist)continue;
            float side=Mathf.Sign(Vector3.Dot(shoulder.position-chest.position,right));
            Aim(shoulder,elbow,right*(side*.48f)+Vector3.down*.4f-forward*.65f,blend);
            Aim(elbow,wrist,right*(side*.2f)+Vector3.up*.08f-forward*.85f,blend);
        }
    }
    static void Aim(Transform joint,Transform next,Vector3 direction,float blend)
    {
        Vector3 current=next.position-joint.position;if(current.sqrMagnitude<.000001f)return;
        joint.rotation=Quaternion.Slerp(Quaternion.identity,Quaternion.FromToRotation(current,direction),blend)*joint.rotation;
    }
}
