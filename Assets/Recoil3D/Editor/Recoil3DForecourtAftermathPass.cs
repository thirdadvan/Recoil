using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

public static partial class Recoil3DOpeningForecourtPass
{
    static Material oldCloth,oldCanvas,oldShoe,oldSole,oldShield,oldCrack,oldRust,oldSteel,oldGlass;
    static void Face(List<Vector3> v,List<int> t,Vector3 a,Vector3 b,Vector3 c,Vector3 d)
    {int n=v.Count;v.AddRange(new[]{a,b,c,d,a,b,c,d});t.AddRange(new[]{n,n+1,n+2,n,n+2,n+3,n+6,n+5,n+4,n+7,n+6,n+4});}
    static void Wreck(Transform parent,Transform source,string name,Vector3 position,float yaw,int variant)
    {
        var root=Group(name,parent,position);root.localRotation=Quaternion.Euler(variant==0?2:-3,yaw,variant==0?3:-5);
        var clone=UnityEngine.Object.Instantiate(source.gameObject,root,false).transform;clone.name="Crumpled shell and remaining wheels";
        foreach(var filter in clone.GetComponentsInChildren<MeshFilter>())
        {
            var mesh=UnityEngine.Object.Instantiate(filter.sharedMesh);mesh.name="Wrecked sedan mesh";
            var vertices=mesh.vertices;
            for(int i=0;i<vertices.Length;i++)
            {
                var p=vertices[i];float damage=Mathf.Clamp01((-p.z-.7f)/1.3f);
                p.y-=damage*(.11f+.12f*Mathf.Sin(p.x*5+variant));
                if(p.y>1.06f)p.y=1.06f+(p.y-1.06f)*(variant==0?.65f:.4f);
                p.x+=damage*.055f*Mathf.Sin(p.y*17+p.z*5);p.z+=damage*.18f;vertices[i]=p;
            }
            mesh.vertices=vertices;mesh.RecalculateBounds();mesh.RecalculateNormals();
            AssetDatabase.CreateAsset(mesh,Folder+"/Meshes/"+(meshSerial++).ToString("0000")+"-wreck.asset");filter.sharedMesh=mesh;
            var renderer=filter.GetComponent<MeshRenderer>();string mat=renderer.sharedMaterial.name;
            renderer.sharedMaterial=mat.Contains("glass")?oldGlass:mat.Contains("Rubber")?oldSole:mat.Contains("steel")?oldSteel:mat.Contains("ivory")?oldCrack:oldRust;
        }
        var damageArt=Group("Bent loose panels and flat tyre",root,Vector3.zero);
        var door=Box("Torn open door",damageArt,new Vector3(variant==0?-1.15f:1.15f,.85f,.35f),new Vector3(.07f,.68f,.85f),oldRust);door.transform.localRotation=Quaternion.Euler(8,variant==0?-55:65,8);
        var hood=Box("Buckled bonnet panel",damageArt,new Vector3(0,1.02f,-1.54f),new Vector3(1.72f,.045f,.65f),oldRust);hood.transform.localRotation=Quaternion.Euler(-22,6,4);
        Box("Detached tyre in dirt",damageArt,new Vector3(1.65f,.08f,-.6f),new Vector3(.66f,.08f,.66f),oldSole,PrimitiveType.Cylinder);
        foreach(float x in new[]{-.5f,.1f,.55f})Rod("Shattered windscreen scar",damageArt,new Vector3(x,1.13f,-.83f),new Vector3(x+.16f,1.37f,-.48f),.008f,oldCrack);
        Combine(damageArt);
    }
    static void Shirt(Transform parent,Vector3 position,float yaw,int variant)
    {
        var root=Group("Torn faded garment",parent,position);root.localRotation=Quaternion.Euler(0,yaw,0);
        var v=new List<Vector3>();var t=new List<int>();
        for(int z=0;z<6;z++)for(int x=0;x<4;x++)
        {
            if((x==1&&z==3)||(x==2&&z==5)||(variant%2==0&&x==0&&z==4))continue;
            float xa=-.26f+x*.13f,xb=xa+.13f,za=-.25f+z*.115f,zb=za+.115f;
            Face(v,t,new Vector3(xa,.015f+.012f*Mathf.Sin(x+z),za),new Vector3(xb,.015f,za),new Vector3(xb,.02f,zb),new Vector3(xa,.035f,zb));
        }
        Face(v,t,new Vector3(-.26f,.015f,-.25f),new Vector3(-.08f,.025f,-.46f),new Vector3(-.3f,.01f,-.48f),new Vector3(-.56f,.015f,-.16f));
        Face(v,t,new Vector3(-.26f,.015f,-.25f),new Vector3(-.56f,.015f,-.16f),new Vector3(-.45f,.01f,.03f),new Vector3(-.22f,.02f,-.06f));
        Face(v,t,new Vector3(.26f,.015f,-.25f),new Vector3(.08f,.025f,-.46f),new Vector3(.3f,.01f,-.48f),new Vector3(.56f,.015f,-.16f));
        Face(v,t,new Vector3(.26f,.015f,-.25f),new Vector3(.56f,.015f,-.16f),new Vector3(.45f,.01f,.03f),new Vector3(.22f,.02f,-.06f));
        MeshObject("Holes and frayed hems",root,SaveMesh("Torn garment",v,t),variant%2==0?oldCloth:oldCanvas);
        Rod("Faded fabric seam",root,new Vector3(-.23f,.032f,-.12f),new Vector3(-.19f,.035f,.35f),.005f,oldCanvas);
        Combine(root);
    }
    static void Shoe(Transform parent,Vector3 position,float yaw)
    {
        var root=Group("Discarded weathered shoe",parent,position);root.localRotation=Quaternion.Euler(0,yaw,0);
        Box("Separated worn sole",root,new Vector3(0,.017f,0),new Vector3(.15f,.035f,.32f),oldSole);
        Box("Collapsed leather upper",root,new Vector3(0,.075f,.035f),new Vector3(.14f,.11f,.21f),oldShoe);
        Box("Scuffed toe",root,new Vector3(0,.057f,-.11f),new Vector3(.145f,.085f,.14f),oldShoe,PrimitiveType.Sphere);
        for(int i=0;i<3;i++)Rod("Old laces",root,new Vector3(-.04f,.13f,-.01f+i*.035f),new Vector3(.04f,.13f,.01f+i*.035f),.004f,oldCanvas);
        Combine(root);
    }
    static void Shield(Transform parent,Vector3 position,Vector3 rotation,int variant)
    {
        var root=Group("Cracked abandoned riot shield",parent,position);root.localRotation=Quaternion.Euler(rotation);
        var v=new List<Vector3>();var t=new List<int>();
        for(int y=0;y<6;y++)for(int x=0;x<4;x++)
        {
            if((variant==1&&x==3&&y<2)||(variant==2&&x==0&&y==5))continue;
            float xa=-.34f+x*.17f,xb=xa+.17f,ya=y*.18f,yb=ya+.18f;
            Face(v,t,new Vector3(xa,ya,xa*xa*.65f),new Vector3(xb,ya,xb*xb*.65f),new Vector3(xb,yb,xb*xb*.65f),new Vector3(xa,yb,xa*xa*.65f));
        }
        MeshObject("Yellowed scarred shield panel",root,SaveMesh("Broken riot shield",v,t),oldShield);
        Box("Faded identification band",root,new Vector3(0,.74f,-.018f),new Vector3(.64f,.17f,.012f),oldCrack);
        Box("Broken handle strap",root,new Vector3(-.11f,.5f,.09f),new Vector3(.035f,.37f,.035f),oldSole);
        Box("Handle fixing",root,new Vector3(.11f,.5f,.09f),new Vector3(.035f,.37f,.035f),oldSole);
        var crackOrigin=new Vector3(.07f,.46f,-.025f);
        foreach(var end in new[]{new Vector3(-.29f,.88f,-.02f),new Vector3(.28f,.65f,-.02f),new Vector3(-.28f,.2f,-.02f),new Vector3(.21f,.04f,-.02f)})Rod("Old fracture",root,crackOrigin,end,.006f,oldCrack);
        Combine(root);
    }
    public static string ApplyAftermath(Scene scene)
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||scene.path!="Assets/Scenes/3D/RecoilPrototype.unity")throw new Exception("Use the outdoor scene outside Play mode.");
        var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
        var forecourt=all.First(t=>t.name=="Forecourt - move all pump islands together");if(forecourt.Find("Old unrest aftermath - aged debris"))return "Already authored.";
        meshSerial=AssetDatabase.FindAssets("t:Mesh",new[]{Folder+"/Meshes"}).Select(AssetDatabase.GUIDToAssetPath).Select(p=>int.Parse(System.IO.Path.GetFileNameWithoutExtension(p).Split('-')[0])).DefaultIfEmpty(0).Max()+1;
        oldCloth=Material("Aftermath faded charcoal fabric",new Color(.18f,.19f,.17f));oldCanvas=Material("Aftermath sun bleached canvas",new Color(.28f,.265f,.21f));oldShoe=Material("Aftermath dried leather",new Color(.16f,.115f,.08f));oldSole=Material("Aftermath perished rubber",new Color(.065f,.07f,.063f));oldShield=Material("Aftermath clouded shield",new Color(.24f,.25f,.215f));oldCrack=Material("Aftermath old fracture and soot",new Color(.08f,.085f,.075f));oldRust=Material("Aftermath oxidised car shell",new Color(.24f,.17f,.11f));oldSteel=Material("Aftermath rusted exposed steel",new Color(.18f,.19f,.17f));oldGlass=Material("Aftermath dusty broken glazing",new Color(.035f,.045f,.044f));
        var rows=forecourt.Cast<Transform>().Where(t=>t.name.Contains("pump row")).ToArray();
        foreach(var row in rows)foreach(Transform pump in row){var p=pump.position;p.z+=.45f;pump.position=p;foreach(var label in pump.GetComponentsInChildren<TextMesh>(true).Where(t=>t.name=="Pump number"))label.gameObject.SetActive(false);}
        foreach(var display in forecourt.GetComponentsInChildren<Recoil3DFuelPumpDisplay>(true))
        {display.startsBroken=display.transform.parent.parent.name.StartsWith("03 ");display.Sync();EditorUtility.SetDirty(display);}
        foreach(var label in all.Select(t=>t.GetComponent<TextMesh>()).Where(t=>t && (t.name=="Service sign text"||t.name=="Roof skylight label"||t.name=="Unbranded service station fascia"||t.name=="FACTORY STORES")))label.gameObject.SetActive(false);
        var root=Group("Old unrest aftermath - aged debris",forecourt,Vector3.zero);
        var source=all.First(t=>t.name=="Dun sedan - front toward camera");
        Wreck(root,source,"Wreck A - roadside shell",new Vector3(-50.5f,.08f,17),-32,0);
        Wreck(root,source,"Wreck B - abandoned sideways",new Vector3(-33,.12f,21),57,1);
        Shirt(root,new Vector3(-52,.035f,-2),34,0);Shirt(root,new Vector3(-46.2f,.035f,1.4f),-72,1);Shirt(root,new Vector3(-38.8f,.035f,-3),115,2);Shirt(root,new Vector3(-32,.035f,3.3f),-25,3);
        foreach(var p in new[]{new Vector3(-52.6f,.035f,-2.4f),new Vector3(-50.9f,.035f,-.2f),new Vector3(-45.7f,.035f,1),new Vector3(-44.2f,.035f,-1.5f),new Vector3(-39.4f,.035f,-2.4f),new Vector3(-36,.035f,2.2f),new Vector3(-32.6f,.035f,3.6f)})Shoe(root,p,p.x*19);
        Shield(root,new Vector3(-47.8f,.05f,-1.6f),new Vector3(79,22,-17),0);Shield(root,new Vector3(-37,.05f,2.4f),new Vector3(83,-35,13),1);Shield(root,new Vector3(-53,.07f,8.9f),new Vector3(25,17,-8),2);
        var placard=Group("Split old placard - paint washed away",root,new Vector3(-40.8f,.045f,1.1f));placard.localRotation=Quaternion.Euler(0,32,0);
        Box("Weathered wooden board",placard,new Vector3(0,.012f,0),new Vector3(.62f,.025f,.4f),oldCanvas);Rod("Snapped placard pole",placard,new Vector3(0,.015f,.1f),new Vector3(.08f,.015f,.95f),.018f,oldShoe);
        for(int i=0;i<4;i++)Box("Illegible old paint",placard,new Vector3(-.15f+i*.11f,.027f,.02f),new Vector3(.045f,.006f,.18f-i*.018f),oldCloth);Combine(placard);
        AdjustBrokenPumps(scene);EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssets();return "Two aged wrecks, three initially broken rear pumps, no pump/building labels, .45m rearward pump adjustment, and old unrest debris.";
    }
}
