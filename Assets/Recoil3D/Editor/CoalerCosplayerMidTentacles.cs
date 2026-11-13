using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
public static class CoalerCosplayerMidTentacles
{
 public static void Apply(GameObject actor){
 foreach(var old in actor.GetComponentsInChildren<Transform>(true))if(old.name=="Mid torso costume tentacles")UnityEngine.Object.DestroyImmediate(old.gameObject);
 var animator=actor.GetComponent<Animator>();var chest=animator.GetBoneTransform(HumanBodyBones.Chest);var hip=animator.GetBoneTransform(HumanBodyBones.Hips).position;
 var existing=Array.Find(actor.GetComponentsInChildren<MeshRenderer>(),r=>r.name=="Costume_Chest");
 var go=new GameObject("Mid torso costume tentacles");go.transform.SetParent(chest,false);
 var verts=new List<Vector3>();var uv=new List<Vector2>();var triangles=new[]{new List<int>(),new List<int>(),new List<int>()};
 Action<Vector3[],int> face=(points,mat)=>{int start=verts.Count;foreach(var p in points){verts.Add(chest.InverseTransformPoint(p));uv.Add(new Vector2(p.x,p.y));}for(int i=1;i<points.Length-1;i++){triangles[mat].Add(start);triangles[mat].Add(start+i);triangles[mat].Add(start+i+1);}};
 foreach(float side in new[]{-1f,1f}){
 float cx=actor.transform.position.x;float y=hip.y+.22f;float z=hip.z-.025f;
 Vector3[] path={new Vector3(cx+side*.19f,y,z),new Vector3(cx+side*.30f,y-.025f,z+.06f),new Vector3(cx+side*.40f,y-.16f,z+.12f),new Vector3(cx+side*.45f,y-.37f,z+.52f),new Vector3(cx+side*.54f,y-.53f,z+.65f)};
 float[] radii={.042f,.053f,.043f,.027f,.0025f};var rings=new Vector3[5][];
 for(int j=0;j<5;j++){var tangent=(path[Mathf.Min(4,j+1)]-path[Mathf.Max(0,j-1)]).normalized;var axis=Vector3.Cross(tangent,Vector3.forward).normalized;var other=Vector3.Cross(tangent,axis).normalized;rings[j]=new Vector3[6];for(int k=0;k<6;k++)rings[j][k]=path[j]+radii[j]*(axis*Mathf.Cos(k*Mathf.PI/3)+other*Mathf.Sin(k*Mathf.PI/3));}
 for(int j=0;j<4;j++)for(int k=0;k<6;k++)face(new[]{rings[j][k],rings[j][(k+1)%6],rings[j+1][(k+1)%6],rings[j+1][k]},(k+j)%7==0?0:1);
 var cap=(Vector3[])rings[0].Clone();Array.Reverse(cap);face(cap,1);face(rings[4],1);
 for(int k=0;k<3;k++){var center=path[0]+new Vector3(side*(k-1)*.018f,.018f,-.038f);Vector3[] v=new Vector3[8];for(int i=0;i<8;i++)v[i]=center+new Vector3((i&1)==0?-.004f:.004f,(i&2)==0?-.018f:.018f,(i&4)==0?-.004f:.004f);int[][] faces={new[]{0,1,3,2},new[]{4,6,7,5},new[]{0,4,5,1},new[]{2,3,7,6},new[]{0,2,6,4},new[]{1,5,7,3}};foreach(var f in faces)face(new[]{v[f[0]],v[f[1]],v[f[2]],v[f[3]]},2);}
 }
 var mesh=new Mesh();mesh.name="Costume_MidTorso";mesh.SetVertices(verts);mesh.SetUVs(0,uv);mesh.subMeshCount=3;for(int i=0;i<3;i++)mesh.SetTriangles(triangles[i],i);mesh.RecalculateBounds();mesh.RecalculateNormals();string pathAsset="Assets/Art/Characters/CoalerCosplayer/Costume_MidTorso.asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(pathAsset);if(saved){saved.Clear();EditorUtility.CopySerialized(mesh,saved);saved.UploadMeshData(false);UnityEngine.Object.DestroyImmediate(mesh);EditorUtility.SetDirty(saved);}else{AssetDatabase.CreateAsset(mesh,pathAsset);saved=mesh;}go.AddComponent<MeshFilter>().sharedMesh=saved;go.AddComponent<MeshRenderer>().sharedMaterials=existing.sharedMaterials;AssetDatabase.SaveAssets();
 }
}
