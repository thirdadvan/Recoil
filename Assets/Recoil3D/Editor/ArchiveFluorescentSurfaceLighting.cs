using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;

// A wide downward cone lights the room while rejecting every upward ray.
public static class ArchiveFluorescentSurfaceLighting
{
    public const string CookiePath="Assets/Art/Environment/ArchiveRoom/Fluorescent downward hemisphere cookie.asset";
    public static Cubemap Cookie()
    {
        var cookie=AssetDatabase.LoadAssetAtPath<Cubemap>(CookiePath);if(cookie)return cookie;
        const int size=32;cookie=new Cubemap(size,TextureFormat.RGBA32,false);cookie.name="Fluorescent reflector - downward and lateral light";cookie.wrapMode=TextureWrapMode.Clamp;cookie.filterMode=FilterMode.Bilinear;
        foreach(CubemapFace face in new[]{CubemapFace.PositiveX,CubemapFace.NegativeX,CubemapFace.PositiveY,CubemapFace.NegativeY,CubemapFace.PositiveZ,CubemapFace.NegativeZ}){
            var pixels=new Color[size*size];for(int y=0;y<size;y++)for(int x=0;x<size;x++){
                float u=(x+.5f)/size*2-1,v=(y+.5f)/size*2-1;Vector3 direction;
                switch(face){case CubemapFace.PositiveX:direction=new Vector3(1,-v,-u);break;case CubemapFace.NegativeX:direction=new Vector3(-1,-v,u);break;case CubemapFace.PositiveY:direction=new Vector3(u,1,v);break;case CubemapFace.NegativeY:direction=new Vector3(u,-1,-v);break;case CubemapFace.PositiveZ:direction=new Vector3(u,-v,1);break;default:direction=new Vector3(-u,-v,-1);break;}
                float alpha=1-Mathf.SmoothStep(-.04f,.20f,direction.normalized.y);pixels[y*size+x]=new Color(alpha,alpha,alpha,alpha);
            }cookie.SetPixels(pixels,face);
        }cookie.Apply();AssetDatabase.CreateAsset(cookie,CookiePath);return cookie;
    }
    public static void Configure(FaultyFluorescentFixture fixture)
    {
        var light=fixture.roomLight;if(!light)throw new Exception("Fixture light missing");
        light.type=LightType.Spot;light.transform.localPosition=new Vector3(0,-.19f,0);light.transform.localRotation=Quaternion.Euler(90,0,0);
        light.spotAngle=160;light.innerSpotAngle=145;
        light.renderMode=LightRenderMode.ForcePixel;light.cookie=null;light.cullingMask=~0;light.range=8.5f;light.color=new Color(.79f,.85f,.74f);
        light.shadows=LightShadows.Soft;light.shadowResolution=UnityEngine.Rendering.LightShadowResolution.Medium;light.shadowBias=.025f;light.shadowNormalBias=.04f;
        var profile=light.GetComponent<VolumetricLightProfile>();if(!profile)profile=light.gameObject.AddComponent<VolumetricLightProfile>();profile.scatteringMultiplier=0;EditorUtility.SetDirty(profile);
        fixture.roomIntensity=FaultyFluorescentFixture.MaximumIntensity;fixture.gameplayIntensity=.30f;EditorUtility.SetDirty(light);EditorUtility.SetDirty(fixture);
    }
    public static string Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Use Edit mode");
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(!scene.path.EndsWith("/FactoryArchiveF3.unity"))throw new Exception("Archive scene required");
        var fixtures=Object.FindObjectsOfType<FaultyFluorescentFixture>();foreach(var fixture in fixtures){Undo.RegisterFullObjectHierarchyUndo(fixture.gameObject,"Correct fluorescent surface illumination");Configure(fixture);}
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        return "Saved broad downward/lateral fluorescent illumination on "+fixtures.Length+" fixtures; wide downward cone excludes ceiling illumination, ForcePixel ensures real surface lighting, synchronized with fault cycles.";
    }
}
