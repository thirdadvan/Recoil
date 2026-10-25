using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

public static partial class Recoil3DOpeningForecourtPass
{
    // Static authored damage, independent of the later station-wide story failure.
    public static string AdjustBrokenPumps(Scene scene)
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode || scene.path!="Assets/Scenes/3D/RecoilPrototype.unity")throw new Exception("Open the outdoor scene outside Play mode.");
        var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
        var forecourt=all.First(t=>t.name=="Forecourt - move all pump islands together");
        var rows=forecourt.Cast<Transform>().Where(t=>t.name.Contains("pump row")).OrderBy(t=>t.name).ToArray();
        var source=rows[1].GetComponentsInChildren<FuelPumpInteraction>(true).OrderBy(t=>t.transform.position.x).First();
        meshSerial=AssetDatabase.FindAssets("t:Mesh",new[]{Folder+"/Meshes"}).Select(AssetDatabase.GUIDToAssetPath).Select(p=>int.Parse(System.IO.Path.GetFileNameWithoutExtension(p).Split('-')[0])).DefaultIfEmpty(0).Max()+1;
        var rust=Material("Pump exposed rust and dents",new Color(.22f,.135f,.075f));
        var black=Material("Pump broken screen and scratches",new Color(.035f,.038f,.032f));
        int damaged=0;
        for(int rowIndex=0;rowIndex<rows.Length;rowIndex++)
        {
            var pumps=rows[rowIndex].Cast<Transform>().OrderBy(t=>t.position.x).ToArray();
            for(int column=0;column<pumps.Length;column++)
            {
                var pump=pumps[column];bool broken=column==(rowIndex==2?0:1);
                var art=pump.Find("Dispenser - native 3D");
                var display=art.GetComponent<Recoil3DFuelPumpDisplay>();
                if(display){display.startsBroken=broken;display.source=source;display.Sync();EditorUtility.SetDirty(display);}
                var interaction=pump.GetComponent<FuelPumpInteraction>();
                if(interaction){interaction.startsBroken=broken;interaction.ResetItem();EditorUtility.SetDirty(interaction);}
                if(!broken)continue;
                damaged++;
                art.Find("Meter - normal service").gameObject.SetActive(false);
                art.Find("Meter - OUT OF ORDER").gameObject.SetActive(false);
                if(art.Find("Broken pump - dented panels and exposed metal"))continue;
                // Each cabinet receives its own mesh, leaving the six usable dispensers unchanged.
                foreach(var filter in art.Find("Static dispenser body").GetComponentsInChildren<MeshFilter>())
                {
                    var mesh=UnityEngine.Object.Instantiate(filter.sharedMesh);mesh.name="Dented dispenser body";
                    var vertices=mesh.vertices;
                    for(int i=0;i<vertices.Length;i++)
                    {
                        var point=vertices[i];
                        if(point.y>.3f)
                        {
                            float impact=Mathf.Clamp01(1-Mathf.Abs(point.y-.82f)/.6f)*Mathf.Clamp01((point.x+.3f)/.8f);
                            point.x-=impact*.12f;point.z+=impact*.12f;
                            if(point.y>1.45f){point.x+=.045f*(rowIndex-1);point.y-=.045f*Mathf.Clamp01(point.x+.5f);}
                        }
                        vertices[i]=point;
                    }
                    mesh.vertices=vertices;mesh.RecalculateNormals();mesh.RecalculateBounds();
                    AssetDatabase.CreateAsset(mesh,Folder+"/Meshes/"+(meshSerial++).ToString("0000")+"-dented-pump.asset");filter.sharedMesh=mesh;
                }
                var damage=Group("Broken pump - dented panels and exposed metal",art,Vector3.zero);
                var panel=Box("Loose buckled lower access panel",damage,new Vector3(-.09f,.43f,-.39f),new Vector3(.55f,.53f,.035f),rust);
                panel.transform.localRotation=Quaternion.Euler(12,5,7+rowIndex*4);
                Box("Open cabinet seam",damage,new Vector3(.22f,.64f,-.345f),new Vector3(.09f,.44f,.02f),black);
                for(int i=0;i<4;i++)Rod("Scraped paint exposing rusty metal",damage,new Vector3(-.29f+i*.13f,1.58f,-.365f),new Vector3(-.22f+i*.13f,1.47f-i*.025f,-.365f),.012f,rust);
                Rod("Cracked blank meter glass",damage,new Vector3(-.3f,1.48f,-.385f),new Vector3(.04f,1.32f,-.385f),.006f,black);
                Rod("Crack branch",damage,new Vector3(.04f,1.32f,-.385f),new Vector3(.24f,1.4f,-.385f),.005f,black);
                Combine(damage);
                var nozzle=interaction?interaction.nozzle:art.Find("Resting nozzle");
                nozzle.position=art.TransformPoint(new Vector3(rowIndex==0?.98f:rowIndex==1?-.9f:1.14f,.11f,rowIndex==1?-.75f:-.91f));
                nozzle.rotation=Quaternion.Euler(84,rowIndex==0?32:rowIndex==1?-58:119,12);
                var hose=art.GetComponentsInChildren<LineRenderer>().First(t=>t.name=="Looped rubber hose");
                var end=art.InverseTransformPoint(nozzle.position);hose.positionCount=19;
                for(int i=0;i<19;i++)
                {
                    float u=i/18f;
                    Vector3 p=u<.4f?Vector3.Lerp(new Vector3(.55f,1.35f,-.18f),new Vector3(.74f,.09f,-.27f),u/.4f):Vector3.Lerp(new Vector3(.74f,.09f,-.27f),end,(u-.4f)/.6f);
                    if(u>.4f)p.x+=Mathf.Sin((u-.4f)/.6f*Mathf.PI)*.33f;
                    hose.SetPosition(i,p);
                }
            }
        }
        EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssets();return "Three damaged black-screen pumps: rear-left 1, middle-left 2, foreground-left 2; dropped nozzles and dented cabinets.";
    }
}
