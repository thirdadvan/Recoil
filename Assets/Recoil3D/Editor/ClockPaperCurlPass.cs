using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class ClockPaperCurlPass
{
 const string Path="Assets/Art/Environment/ArchiveRoom/Clock note - curled paper mesh.asset";
 public static Mesh Build()
 {
  var existing=AssetDatabase.LoadAssetAtPath<Mesh>(Path);if(existing)return existing;
  const int nx=48,ny=32;int count=(nx+1)*(ny+1);var vertices=new Vector3[count*2];var uv=new Vector2[count*2];var front=new List<int>();var back=new List<int>();
  for(int y=0;y<=ny;y++)for(int x=0;x<=nx;x++){
   float u=(float)x/nx,v=(float)y/ny;var p=new Vector3((u-.5f)*1.5f,v-.5f,-.002f*Mathf.Sin(u*Mathf.PI)*Mathf.Sin(v*Mathf.PI));
   for(int corner=0;corner<4;corner++){
    float sx=(corner%2==0?-1:1),sy=(corner<2?-1:1);float dx=sx<0?u:1-u,dy=sy<0?v:1-v;
    float t=Mathf.Clamp01(1-dx/.17f-dy/.23f);float angle=(corner==0?65:corner==1?112:corner==2?30:48)*Mathf.Deg2Rad;
    float length=corner==1?.19f:.145f;float radius=length/angle;float a=t*angle;
    float shortened=length*t-radius*Mathf.Sin(a),lift=radius*(1-Mathf.Cos(a));
    p.x-=sx*shortened*.7071f;p.y-=sy*shortened*.7071f;p.z-=lift;
   }
   int i=y*(nx+1)+x;vertices[i]=p;vertices[i+count]=p+Vector3.forward*.0018f;uv[i]=uv[i+count]=new Vector2(u,v);
  }
  for(int y=0;y<ny;y++)for(int x=0;x<nx;x++){int a=y*(nx+1)+x,b=a+1,c=a+nx+1,d=c+1;front.AddRange(new[]{a,c,d,a,d,b});back.AddRange(new[]{a+count,d+count,c+count,a+count,b+count,d+count});}
  var rim=new List<int>();for(int x=0;x<=nx;x++)rim.Add(x);for(int y=1;y<=ny;y++)rim.Add(y*(nx+1)+nx);for(int x=nx-1;x>=0;x--)rim.Add(ny*(nx+1)+x);for(int y=ny-1;y>0;y--)rim.Add(y*(nx+1));
  for(int i=0;i<rim.Count;i++){int a=rim[i],b=rim[(i+1)%rim.Count];back.AddRange(new[]{a,b,a+count,b,b+count,a+count});}
  var mesh=new Mesh{name="Clock note - uneven curled corners with paper thickness"};mesh.vertices=vertices;mesh.uv=uv;mesh.subMeshCount=2;mesh.SetTriangles(front,0);mesh.SetTriangles(back,1);mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,Path);return mesh;
 }
 public static string Apply()
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play first");var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(!scene.path.EndsWith("FactoryArchiveF3.unity"))throw new Exception("Archive required");
  var note=UnityEngine.Object.FindObjectsOfType<ReadableProp>().First(p=>p.readProgressFlag=="archive_clock_storage_note_read");var renderer=note.GetComponentsInChildren<MeshRenderer>(true).First(r=>r.name=="Handwritten clock storage warning");var mesh=Build();
  var backPath="Assets/Art/Environment/ArchiveRoom/Clock note - aged paper reverse.mat";var back=AssetDatabase.LoadAssetAtPath<Material>(backPath);if(!back){back=new Material(Shader.Find("Standard"));back.color=new Color(.65f,.56f,.37f);back.SetFloat("_Glossiness",0);AssetDatabase.CreateAsset(back,backPath);}
  var filter=renderer.GetComponent<MeshFilter>();if(filter.sharedMesh!=mesh){var size=renderer.transform.localScale;size.x/=1.5f;size.z=(size.x+size.y)*.5f;renderer.transform.localScale=size;}
  filter.sharedMesh=mesh;renderer.sharedMaterials=new[]{renderer.sharedMaterial,back};renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;note.pageMesh=mesh;
  EditorUtility.SetDirty(note);EditorUtility.SetDirty(filter);EditorUtility.SetDirty(renderer);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Selection.activeGameObject=note.gameObject;
  return "Saved one shared curled paper mesh for scene and reader: "+mesh.vertexCount+" vertices, "+mesh.triangles.Length/3+" triangles; front, reverse, thin edges; interaction unchanged.";
 }
}
