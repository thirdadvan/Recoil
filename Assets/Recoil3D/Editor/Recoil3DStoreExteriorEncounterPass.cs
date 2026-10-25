using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;

public static class Recoil3DStoreExteriorEncounterPass
{
    static Transform Group(string name,Transform parent,Vector3 worldPosition)
    {var t=new GameObject(name).transform;t.SetParent(parent,false);t.position=worldPosition;var s=parent.lossyScale;t.localScale=new Vector3(1/s.x,1/s.y,1/s.z);return t;}
    static GameObject Box(string name,Transform parent,Vector3 pos,Vector3 size,Material mat,PrimitiveType type=PrimitiveType.Cube)
    {var g=GameObject.CreatePrimitive(type);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=pos;g.transform.localScale=size;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());g.GetComponent<Renderer>().sharedMaterial=mat;return g;}
    static Material Mat(string name,Color colour)
    {var path="Assets/Recoil3D/Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Recoil/3D/Weathered Surface"));AssetDatabase.CreateAsset(m,path);}m.color=colour;m.SetColor("_EmissionColor",Color.black);m.SetFloat("_Metallic",.15f);EditorUtility.SetDirty(m);return m;}
    static Transform[] All(Scene scene){return scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();}
    static void Guard(string path){if(EditorApplication.isPlayingOrWillChangePlaymode||SceneManager.GetActiveScene().path!=path)throw new Exception("Open expected 3D scene outside Play.");}
    static void RefreshEnemies(RecoilPrototype world)
    {var so=new SerializedObject(world);var list=so.FindProperty("enemies");var enemies=world.GetComponentsInChildren<RampagerController>(true);list.arraySize=enemies.Length;for(int i=0;i<enemies.Length;i++)list.GetArrayElementAtIndex(i).objectReferenceValue=enemies[i];so.ApplyModifiedPropertiesWithoutUndo();}
    public static string Outdoor()
    {
        Guard("Assets/Scenes/3D/RecoilPrototype.unity");var scene=SceneManager.GetActiveScene();var all=All(scene);var world=all.Select(t=>t.GetComponent<RecoilPrototype>()).First(w=>w);
        if(world.GetComponentInChildren<ServiceStoreOutdoorEncounter>(true))return "Already placed";
        var template=all.Select(t=>t.GetComponent<RampagerController>()).First(m=>m&&m.name=="Rampager (3)");
        var root=Group("Service store - three roaming sentries and warning",world.transform,Vector3.zero);
        RampagerController first=null;
        for(int i=0;i<3;i++)
        {
            var enemy=i==0?template:UnityEngine.Object.Instantiate(template,root,false);enemy.transform.SetParent(root,true);enemy.name="Store exterior Rampager "+(i+1);
            enemy.transform.position=new Vector3(new[]{-29.2f,-24.7f,-20.5f}[i],.8f,0);enemy.gameObject.SetActive(true);enemy.initialState=RampagerController.Behaviour.Roaming;enemy.transform.rotation=Quaternion.Euler(0,90,0);enemy.Configure(world,-1,1);enemy.returnToSleepAfterSound=false;
            var points=enemy.GetComponentsInChildren<RampagerPatrolPoint>(true).OrderBy(p=>p.order).ToArray();
            for(int j=0;j<points.Length;j++){points[j].transform.position=enemy.transform.position+Vector3.right*new[]{-1f,0,1f,0}[j%4];points[j].duration=j==1?2:1.5f;}
            if(i==0)first=enemy;EditorUtility.SetDirty(enemy);
        }
        var sequence=ScriptableObject.CreateInstance<NpcDialogueSequence>();sequence.lines=new[]{new NpcDialogueSequence.Line{speaker="VOICE FROM THE STORE",text=ServiceStoreOutdoorEncounter.OpeningLineOne},new NpcDialogueSequence.Line{speaker="VOICE FROM THE STORE",text=ServiceStoreOutdoorEncounter.OpeningLineTwo}};
        AssetDatabase.CreateAsset(sequence,"Assets/RecoilPrototype/Dialogue/StoreClerk_OutdoorWarning.asset");
        var story=root.gameObject.AddComponent<ServiceStoreOutdoorEncounter>();story.world=world;story.leftMonster=first;story.warning=sequence;
        var entrance=all.Select(t=>t.GetComponent<RecoilDoor>()).First(d=>d&&d.name=="Service store entrance");
        foreach(var v in entrance.GetComponentsInChildren<Recoil3DSpriteVisual>(true)){v.suppressed=true;v.Sync();EditorUtility.SetDirty(v);}
        foreach(var r in entrance.GetComponentsInChildren<Renderer>(true))r.enabled=false;
        var facade=all.First(t=>t.name=="Store facade").GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&r.bounds.size.x>8).OrderBy(r=>r.bounds.min.z).First().bounds;
        var pos=entrance.transform.position;pos.z=facade.min.z-.13f;entrance.transform.position=pos;entrance.rememberOnTravel="";
        var door=Group("Transparent entrance - dark interior beyond glass",entrance.transform,new Vector3(pos.x,0,pos.z));
        var metal=Mat("Store door aluminium",new Color(.18f,.22f,.22f));var black=Mat("Store doorway unlit interior",new Color(.008f,.012f,.013f));
        Box("Interior darkness behind glass",door,new Vector3(0,1.3f,.08f),new Vector3(1.2f,2.6f,.025f),black);
        var glass=new Material(Shader.Find("Standard"));glass.name="Store entrance smoked clear glass";glass.SetFloat("_Mode",3);glass.SetInt("_SrcBlend",(int)BlendMode.SrcAlpha);glass.SetInt("_DstBlend",(int)BlendMode.OneMinusSrcAlpha);glass.SetInt("_ZWrite",0);glass.EnableKeyword("_ALPHAPREMULTIPLY_ON");glass.renderQueue=3000;glass.color=new Color(.10f,.17f,.18f,.24f);glass.SetFloat("_Glossiness",.8f);AssetDatabase.CreateAsset(glass,"Assets/Recoil3D/Materials/Store entrance smoked clear glass.mat");
        Box("Transparent full-height glazing",door,new Vector3(0,1.3f,-.025f),new Vector3(1.12f,2.48f,.035f),glass);
        foreach(float x in new[]{-.65f,.65f})Box("Recessed aluminium jamb",door,new Vector3(x,1.34f,0),new Vector3(.075f,2.76f,.19f),metal);
        foreach(float y in new[]{.035f,2.68f})Box("Door perimeter rail",door,new Vector3(0,y,0),new Vector3(1.38f,.075f,.19f),metal);
        Box("Glass door push rail",door,new Vector3(0,1.11f,-.08f),new Vector3(1.18f,.045f,.055f),metal);Box("Door handle",door,new Vector3(.43f,1.3f,-.15f),new Vector3(.035f,.48f,.06f),metal);
        var glint=Mat("Store glass faint reflection",new Color(.10f,.17f,.18f));Box("Subtle glass edge reflection",door,new Vector3(-.48f,1.39f,-.049f),new Vector3(.009f,2.1f,.005f),glint);
        var spawn=Group("Store Outside Door Spawn",world.transform,new Vector3(pos.x,.85f,0));
        RefreshEnemies(world);EditorUtility.SetDirty(entrance);EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);return "Three standard roaming monsters and footstep-watch warning; dark glass door aligned to facade.";
    }
    public static string Interior()
    {
        Guard("Assets/Scenes/3D/RecoilServiceStore.unity");var scene=SceneManager.GetActiveScene();var all=All(scene);var world=all.Select(t=>t.GetComponent<RecoilPrototype>()).First(w=>w);
        if(world.transform.Find("Foreground entrance - light and two restless shadows"))return "Already placed";
        foreach(var encounter in world.GetComponentsInChildren<ServiceStoreEncounter>(true)){if(encounter.monster)UnityEngine.Object.DestroyImmediate(encounter.monster.gameObject);UnityEngine.Object.DestroyImmediate(encounter);}
        foreach(var label in all.Where(t=>t&&t.name=="Broken entrance sign"))label.gameObject.SetActive(false);
        var old=all.Select(t=>t?t.GetComponent<RecoilDoor>():null).First(d=>d&&d.name=="Damaged store entrance");
        foreach(var v in old.GetComponentsInChildren<Recoil3DSpriteVisual>(true)){v.suppressed=true;v.Sync();EditorUtility.SetDirty(v);}
        foreach(var r in old.GetComponentsInChildren<Renderer>(true))r.enabled=false;
        old.name="Store foreground entrance";old.blockedByThreat=true;old.blockedPrompt="E / DOOR BLOCKED";old.permanentlyBlocked=false;old.irreparableDamage=false;old.blockedByItemId="";old.blockedMessage="The Coalers are blocking the door.";old.damagedVisual=null;old.destinationSpawn="Store Outside Door Spawn";old.interactionPrompt="E / RETURN OUTSIDE";old.transform.position=new Vector3(2.1f,1.3f,-2.2f);
        var root=Group("Foreground entrance - light and two restless shadows",old.transform,new Vector3(2.1f,0,-2.2f));
        var steel=Mat("Store front entrance steel",new Color(.11f,.14f,.14f));
        // The camera-facing door leaf is cut away; only the frame defines the foreground entrance.
        foreach(float x in new[]{-1.05f,1.05f})Box("Foreground door jamb",root,new Vector3(x,1.4f,0),new Vector3(.09f,2.8f,.15f),steel);
        Box("Foreground door header",root,new Vector3(0,2.8f,0),new Vector3(2.19f,.10f,.15f),steel);
        Box("Worn entrance threshold",root,new Vector3(0,.025f,0),new Vector3(2.1f,.05f,.3f),steel);
        foreach(var frame in root.GetComponentsInChildren<Renderer>())frame.shadowCastingMode=ShadowCastingMode.ShadowsOnly;
        var fixture=Group("Outside entrance light - projects into room",root,new Vector3(2.1f,3.8f,-6.5f));
        var lamp=fixture.gameObject.AddComponent<Light>();lamp.type=LightType.Spot;lamp.transform.rotation=Quaternion.LookRotation(new Vector3(0,-.40f,1));lamp.spotAngle=68;lamp.range=16;lamp.intensity=1.2f;lamp.color=new Color(.62f,.77f,.84f);lamp.shadows=LightShadows.Soft;lamp.shadowStrength=.95f;lamp.shadowResolution=LightShadowResolution.High;lamp.shadowBias=.015f;lamp.shadowNormalBias=.03f;lamp.renderMode=LightRenderMode.ForcePixel;
        var mask=new Texture2D(128,128,TextureFormat.Alpha8,false,true);mask.name="Entrance rectangular light aperture";mask.wrapMode=TextureWrapMode.Clamp;var pixels=new Color[128*128];for(int y=0;y<128;y++)for(int x=0;x<128;x++){float a=(x>29&&x<98&&y>10&&y<117)?1:0;pixels[y*128+x]=new Color(1,1,1,a);}mask.SetPixels(pixels);mask.Apply();AssetDatabase.CreateAsset(mask,"Assets/Recoil3D/Materials/Store entrance aperture.asset");lamp.cookie=mask;
        var proxies=Group("Two pounding exterior silhouettes - shadows only",root,new Vector3(2.1f,0,-3.6f));
        for(int i=0;i<2;i++)
        {
            var person=Group("Unseen monster shadow "+(i+1),proxies,new Vector3(2.1f+(i-.5f)*.62f,0,-3.6f+(i==1?-.2f:.1f)));
            float h=i==1?2.15f:1.95f;
            Box("Torso shadow",person,new Vector3(0,h*.58f,0),new Vector3(.42f,.9f,.3f),steel,PrimitiveType.Capsule);
            Box("Head shadow",person,new Vector3(.05f,h-.15f,-.03f),new Vector3(.31f,.39f,.31f),steel,PrimitiveType.Sphere);
            foreach(float sign in new[]{-1f,1f}){Box("Leg shadow",person,new Vector3(sign*.13f,.43f,0),new Vector3(.14f,.86f,.17f),steel,PrimitiveType.Capsule);var arm=Box("Limp arm shadow",person,new Vector3(sign*.29f,h*.54f,0),new Vector3(.12f,.8f,.12f),steel,PrimitiveType.Capsule);arm.transform.localRotation=Quaternion.Euler(0,0,sign*12);}
            person.localScale=new Vector3(1,1.15f,1);
            foreach(var renderer in person.GetComponentsInChildren<Renderer>()){renderer.shadowCastingMode=ShadowCastingMode.ShadowsOnly;renderer.receiveShadows=false;}
        }
        foreach(var drum in all.Where(t=>t&&t.name=="Foreground abandoned drum"&&t.position.x<4))drum.gameObject.SetActive(false);
        world.objectiveWithoutCard="Find the water gun at the counter. Fire, then use the direction keys to fly.";world.objectiveWithCard=world.objectiveWithoutCard;
        var departure=proxies.gameObject.AddComponent<Recoil3DDoorwayShadows>();departure.silhouettes=proxies.Cast<Transform>().ToArray();
        Recoil3DDoorAperturePass.Apply();RefreshEnemies(world);EditorUtility.SetDirty(old);EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);return "Removed indoor story and monster; cutaway foreground entrance with three shadow-only silhouettes and projected light.";
    }
}
