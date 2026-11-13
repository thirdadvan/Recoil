using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

public static class RetroWeaponGripVerification
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static string clearanceDetails;
    static void Need(bool ok, string message) { if (!ok) throw new Exception("Rigid grip: " + message); }
    static void Set(object target, string name, object value) { target.GetType().GetField(name, Private).SetValue(target, value); }

    // A continuous sweep checks the motion and the skinned torso surface.
    public static string RunDownwardSweep()
    {
        var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
        GameObject model=null,weapon=null,probes=null;
        try
        {
            probes=new GameObject("Downward sweep clearance probes");
            SceneManager.MoveGameObjectToScene(probes,scene);
            model=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(DawnCharacterIntegration.Root+"/Dawn.fbx"));
            SceneManager.MoveGameObjectToScene(model,scene);
            var animator=model.GetComponent<Animator>();
            animator.runtimeAnimatorController=AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(DawnCharacterIntegration.Root+"/Dawn.controller");
            animator.Rebind();animator.Update(0);
            var foot=animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            var head=animator.GetBoneTransform(HumanBodyBones.Head);
            var top=model.GetComponentsInChildren<Transform>().FirstOrDefault(t=>t.name.EndsWith("HeadTop_End"));
            float height=(top?top.position.y+.10f:head.position.y+.25f)-(foot.position.y-.065f);
            model.transform.localScale*=1.65f*DawnCharacterIntegration.VisualScale/Mathf.Max(.01f,height);
            weapon=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RetroWaterBlasterIntegration.Assembly));
            SceneManager.MoveGameObjectToScene(weapon,scene);
            var gun=weapon.GetComponent<RetroWaterBlaster>();
            var report=new List<string>();
            float maxGripError=0,maxRotationError=0,maxBoneChange=0,maxRepeatStep=0;
            foreach(int sign in new[]{1,-1})
            foreach(float recoil in new[]{0f,1f})
            {
                model.transform.rotation=Quaternion.Euler(0,sign*90,0);
                float previousMuzzle=0,levelHand=0,maxStep=0;
                Vector3 previousPosition=Vector3.zero;
                for(int degree=0;degree>=-90;degree--)
                {
                    animator.Rebind();animator.SetLayerWeight(1,1);animator.Update(0);
                    var shoulder=animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
                    var elbow=animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
                    var hand=animator.GetBoneTransform(HumanBodyBones.RightHand);
                    var ls=animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
                    var le=animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
                    var lh=animator.GetBoneTransform(HumanBodyBones.LeftHand);
                    var lengths=new[]{Vector3.Distance(shoulder.position,elbow.position),Vector3.Distance(elbow.position,hand.position),Vector3.Distance(ls.position,le.position),Vector3.Distance(le.position,lh.position)};
                    float baseShoulderY=shoulder.position.y;
                    float baseLeftShoulderY=ls.position.y;
                    float radians=degree*Mathf.Deg2Rad;
                    var aim=new Vector3(sign*Mathf.Cos(radians),Mathf.Sin(radians),0);
                    weapon.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(aim.y,aim.x)*Mathf.Rad2Deg);
                    var solver=new DawnRigidWeaponGrip(animator);
                    solver.Apply(weapon.transform,gun,aim,Vector3.right*sign,recoil,true,1);
                    var firstPosition=weapon.transform.position;
                    solver.Apply(weapon.transform,gun,aim,Vector3.right*sign,recoil,true,1);
                    maxRepeatStep=Mathf.Max(maxRepeatStep,Vector3.Distance(firstPosition,weapon.transform.position));
                    float handY=hand.position.y,muzzleY=gun.muzzle.position.y;
                    var leftKnuckles=(animator.GetBoneTransform(HumanBodyBones.LeftIndexProximal).position+animator.GetBoneTransform(HumanBodyBones.LeftMiddleProximal).position+animator.GetBoneTransform(HumanBodyBones.LeftLittleProximal).position)/3;
                    Vector3 palmAxis=leftKnuckles-lh.position;
                    Vector3 contact=gun.supportGrip.position-lh.position;
                    float palmAlong=Vector3.Dot(contact,palmAxis.normalized)/palmAxis.magnitude;
                    float palmSide=Vector3.ProjectOnPlane(contact,palmAxis).magnitude;
                    Need(palmAlong>.3f && palmAlong<.9f && palmSide<.02f,"Support grip left the anatomical left palm (wrist contact is insufficient).");
                    var across=Vector3.Cross(Vector3.up,Vector3.right*sign).normalized;
                    var shoulderMiddle=(shoulder.position+ls.position)*.5f;
                    if(degree<=-15)
                    {
                        Vector3 rightChord=hand.position-shoulder.position;
                        Vector3 rightBend=elbow.position-shoulder.position-Vector3.Project(elbow.position-shoulder.position,rightChord);
                        Need(rightBend.y<.0001f,"Anatomical right elbow bends upward while aiming down, facing "+sign);
                    }
                    Need(Vector3.Dot(elbow.position-shoulderMiddle,across)>-.02f && Vector3.Dot(le.position-shoulderMiddle,-across)>-.02f,"An elbow crossed the torso centreline.");
                    Need(solver.DownwardBodyLean<=-degree+.01f,"Torso consumes more than the requested downward aim.");
                    if(degree==0)
                    {
                        levelHand=handY;
                        Need(solver.DownwardBodyLean<.01f,"Level aim starts with torso lean.");
                    }
                    else
                    {
                        Need(handY<=levelHand+.045f,"Rear hand lifts excessively.");
                        Need(muzzleY<=previousMuzzle+.001f,"Nozzle rises during downward sweep.");
                        maxStep=Mathf.Max(maxStep,Vector3.Distance(previousPosition,weapon.transform.position));
                    }
                    Need(shoulder.position.y-baseShoulderY<.035f,"Downward pose lifts the rear shoulder.");
                    maxGripError=Mathf.Max(maxGripError,solver.MainPositionError,solver.SupportPositionError);
                    maxRotationError=Mathf.Max(maxRotationError,solver.MainRotationError,solver.SupportRotationError);
                    var posedLengths=new[]{Vector3.Distance(shoulder.position,elbow.position),Vector3.Distance(elbow.position,hand.position),Vector3.Distance(ls.position,le.position),Vector3.Distance(le.position,lh.position)};
                    for(int bone=0;bone<lengths.Length;bone++)maxBoneChange=Mathf.Max(maxBoneChange,Mathf.Abs(lengths[bone]-posedLengths[bone]));
                    if(degree%5==0)
                    foreach(var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>())
                    {
                        var mesh=new Mesh();
                        try
                        {
                            skin.BakeMesh(mesh,false);
                            Need(TorsoCrossings(animator,skin,mesh,probes.transform)==0,"Downward sweep intersects torso at "+degree+" degrees: "+clearanceDetails);
                        }
                        finally{UnityEngine.Object.DestroyImmediate(mesh);}
                    }
                    if(degree==-90)
                    {
                        Need(maxStep<.02f,"Downward pose contains a position jump.");
                        float rearBend=Vector3.Angle(shoulder.position-elbow.position,hand.position-elbow.position);
                        float frontBend=Vector3.Angle(ls.position-le.position,lh.position-le.position);
                        var hips=animator.GetBoneTransform(HumanBodyBones.Hips).position;
                        var bodyDown=(hips-shoulderMiddle).normalized;
                        float rearAngle=Vector3.Angle(hand.position-elbow.position,bodyDown),frontAngle=Vector3.Angle(lh.position-le.position,bodyDown);
                        var axis=hips-shoulderMiddle;
                        Vector3 rearMid=(elbow.position+hand.position)*.5f,frontMid=(le.position+lh.position)*.5f;
                        float rearGap=Vector3.Distance(rearMid,shoulderMiddle+axis*Mathf.Clamp01(Vector3.Dot(rearMid-shoulderMiddle,axis)/axis.sqrMagnitude));
                        float frontGap=Vector3.Distance(frontMid,shoulderMiddle+axis*Mathf.Clamp01(Vector3.Dot(frontMid-shoulderMiddle,axis)/axis.sqrMagnitude));
                        Need(rearBend>100 && rearBend<145 && frontBend>150 && frontBend>rearBend+20,"Down aim must keep the anatomical right arm bent and the left arm straighter.");
                        Need(handY<levelHand-.12f,"Downward hands stayed at chest height.");
                        Need(ls.position.y>baseLeftShoulderY-.24f,"Left shoulder sinks excessively despite palm contact.");
                        Need(frontAngle<40 && rearGap<.27f && frontGap<.27f,"Downward forearms remain too far from the torso.");
                        report.Add("Facing "+sign+", recoil "+recoil+": anatomical right/left elbow "+rearBend.ToString("F1")+" / "+frontBend.ToString("F1")+" degrees; left palm contact at "+palmAlong.ToString("F2")+" of wrist/knuckle span, side gap "+palmSide.ToString("F5")+" m; left shoulder drop "+(baseLeftShoulderY-ls.position.y).ToString("F3")+" m; forearm-midpoint/torso-axis distances "+rearGap.ToString("F3")+" / "+frontGap.ToString("F3")+" m; wrist drop "+(levelHand-handY).ToString("F3")+" m; max pose step "+maxStep.ToString("F4")+" m.");
                    }
                    previousMuzzle=muzzleY;previousPosition=weapon.transform.position;
                }
            }
            Need(maxGripError<.001f && maxRotationError<.2f,"Hands detached from their grips.");
            Need(maxBoneChange<.0001f,"Arm bones stretched.");
            Need(maxRepeatStep<.0001f,"Repeated pose evaluation drifts.");
            report.Add("PASS: 364 poses; both facings and recoil; anatomical right elbow bends down/left arm straighter; forearms close to torso; waist-level hands; no shoulder shrug; downward nozzle; fixed grips; no bone stretch or evaluation drift.");
            report.Add("PASS: skinned torso/arm clearance sampled every 5 degrees (76 poses). Max grip error "+maxGripError.ToString("F7")+" m.");
            return string.Join("\n",report);
        }
        finally
        {
            if(weapon)UnityEngine.Object.DestroyImmediate(weapon);
            if(model)UnityEngine.Object.DestroyImmediate(model);
            if(probes)UnityEngine.Object.DestroyImmediate(probes);
            UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);
        }
    }


    public static string Run(bool downwardMovement=false)
    {
        Need(EditorApplication.isPlaying, "Run in Play mode.");
        var source = UnityEngine.Object.FindObjectsOfType<DawnPlayerAnimation>().First(d => d.player.gameObject.scene == SceneManager.GetActiveScene());
        var world = source.player.GetComponentInParent<RecoilPrototype>();
        bool previousGun = world.startWithWaterGun;
        var scene = SceneManager.CreateScene("Rigid grip verification only");
        var root = new GameObject("Rigid grip verification"); SceneManager.MoveGameObjectToScene(root, scene);
        RenderTexture rt = null; Texture2D texture = null; Camera camera = null;
        var oldTarget = RenderTexture.active;
        try
        {
            world.startWithWaterGun = true;
            var pg = new GameObject("Test player"); pg.transform.SetParent(root.transform);
            var p = pg.AddComponent<RecoilPlayerController>(); p.enabled = false;
            Func<string, Transform> part = n => { var t = new GameObject(n).transform; t.SetParent(pg.transform); return t; };
            var gun = UnityEngine.Object.Instantiate(source.weapon.gameObject, pg.transform).transform;
            p.Configure(world, null, part("coat"), part("head"), part("hat"), gun);
            var old = pg.AddComponent<Recoil3DActorVisual>(); old.model = part("legacy"); old.parts = new Renderer[0];
            var d = DawnCharacterIntegration.Install(p, old); d.manuallyDriven = true;
            typeof(DawnPlayerAnimation).GetMethod("Awake", Private).Invoke(d, null);
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 31;
            p.ResetController(new Vector2(0, .825f)); Set(p, "grounded", true);
            var a = gun.GetComponentInChildren<RetroWaterBlaster>(true);
            var cg = new GameObject("Pose camera"); cg.transform.SetParent(root.transform);
            camera = cg.AddComponent<Camera>(); camera.scene = scene; camera.cullingMask = 1 << 31;
            camera.orthographic = true; camera.orthographicSize = 1.55f;
            camera.transform.position = new Vector3(2, 1.45f, -4); camera.transform.LookAt(new Vector3(.15f, 1.25f, 0));
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.16f, .18f, .20f);
            foreach (var direction in new[] { Quaternion.Euler(25, -25, 0), Quaternion.Euler(10, 145, 0) })
            {
                var lg = new GameObject("Pose light"); lg.transform.SetParent(root.transform);
                var light = lg.AddComponent<Light>(); light.type = LightType.Directional; light.intensity = .8f; light.cullingMask = 1 << 31; light.transform.rotation = direction;
            }
            rt = new RenderTexture(900, 1100, 24); camera.targetTexture = rt;
            texture = new Texture2D(2700, 1100, TextureFormat.RGB24, false);
            Directory.CreateDirectory("Captures/WeaponGrip");
            float positionError = 0, rotationError = 0, boneLengthError = 0;
            var report = new List<string>();
            var upper = new[] { HumanBodyBones.RightUpperArm, HumanBodyBones.LeftUpperArm };
            var lower = new[] { HumanBodyBones.RightLowerArm, HumanBodyBones.LeftLowerArm };
            var hands = new[] { HumanBodyBones.RightHand, HumanBodyBones.LeftHand };
            var lengths = upper.Select((b, i) => Vector3.Distance(d.animator.GetBoneTransform(b).position, d.animator.GetBoneTransform(lower[i]).position) + Vector3.Distance(d.animator.GetBoneTransform(lower[i]).position, d.animator.GetBoneTransform(hands[i]).position)).ToArray();
            var angles = new[] { -90f, 0, 90, -75, -45, -15, 15, 45, 75, 120, 180, -135 };
            for (int k = 0; k < angles.Length; k++)
            {
                float radians = angles[k] * Mathf.Deg2Rad;
                p.Aim = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
                for (int frame = 0; frame < 45; frame++)
                {
                    p.SyncVisuals(1f / 60); d.AdvancePresentation(1f / 60); d.animator.Update(1f / 60);
                    typeof(DawnPlayerAnimation).GetMethod("LateUpdate", Private).Invoke(d, null);
                    Need(d.RigidGrip != null && d.RigidGrip.Valid, "Missing arm solver.");
                    positionError = Mathf.Max(positionError, d.RigidGrip.MainPositionError, d.RigidGrip.SupportPositionError);
                    rotationError = Mathf.Max(rotationError, d.RigidGrip.MainRotationError, d.RigidGrip.SupportRotationError);
                    for (int i = 0; i < 2; i++)
                    {
                        float length = Vector3.Distance(d.animator.GetBoneTransform(upper[i]).position, d.animator.GetBoneTransform(lower[i]).position) + Vector3.Distance(d.animator.GetBoneTransform(lower[i]).position, d.animator.GetBoneTransform(hands[i]).position);
                        boneLengthError = Mathf.Max(boneLengthError, Mathf.Abs(length - lengths[i]));
                    }
                }
                if (k < 3)
                {
                    float mainElbow=Vector3.Angle(d.animator.GetBoneTransform(upper[0]).position-d.animator.GetBoneTransform(lower[0]).position,d.animator.GetBoneTransform(hands[0]).position-d.animator.GetBoneTransform(lower[0]).position);
                    float supportElbow=Vector3.Angle(d.animator.GetBoneTransform(upper[1]).position-d.animator.GetBoneTransform(lower[1]).position,d.animator.GetBoneTransform(hands[1]).position-d.animator.GetBoneTransform(lower[1]).position);
                    if(k>0)Need(mainElbow>125 && supportElbow>155,"Level/upward arms are tucked: rear="+mainElbow+", front="+supportElbow);
                    Vector3 shoulderSide=(d.animator.GetBoneTransform(upper[0]).position-d.animator.GetBoneTransform(upper[1]).position).normalized;
                    Vector3 shoulderMiddle=(d.animator.GetBoneTransform(upper[0]).position+d.animator.GetBoneTransform(upper[1]).position)*.5f;
                    float mainSide=Vector3.Dot(d.animator.GetBoneTransform(lower[0]).position-shoulderMiddle,shoulderSide);
                    float supportSide=Vector3.Dot(d.animator.GetBoneTransform(lower[1]).position-shoulderMiddle,-shoulderSide);
                    Need(mainSide>0 && supportSide>0,"An elbow crossed through the torso centre.");
                    report.Add(new[]{"DOWN","LEVEL","UP"}[k]+" elbow lateral distances: rear "+mainSide.ToString("F3")+", front "+supportSide.ToString("F3")+" m.");
                    if(k==1)
                    {
                        float chestOffset=a.muzzle.position.y-d.animator.GetBoneTransform(HumanBodyBones.Chest).position.y;
                        Need(Mathf.Abs(chestOffset)<.06f,"Level gun is not at chest height: "+chestOffset);
                        Need(a.muzzle.position.y<d.animator.GetBoneTransform(HumanBodyBones.Head).position.y-.15f,"Level gun is too close to head height.");
                        report.Add("LEVEL nozzle/chest height difference: "+chestOffset.ToString("F3")+" m.");
                    }
                    if(k==0)
                    {
                        Vector3 torsoDown=(d.animator.GetBoneTransform(HumanBodyBones.Hips).position-d.animator.GetBoneTransform(HumanBodyBones.Chest).position).normalized;
                        float mainToBody=Vector3.Angle(d.animator.GetBoneTransform(lower[0]).position-d.animator.GetBoneTransform(upper[0]).position,torsoDown);
                        float supportToBody=Vector3.Angle(d.animator.GetBoneTransform(lower[1]).position-d.animator.GetBoneTransform(upper[1]).position,torsoDown);
                        Vector3 torsoSagittal=Vector3.ProjectOnPlane(torsoDown,shoulderSide).normalized;
                        float mainForwardAngle=Vector3.Angle(Vector3.ProjectOnPlane(d.animator.GetBoneTransform(lower[0]).position-d.animator.GetBoneTransform(upper[0]).position,shoulderSide),torsoSagittal);
                        float supportForwardAngle=Vector3.Angle(Vector3.ProjectOnPlane(d.animator.GetBoneTransform(lower[1]).position-d.animator.GetBoneTransform(upper[1]).position,shoulderSide),torsoSagittal);
                        report.Add("DOWN upper-arm/torso angles: rear "+mainToBody.ToString("F1")+", front "+supportToBody.ToString("F1")+" degrees.");
                        report.Add("DOWN forward tilt excluding lateral elbow flare: rear "+mainForwardAngle.ToString("F1")+", front "+supportForwardAngle.ToString("F1")+" degrees.");
                    }
                    // Bake the evaluated bones for each capture: GPU skinning
                    // otherwise reuses the first pose within this same frame.
                    var frozen = new List<GameObject>(); var bakedMeshes = new List<Mesh>();
                    var skins = d.GetComponentsInChildren<SkinnedMeshRenderer>().Where(s => s.enabled).ToArray();
                    foreach (var skin in skins)
                    {
                        var mesh = new Mesh(); skin.BakeMesh(mesh, false); bakedMeshes.Add(mesh);
                        int crossings=TorsoCrossings(d.animator,skin,mesh,root.transform);
                        Need(crossings==0,"Arm/sleeve probes cross the torso surface: "+crossings+clearanceDetails);
                        report.Add(new[]{"DOWN","LEVEL","UP"}[k]+": torso/arm surface crossings "+crossings+clearanceDetails);
                        var go = new GameObject("Evaluated pose snapshot"); go.transform.SetParent(root.transform); go.layer = 31;
                        // This imported avatar's CPU bake already contains its
                        // centimetre/root scale; applying lossyScale doubles it.
                        go.transform.SetPositionAndRotation(skin.transform.position, skin.transform.rotation); go.transform.localScale = Vector3.one;
                        go.AddComponent<MeshFilter>().sharedMesh = mesh; go.AddComponent<MeshRenderer>().sharedMaterials = skin.sharedMaterials;
                        frozen.Add(go); skin.enabled = false;
                    }
                    var frameImage = new Texture2D(900, 1100, TextureFormat.RGB24, false);
                    try
                    {
                        camera.Render(); RenderTexture.active = rt;
                        frameImage.ReadPixels(new Rect(0, 0, 900, 1100), 0, 0); frameImage.Apply();
                        texture.SetPixels(k * 900, 0, 900, 1100, frameImage.GetPixels());
                        File.WriteAllBytes("Captures/WeaponGrip/" + new[] { "01-Down", "02-Level", "03-Up" }[k] + ".png", frameImage.EncodeToPNG());
                        if(k<2)
                        {
                            var oldPosition=camera.transform.position;var oldRotation=camera.transform.rotation;
                            camera.transform.position=new Vector3(4,1.45f,0);camera.transform.LookAt(new Vector3(.1f,1.25f,0));
                            camera.Render();RenderTexture.active=rt;
                            frameImage.ReadPixels(new Rect(0,0,900,1100),0,0);frameImage.Apply();
                            File.WriteAllBytes("Captures/WeaponGrip/"+new[]{"01-Down-Front","02-Level-Front"}[k]+".png",frameImage.EncodeToPNG());
                            camera.transform.SetPositionAndRotation(oldPosition,oldRotation);
                        }
                        report.Add(new[] { "DOWN", "LEVEL", "UP" }[k] + " arm pose: rear elbow " + mainElbow.ToString("F1") + ", front elbow " + supportElbow.ToString("F1") + " degrees.");
                    }
                    finally
                    {
                        foreach (var skin in skins) skin.enabled = true;
                        foreach (var go in frozen) UnityEngine.Object.DestroyImmediate(go);
                        foreach (var mesh in bakedMeshes) UnityEngine.Object.DestroyImmediate(mesh);
                        UnityEngine.Object.DestroyImmediate(frameImage);
                    }
                }
            }
            p.Aim=downwardMovement?Vector2.down:Vector2.right;
            for(int phase=0;phase<2;phase++)
            {
                Set(p,"grounded",phase==0);Set(p,"flying",phase==1);
                for(int frame=0;frame<60;frame++)
                {
                    p.Position+=new Vector2(2.772f/60,phase==1 ? .01f : 0);
                    p.SyncVisuals(1f/60);d.AdvancePresentation(1f/60);d.animator.Update(1f/60);
                    typeof(DawnPlayerAnimation).GetMethod("LateUpdate",Private).Invoke(d,null);
                    positionError=Mathf.Max(positionError,d.RigidGrip.MainPositionError,d.RigidGrip.SupportPositionError);
                    rotationError=Mathf.Max(rotationError,d.RigidGrip.MainRotationError,d.RigidGrip.SupportRotationError);
                    for(int i=0;i<2;i++)
                    {
                        float length=Vector3.Distance(d.animator.GetBoneTransform(upper[i]).position,d.animator.GetBoneTransform(lower[i]).position)+Vector3.Distance(d.animator.GetBoneTransform(lower[i]).position,d.animator.GetBoneTransform(hands[i]).position);
                        boneLengthError=Mathf.Max(boneLengthError,Mathf.Abs(length-lengths[i]));
                    }
                }
            }
            Set(d, "shotKick", 1f); d.FinalizeWeaponGrip();
            positionError = Mathf.Max(positionError, d.RigidGrip.MainPositionError, d.RigidGrip.SupportPositionError);
            Need(positionError < .001f, "Wrist moved off its grip by " + positionError + " m.");
            Need(rotationError < .2f, "Wrist changed relative rotation by " + rotationError + " degrees.");
            Need(boneLengthError < .0001f, "Arm bones stretched by " + boneLengthError + " m.");
            report.Add("PASS: 12 aiming directions, including straight down / level / straight up and facing left; 540 evaluated frames.");
            report.Add("PASS: 120 additional running and flight frames keep both palm frames fixed on the gun"+(downwardMovement?" while aiming down.":" while aiming forward."));
            report.Add("PASS: maximum hand position error " + positionError.ToString("F7") + " m; rotation error " + rotationError.ToString("F4") + " degrees.");
            report.Add("PASS: maximum arm-length change " + boneLengthError.ToString("F7") + " m; no arm stretch.");
            report.Add("PASS: recoil moves the rigid weapon and both attached hands together.");
            Vector3 origin;
            Need(d.TryGetWaterMuzzle(out origin) && Vector3.Distance(origin,a.muzzle.position)<.00001f,"Firing origin detached from the nozzle.");
            report.Add("PASS: firing origin follows the posed water nozzle.");
            var ladderObject = new GameObject("Test ladder"); ladderObject.transform.SetParent(root.transform);
            Set(p, "climbingLadder", ladderObject.AddComponent<ClimbableLadder>());
            p.SyncVisuals(1f / 60); d.AdvancePresentation(1f / 60); d.animator.Update(1f / 60); d.FinalizeWeaponGrip();
            Need(!gun.gameObject.activeSelf, "Weapon not released during climbing.");
            report.Add("PASS: climbing still hides the weapon and releases the gun grip constraints.");
            texture.Apply();
            File.WriteAllBytes("Captures/WeaponGrip/Reference-Poses.png", texture.EncodeToPNG());
            File.WriteAllLines("Captures/WeaponGrip/Verification.txt", report);
            return string.Join("\n", report);
        }
        finally
        {
            world.startWithWaterGun = previousGun; RenderTexture.active = oldTarget;
            if(camera)camera.targetTexture=null;
            if (rt) UnityEngine.Object.DestroyImmediate(rt); if (texture) UnityEngine.Object.DestroyImmediate(texture);
            UnityEngine.Object.DestroyImmediate(root);
            if (scene.IsValid() && scene.isLoaded) SceneManager.UnloadSceneAsync(scene);
        }
    }

    static int TorsoCrossings(Animator animator,SkinnedMeshRenderer skin,Mesh baked,Transform root)
    {
        var torso=new HashSet<Transform>(new[]{HumanBodyBones.Hips,HumanBodyBones.Spine,HumanBodyBones.Chest,HumanBodyBones.UpperChest}.Select(animator.GetBoneTransform).Where(t=>t));
        var weights=skin.sharedMesh.boneWeights;var bones=skin.bones;
        var bodyVertex=new bool[weights.Length];
        for(int i=0;i<weights.Length;i++)
        {
            var w=weights[i];int bone=w.boneIndex0;float maximum=w.weight0;
            if(w.weight1>maximum){bone=w.boneIndex1;maximum=w.weight1;}
            if(w.weight2>maximum){bone=w.boneIndex2;maximum=w.weight2;}
            if(w.weight3>maximum)bone=w.boneIndex3;
            bodyVertex[i]=bone<bones.Length&&torso.Contains(bones[bone]);
        }
        var source=baked.triangles;var triangles=new List<int>();
        for(int i=0;i<source.Length;i+=3)
            if(bodyVertex[source[i]]&&bodyVertex[source[i+1]]&&bodyVertex[source[i+2]])
            {triangles.Add(source[i]);triangles.Add(source[i+1]);triangles.Add(source[i+2]);}
        var mesh=new Mesh{vertices=baked.vertices,triangles=triangles.ToArray()};
        var go=new GameObject("Torso surface clearance probe");go.transform.SetParent(root,false);
        go.transform.SetPositionAndRotation(skin.transform.position,skin.transform.rotation);go.transform.localScale=Vector3.one;
        var collider=go.AddComponent<MeshCollider>();collider.sharedMesh=mesh;
        int hits=0;
        var details=new List<string>();
        try
        {
            Physics.SyncTransforms();
            var shoulderSide=(animator.GetBoneTransform(HumanBodyBones.RightUpperArm).position-animator.GetBoneTransform(HumanBodyBones.LeftUpperArm).position).normalized;
            foreach(bool right in new[]{true,false})
            {
                var s=animator.GetBoneTransform(right?HumanBodyBones.RightUpperArm:HumanBodyBones.LeftUpperArm).position;
                var e=animator.GetBoneTransform(right?HumanBodyBones.RightLowerArm:HumanBodyBones.LeftLowerArm).position;
                var h=animator.GetBoneTransform(right?HumanBodyBones.RightHand:HumanBodyBones.LeftHand).position;
                Vector3 inward=shoulderSide*(right?-1:1)*.025f;
                foreach(var offset in new[]{Vector3.zero,inward})
                foreach(var segment in new[]{new[]{Vector3.Lerp(s,e,.4f)+offset,e+offset},new[]{e+offset,Vector3.Lerp(e,h,.9f)+offset}})
                {
                    var delta=segment[1]-segment[0];RaycastHit hit;
                    if(collider.Raycast(new Ray(segment[0],delta.normalized),out hit,delta.magnitude)||collider.Raycast(new Ray(segment[1],-delta.normalized),out hit,delta.magnitude))
                    {hits++;details.Add((right?"rear":"front")+" at "+hit.point.ToString("F3"));}
                }
            }
            clearanceDetails=hits==0?"":" ("+string.Join("; ",details)+")";return hits;
        }
        finally{UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(mesh);}
    }
}
