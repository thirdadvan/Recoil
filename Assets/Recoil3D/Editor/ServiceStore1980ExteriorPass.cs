using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

public static class ServiceStore1980ExteriorPass
{
 const string Folder="Assets/Recoil3D/ServiceStore1980",ScenePath="Assets/Scenes/3D/RecoilPrototype.unity";
 public const string Marker="1980s service store - native detailed pavilion and mining gantry";
 static Material brick,plaster,iron,rust,cream,red,wood,glass,dark,paper,brown,wallDirt,groundDirt;
 [Serializable] public class Glyph {public string character;public float offset;public float[] vertices;public int[] triangles;}
 [Serializable] public class Glyphs {public Glyph[] glyphs;}
 static T[] All<T>(Scene s)where T:Component{return s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true)).ToArray();}
 static Transform G(Transform p,string n,Vector3 at){var t=new GameObject(n).transform;t.SetParent(p,false);t.localPosition=at;return t;}
 static Transform B(Transform p,string n,Vector3 at,Vector3 size,Material m,PrimitiveType kind=PrimitiveType.Cube)
 {var t=GameObject.CreatePrimitive(kind).transform;t.name=n;Object.DestroyImmediate(t.GetComponent<Collider>());t.SetParent(p,false);t.localPosition=at;t.localScale=size;t.GetComponent<Renderer>().sharedMaterial=m;return t;}
 static void Beam(Transform p,string n,Vector3 a,Vector3 b,float width,Material mat)
 {var t=B(p,n,(a+b)*.5f,new Vector3(width,width,(b-a).magnitude),mat);t.localRotation=Quaternion.LookRotation(b-a);}
 static Material M(string n,int kind,Color tint,float metal=0)
 {var path=Folder+"/"+n+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Recoil/3D/Metric Architectural Surface"));AssetDatabase.CreateAsset(m,path);}m.SetColor("_Color",Color.white);m.SetColor("_BaseTint",tint);m.SetColor("_MortarTint",new Color(.16f,.165f,.14f));m.SetFloat("_Kind",kind);m.SetVector("_BrickSize",new Vector4(.26f,.085f,.006f,0));m.SetFloat("_PlasterCoverage",.36f);m.SetFloat("_Metallic",metal);m.SetFloat("_Glossiness",.13f);return m;}
 static void Materials()
 {
  brick=M("Old pavilion exposed brick",1,new Color(.29f,.25f,.20f));plaster=M("Flaking grey cream pavilion plaster",2,new Color(.37f,.38f,.33f));iron=M("Chipped grey green aluminium and iron",3,new Color(.19f,.24f,.215f),.38f);rust=M("Deep old frame corrosion",3,new Color(.23f,.105f,.047f),.2f);cream=M("Faded ivory metal letters and panels",3,new Color(.56f,.55f,.46f),.15f);red=M("Faded petrol red enamel",3,new Color(.29f,.07f,.04f),.1f);wood=M("Old shop counter veneer",4,new Color(.23f,.18f,.115f));dark=M("Dark dusty store recess",0,new Color(.035f,.043f,.04f));paper=M("Yellowed shop notices and labels",2,new Color(.48f,.45f,.33f));brown=M("Dark amber shop bottles",3,new Color(.12f,.075f,.03f),.1f);
  glass=new Material(Shader.Find("Standard"));glass.name="Dirty thin pavilion window glass";glass.color=new Color(.15f,.21f,.20f,.23f);glass.SetFloat("_Mode",3);glass.SetInt("_SrcBlend",5);glass.SetInt("_DstBlend",10);glass.SetInt("_ZWrite",0);glass.EnableKeyword("_ALPHABLEND_ON");glass.renderQueue=3000;glass.SetFloat("_Glossiness",.38f);AssetDatabase.CreateAsset(glass,Folder+"/Dirty thin pavilion window glass.mat");
  wallDirt=new Material(Shader.Find("Recoil/3D/Building Footing Moss And Silt"));wallDirt.name="Irregular damp moss up wall footing";wallDirt.SetFloat("_Ground",0);wallDirt.SetFloat("_Opacity",.78f);AssetDatabase.CreateAsset(wallDirt,Folder+"/"+wallDirt.name+".mat");groundDirt=new Material(wallDirt);groundDirt.name="Muddy wash at building road joint";groundDirt.SetFloat("_Ground",1);groundDirt.SetFloat("_Opacity",.65f);AssetDatabase.CreateAsset(groundDirt,Folder+"/"+groundDirt.name+".mat");
 }
 static Mesh Save(Mesh m,string name){m.name=name;AssetDatabase.CreateAsset(m,AssetDatabase.GenerateUniqueAssetPath(Folder+"/Models/"+name+".asset"));return m;}
 static void Quad(Transform root,string name,Vector3 a,Vector3 b,Vector3 c,Vector3 d,Material m)
 {var mesh=new Mesh();mesh.vertices=new[]{a,b,c,d};mesh.uv=new[]{Vector2.zero,Vector2.right,Vector2.up,Vector2.one};mesh.triangles=new[]{0,2,1,1,2,3};mesh.RecalculateNormals();if(name=="Mud and moss washed onto ground joint"){mesh.triangles=new[]{0,1,2,1,3,2};mesh.RecalculateNormals();}var t=G(root,name,Vector3.zero);t.gameObject.AddComponent<MeshFilter>().sharedMesh=Save(mesh,name);var r=t.gameObject.AddComponent<MeshRenderer>();r.sharedMaterial=m;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;}
 static void Label(Transform p,string name,string text,Vector3 at,float size,Color colour)
 {var t=G(p,name,at);var tm=t.gameObject.AddComponent<TextMesh>();tm.text=text;tm.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");tm.fontSize=64;tm.characterSize=size;tm.anchor=TextAnchor.MiddleCenter;tm.color=colour;t.GetComponent<MeshRenderer>().sharedMaterial=tm.font.material;}
 static void Bottle(Transform p,Vector3 at,int i)
 {B(p,"Dusty amber bottle",at+Vector3.up*.14f,new Vector3(.085f,.14f,.085f),brown,PrimitiveType.Cylinder);B(p,"Bottle neck",at+Vector3.up*.31f,new Vector3(.04f,.04f,.04f),brown,PrimitiveType.Cylinder);B(p,"Dull bottle cap",at+Vector3.up*.36f,new Vector3(.047f,.015f,.047f),cream,PrimitiveType.Cylinder);B(p,"Faded bottle paper label",at+new Vector3(0,.15f,-.046f),new Vector3(.068f,.10f,.004f),paper);}
 static void Tin(Transform p,Vector3 at,int i)
 {B(p,"Old square motor oil tin",at+Vector3.up*.17f,new Vector3(.20f,.34f,.13f),i%3==0?red:cream);B(p,"Oil tin cap",at+new Vector3(.04f,.355f,0),new Vector3(.045f,.025f,.045f),iron,PrimitiveType.Cylinder);B(p,"Oil tin printed label",at+new Vector3(0,.17f,-.068f),new Vector3(.16f,.19f,.003f),paper);Beam(p,"Small tin carrying handle",at+new Vector3(-.045f,.37f,0),at+new Vector3(.03f,.37f,0),.013f,iron);}
 static void Pavilion(Transform p)
 {
  // Same footprint and door/roof heights as the old playable shell, with actual window openings.
  B(p,"Brick footing left of entrance",new Vector3(-5.49f,.44f,.12f),new Vector3(1.02f,.88f,.30f),brick);
  B(p,"Peeled plaster left entrance pier",new Vector3(-5.49f,1.93f,.12f),new Vector3(1.02f,2.10f,.30f),plaster);
  B(p,"Entrance right structural pier",new Vector3(-3.54f,1.50f,.12f),new Vector3(.25f,3,.30f),plaster);
  B(p,"Low brick shopfront wall under glazing",new Vector3(1.0f,.43f,.12f),new Vector3(8.83f,.86f,.30f),brick);
  B(p,"Right shopfront corner pier",new Vector3(5.72f,1.50f,.12f),new Vector3(.56f,3,.30f),plaster);
  B(p,"Solid front head above glazing",new Vector3(0,3.61f,.12f),new Vector3(12,1.22f,.30f),plaster);
  foreach(float side in new[]{-1f,1f}){B(p,"Pavilion side masonry",new Vector3(side*5.85f,2.1f,9.35f),new Vector3(.30f,4.2f,18.7f),plaster);B(p,"Side plinth exposed repair brick",new Vector3(side*5.88f,.34f,9.35f),new Vector3(.35f,.68f,18.7f),brick);}
  B(p,"Rear service masonry wall",new Vector3(0,2.1f,18.55f),new Vector3(11.7f,4.2f,.30f),plaster);
  var door=G(p,"Recessed glazed steel entrance - native whole prop",new Vector3(-4.32f,0,-.035f));
  foreach(float x in new[]{-.64f,.64f})B(door,"Weathered entrance frame jamb",new Vector3(x,1.32f,0),new Vector3(.06f,2.64f,.13f),iron);
  B(door,"Entrance frame transom",new Vector3(0,2.64f,0),new Vector3(1.34f,.07f,.13f),iron);
  B(door,"Old door lower enamel kick plate",new Vector3(0,.31f,-.018f),new Vector3(1.16f,.56f,.065f),iron);
  B(door,"Entrance glazing",new Vector3(0,1.54f,-.006f),new Vector3(1.12f,1.86f,.018f),glass);
  foreach(float y in new[]{.62f,2.47f})B(door,"Door leaf cross rail",new Vector3(0,y,-.03f),new Vector3(1.2f,.045f,.07f),iron);
  foreach(float x in new[]{-.59f,.59f})B(door,"Door leaf vertical stile",new Vector3(x,1.54f,-.03f),new Vector3(.045f,1.9f,.07f),iron);
  Beam(door,"Worn straight door pull",new Vector3(.41f,1.11f,-.14f),new Vector3(.41f,1.47f,-.14f),.026f,iron);
  foreach(float y in new[]{.34f,1.37f,2.31f})B(door,"Rusting external door hinge",new Vector3(-.61f,y,-.08f),new Vector3(.095f,.13f,.05f),rust);
  B(door,"Inset lock plate",new Vector3(.47f,1.05f,-.07f),new Vector3(.065f,.12f,.018f),iron);
  B(door,"Worn concrete threshold",new Vector3(0,.025f,-.10f),new Vector3(1.34f,.05f,.33f),plaster);
  Label(door,"Old opening-hours paper","8 - 20",new Vector3(0,1.95f,-.035f),.045f,new Color(.65f,.62f,.47f));
  for(int bay=0;bay<4;bay++)
  {float x=-2.30f+bay*2.08f;B(p,"Recessed display window glass",new Vector3(x,1.91f,.05f),new Vector3(2.00f,1.99f,.018f),glass);foreach(float xx in new[]{x-1.015f,x+1.015f})B(p,"Fixed metal window mullion",new Vector3(xx,1.91f,-.008f),new Vector3(.045f,2.08f,.10f),iron);foreach(float y in new[]{.88f,2.94f})B(p,"Window sill and head rail",new Vector3(x,y,-.008f),new Vector3(2.07f,.06f,.14f),iron);B(p,"Window stone drip sill",new Vector3(x,.84f,-.08f),new Vector3(2.11f,.09f,.38f),plaster);}
  B(p,"Continuous narrow window transom",new Vector3(.83f,2.43f,-.014f),new Vector3(8.36f,.026f,.08f),iron);
  // A dimensional shop display behind the glazing, readable from the outdoor camera.
  B(p,"Dim shop display rear wall",new Vector3(.95f,1.65f,2.22f),new Vector3(8.75f,3.10f,.11f),dark);
  B(p,"Old veneer sales counter",new Vector3(-.4f,.66f,1.04f),new Vector3(5.6f,1.24f,.64f),wood);
  B(p,"Worn counter top",new Vector3(-.4f,1.31f,1.02f),new Vector3(5.77f,.07f,.74f),iron);
  foreach(float y in new[]{1.05f,1.66f,2.26f})B(p,"Dusty timber display shelf",new Vector3(1.15f,y,1.86f),new Vector3(8.15f,.05f,.40f),wood);
  foreach(float x in new[]{-2.8f,1.2f,5.1f})B(p,"Upright shelf support",new Vector3(x,1.67f,2.03f),new Vector3(.045f,2.25f,.045f),iron);
  for(int i=0;i<28;i++)Tin(p,new Vector3(-2.5f+(i%14)*.53f,1.70f+(i/14)*.6f,1.67f),i);
  for(int i=0;i<14;i++)Bottle(p,new Vector3(-2.55f+i*.43f,1.08f,1.67f),i);
  var cash=G(p,"Meshy cash register placement",new Vector3(-1.15f,1.345f,.92f));
  B(p,"Folded old receipt on counter",new Vector3(-.6f,1.354f,.76f),new Vector3(.20f,.006f,.15f),paper).localRotation=Quaternion.Euler(0,14,0);
  B(p,"Counter cash tray",new Vector3(-.1f,1.37f,.83f),new Vector3(.23f,.035f,.18f),iron);
  Label(p,"Oil shelf category","OLEJE   /   SMARY",new Vector3(1.0f,2.77f,1.55f),.065f,new Color(.62f,.60f,.49f));
  for(int i=0;i<12;i++)B(p,"Vertical rib in old enamel fascia",new Vector3(-5.7f+i*1.03f,3.57f,-.06f),new Vector3(.028f,1.05f,.06f),iron);
  B(p,"Narrow petrol-red fascia stripe",new Vector3(0,3.30f,-.095f),new Vector3(12.1f,.10f,.05f),red);
  Label(p,"Old shopfront enamel lettering","SERVICE STORE",new Vector3(0,3.78f,-.115f),.10f,new Color(.67f,.65f,.54f));
  B(p,"Thin front rain canopy",new Vector3(0,3.09f,-.43f),new Vector3(12.25f,.09f,1.1f),iron);
  B(p,"Rolled front canopy drip edge",new Vector3(0,3.04f,-1.0f),new Vector3(12.35f,.10f,.05f),rust);
  for(int i=0;i<7;i++)Beam(p,"Canopy wall bracket",new Vector3(-5.6f+i*1.85f,2.75f,-.01f),new Vector3(-5.6f+i*1.85f,3.02f,-.8f),.035f,iron);
  // Roof remains at the existing playable platform top, including its original skylight.
  foreach(var pair in new[]{new Vector2(-6.2f,-1.08f),new Vector2(1.08f,6.2f)})B(p,"Tar roof beside existing skylight",new Vector3((pair.x+pair.y)*.5f,4.30f,8.25f),new Vector3(pair.y-pair.x,.20f,21.1f),dark);
  B(p,"Tar roof behind existing skylight",new Vector3(0,4.30f,9.42f),new Vector3(2.16f,.20f,18.16f),dark);
  foreach(float x in new[]{-6.14f,6.14f})B(p,"Concrete coping along roof side",new Vector3(x,4.45f,8.3f),new Vector3(.16f,.14f,21.2f),plaster);
  B(p,"Front concrete coping with old drip",new Vector3(0,4.45f,-2.23f),new Vector3(12.4f,.14f,.17f),plaster);
  for(float z=1;z<18;z+=1.1f)B(p,"Overlapped old roof-felt seam",new Vector3(0,4.414f,z),new Vector3(11.9f,.018f,.045f),iron);
  B(p,"Roof access service vent housing",new Vector3(3.4f,4.68f,14.7f),new Vector3(1.30f,.55f,1.1f),iron);
  for(int i=0;i<8;i++)B(p,"Vent louvre blade",new Vector3(3.4f,4.52f+i*.045f,14.135f),new Vector3(1.12f,.025f,.04f),dark);
  Beam(p,"Roof vent flue",new Vector3(-4.6f,4.4f,15.7f),new Vector3(-4.6f,5.24f,15.7f),.18f,iron);B(p,"Old flue rain cap",new Vector3(-4.6f,5.30f,15.7f),new Vector3(.44f,.07f,.44f),rust);
  foreach(float x in new[]{-5.93f,5.93f}){Beam(p,"Old roof downpipe",new Vector3(x,4.4f,.32f),new Vector3(x,.13f,.32f),.073f,iron);foreach(float y in new[]{.6f,2.2f,3.8f})B(p,"Rusty downpipe bracket",new Vector3(x,y,.31f),new Vector3(.15f,.045f,.12f),rust);}
  B(p,"Exterior junction box beside shop door",new Vector3(-5.57f,1.0f,-.06f),new Vector3(.27f,.45f,.15f),iron);Beam(p,"Surface conduit to fascia",new Vector3(-5.57f,1.25f,-.09f),new Vector3(-5.57f,3.04f,-.09f),.025f,iron);
  foreach(float x in new[]{-5.55f,5.48f}){B(p,"Old sheet-metal refuse bin",new Vector3(x,.39f,-.63f),new Vector3(.43f,.75f,.36f),iron);B(p,"Bin shallow lid",new Vector3(x,.79f,-.63f),new Vector3(.48f,.045f,.40f),rust);}
  Beam(p,"Worn broom propped beside entrance",new Vector3(-5.9f,.05f,-.45f),new Vector3(-5.72f,1.47f,-.08f),.026f,wood);B(p,"Broom head",new Vector3(-5.9f,.08f,-.45f),new Vector3(.28f,.11f,.09f),wood);
  var fill=G(p,"Weak warm shop display fixture",new Vector3(.7f,2.55f,.85f));var light=fill.gameObject.AddComponent<Light>();light.type=LightType.Point;light.color=new Color(.88f,.72f,.44f);light.intensity=.22f;light.range=5.5f;light.shadows=LightShadows.None;
 }
 static void Gantry(Transform root)
 {
  var g=G(root,"ZHAKOV MINING - open steel letter gantry from store over road behind camera",Vector3.zero);
  Vector3 a=new Vector3(-7.1f,0,3.2f),b=new Vector3(6.1f,0,-34);var d=(b-a).normalized;var normal=Vector3.Cross(d,Vector3.up);float length=Vector3.Distance(a,b);
  foreach(float y in new[]{4.95f,7.95f})foreach(float side in new[]{-.26f,.26f})Beam(g,"Continuous old angle-iron sign chord",a+Vector3.up*y+normal*side,b+Vector3.up*y+normal*side,.085f,iron);
  int bays=Mathf.CeilToInt(length/2.1f);
  for(int i=0;i<=bays;i++){var at=Vector3.Lerp(a,b,i/(float)bays);foreach(float side in new[]{-.26f,.26f})Beam(g,"Sign lattice vertical angle",at+Vector3.up*4.95f+normal*side,at+Vector3.up*7.95f+normal*side,.055f,iron);if(i<bays){var next=Vector3.Lerp(a,b,(i+1)/(float)bays);Beam(g,"Diagonal rear sign lattice brace",at+Vector3.up*(i%2==0?4.95f:7.95f)+normal*.26f,next+Vector3.up*(i%2==0?7.95f:4.95f)+normal*.26f,.045f,rust);}Beam(g,"Cross frame spacer",at+Vector3.up*4.95f-normal*.3f,at+Vector3.up*4.95f+normal*.3f,.045f,iron);}
  foreach(float t in new[]{0f,.53f,1f}){var at=Vector3.Lerp(a,b,t);float baseY=t==0?4.4f:0;foreach(float side in new[]{-.32f,.32f}){Beam(g,"Riveted gantry support column",at+normal*side+Vector3.up*baseY,at+normal*side+Vector3.up*7.95f,.16f,iron);B(g,"Gantry bolted base plate",at+normal*side+Vector3.up*(baseY+.035f),new Vector3(.48f,.07f,.48f),rust);foreach(float dx in new[]{-.17f,.17f})foreach(float dz in new[]{-.17f,.17f})B(g,"Old gantry base anchor bolt",at+normal*side+new Vector3(dx,baseY+.09f,dz),new Vector3(.045f,.05f,.045f),iron,PrimitiveType.Cylinder);}if(t>0)B(g,"Low gantry concrete footing",at+Vector3.up*.12f,new Vector3(1.1f,.24f,.85f),plaster);}
  var letterMat=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Aged ivory painted mining letters.mat");if(!letterMat){letterMat=new Material(Shader.Find("Standard"));letterMat.name="Aged ivory painted mining letters";AssetDatabase.CreateAsset(letterMat,Folder+"/Aged ivory painted mining letters.mat");}letterMat.color=new Color(.74f,.72f,.61f);letterMat.SetFloat("_Metallic",.1f);letterMat.SetFloat("_Glossiness",.16f);letterMat.globalIlluminationFlags=MaterialGlobalIlluminationFlags.BakedEmissive;letterMat.EnableKeyword("_EMISSION");letterMat.SetColor("_EmissionColor",new Color(.075f,.071f,.055f));
  var bundle=JsonUtility.FromJson<Glyphs>(AssetDatabase.LoadAssetAtPath<TextAsset>(Folder+"/Models/MiningSignGlyphs.json").text);if(bundle.glyphs.Length!=12)throw new Exception("Missing English sign glyphs");
  for(int i=0;i<bundle.glyphs.Length;i++){var data=bundle.glyphs[i];var vertices=new Vector3[data.vertices.Length/3];for(int k=0;k<vertices.Length;k++)vertices[k]=new Vector3(data.vertices[k*3],data.vertices[k*3+1],data.vertices[k*3+2]);var mesh=new Mesh{indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};mesh.vertices=vertices;mesh.triangles=data.triangles;mesh.RecalculateNormals();mesh.RecalculateBounds();var t=G(g,"Solid freestanding English metal character "+data.character,a+d*data.offset+Vector3.up*5.12f-normal*.33f);t.rotation=Quaternion.LookRotation(normal,Vector3.up);t.gameObject.AddComponent<MeshFilter>().sharedMesh=Save(mesh,"Extruded English character "+data.character);t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=letterMat;}
  Batch(g);
 }
 static void Batch(Transform root)
 {var rs=root.GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled&&!r.GetComponent<TextMesh>()&&r.sharedMaterial!=glass).ToArray();foreach(var mat in rs.Select(r=>r.sharedMaterial).Distinct()){var parts=rs.Where(r=>r.sharedMaterial==mat).ToArray();var mesh=new Mesh{indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};mesh.CombineMeshes(parts.Select(r=>new CombineInstance{mesh=r.GetComponent<MeshFilter>().sharedMesh,transform=root.worldToLocalMatrix*r.localToWorldMatrix}).ToArray(),true,true);var t=G(root,"Combined editable detail - "+mat.name,Vector3.zero);t.gameObject.AddComponent<MeshFilter>().sharedMesh=Save(mesh,"Combined "+root.name+" "+mat.name);var renderer=t.gameObject.AddComponent<MeshRenderer>();renderer.sharedMaterial=mat;if(mat==wallDirt||mat==groundDirt)renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;foreach(var r in parts)r.enabled=false;}}
 static void FootFace(Transform root,Vector3 start,Vector3 end,Vector3 outward)
 {
  if(Vector3.Distance(start,end)<1)return;var lo=start+outward*.013f+Vector3.up*.009f;var ro=end+outward*.013f+Vector3.up*.009f;
  Quad(root,"Irregular rising moss and rain silt",lo,ro,lo+Vector3.up*.52f,ro+Vector3.up*.52f,wallDirt);
  Quad(root,"Mud and moss washed onto ground joint",lo,ro,lo+outward*.48f,ro+outward*.48f,groundDirt);
 }
 static int Footings(Scene scene,Transform root)
 {
  int count=0;var candidates=All<MeshRenderer>(scene).Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&!r.transform.IsChildOf(root.parent)&&r.bounds.min.y>-.25f&&r.bounds.min.y<.4f&&r.bounds.size.y>2&&Mathf.Max(r.bounds.size.x,r.bounds.size.z)>4&&r.bounds.min.z>-.2f&&r.bounds.min.z<55).ToArray();var used=new HashSet<string>();
  foreach(var r in candidates){string context=(r.name+" "+r.transform.parent.name+" "+(r.transform.parent.parent?r.transform.parent.parent.name:"")).ToLowerInvariant();if(!(context.Contains("wall")||context.Contains("facade")||context.Contains("masonry")||context.Contains("building")||context.Contains("workshop")||context.Contains("hut")))continue;var b=r.bounds;string key=b.center.ToString("F1")+b.size.ToString("F1");if(!used.Add(key))continue;float y=Mathf.Max(0,b.min.y);var p0=new Vector3(b.min.x,y,b.min.z);var p1=new Vector3(b.max.x,y,b.min.z);var p2=new Vector3(b.max.x,y,b.max.z);var p3=new Vector3(b.min.x,y,b.max.z);if(b.size.x>1)FootFace(root,p0,p1,Vector3.back);if(b.size.z>1){FootFace(root,p1,p2,Vector3.right);FootFace(root,p3,p0,Vector3.left);}if(b.size.x>1&&b.size.z>1)FootFace(root,p2,p3,Vector3.forward);count++;}
  FootFace(root,new Vector3(-16.28f,0,2.84f),new Vector3(-4.28f,0,2.84f),Vector3.back);FootFace(root,new Vector3(-16.29f,0,21.58f),new Vector3(-16.29f,0,2.84f),Vector3.left);FootFace(root,new Vector3(-4.27f,0,2.84f),new Vector3(-4.27f,0,21.58f),Vector3.right);FootFace(root,new Vector3(-4.28f,0,21.59f),new Vector3(-16.28f,0,21.59f),Vector3.forward);return count+1;
 }
 static void SuppressArt(Transform t)
 {foreach(var v in t.GetComponentsInChildren<Recoil3DSpriteVisual>(true)){v.suppressed=true;v.Sync();EditorUtility.SetDirty(v);}foreach(var r in t.GetComponentsInChildren<Renderer>(true)){r.enabled=false;EditorUtility.SetDirty(r);}}
 public static string Apply()
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Exit Play before rebuilding");Directory.CreateDirectory(Folder+"/Models");Directory.CreateDirectory("output/service-store-1980");Directory.CreateDirectory("Backups/ServiceStore1980");var prev=SceneManager.GetActiveScene();var s=SceneManager.GetSceneByPath(ScenePath);bool opened=!s.isLoaded;if(opened)s=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
  try{if(All<Transform>(s).Any(t=>t.name==Marker))throw new Exception("Store pass already exists");var backup="Backups/ServiceStore1980/"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+"-RecoilPrototype.unity";EditorSceneManager.SaveScene(s,backup,true);var collisions=All<Collider2D>(s).Select(c=>c.GetInstanceID()+"|"+c.bounds+"|"+c.enabled).ToArray();var doors=All<RecoilDoor>(s).Select(d=>EditorJsonUtility.ToJson(d)+"|"+d.transform.position).ToArray();var spawns=All<Transform>(s).Where(t=>t.name.Contains("Spawn")).Select(t=>t.GetInstanceID()+"|"+t.position).ToArray();var all=All<Transform>(s);var station=all.Single(t=>t.name=="Service Station - editable exterior");Materials();int blocks=0;foreach(var t in all.Where(t=>t.name=="Foreground enclosure remnant")){if(t.GetComponentsInChildren<Collider2D>(true).Length+t.GetComponentsInChildren<Collider>(true).Length!=0)throw new Exception("Unexpected road-block collider");t.gameObject.SetActive(false);EditorUtility.SetDirty(t.gameObject);blocks++;}
   foreach(string name in new[]{"Store facade","Roof fascia","Service roof terrain","Shop sign","Service sign text","Roof skylight label"}){var old=station.Cast<Transform>().FirstOrDefault(t=>t.name==name);if(old)SuppressArt(old);}var windows=station.Find("Wall-mounted windows and poster");if(windows)foreach(Transform child in windows)if(!child.GetComponentInChildren<ReadableProp>(true))SuppressArt(child);var entrance=All<RecoilDoor>(s).Single(d=>d.name=="Service store entrance");SuppressArt(entrance.transform);
   var world=All<RecoilPrototype>(s).Single();var root=G(world.transform,Marker,Vector3.zero);var pavilion=G(root,"Service store 1980 - dimensional masonry glazing fittings and stock",new Vector3(-10.28f,0,2.88f));Pavilion(pavilion);Batch(pavilion);Gantry(root);var footing=G(root,"Near and middle distance building footings - organic moss and silt",Vector3.zero);int footings=Footings(s,footing);Batch(footing);
   if(!collisions.SequenceEqual(All<Collider2D>(s).Select(c=>c.GetInstanceID()+"|"+c.bounds+"|"+c.enabled)))throw new Exception("Collision changed");if(!doors.SequenceEqual(All<RecoilDoor>(s).Select(d=>EditorJsonUtility.ToJson(d)+"|"+d.transform.position)))throw new Exception("Door route changed");if(!spawns.SequenceEqual(All<Transform>(s).Where(t=>t.name.Contains("Spawn")).Select(t=>t.GetInstanceID()+"|"+t.position)))throw new Exception("Spawn changed");if(root.GetComponentsInChildren<Collider2D>(true).Length+root.GetComponentsInChildren<Collider>(true).Length>0)throw new Exception("New scenery blocks route");
   EditorSceneManager.MarkSceneDirty(s);EditorSceneManager.SaveScene(s);AssetDatabase.SaveAssets();string report="PASS removed "+blocks+" indistinct foreground road blocks. Replaced old store extrusion with native masonry/glazed entrance and four glazed display bays, real mullions, sills, fascia, canopy brackets, counter and stocked shelves, roof coping/felt/flue/louvres/downpipes, small shop fixtures. Existing roof top=4.4, original roof skylight kept. Exact 12 independent extruded Latin letters ZHAKOV MINING on 39m open lattice gantry from store Z=3.2 to behind camera Z=-34. "+footings+" near/middle building wall groups treated with rising moss and ground silt. Original Collider2D, door JSON and spawn positions unchanged. Meshy cash register anchor ready. Backup="+backup;File.WriteAllText("output/service-store-1980/verification.txt",report);return report;
  }finally{if(opened)EditorSceneManager.CloseScene(s,true);SceneManager.SetActiveScene(prev);var w=All<Recoil3DWorld>(prev).FirstOrDefault();if(w)w.ApplyAtmosphere();}
 }
}
