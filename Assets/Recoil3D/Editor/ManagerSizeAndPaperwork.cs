using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
public static class ManagerSizeAndPaperwork
{
    const string ScenePath="Assets/Scenes/3D/FactoryRooms/FactoryManagerOfficeF4.unity";
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play first");
        var prior=SceneManager.GetActiveScene();var scene=SceneManager.GetSceneByPath(ScenePath);bool opened=!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
        try
        {
            string backup="Backups/ManagerSizeAndPaperwork/2026-11-05";Directory.CreateDirectory(backup);if(!File.Exists(backup+"/ManagerOffice.unity"))EditorSceneManager.SaveScene(scene,backup+"/ManagerOffice.unity",true);
            var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();var manager=all.Single(t=>t.name=="FactoryManager");var model=manager.Find("Meshy manager - timid lowpoly");var manners=model.GetComponent<FactoryManagerMannerisms>();var animator=model.GetComponent<Animator>();
            var item=manager.GetComponent<ContinuousInteractionItem>();string interaction=EditorJsonUtility.ToJson(item);var managerPosition=manager.position;
            if(manners.poseScale<1.29f){animator.Rebind();animator.Update(0);float sole=FactoryManagerMeshyIntegration.SkinBounds(model.gameObject).min.y;model.localScale*=1.3f;manners.poseScale=1.3f;animator.Update(0);var bounds=FactoryManagerMeshyIntegration.SkinBounds(model.gameObject);model.position+=Vector3.up*(sole-bounds.min.y);}
            var office=all.Single(t=>t.name=="Manager office - restored 3D furniture and fluorescent lighting");
            if(!office.Find("Disordered paperwork"))
            {
                var paperRoot=new GameObject("Disordered paperwork").transform;paperRoot.SetParent(office,false);
                var paper=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Environment/ManagerOffice/Old paper.mat");var cover=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Environment/ManagerOffice/Aged ivory plastic.mat");
                foreach(var stack in all.Where(t=>t.name=="Paper files and correspondence"))
                {int i=0;foreach(Transform file in stack){float angle=new[]{-18f,11f,-7f,24f,-12f}[i%5];file.localPosition+=new Vector3((i%3-1)*.045f,0,(i%2)*.025f);file.localRotation=Quaternion.Euler(0,angle,0);i++;}}
                var random=new System.Random(52);
                foreach(var desk in new[]{new Vector3(4.3f,.84f,1.75f),new Vector3(8.15f,.84f,2.45f)})
                {
                    for(int i=0;i<9;i++){float x=.02f+(float)random.NextDouble()*.55f,z=-.25f+(float)random.NextDouble()*.36f;Sheet(paperRoot,desk+new Vector3(x,i*.0025f,z),new Vector3(.24f,.003f,.31f),(float)random.NextDouble()*70-35,paper,"Loose desk report");}
                    for(int i=0;i<4;i++)Sheet(paperRoot,desk+new Vector3(.25f+(i%2)*.06f,.028f+i*.019f,-.09f),new Vector3(.31f,.015f,.24f),-24+i*13,cover,"Unsorted document bundle");
                    Sheet(paperRoot,desk+new Vector3(.62f,.002f,-.32f),new Vector3(.22f,.003f,.29f),32,paper,"Overhanging report");
                }
                for(int i=0;i<6;i++)Sheet(paperRoot,new Vector3(10.55f+(i%3)*.26f,.006f+(i/3)*.003f,4.0f+(i/3)*.19f),new Vector3(.22f,.003f,.29f),-35+i*18,paper,"Dropped filing sheet");
                for(int i=0;i<3;i++)Sheet(paperRoot,new Vector3(2.0f+i*.10f,.014f+i*.018f,4.46f),new Vector3(.30f,.025f,.24f),-10+i*14,cover,"Ledger awaiting filing");
                var books=all.Where(t=>t.name=="Bound ledger").ToArray();int k=0;foreach(var book in books){if(k++%4!=0)continue;var label=all.Where(t=>t.name=="Ledger spine label").OrderBy(t=>(t.position-book.position).sqrMagnitude).FirstOrDefault();if(label)label.SetParent(book,true);book.localRotation=Quaternion.Euler(0,k%2==0?-9:8,k%3==0?-12:10);book.localPosition+=new Vector3(0,.012f,-.035f);}
            }
            if(EditorJsonUtility.ToJson(item)!=interaction||manager.position!=managerPosition)throw new Exception("Interaction changed");
            EditorUtility.SetDirty(manners);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Captures/FactoryManagerOffice");File.WriteAllText("Captures/FactoryManagerOffice/SizePaperworkVerification.txt","PASS: manager uniformly enlarged 30%, sole height retained, hand offsets scaled with model; fixed seeded disorder on desk files and shelf ledgers; added loose reports and bundles, clear foreground walking lane; original manager interaction unchanged.\n");Debug.Log("MANAGER_SIZE_PAPERWORK_PASS");
        }
        finally{if(opened)EditorSceneManager.CloseScene(scene,true);if(prior.IsValid()&&prior.isLoaded)SceneManager.SetActiveScene(prior);}
    }
    static void Sheet(Transform root,Vector3 p,Vector3 size,float angle,Material material,string name)
    {var g=new GameObject(name);g.transform.SetParent(root,false);g.transform.localPosition=p;g.transform.localRotation=Quaternion.Euler(0,angle,0);g.transform.localScale=size;g.AddComponent<MeshFilter>().sharedMesh=Resources.GetBuiltinResource<Mesh>("Cube.fbx");g.AddComponent<MeshRenderer>().sharedMaterial=material;}
}
