using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;

// Copies complete authored scenes, then adds native editable meshes; never regenerates original 2D layouts.
public static class Recoil3DMigration
{
    public const string Root="Assets/Recoil3D";
    static Scene scene;
    static Mesh cube;
    static Material concrete,steel,rust,black,green,skin,coat,glass,emissive,plain;
    static Dictionary<string,Material> textureMaterials=new Dictionary<string,Material>();
    static int converted;
    static Transform Group(string name,Transform parent,Vector3 position)
    {var t=new GameObject(name).transform;SceneManager.MoveGameObjectToScene(t.gameObject,scene);t.SetParent(parent,false);t.localPosition=position;return t;}
    static GameObject Shape(string name,Transform parent,Vector3 position,Vector3 scale,Material material,PrimitiveType type=PrimitiveType.Cube)
    {
        var g=GameObject.CreatePrimitive(type);g.name=name;SceneManager.MoveGameObjectToScene(g,scene);g.transform.SetParent(parent,false);g.transform.localPosition=position;g.transform.localScale=scale;
        UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());var r=g.GetComponent<Renderer>();r.sharedMaterial=material;r.shadowCastingMode=scale.sqrMagnitude>.15f?ShadowCastingMode.On:ShadowCastingMode.Off;
        return g;
    }
    static void Rod(string name,Transform parent,Vector3 a,Vector3 b,float radius,Material material)
    {var g=Shape(name,parent,(a+b)*.5f,new Vector3(radius*2,(a-b).magnitude*.5f,radius*2),material,PrimitiveType.Cylinder);g.transform.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);}
    static Material Mat(string name,Color c,float metal=.15f,float smooth=.25f)
    {
        string path=Root+"/Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){m=new Material(Shader.Find("Recoil/3D/Weathered Surface"));AssetDatabase.CreateAsset(m,path);}
        m.color=c;m.SetFloat("_Metallic",metal);m.SetFloat("_Glossiness",smooth);m.enableInstancing=true;EditorUtility.SetDirty(m);return m;
    }
    static void Materials()
    {
        concrete=Mat("Pitted concrete",new Color(.3f,.34f,.34f));steel=Mat("Worn industrial steel",new Color(.21f,.27f,.27f),.55f);rust=Mat("Flaking oxidised metal",new Color(.3f,.17f,.105f),.35f);black=Mat("Near silhouette",new Color(.045f,.055f,.06f));green=Mat("Faded machine enamel",new Color(.19f,.27f,.23f),.35f);skin=Mat("Pale skin",new Color(.54f,.48f,.41f));coat=Mat("Detective coat",new Color(.33f,.26f,.15f));glass=Mat("Soot covered glass",new Color(.15f,.22f,.24f),.3f,.65f);emissive=Mat("Work light glass",new Color(.75f,.83f,.76f));emissive.SetColor("_EmissionColor",new Color(.7f,.85f,.8f));plain=Mat("Sprite state mesh",Color.white);
        var temp=GameObject.CreatePrimitive(PrimitiveType.Cube);cube=temp.GetComponent<MeshFilter>().sharedMesh;UnityEngine.Object.DestroyImmediate(temp);
    }
    static Material SpriteMaterial(SpriteRenderer s)
    {
        if(!s.sprite || s.sprite.texture.width<=4 || s.sprite.name.Contains("Square"))return plain;
        string id=s.sprite.GetInstanceID().ToString();Material found;if(textureMaterials.TryGetValue(id,out found))return found;
        string path=Root+"/Materials/Sprite_"+s.sprite.GetInstanceID()+".mat";
        // Store stable names using the original asset GUID and sprite rectangle.
        path=Root+"/Materials/Surface_"+AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(s.sprite))+"_"+s.sprite.rect.x+"_"+s.sprite.rect.y+".mat";
        found=AssetDatabase.LoadAssetAtPath<Material>(path);if(!found){found=new Material(plain);AssetDatabase.CreateAsset(found,path);}
        found.mainTexture=s.sprite.texture;var rect=s.sprite.textureRect;found.mainTextureScale=new Vector2(rect.width/s.sprite.texture.width,rect.height/s.sprite.texture.height);found.mainTextureOffset=new Vector2(rect.x/s.sprite.texture.width,rect.y/s.sprite.texture.height);textureMaterials[id]=found;return found;
    }
    static bool BackgroundName(string n){return n.Contains("background")||n.Contains("backing")||n.Contains("back wall")||n.Contains("facade")||n.Contains("window")||n.Contains("mullion")||n.Contains("wall seam");}
    static bool InvisibleSource(SpriteRenderer s)
    {return s.name.Contains("Darkness") || s.name.Contains("shadow mesh") || s.GetComponent<Recoil3DActorVisual>();}
    static void ConvertSprite(SpriteRenderer s)
    {
        if(s.GetComponent<Recoil3DSpriteVisual>() || InvisibleSource(s))return;
        string n=s.name.ToLowerInvariant();var b=s.bounds;
        var g=new GameObject("3D mesh - "+s.name);g.transform.SetParent(s.transform,false);g.AddComponent<MeshFilter>().sharedMesh=cube;
        var r=g.AddComponent<MeshRenderer>();r.sharedMaterial=SpriteMaterial(s);r.shadowCastingMode=b.size.magnitude>1?ShadowCastingMode.On:ShadowCastingMode.Off;
        var v=s.gameObject.AddComponent<Recoil3DSpriteVisual>();v.source=s;v.meshRenderer=r;v.depth=.24f;v.depthOffset=s.sortingOrder<0?3:0;
        var terrain=s.GetComponent<BoxCollider2D>();
        if(terrain && !terrain.isTrigger){v.depth=b.size.x>b.size.y?12:6;v.depthOffset=3;}
        if(s.GetComponent<SoftRoofPlatform>()){v.depth=3;v.depthOffset=.8f;}
        if(BackgroundName(n)){v.depth=.45f;v.depthOffset=9-Mathf.Clamp(s.sortingOrder,-12,10)*.045f;}
        else if(n.Contains("wall") && !terrain){v.depth=.4f;v.depthOffset=7;}
        bool fixture=n.Contains("lamp")||n.Contains("fixture")||n.Contains("indicator")||n.Contains("status")||n.Contains("lens");
        if((n.Contains("ceiling")||n.Contains("floor")) && !fixture){v.depth=14;v.depthOffset=4;}
        if(s.GetComponentInParent<ContinuousInteractionItem>() || s.GetComponentInParent<ReadableProp>()){v.depth=.18f;v.depthOffset=-.3f;}
        if(s.GetComponentInParent<RecoilDoor>()){v.depth=.18f;v.depthOffset=2-s.sortingOrder*.006f;}
        if(s.GetComponentInParent<ClimbableLadder>()){v.depth=.16f;v.depthOffset=.35f;}
        if(n.Contains("wheel")||n.Contains("valve")||n.Contains("hub")){v.depth=.4f;}
        if(fixture && Mathf.Max(b.size.x,b.size.y)<2 && !n.Contains("pole")){v.depth=Mathf.Clamp(Mathf.Min(b.size.x,b.size.y)*.45f,.035f,.12f);if(n.Contains("ceiling"))v.depthOffset=0;}
        v.Sync();converted++;
    }
    static Transform Joint(string name,Transform parent,Vector3 position){return Group(name,parent,position);}
    static Transform Limb(string name,Transform parent,Vector3 hip,float length,Material material)
    {var pivot=Joint(name,parent,hip);Shape(name+" mesh",pivot,new Vector3(0,-length*.5f,0),new Vector3(.13f,length,.15f),material);return pivot;}
    static Recoil3DActorVisual Actor(GameObject actor,bool enemy,bool isChild=false)
    {
        var visual=actor.GetComponent<Recoil3DActorVisual>();if(visual)return visual;
        if(!enemy && actor.GetComponent<RecoilPlayerController>() && !actor.GetComponent<PlayerAimLocomotion>())actor.AddComponent<PlayerAimLocomotion>();
        visual=actor.AddComponent<Recoil3DActorVisual>();visual.monster=enemy;visual.child=isChild;visual.visibilitySource=actor.GetComponent<SpriteRenderer>();
        var root=Group("3D character - editable model",actor.transform,Vector3.zero);visual.model=root;root.localRotation=Quaternion.Euler(0,enemy?90:-90,0);
        // Author in world-sized local units even if the original sprite root was scaled.
        var lossy=actor.transform.lossyScale;root.localScale=new Vector3(1/Mathf.Max(.01f,Mathf.Abs(lossy.x)),1/Mathf.Max(.01f,Mathf.Abs(lossy.y)),1/Mathf.Max(.01f,Mathf.Abs(lossy.z)));
        float h=isChild?1.08f:1.65f;float scale=h/1.65f;var body=Group("Body dimensions",root,Vector3.zero);body.localScale=Vector3.one*scale;
        var fabric=enemy?green:coat;
        Shape("Torso",body,new Vector3(0,.06f,0),new Vector3(.43f,.325f,.29f),fabric,PrimitiveType.Capsule);
        if(!enemy && !isChild)Shape("Long coat hem",body,new Vector3(0,-.25f,0),new Vector3(.46f,.5f,.32f),coat);
        visual.head=Joint("Head pivot",body,new Vector3(0,.5f,0));Shape("Head",visual.head,new Vector3(0,.12f,0),new Vector3(.24f,.32f,.26f),skin,PrimitiveType.Sphere);
        Shape("Nose",visual.head,new Vector3(0,.12f,-.135f),new Vector3(.08f,.08f,.09f),skin);
        if(!enemy && !isChild){Shape("Hat crown",visual.head,new Vector3(0,.28f,0),new Vector3(.29f,.14f,.29f),black);Shape("Hat brim",visual.head,new Vector3(0,.23f,-.02f),new Vector3(.37f,.035f,.37f),black);}
        visual.leftLeg=Limb("Left leg",body,new Vector3(-.105f,-.28f,0),.52f,black);visual.rightLeg=Limb("Right leg",body,new Vector3(.105f,-.28f,0),.52f,black);
        visual.leftArm=Limb("Left arm",body,new Vector3(-.27f,.32f,0),.54f,fabric);visual.rightArm=Limb("Right arm",body,new Vector3(.27f,.32f,0),.54f,fabric);
        Shape("Left hand",visual.leftArm,new Vector3(0,-.53f,0),new Vector3(.12f,.15f,.12f),skin);Shape("Right hand",visual.rightArm,new Vector3(0,-.53f,0),new Vector3(.12f,.15f,.12f),skin);
        if(enemy)visual.eye=Shape("Red eye",visual.head,new Vector3(-.05f,.16f,-.135f),new Vector3(.04f,.025f,.015f),emissive).GetComponent<Renderer>();
        visual.parts=root.GetComponentsInChildren<Renderer>();
        if(enemy)Recoil3DMonsterWorkwear.Dress(visual);
        else if(actor.GetComponent<RecoilPlayerController>())Recoil3DPlayerLegRig.Configure(visual);
        foreach(var s in actor.GetComponentsInChildren<SpriteRenderer>(true))
        {
            s.forceRenderingOff=true;
            if(s.name.ToLower().Contains("gun"))ConvertSprite(s);
        }
        return visual;
    }
    static Light Lamp(string name,Transform parent,Vector3 p,Color colour,float intensity,float range,LightType type=LightType.Point)
    {
        var root=Group(name,parent,p);
        var light=Group("Light - child of fixture",root,new Vector3(0,-.12f,-.08f)).gameObject.AddComponent<Light>();light.type=type;light.color=colour;light.intensity=intensity;light.range=range;light.spotAngle=85;light.transform.localRotation=Quaternion.Euler(90,0,0);light.shadows=LightShadows.None;return light;
    }
    static void Lighting(RecoilPrototype world,Camera camera,bool outdoor)
    {
        var group=Group("3D lighting - source fixtures own lights",world.transform,Vector3.zero);
        var fill=Group("Moonlight" ,group,Vector3.zero).gameObject.AddComponent<Light>();fill.type=LightType.Directional;fill.intensity=outdoor?.65f:.24f;fill.color=new Color(.66f,.76f,.8f);fill.transform.rotation=Quaternion.Euler(34,-25,0);fill.shadows=LightShadows.Soft;fill.shadowStrength=.7f;fill.shadowBias=.035f;
        foreach(var original in world.GetComponentsInChildren<RecoilLight2D>(true))
        {
            var industrial=original as RecoilIndustrialLamp;
            var t=Group("3D light - source owned",original.transform,new Vector3(0,0,-.6f));var l=t.gameObject.AddComponent<Light>();l.type=industrial?LightType.Spot:LightType.Point;l.range=industrial?industrial.coneLength+2:Mathf.Max(3,original.radius*1.6f);l.intensity=original.intensity*3.5f;l.color=new Color(.72f,.81f,.73f);l.spotAngle=industrial?Mathf.Atan2(industrial.coneHalfWidth,industrial.coneLength)*Mathf.Rad2Deg*2+15:85;l.transform.rotation=Quaternion.Euler(90,0,0);
            var adapter=original.gameObject.AddComponent<Recoil3DLight>();adapter.source=original;adapter.lightSource=l;
        }
        foreach(var original in world.GetComponentsInChildren<RecoilStreetLamp>(true))
        {
            var t=Group("3D spotlight - child of lamp head",original.lampHead?original.lampHead:original.transform,new Vector3(0,-.05f,-.3f));var l=t.gameObject.AddComponent<Light>();l.type=LightType.Spot;l.range=original.coneLength+2;l.spotAngle=Mathf.Atan2(original.coneHalfWidth,original.coneLength)*Mathf.Rad2Deg*2+12;l.transform.rotation=Quaternion.Euler(90,0,0);l.shadows=LightShadows.Soft;
            var adapter=original.gameObject.AddComponent<Recoil3DLight>();adapter.streetLamp=original;adapter.lightSource=l;adapter.intensityMultiplier=2.2f;
        }
        var presentation=world.gameObject.AddComponent<Recoil3DWorld>();presentation.world=world;
        presentation.playerVisualLight=Lamp("Player visual fill - excluded from perception",group,new Vector3(0,1,-2),new Color(.56f,.67f,.74f),.65f,5);
        var alarms=new List<Light>();
        if(!outdoor)for(int x=3;x<40;x+=9)alarms.Add(Lamp("Emergency beacon "+x,group,new Vector3(x,3.2f,5),Color.red,0,8));
        presentation.emergencyLights=alarms.ToArray();
        RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=outdoor?new Color(.2f,.23f,.25f):new Color(.12f,.14f,.145f);
        RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogColor=new Color(.13f,.16f,.17f);RenderSettings.fogDensity=outdoor?.012f:.019f;RenderSettings.skybox=null;
        camera.backgroundColor=RenderSettings.fogColor*.62f;camera.clearFlags=CameraClearFlags.SolidColor;camera.allowHDR=true;camera.farClipPlane=180;
        var follow=camera.GetComponent<RecoilCameraController>();follow.perspective3D=true;follow.cameraDepth=-24;camera.transform.rotation=Quaternion.Euler(5,-5,0);camera.orthographic=false;camera.fieldOfView=2*Mathf.Atan(follow.viewSize/24)*Mathf.Rad2Deg;
        var waves=world.GetComponentInChildren<SoundWaveManager>(true);var fx=camera.GetComponent<BroadcastRefractionEffect>();if(!fx)fx=camera.gameObject.AddComponent<BroadcastRefractionEffect>();fx.broadcast=waves;fx.refractionShader=Shader.Find("Recoil/Effects/Broadcast Refraction");if(!fx.refractionShader)fx.refractionShader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/RecoilPrototype/Effects/BroadcastRefraction.shader");fx.perspectiveReferenceDistance=24;
        QualitySettings.pixelLightCount=8;QualitySettings.shadowDistance=45;
    }
    static void Machinery(Transform parent,Vector3 position,int index)
    {
        var machine=Group("Operating process unit "+index,parent,position);
        Shape("Concrete foundation",machine,new Vector3(0,.22f,0),new Vector3(3.4f,.44f,2.6f),concrete);
        Shape("Motor enclosure",machine,new Vector3(0,1.25f,0),new Vector3(2.2f,1.8f,1.8f),green);
        Shape("Pressure vessel",machine,new Vector3(.5f,3,1),new Vector3(1.5f,1.8f,1.5f),steel,PrimitiveType.Cylinder);
        for(int n=0;n<3;n++)Shape("Vessel band",machine,new Vector3(.5f,1.7f+n*1.2f,1),new Vector3(1.58f,.035f,1.58f),rust,PrimitiveType.Cylinder);
        for(int n=0;n<8;n++)Shape("Cooling fin",machine,new Vector3(-.95f+n*.24f,1.3f,-.94f),new Vector3(.07f,1.4f,.12f),black);
        Rod("Overhead outlet",machine,new Vector3(.5f,4.7f,1),new Vector3(.5f,5.7f,1),.13f,rust);
        Rod("Feed pipe",machine,new Vector3(-1.4f,.7f,-.2f),new Vector3(-2.5f,.7f,-.2f),.11f,steel);
        var valve=Shape("Valve wheel",machine,new Vector3(-1.8f,.7f,-.3f),new Vector3(.54f,.1f,.54f),rust,PrimitiveType.Cylinder);valve.transform.localRotation=Quaternion.Euler(90,0,0);
        for(int n=0;n<6;n++)Shape("Fastener",machine,new Vector3(-.9f+n*.36f,.47f,-1.02f),new Vector3(.07f,.07f,.07f),steel,PrimitiveType.Sphere);
        Shape("Gauge face",machine,new Vector3(.72f,1.7f,-.94f),new Vector3(.23f,.23f,.05f),emissive);
        Shape("Control box",machine,new Vector3(1.2f,1.1f,-.5f),new Vector3(.45f,.6f,.24f),steel);
        Shape("Working green lamp",machine,new Vector3(1.2f,1.25f,-.64f),new Vector3(.08f,.08f,.03f),emissive);
        var rotor=Shape("Slowly rotating drive",machine,new Vector3(-.6f,1.2f,-1.1f),new Vector3(.75f,.12f,.75f),black,PrimitiveType.Cylinder);rotor.transform.localRotation=Quaternion.Euler(90,0,0);rotor.AddComponent<Recoil3DMachineMotion>().degreesPerSecond=8+index;
    }
    static void InteriorDepth(RecoilPrototype world,bool factory,bool warehouse)
    {
        var floors=world.GetComponentsInChildren<BoxCollider2D>(true).Where(c=>!c.isTrigger && c.bounds.size.x>5 && c.bounds.size.y<1.6f).ToArray();
        if(floors.Length==0)return;float left=floors.Min(c=>c.bounds.min.x),right=floors.Max(c=>c.bounds.max.x),top=factory?20:warehouse?7.5f:5;
        var set=Group("3D depth set - foreground midground distant",world.transform,Vector3.zero);
        var rear=Group("Distant architecture Z 15 to 24",set,Vector3.zero);var mid=Group("Background work area Z 4 to 12",set,Vector3.zero);var front=Group("Foreground silhouettes Z minus 5",set,Vector3.zero);
        Shape("Rear loadbearing wall",rear,new Vector3((left+right)*.5f,top*.5f,22),new Vector3(right-left,top,.55f),concrete);
        Shape("Floor into depth",rear,new Vector3((left+right)*.5f,-.25f,13),new Vector3(right-left,.5f,19),concrete);
        for(float x=left+2;x<right;x+=6)
        {
            Shape("Rear column",rear,new Vector3(x,top*.5f,19),new Vector3(.45f,top,.6f),steel);
            Shape("Roof girder",rear,new Vector3(x,top-.3f,9),new Vector3(.3f,.4f,25),black);
            for(float y=3;y<top;y+=4)
            {
                // Underground drains have sealed concrete walls, never facade windows.
                if(!world.gameObject.scene.path.EndsWith("/RecoilFactorySewer.unity"))
                {
                Shape("High wire glass window",rear,new Vector3(x+1.7f,y,21.6f),new Vector3(2.6f,1.7f,.1f),glass);
                for(int n=0;n<4;n++)Shape("Window mullion",rear,new Vector3(x+.5f+n*.8f,y,21.5f),new Vector3(.04f,1.7f,.07f),steel);
                }
                Shape("Damp patch",rear,new Vector3(x-.3f,y-1.3f,21.65f),new Vector3(.9f,1.3f,.03f),black);
            }
            Rod("Ceiling service main",mid,new Vector3(x,top-.7f,5),new Vector3(x+5.9f,top-.7f,5),.095f,rust);
            Rod("Small cable tray",mid,new Vector3(x,top-1.1f,7),new Vector3(x+5.9f,top-1.1f,7),.035f,black);
            if(factory && x<right-3)Machinery(mid,new Vector3(x+1,0,7+(int)x%3),Mathf.RoundToInt(x));
            Lamp("Rear working lamp",rear,new Vector3(x+1,Mathf.Min(top-.7f,5),12),new Color(.66f,.78f,.72f),1.2f,7,LightType.Spot);
            if(((int)x)%3==0)Shape("Near structural silhouette",front,new Vector3(x,top*.5f,-4),new Vector3(.16f,top,.22f),black);
            for(int n=0;n<3;n++)Shape("Foreground abandoned drum",front,new Vector3(x+n*.6f,.35f,-5.5f),new Vector3(.45f,.35f,.45f),black,PrimitiveType.Cylinder);
        }
        foreach(var floor in floors.Where(c=>c.bounds.max.y>2))
        {var b=floor.bounds;Shape("Upper floor extends into office depth",rear,new Vector3(b.center.x,b.center.y,14),new Vector3(b.size.x,b.size.y,14),concrete);}
        foreach(var stair in world.GetComponentsInChildren<WalkableStairway>(true))
        {
            if(stair.routePoints==null)continue;
            var g=Group("3D stairs - "+stair.name,set,Vector3.zero);
            for(int n=1;n<stair.routePoints.Length;n++)
            {
                Vector3 a=stair.routePoints[n-1].position,b=stair.routePoints[n].position;
                int count=Mathf.Max(2,Mathf.CeilToInt(Vector3.Distance(a,b)/.27f));
                for(int k=0;k<=count;k++){var p=Vector3.Lerp(a,b,k/(float)count);p.z=.15f;Shape("Steel stair tread",g,p,new Vector3(.32f,.08f,1.7f),steel);}
                Rod("Stair back handrail",g,a+new Vector3(0,1,1),b+new Vector3(0,1,1),.035f,rust);
            }
        }
    }
    static void SolidBox(string name,Transform parent,Vector3 centre,Vector3 size,Material material)
    {
        var g=Shape(name,parent,centre,size,material);var collider=g.AddComponent<BoxCollider2D>();collider.size=Vector2.one;
    }
    static void OutdoorChanges(RecoilPrototype world)
    {
        var additions=Group("3D factory yard - editable perimeter truck and gate",world.transform,Vector3.zero);
        var truck=world.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Truck - soft canvas roof");
        var leftDoor=world.GetComponentsInChildren<RecoilDoor>(true).First(d=>d.requiredItemId=="employee_access_card");var rightDoor=world.GetComponentsInChildren<RecoilDoor>(true).First(d=>d.name=="Factory right service door");
        float centre=(leftDoor.transform.position.x+rightDoor.transform.position.x)*.5f;truck.position+=Vector3.right*(centre-truck.position.x);
        // The cab and cargo body now physically block ground traversal; loading pallets form a jump route.
        SolidBox("Truck solid cargo body",truck,new Vector3(-.5f,.85f,0),new Vector3(5.8f,1.7f,2.6f),green);
        SolidBox("Truck solid cab",truck,new Vector3(3.05f,.95f,0),new Vector3(1.85f,1.9f,2.4f),steel);
        for(int side=-1;side<=1;side+=2)for(int k=0;k<4;k++)
        {float x=centre+side*(5.7f-k*.6f);float h=.6f*(k+1);SolidBox("Loading pallet step "+side+" "+k,additions,new Vector3(x,h*.5f,0),new Vector3(.8f,h,1.8f),rust);}
        var wall=world.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Factory right perimeter wall");float gateX=wall.position.x;
        foreach(var old in wall.GetComponentsInChildren<Recoil3DSpriteVisual>(true)){old.suppressed=true;old.Sync();}
        var gate=Group("Heavy barred factory main gate - chained",additions,new Vector3(gateX,0,0));
        for(int side=-1;side<=1;side+=2)
        {
            Shape("Concrete gate pier",gate,new Vector3(side*2.8f,3.8f,side*8),new Vector3(.9f,7.6f,.9f),concrete);
            for(int n=0;n<10;n++)
            {float z=side*(n*.8f+.4f);float x=z*.32f;Rod("Forged iron gate bar",gate,new Vector3(x,.15f,z),new Vector3(x,6.7f,z),.085f,black);}
            Rod("Heavy gate upper rail",gate,new Vector3(0,6.4f,0),new Vector3(side*2.6f,6.4f,side*8),.12f,steel);
            Rod("Heavy gate lower rail",gate,new Vector3(0,.45f,0),new Vector3(side*2.6f,.45f,side*8),.12f,steel);
        }
        var links=Group("Thick security chain and lock",gate,new Vector3(0,2.4f,-.12f));
        for(int n=0;n<16;n++)
        {float z=-1.6f+n*.21f,x=z*.32f;var ring=Group("Forged chain link "+n,links,new Vector3(x,Mathf.Sin(n*.65f)*.05f,z));
         for(int k=0;k<8;k++){float a=k*Mathf.PI/4,b=(k+1)*Mathf.PI/4;Rod("Link segment",ring,new Vector3(Mathf.Cos(a)*.15f,Mathf.Sin(a)*.09f,0),new Vector3(Mathf.Cos(b)*.15f,Mathf.Sin(b)*.09f,0),.028f,rust);}ring.localRotation=Quaternion.Euler(0,n%2==0?0:90,0);}
        Shape("Heavy padlock",links,new Vector3(0,-.22f,-.08f),new Vector3(.34f,.42f,.2f),steel);
        // Existing left-side wall is the enclosure edge, extended continuously through depth.
        var leftWall=world.GetComponentsInChildren<BoxCollider2D>(true).Where(c=>c.bounds.size.x<1 && c.bounds.size.y>3 && c.bounds.center.x>0 && c.bounds.center.x<leftDoor.transform.position.x).OrderBy(c=>Mathf.Abs(c.bounds.center.x-10)).First();
        float wallX=leftWall.bounds.center.x,wallH=leftWall.bounds.max.y;
        Shape("Left perimeter wall foreground to background",additions,new Vector3(wallX,wallH*.5f,8),new Vector3(.7f,wallH,36),concrete);
        Shape("Left perimeter coping",additions,new Vector3(wallX,wallH+.12f,8),new Vector3(.9f,.24f,36),steel);
        Shape("Rear enclosure wall",additions,new Vector3((wallX+gateX)*.5f,2,26),new Vector3(gateX-wallX,4,.7f),concrete);
        for(float x=wallX+2;x<gateX;x+=7)
        {Shape("Rear perimeter pier",additions,new Vector3(x,2.3f,25.6f),new Vector3(.55f,4.6f,.85f),steel);Shape("Foreground enclosure remnant",additions,new Vector3(x,.45f,-9),new Vector3(3,.9f,.6f),black);}
        var far=Group("Distant industrial town Z 35 to 70",additions,Vector3.zero);
        for(int n=0;n<18;n++)
        {float x=-50+n*10,h=7+(n*7)%13,z=42+n%3*8;Shape("Distant mill block "+n,far,new Vector3(x,h*.5f,z),new Vector3(7,h,7),steel);if(n%3==0)Shape("Chimney",far,new Vector3(x+1,h+4,z),new Vector3(1,8,1),black,PrimitiveType.Cylinder);}
        // Building depth and rear side walls; old facade details remain preserved as editable meshes.
        var facade=world.GetComponentsInChildren<SpriteRenderer>(true).First(t=>t.name=="Factory main facade").bounds;
        Shape("Factory left return wall into Z",additions,new Vector3(facade.min.x,facade.center.y,15),new Vector3(.65f,facade.size.y,12),concrete);
        Shape("Factory right return wall into Z",additions,new Vector3(facade.max.x,facade.center.y,15),new Vector3(.65f,facade.size.y,12),concrete);
        Shape("Factory roof with depth",additions,new Vector3(facade.center.x,facade.max.y,14),new Vector3(facade.size.x,.3f,14),black);
    }
    [MenuItem("Recoil/3D/Migrate All Gameplay Scenes (Preserve 2D)")]
    public static string Build()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play first.");
        var original=SceneManager.GetActiveScene();var entries=EditorBuildSettings.scenes.Where(s=>s.enabled && !s.path.StartsWith(Recoil3DScene.Folder)).ToArray();
        if(entries.Length==0)throw new InvalidOperationException("Original gameplay scenes missing from build list.");
        foreach(var e in entries)if(File.Exists(Recoil3DScene.ConvertPath(e.path)))throw new InvalidOperationException("3D scene already exists: "+e.path+". Edit it instead of regenerating.");
        Directory.CreateDirectory(Root+"/Materials");Directory.CreateDirectory("Assets/Scenes/3D/FactoryRooms");AssetDatabase.Refresh();Materials();converted=0;
        var summary=new List<string>();
        foreach(var entry in entries)
        {
            string destination=Recoil3DScene.ConvertPath(entry.path);
            var source=SceneManager.GetSceneByPath(entry.path);bool dirty=source.isLoaded && source.isDirty;
            if(dirty){if(!EditorSceneManager.SaveScene(source,destination,true))throw new Exception("Could not snapshot unsaved scene.");}
            else if(!AssetDatabase.CopyAsset(entry.path,destination))throw new Exception("Could not copy "+entry.path);
            AssetDatabase.ImportAsset(destination);scene=EditorSceneManager.OpenScene(destination,OpenSceneMode.Additive);SceneManager.SetActiveScene(scene);
            try
            {
                var world=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<RecoilPrototype>(true)).Single();world.gameObject.AddComponent<Recoil3DScene>().originalScene=entry.path;
                var camera=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Camera>(true)).First();
                foreach(var d in world.GetComponentsInChildren<RecoilDoor>(true))if(!string.IsNullOrEmpty(d.destinationScene))d.destinationScene=Recoil3DScene.ConvertPath(d.destinationScene);
                foreach(var p in world.GetComponentsInChildren<RecoilPlayerController>(true))Actor(p.gameObject,false,p.GetComponent<RecoilPlayerBodyProfile>()!=null);
                foreach(var p in world.GetComponentsInChildren<RampagerController>(true))Actor(p.gameObject,true);
                foreach(var p in world.GetComponentsInChildren<EscortChild>(true))Actor(p.gameObject,false,true);
                foreach(var s in world.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    if(s.GetComponentInParent<Recoil3DActorVisual>())continue;
                    if(s.name.Contains("Darkness")){s.forceRenderingOff=true;s.enabled=false;continue;}
                    ConvertSprite(s);
                    var bridge=s.GetComponent<Recoil3DSpriteVisual>();
                    if(bridge && (s.name.ToLowerInvariant().Contains("back wall")||s.name.ToLowerInvariant().Contains("background")) && s.bounds.size.x>5){bridge.suppressed=true;bridge.Sync();}
                }
                foreach(var light in world.GetComponentsInChildren<RecoilLighting2D>(true))light.enabled=false;
                foreach(var shadow in world.GetComponentsInChildren<RecoilSightOcclusion>(true))shadow.enabled=false;
                bool outside=entry.path.EndsWith("RecoilPrototype.unity");Lighting(world,camera,outside);
                if(outside)OutdoorChanges(world);else InteriorDepth(world,entry.path.EndsWith("RecoilFactoryInterior.unity"),entry.path.EndsWith("RecoilFactoryWarehouse.unity"));
                Physics2D.SyncTransforms();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
                File.WriteAllText("Documents/3D-migration-progress.txt",destination);
                summary.Add(destination+" | meshes "+world.GetComponentsInChildren<MeshRenderer>(true).Length+" | actors "+world.GetComponentsInChildren<RampagerController>(true).Length+" | doors "+world.GetComponentsInChildren<RecoilDoor>(true).Length);
            }
            finally{EditorSceneManager.CloseScene(scene,true);}
        }
        // Dynamic escort scene travel uses a separate 3D resource prefab.
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/RecoilPrototype/Resources/RecoilEscortChild.prefab");
        if(prefab){scene=EditorSceneManager.NewPreviewScene();var actor=UnityEngine.Object.Instantiate(prefab);SceneManager.MoveGameObjectToScene(actor,scene);Actor(actor,false,true);PrefabUtility.SaveAsPrefabAsset(actor,"Assets/RecoilPrototype/Resources/Recoil3DEscortChild.prefab");UnityEngine.Object.DestroyImmediate(actor);EditorSceneManager.ClosePreviewScene(scene);}
        EditorBuildSettings.scenes=entries.Select(e=>new EditorBuildSettingsScene(Recoil3DScene.ConvertPath(e.path),true)).Concat(entries.Select(e=>new EditorBuildSettingsScene(e.path,false))).ToArray();
        AssetDatabase.SaveAssets();SceneManager.SetActiveScene(original);
        Directory.CreateDirectory("Documents");File.WriteAllLines("Documents/3D-Migration-Manifest.txt",summary);
        return "MIGRATED "+entries.Length+" complete scenes; "+converted+" source sprite surfaces converted. Originals retained.";
    }
    public static string CreateComponentPrefabs()
    {
        if(EditorApplication.isPlaying)throw new Exception("Exit Play first.");
        System.IO.Directory.CreateDirectory(Root+"/Prefabs");AssetDatabase.Refresh();Materials();int count=0;
        foreach(string path in new[]{"Assets/RecoilPrototype/Prefabs/KBKStackerCrane.prefab","Assets/RecoilPrototype/Prefabs/WalkableSwitchbackStairway.prefab","Assets/RecoilPrototype/Prefabs/SwingingChain.prefab","Assets/RecoilPrototype/Prefabs/SurfaceCrawler.prefab"})
        {
            var root=PrefabUtility.LoadPrefabContents(path);scene=root.scene;
            try
            {
                foreach(var sprite in root.GetComponentsInChildren<SpriteRenderer>(true))ConvertSprite(sprite);
                foreach(var light in root.GetComponentsInChildren<RecoilLight2D>(true))
                {
                    if(light.GetComponent<Recoil3DLight>())continue;
                    var child=Group("3D component light",light.transform,new Vector3(0,0,-.3f));var native=child.gameObject.AddComponent<Light>();native.type=LightType.Point;native.range=Mathf.Max(3,light.radius);native.intensity=light.intensity*3.5f;
                    var adapter=light.gameObject.AddComponent<Recoil3DLight>();adapter.source=light;adapter.lightSource=native;
                }
                PrefabUtility.SaveAsPrefabAsset(root,Root+"/Prefabs/"+System.IO.Path.GetFileName(path));count++;
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        var childPath="Assets/RecoilPrototype/Resources/Recoil3DEscortChild.prefab";var escort=PrefabUtility.LoadPrefabContents(childPath);
        try{var body=escort.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Torso");var size=body.localScale;size.y=.325f;body.localScale=size;PrefabUtility.SaveAsPrefabAsset(escort,childPath);}finally{PrefabUtility.UnloadPrefabContents(escort);}
        AssetDatabase.SaveAssets();return count+" reusable 3D component prefabs created; original 2D prefabs retained.";
    }

}
