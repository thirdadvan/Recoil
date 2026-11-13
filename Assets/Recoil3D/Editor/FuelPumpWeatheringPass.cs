using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class FuelPumpWeatheringPass
{
    const string Folder="Assets/Recoil3D/Art/FuelPumpWeathering";
    [MenuItem("Recoil/Environment/All Fuel Pumps Off And Varied Rust")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Apply outside Play mode.");
        var scene=SceneManager.GetActiveScene();if(scene.path!="Assets/Scenes/3D/RecoilPrototype.unity")throw new Exception("Outdoor scene required.");
        var forecourt=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).First(t=>t.name=="Forecourt - move all pump islands together");
        var arts=forecourt.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Dispenser - native 3D").OrderBy(t=>t.parent.name).ToArray();
        if(arts.Length!=9)throw new Exception("Expected nine dispensers, got "+arts.Length);
        var shader=Shader.Find("Recoil/Weathered Pump Paint");if(!shader)throw new Exception("Rust shader not imported.");
        var texture=NoiseTexture();var report=new List<string>();int lights=0,paintSlots=0;
        var before=arts.ToDictionary(t=>t,t=>t.position);
        for(int i=0;i<arts.Length;i++)
        {
            var art=arts[i];var pump=art.parent;
            foreach(var item in pump.GetComponentsInChildren<FuelPumpInteraction>(true))
            {
                item.screenBlack=true;if(item.serviceDisplay)item.serviceDisplay.SetActive(false);if(item.outOfOrderDisplay)item.outOfOrderDisplay.SetActive(false);EditorUtility.SetDirty(item);
            }
            foreach(var display in pump.GetComponentsInChildren<Recoil3DFuelPumpDisplay>(true)){display.screenBlack=true;display.Sync();EditorUtility.SetDirty(display);}
            foreach(var t in art.GetComponentsInChildren<Transform>(true))if(t.name=="Meter - normal service"||t.name=="Meter - OUT OF ORDER")t.gameObject.SetActive(false);
            foreach(var light in pump.GetComponentsInChildren<Light>(true)){light.enabled=false;EditorUtility.SetDirty(light);lights++;}
            float threshold=new[]{.59f,.52f,.63f,.56f,.48f,.60f,.54f,.62f,.50f}[i];
            foreach(var renderer in art.GetComponentsInChildren<Renderer>(true))
            {
                var materials=renderer.sharedMaterials;
                for(int slot=0;slot<materials.Length;slot++)
                {
                    var original=materials[slot];if(!original)continue;
                    bool ivory=original.name.Contains("Pump aged ivory")||original.name.Contains("Ivory");bool red=original.name=="Faded petrol red"||original.name.Contains("RedPaint");
                    if(!ivory&&!red)continue;
                    string path=Folder+"/Pump"+(i+1).ToString("D2")+"_"+(ivory?"Ivory":"RedPaint")+".mat";
                    var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
                    if(!mat){mat=new Material(shader);AssetDatabase.CreateAsset(mat,path);}
                    // Original shared paint assets are never edited.
                    Color baseColor=original.HasProperty("_Color")?original.GetColor("_Color"):Color.gray;
                    mat.shader=shader;mat.SetColor("_Color",baseColor);
                    if(original.HasProperty("_MainTex")){mat.SetTexture("_MainTex",original.GetTexture("_MainTex"));mat.SetTextureScale("_MainTex",original.GetTextureScale("_MainTex"));}
                    mat.SetTexture("_RustNoise",texture);mat.SetVector("_RustSeed",new Vector4(i*1.713f+.21f,i*.391f+.07f,i*.927f+.43f,0));
                    mat.SetFloat("_RustAmount",threshold);mat.SetColor("_RustColor",Color.Lerp(new Color(.19f,.073f,.027f),new Color(.26f,.12f,.043f),(i%4)/3f));mat.SetFloat("_Glossiness",.16f);mat.enableInstancing=true;EditorUtility.SetDirty(mat);
                    materials[slot]=mat;paintSlots++;
                }
                renderer.sharedMaterials=materials;EditorUtility.SetDirty(renderer);
            }
            if(pump.GetComponentsInChildren<Transform>(true).Any(t=>(t.name=="Meter - normal service"||t.name=="Meter - OUT OF ORDER")&&t.gameObject.activeSelf))throw new Exception("Lit screen remains: "+pump.name);
            if(art.position!=before[art])throw new Exception("Pump moved");
            report.Add(pump.name+": black screens; rust threshold="+threshold+" seed="+i+"; no added geometry.");
        }
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new Exception("Scene save failed");
        report.Insert(0,"PASS 9 dispensers off; "+paintSlots+" painted material slots weathered; "+lights+" pump-local lights disabled. One shared 256x256 noise map; individual seeded patterns and 4 rust tones. No new meshes or colliders.");
        Directory.CreateDirectory("output/fuel-pump-weathering");File.WriteAllLines("output/fuel-pump-weathering/verification.txt",report);Debug.Log(string.Join("\n",report));
    }
    static Texture2D NoiseTexture()
    {
        string path=Folder+"/SharedRustNoise.png";
        if(!File.Exists(path))
        {
            var tex=new Texture2D(256,256,TextureFormat.RGB24,false,true);var pixels=new Color[256*256];
            for(int y=0;y<256;y++)for(int x=0;x<256;x++)
            {
                float u=x/256f,v=y/256f;
                // Blend periodic samples to avoid seams without diagonal warping.
                Func<float,float,float> field=(ox,oy)=>{float value=0;for(int k=0;k<4;k++){float f=(1<<k)*4f;float a=Mathf.PerlinNoise(ox+u*f,oy+v*f),b=Mathf.PerlinNoise(ox+(u-1)*f,oy+v*f),c=Mathf.PerlinNoise(ox+u*f,oy+(v-1)*f),d=Mathf.PerlinNoise(ox+(u-1)*f,oy+(v-1)*f);value+=Mathf.Lerp(Mathf.Lerp(a,b,u),Mathf.Lerp(c,d,u),v)/(1<<k);}return value/1.875f;};
                pixels[y*256+x]=new Color(field(14.3f,8.7f),field(41.1f,51.2f),field(74.2f,13.3f));
            }
            tex.SetPixels(pixels);tex.Apply();File.WriteAllBytes(path,tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);AssetDatabase.ImportAsset(path);
        }
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.sRGBTexture=false;importer.wrapMode=TextureWrapMode.Repeat;importer.filterMode=FilterMode.Bilinear;importer.maxTextureSize=256;importer.mipmapEnabled=true;importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
}
