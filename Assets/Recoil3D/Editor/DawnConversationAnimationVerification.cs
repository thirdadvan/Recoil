using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class DawnConversationAnimationVerification
{
    const string Output="Captures/ConversationAnimation/";
    static IEnumerator routine;
    static int frame=-1;
    static readonly List<string> report=new List<string>();
    static readonly Dictionary<RampagerController,bool> enemies=new Dictionary<RampagerController,bool>();
    static RecoilPlayerController player;
    static bool previousPlayerEnabled,previousGun;
    static RecoilPrototype world;
    static DawnPlayerAnimation animation;
    static void Need(bool ok,string message){if(!ok)throw new Exception(message);}
    static void Set(object target,string name,object value){target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(target,value);}
    static void Restore()
    {
        if(world)world.startWithWaterGun=previousGun;
        if(player)player.enabled=previousPlayerEnabled;
        if(animation)animation.manuallyDriven=false;
        foreach(var e in enemies)if(e.Key)e.Key.enabled=e.Value;enemies.Clear();
    }
    static void Step()
    {
        if(!EditorApplication.isPlaying){EditorApplication.update-=Step;routine=null;Restore();return;}
        if(frame==Time.frameCount)return;frame=Time.frameCount;
        try{if(!routine.MoveNext()){EditorApplication.update-=Step;routine=null;Restore();File.WriteAllLines(Output+"Verification.txt",report);Debug.Log("Conversation animation PASS\n"+string.Join("\n",report));}}
        catch(Exception e){EditorApplication.update-=Step;routine=null;Restore();report.Add("FAIL "+e);File.WriteAllLines(Output+"Verification.txt",report);Debug.LogException(e);}
    }
    public static string Run()
    {
        Need(EditorApplication.isPlaying && routine==null,"Run once in Play mode.");
        Directory.CreateDirectory(Output);report.Clear();frame=-1;routine=Verify();EditorApplication.update+=Step;return "Conversation animation verification started";
    }
    static void Capture(string name)
    {
        var go=new GameObject("Temporary conversation closeup");var cam=go.AddComponent<Camera>();
        var lightGo=new GameObject("Temporary conversation key light");var light=lightGo.AddComponent<Light>();light.type=LightType.Point;light.range=8;light.intensity=4;
        lightGo.transform.position=player.WorldPosition+new Vector3(-1,1.5f,-2.5f);
        cam.orthographic=true;cam.orthographicSize=1.15f;cam.nearClipPlane=.01f;cam.farClipPlane=30;
        cam.transform.position=player.WorldPosition+new Vector3(.3f,.15f,-4);cam.transform.LookAt(player.WorldPosition+Vector3.up*.06f);
        var rt=new RenderTexture(900,900,24);var tex=new Texture2D(900,900,TextureFormat.RGB24,false);var old=RenderTexture.active;
        try{cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,900,900),0,0);tex.Apply();File.WriteAllBytes(Output+name+".png",tex.EncodeToPNG());}
        finally{RenderTexture.active=old;cam.targetTexture=null;UnityEngine.Object.DestroyImmediate(tex);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(lightGo);}
    }
    static IEnumerator Verify()
    {
        var actor=UnityEngine.Object.FindObjectOfType<CoalerCosplayerConversation>();Need(actor!=null,"Cosplayer missing");
        world=actor.world;player=world.Player;animation=player.GetComponentInChildren<DawnPlayerAnimation>();
        previousPlayerEnabled=player.enabled;previousGun=world.startWithWaterGun;player.enabled=false;Set(world,"started",true);
        foreach(var e in UnityEngine.Object.FindObjectsOfType<RampagerController>()){enemies[e]=e.enabled;e.enabled=false;}
        if(world.ConversationActive){world.CancelConversationWith(actor.headAnchor);yield return null;}
        var animator=animation.animator;var root=animation.facingRoot;
        var rightUpper=animator.GetBoneTransform(HumanBodyBones.RightUpperArm);var rightLower=animator.GetBoneTransform(HumanBodyBones.RightLowerArm);var rightHand=animator.GetBoneTransform(HumanBodyBones.RightHand);
        var leftFoot=animator.GetBoneTransform(HumanBodyBones.LeftFoot);var rightFoot=animator.GetBoneTransform(HumanBodyBones.RightFoot);
        for(int side=0;side<2;side++)for(int mode=0;mode<3;mode++){
            bool armed=mode!=2,backwards=mode!=0;float targetSign=side==0?1:-1,initialSign=backwards?-targetSign:targetSign;
            world.startWithWaterGun=armed;animation.manuallyDriven=true;
            player.Position=new Vector2(actor.distanceOrigin.position.x+(side==0?-1.7f:1.7f),7.615f+player.BodyHeight*.5f);
            player.AimLocomotion.FaceConversationPartner(initialSign);player.Aim=new Vector2(initialSign,0);
            root.localRotation=Quaternion.Euler(0,initialSign>0?-90:90,0);Set(animation,"yaw",initialSign>0?-90f:90f);
            animator.speed=1;animator.SetFloat("Speed",0);animator.Play("Locomotion",0,0);animator.SetLayerWeight(1,armed?1:0);animator.Update(0);
            player.SyncVisuals(0);animation.FinalizeWeaponGrip();world.ViewCamera.GetComponent<RecoilCameraController>().SnapToTarget();
            Vector3 position=player.WorldPosition;int ammo=player.Ammo,shots=player.ShotSequence;
            float upperLength=Vector3.Distance(rightUpper.position,rightLower.position),lowerLength=Vector3.Distance(rightLower.position,rightHand.position);
            Need(actor.StartConversation(),"Failed to start test conversation");animation.manuallyDriven=false;
            Need(animation.ConversationNeededTurn==backwards,"Back-facing detection incorrect");
            Need(animation.ConversationHadWeapon==armed,"Equipped weapon detection incorrect");
            world.AdvanceConversation();Need(world.ConversationLineIndex==0 && world.ConversationPreparing,"0.8-second gate skipped");
            bool turnCaptured=false,lowerCaptured=false;
            while(animation.ConversationPresentationTime<.86f){
                float t=animation.ConversationPresentationTime;
                Need(Vector3.Distance(player.WorldPosition,position)<.00001f,"Player moved during preparation");
                Need(Mathf.Abs(Vector3.Distance(rightUpper.position,rightLower.position)-upperLength)<.001f && Mathf.Abs(Vector3.Distance(rightLower.position,rightHand.position)-lowerLength)<.001f,"Arm bone length changed");
                if(backwards && t<animation.conversationTurnDuration-.04f)Need(animation.ConversationHolsterProgress==0,"Holstering began before turn completed");
                if(t<.77f)Need(world.ConversationPreparing,"Dialogue appeared before 0.8 seconds");
                if(side==0 && mode==1 && !turnCaptured && t>.13f){Capture("01-Turning");turnCaptured=true;}
                if(side==0 && mode==1 && !lowerCaptured && t>.53f){Capture("02-Holstering");lowerCaptured=true;}
                yield return null;
            }
            Need(!world.ConversationPreparing,"Preparation did not finish");
            Need(Quaternion.Angle(root.localRotation,Quaternion.Euler(0,targetSign>0?-90:90,0))<.05f,"Body did not face NPC");
            Need(player.Aim.x*targetSign>.99f && player.AimLocomotion.FacingSign==targetSign,"Gameplay aim/facing not synchronized");
            Need(!animation.weapon.gameObject.activeSelf,"Gun not stowed");Need(animation.AimWeight==0,"Aiming layer still active");
            Need(player.Ammo==ammo && player.ShotSequence==shots && player.HasWaterGun==armed,"Stowing changed equipment or ammo");
            var chest=animator.GetBoneTransform(HumanBodyBones.Chest);Quaternion baseChest=chest.localRotation;float maxChest=0,minBreath=1,maxBreath=-1;
            Vector3 footL=leftFoot.position,footR=rightFoot.position;
            float until=animation.ConversationPresentationTime+3.3f;
            while(animation.ConversationPresentationTime<until){
                maxChest=Mathf.Max(maxChest,Quaternion.Angle(baseChest,chest.localRotation));minBreath=Mathf.Min(minBreath,animation.ConversationBreathAmount);maxBreath=Mathf.Max(maxBreath,animation.ConversationBreathAmount);
                Need(Vector3.Distance(player.WorldPosition,position)<.00001f,"Breathing moved gameplay root");
                Need(Vector3.Distance(leftFoot.position,footL)<.015f && Vector3.Distance(rightFoot.position,footR)<.015f,"Breathing moved planted feet");yield return null;
            }
            Need(maxChest>2.5f && maxBreath-minBreath>1.8f,"Conversation breathing not visibly animated");
            if(side==0 && mode==1){Capture("03-ConversationIdle");ScreenCapture.CaptureScreenshot(Output+"ConversationGameView.png");}
            report.Add("PASS "+(side==0?"NPC-right":"NPC-left")+" / "+(armed?backwards?"armed-backwards":"armed-facing":"unarmed-backwards")+": turn before stow, 0.8s gate, stable arm lengths/root/feet, hidden gun, breath range="+(maxBreath-minBreath).ToString("F2")+", chest motion="+maxChest.ToString("F2"));
            world.CancelConversationWith(actor.headAnchor);yield return null;
            Need(!animation.ConversationPresentationActive && world.Playing,"Cancellation did not restore gameplay");
            for(int i=0;i<20;i++)yield return null;
            Need(animation.weapon.gameObject.activeSelf==armed,"Weapon visibility not restored after conversation");
            if(armed)Need(animation.AimWeight>.99f,"Aiming layer not restored");
        }
        world.startWithWaterGun=previousGun;
        report.Add("PASS exit restores normal aiming and weapon visibility without changing ammo");
    }
}
