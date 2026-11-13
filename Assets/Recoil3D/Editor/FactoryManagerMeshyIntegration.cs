using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class FactoryManagerMeshyIntegration
{
    public const string Folder="Assets/Art/Characters/FactoryManagerMeshy";
    public const string ScenePath="Assets/Scenes/3D/FactoryRooms/FactoryManagerOfficeF4.unity";
    static void Need(bool value,string message){if(!value)throw new Exception(message);}
    [MenuItem("Recoil/Factory Manager/Install Meshy manager")]
    public static void Apply()
    {
        Need(!EditorApplication.isPlayingOrWillChangePlaymode,"Stop Play before replacing manager");
        Directory.CreateDirectory("Captures/FactoryManager");
        Directory.CreateDirectory("Backups/FactoryManagerMeshy/2026-11-05");
        for(int i=0;i<SceneManager.sceneCount;i++)
        {
            var loaded=SceneManager.GetSceneAt(i);
            if(loaded.isDirty && !string.IsNullOrEmpty(loaded.path))
            {
                EditorSceneManager.SaveScene(loaded,"Backups/FactoryManagerMeshy/2026-11-05/Unsaved-"+loaded.name+".unity",true);
                EditorSceneManager.SaveScene(loaded);
            }
        }
        var importer=(ModelImporter)AssetImporter.GetAtPath(Folder+"/Manager.fbx");
        Need(importer!=null,"Manager FBX missing");
        importer.animationType=ModelImporterAnimationType.Human;importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
        importer.isReadable=true;importer.importAnimation=false;importer.SaveAndReimport();
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/Manager.fbx");
        var temp=UnityEngine.Object.Instantiate(source);var animator=temp.GetComponent<Animator>();
        Need(animator && animator.avatar && animator.avatar.isHuman && animator.avatar.isValid,"Invalid manager humanoid avatar");
        var handler=new HumanPoseHandler(animator.avatar,temp.transform);var pose=new HumanPose();handler.GetHumanPose(ref pose);handler.Dispose();
        var neutral=(float[])pose.muscles.Clone();var names=HumanTrait.MuscleName;
        Action<string,float> set=(n,v)=>{int i=Array.IndexOf(names,n);if(i>=0)neutral[i]=v;};
        set("Spine Front-Back",.18f);set("Chest Front-Back",.18f);set("Neck Nod Down-Up",-.17f);set("Head Nod Down-Up",-.23f);set("Head Turn Left-Right",-.18f);
        foreach(string side in new[]{"Left","Right"})
        {
            set(side+" Shoulder Down-Up",.13f);set(side+" Shoulder Front-Back",-.10f);
            set(side+" Arm Down-Up",-.78f);set(side+" Arm Front-Back",.14f);set(side+" Forearm Stretch",-.25f);
            foreach(var finger in new[]{"Index","Middle","Ring","Little"}){set(side+" "+finger+" 1 Stretched",.4f);set(side+" "+finger+" 2 Stretched",.2f);set(side+" "+finger+" 3 Stretched",.1f);set(side+" "+finger+" Spread",0);}
        }
        var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(Folder+"/ManagerTimidIdle.anim");
        if(!clip){clip=new AnimationClip();AssetDatabase.CreateAsset(clip,Folder+"/ManagerTimidIdle.anim");}
        clip.ClearCurves();clip.frameRate=30;clip.name="ManagerTimidIdle";
        const float duration=4.8f;
        for(int i=0;i<names.Length;i++)
        {
            var curve=new AnimationCurve();for(int k=0;k<=144;k++)
            {
                float t=k/30f,v=neutral[i],phase=t*Mathf.PI*2/duration;
                if(names[i]=="Chest Front-Back")v+=Mathf.Sin(phase)*.012f;
                if(names[i]=="Head Turn Left-Right")v+=Mathf.Sin(phase)*.025f;
                curve.AddKey(t,v);
            }
            clip.SetCurve("",typeof(Animator),names[i],curve);
        }
        foreach(var p in new[]{("RootT.x",0f),("RootT.y",pose.bodyPosition.y),("RootT.z",0f),("RootQ.x",pose.bodyRotation.x),("RootQ.y",pose.bodyRotation.y),("RootQ.z",pose.bodyRotation.z),("RootQ.w",pose.bodyRotation.w)})clip.SetCurve("",typeof(Animator),p.Item1,AnimationCurve.Constant(0,duration,p.Item2));
        var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=true;settings.loopBlend=true;settings.keepOriginalOrientation=true;settings.keepOriginalPositionXZ=true;settings.keepOriginalPositionY=true;AnimationUtility.SetAnimationClipSettings(clip,settings);
        UnityEngine.Object.DestroyImmediate(temp);
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(Folder+"/Manager.controller");
        if(!controller)controller=AnimatorController.CreateAnimatorControllerAtPath(Folder+"/Manager.controller");
        var machine=controller.layers[0].stateMachine;var state=machine.states.FirstOrDefault().state;if(!state)state=machine.AddState("Timid folder idle");state.motion=clip;machine.defaultState=state;
        var layers=controller.layers;layers[0].iKPass=true;controller.layers=layers;
        var material=Material("Manager",Color.white);material.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/Manager_BaseColor.png");Need(material.mainTexture,"Missing manager texture");EditorUtility.SetDirty(material);EditorUtility.SetDirty(clip);AssetDatabase.SaveAssets();
        var previous=SceneManager.GetActiveScene();var scene=SceneManager.GetSceneByPath(ScenePath);bool opened=!scene.isLoaded;
        if(opened)scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
        try
        {
            string backup="Backups/FactoryManagerMeshy/2026-11-05/FactoryManagerOfficeF4.unity";if(!File.Exists(backup))EditorSceneManager.SaveScene(scene,backup,true);
            var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
            var manager=all.Single(t=>t.name=="FactoryManager");var item=manager.GetComponent<ContinuousInteractionItem>();Need(item,"Manager key interaction missing");
            string story=EditorJsonUtility.ToJson(item);Vector3 managerPosition=manager.position;
            string collisions=Collisions(scene);
            var appearance=manager.Find("Appearance - grouped");Need(appearance,"Old manager appearance not found");appearance.gameObject.SetActive(false);
            var old=manager.Find("Meshy manager - timid lowpoly");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
            var go=(GameObject)PrefabUtility.InstantiatePrefab(source,scene);go.name="Meshy manager - timid lowpoly";go.transform.SetParent(manager,false);go.transform.localRotation=Quaternion.Euler(0,195,0);go.transform.position+=new Vector3(0,0,-.3f);
            animator=go.GetComponent<Animator>();animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            foreach(var r in go.GetComponentsInChildren<SkinnedMeshRenderer>()){r.sharedMaterial=material;r.updateWhenOffscreen=true;r.localBounds=new Bounds(Vector3.up,new Vector3(4,4,4));}
            animator.Rebind();animator.Update(0);var bounds=SkinBounds(go);go.transform.localScale*=1.78f/bounds.size.y;bounds=SkinBounds(go);
            float floor=manager.position.y-.825f;go.transform.position+=Vector3.up*(floor+.025f-bounds.min.y);bounds=SkinBounds(go);
            var chest=animator.GetBoneTransform(HumanBodyBones.Chest);if(!chest)chest=animator.GetBoneTransform(HumanBodyBones.Spine);
            var anchor=new GameObject("Document folder anchor").transform;anchor.position=chest.position+go.transform.forward*.16f-Vector3.up*.13f;anchor.rotation=go.transform.rotation*Quaternion.Euler(-8,0,0);anchor.SetParent(chest,true);
            var cover=Material("Folder cover",new Color(.29f,.24f,.16f));var paper=Material("Folder papers",new Color(.56f,.52f,.42f));
            PropBox("Worn document folder",anchor,new Vector3(0,0,0),new Vector3(.26f,.34f,.018f),cover);
            PropBox("Paper edge",anchor,new Vector3(0,.005f,-.008f),new Vector3(.247f,.335f,.009f),paper);
            var manners=go.AddComponent<FactoryManagerMannerisms>();manners.folderAnchor=anchor;
            animator.Update(.01f);bounds=SkinBounds(go);
            Need(EditorJsonUtility.ToJson(item)==story && manager.position==managerPosition,"Manager interaction changed");Need(Collisions(scene)==collisions,"Scene colliders changed");
            int triangles=go.GetComponentsInChildren<SkinnedMeshRenderer>().Sum(r=>r.sharedMesh.triangles.Length/3);Need(triangles<4000,"Manager exceeds lowpoly budget");Need(Mathf.Abs(bounds.min.y-floor-.025f)<.025f,"Feet alignment changed");
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Capture(scene,go,bounds,"ManagerInOffice",false);Capture(scene,go,bounds,"ManagerClose",true);
            File.WriteAllText("Captures/FactoryManager/Verification.txt","PASS edit mode\nTriangles (character): "+triangles+"\nFolder: 24 triangles\nBones: "+go.GetComponentsInChildren<SkinnedMeshRenderer>().SelectMany(r=>r.bones).Distinct().Count()+"\nSole: "+bounds.min.y+"\nHeight: "+bounds.size.y+"\nHumanoid avatar valid, 4.8s idle, folder hand IK. Original key interaction, position and colliders unchanged.\n");
            Debug.Log("FACTORY_MANAGER_MESHY_PASS "+triangles+" triangles");
        }
        finally{if(opened)EditorSceneManager.CloseScene(scene,true);if(previous.isValidForScene())SceneManager.SetActiveScene(previous);}
    }
    static bool isValidForScene(this Scene s){return s.IsValid()&&s.isLoaded;}
    static Material Material(string name,Color colour)
    {
        string path=Folder+"/"+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!material){material=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(material,path);}
        material.color=colour;material.SetFloat("_Glossiness",0);material.SetFloat("_Metallic",0);EditorUtility.SetDirty(material);return material;
    }
    static void PropBox(string name,Transform parent,Vector3 offset,Vector3 size,Material material)
    {
        var obj=new GameObject(name);obj.AddComponent<MeshFilter>().sharedMesh=Resources.GetBuiltinResource<Mesh>("Cube.fbx");obj.AddComponent<MeshRenderer>().sharedMaterial=material;
        obj.transform.position=parent.position+parent.rotation*offset;obj.transform.rotation=parent.rotation;obj.transform.localScale=size;obj.transform.SetParent(parent,true);
    }
    static string Collisions(Scene scene){return string.Join("\n",scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Collider2D>(true)).Select(c=>c.GetInstanceID()+":"+EditorJsonUtility.ToJson(c)).OrderBy(s=>s));}
    public static Bounds SkinBounds(GameObject root)
    {
        bool initialized=false;Bounds bounds=new Bounds();
        foreach(var renderer in root.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            var mesh=renderer.sharedMesh;var matrices=renderer.bones.Select((b,i)=>b.localToWorldMatrix*mesh.bindposes[i]).ToArray();var weights=mesh.boneWeights;var vertices=mesh.vertices;
            for(int i=0;i<vertices.Length;i++){var w=weights[i];var v=vertices[i];var p=matrices[w.boneIndex0].MultiplyPoint3x4(v)*w.weight0+matrices[w.boneIndex1].MultiplyPoint3x4(v)*w.weight1+matrices[w.boneIndex2].MultiplyPoint3x4(v)*w.weight2+matrices[w.boneIndex3].MultiplyPoint3x4(v)*w.weight3;if(!initialized){bounds=new Bounds(p,Vector3.zero);initialized=true;}else bounds.Encapsulate(p);}
        }
        return bounds;
    }
    public static void Capture(Scene scene,GameObject model,Bounds bounds,string name,bool close)
    {
        var obj=new GameObject("Manager verification camera");SceneManager.MoveGameObjectToScene(obj,scene);var camera=obj.AddComponent<Camera>();camera.scene=scene;camera.overrideSceneCullingMask=EditorSceneManager.GetSceneCullingMask(scene);
        camera.orthographic=true;camera.orthographicSize=close?.65f:1.7f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.12f,.13f,.14f);camera.nearClipPlane=.01f;camera.farClipPlane=100;
        Vector3 target=close?bounds.center+Vector3.up*.42f:bounds.center;camera.transform.position=target+new Vector3(-1,.35f,-4);camera.transform.LookAt(target);
        var rt=new RenderTexture(1200,1100,24);camera.targetTexture=rt;camera.Render();var previous=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(1200,1100,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1200,1100),0,0);image.Apply();File.WriteAllBytes("Captures/FactoryManager/"+name+".png",image.EncodeToPNG());RenderTexture.active=previous;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(obj);
    }
}
