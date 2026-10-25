using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

// A deliberate visual pass over saved 3D scenes, never over the retained 2D campaign.
public static class Recoil3DDepthRevision
{
    const string Pass="Depth revision - continuous barriers and selective light";
    static Material Mat(string n){return AssetDatabase.LoadAssetAtPath<Material>("Assets/Recoil3D/Materials/"+n+".mat");}
    static Material Custom(string n,Color c,Color emission)
    {
        string path="Assets/Recoil3D/Materials/"+n+".mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Recoil/3D/Weathered Surface"));AssetDatabase.CreateAsset(m,path);}
        m.color=c;m.SetColor("_EmissionColor",emission);m.SetColor("_DepthHazeColour",new Color(.012f,.016f,.018f));m.SetFloat("_HazeDensity",0);m.enableInstancing=true;EditorUtility.SetDirty(m);return m;
    }
    static Transform Group(string n,Transform p,Vector3 pos)
    {var t=new GameObject(n).transform;t.SetParent(p,false);t.localPosition=pos;return t;}
    static GameObject Box(string n,Transform p,Vector3 pos,Vector3 size,string mat="Worn industrial steel",PrimitiveType type=PrimitiveType.Cube)
    {
        var g=GameObject.CreatePrimitive(type);g.name=n;g.transform.SetParent(p,false);g.transform.localPosition=pos;g.transform.localScale=size;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());g.GetComponent<Renderer>().sharedMaterial=Mat(mat);g.isStatic=true;return g;
    }
    static void Rod(string n,Transform p,Vector3 a,Vector3 b,float r,string mat="Worn industrial steel")
    {var g=Box(n,p,(a+b)*.5f,new Vector3(r*2,(b-a).magnitude*.5f,r*2),mat,PrimitiveType.Cylinder);g.transform.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);}
    static void LightAt(Transform parent,Vector3 p,Color c,float intensity,float range,bool spot=false)
    {var l=Group("Light - owned by fixture",parent,p).gameObject.AddComponent<Light>();l.type=spot?LightType.Spot:LightType.Point;l.color=c;l.intensity=intensity;l.range=range;l.spotAngle=65;l.transform.localRotation=Quaternion.Euler(90,0,0);l.shadows=LightShadows.None;}
    static void Window(Transform p,Vector3 pos,Vector2 size,int panes)
    {
        var t=Group("Isolated lit window",p,pos);Box("Dirty illuminated glass",t,Vector3.zero,new Vector3(size.x,size.y,.08f),"Distant window glow");
        for(int i=0;i<=panes;i++)Box("Window vertical glazing bar",t,new Vector3(-size.x*.5f+size.x*i/panes,0,-.06f),new Vector3(.055f,size.y,.1f),"Near silhouette");
        Box("Window crossbar",t,new Vector3(0,0,-.07f),new Vector3(size.x,.055f,.1f),"Near silhouette");
        LightAt(t,new Vector3(0,0,-.5f),new Color(.55f,.65f,.68f),.35f,4);
    }
    static void Machine(Transform p,Vector3 pos,float s,int index)
    {
        var t=Group("Process train - repeated scale reference "+index,p,pos);t.localScale=Vector3.one*s;
        Box("Mounting plinth",t,new Vector3(0,.18f,0),new Vector3(3,.36f,2.4f),"Pitted concrete");
        Box("Motor shell",t,new Vector3(-.4f,1.1f,0),new Vector3(1.9f,1.6f,1.7f),"Faded machine enamel");
        Box("Separator vessel",t,new Vector3(.6f,2.4f,.5f),new Vector3(1.4f,2.1f,1.4f),"Worn industrial steel",PrimitiveType.Cylinder);
        for(int i=0;i<3;i++)Box("Vessel stiffening band",t,new Vector3(.6f,.8f+i*1.5f,.5f),new Vector3(1.48f,.035f,1.48f),"Flaking oxidised metal",PrimitiveType.Cylinder);
        Rod("Outlet riser",t,new Vector3(.6f,4.45f,.5f),new Vector3(.6f,5.25f,.5f),.11f);
        Rod("Service connection",t,new Vector3(-1.2f,.7f,0),new Vector3(-2.4f,.7f,0),.12f);
        for(int i=0;i<6;i++)Box("Motor fin",t,new Vector3(-1.15f+i*.26f,1,-.9f),new Vector3(.06f,1.15f,.15f),"Near silhouette");
        var lamp=Group("Operating control light",t,new Vector3(-.35f,1.45f,-.92f));Box("Small green pilot lens",lamp,Vector3.zero,new Vector3(.12f,.10f,.07f),"Operating indicator");
        LightAt(lamp,new Vector3(0,0,-.2f),new Color(.35f,.65f,.48f),.45f,3.8f);
        var shade=Group("Local inspection lamp",t,new Vector3(0,4.4f,-.7f));Box("Hood",shade,Vector3.zero,new Vector3(.65f,.08f,.4f));LightAt(shade,new Vector3(0,-.12f,0),new Color(.68f,.73f,.66f),.65f,6,true);
    }
    static void AdjustCameraAndLights(RecoilPrototype w,bool outdoor)
    {
        var camera=w.gameObject.scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Camera>(true)).First();var follow=camera.GetComponent<RecoilCameraController>();
        follow.frameOnGameplayPlane=!outdoor;
        if(!outdoor){follow.followOffset.y+=.25f;follow.preserveFootFraming=true;follow.minimumFootScreenMargin=.10f;follow.minimumY+=.25f;follow.maximumY+=.25f;follow.limitDownwardMouseOffset=true;follow.maximumDownwardMouseOffset=.35f;}
        var presentation=w.GetComponent<Recoil3DWorld>();presentation.ambientColour=outdoor?new Color(.085f,.095f,.105f):new Color(.038f,.046f,.052f);presentation.fogColour=outdoor?new Color(.065f,.077f,.088f):new Color(.022f,.029f,.034f);presentation.fogDensity=outdoor?.006f:.007f;presentation.ApplyAtmosphere();
        foreach(var l in w.GetComponentsInChildren<Light>(true))
        {
            if(l.type==LightType.Directional)l.intensity=l.name=="3D soft ambient bounce"?(outdoor?.08f:.035f):(outdoor?.38f:.10f);
            else if(l.transform.position.z>8){l.intensity*=.22f;l.range=Mathf.Min(l.range,5);var adapter=l.GetComponentInParent<Recoil3DLight>();if(adapter)adapter.intensityMultiplier*=.22f;}
        }
        if(!outdoor && w.gameObject.scene.path.EndsWith("/RecoilFactoryInterior.unity"))
        foreach(var adapter in w.GetComponentsInChildren<Recoil3DLight>(true))
        {if(adapter.source && adapter.transform.position.y<5 && adapter.lightSource.transform.position.z<2){adapter.intensityMultiplier=8;adapter.lightSource.type=LightType.Spot;adapter.lightSource.spotAngle=95;adapter.lightSource.transform.rotation=Quaternion.Euler(90,0,0);adapter.Sync();}}
        if(presentation.playerVisualLight)presentation.playerVisualLight.intensity=.85f;
        var wave=w.GetComponentsInChildren<SoundWaveManager>(true).FirstOrDefault();if(wave)wave.drawBroadcastCircles=false;
        var fx=camera.GetComponent<BroadcastRefractionEffect>();if(fx){fx.effectEnabled=true;fx.strength=.95f;fx.packetWidth=3.8f;fx.wavelength=1.65f;fx.irregularity=.32f;fx.chromaticAberration=.07f;fx.warningTint=.1f;}
        follow.SnapToTarget();
    }
    static void ContinueHardWalls(RecoilPrototype w)
    {
        foreach(var c in w.GetComponentsInChildren<BoxCollider2D>(true))
        {
            if(c.isTrigger || c.GetComponentInParent<RecoilPlayerController>() || c.GetComponentInParent<RampagerController>() || c.GetComponentInParent<ContinuousInteractionItem>() || c.GetComponentInParent<RecoilDoor>())continue;
            if(c.bounds.size.x>1.6f || c.bounds.size.y<2.1f)continue;
            var v=c.GetComponent<Recoil3DSpriteVisual>();if(!v || v.suppressed)continue;
            // Side partitions and world boundaries have no visible gap at either side of the gameplay plane.
            v.depth=86;v.depthOffset=-4;v.Sync();
        }
    }
    static void Interior(RecoilPrototype w,Transform root,bool factory,bool warehouse)
    {
        if(!factory&&!warehouse)return;
        var set=w.transform.Find("3D depth set - foreground midground distant");if(!set)return;
        foreach(var t in set.GetComponentsInChildren<Transform>(true))
        {
            if(t.name=="Rear loadbearing wall" || t.name=="High wire glass window" || t.name=="Window mullion" || t.name=="Damp patch")t.gameObject.SetActive(false);
            if(t.name=="Upper floor extends into office depth"){var sc=t.localScale;sc.z=44;t.localScale=sc;var p=t.localPosition;p.z=28;t.localPosition=p;}
        }
        var hall=Group("Deep workshop bays - selective silhouettes",root,Vector3.zero);
        float left=0,right=warehouse?39:40,top=factory?20:7.5f;
        Box("Deep hall floor",hall,new Vector3((left+right)/2,-.3f,31),new Vector3(right-left,.55f,58),"Pitted concrete");
        Box("Far wall",hall,new Vector3((left+right)/2,top/2,54),new Vector3(right-left,top,.6f),"Pitted concrete");
        for(float x=2;x<right;x+=8)
        {
            for(float z=18;z<=48;z+=10){Box("Depth column",hall,new Vector3(x,top/2,z),new Vector3(.38f,top,.4f));Box("Depth roof beam",hall,new Vector3(x,top-.25f,z),new Vector3(8,.35f,.4f));}
            if(factory){Machine(hall,new Vector3(x+2,0,24+(int)x%3*5),1.35f,(int)x);Machine(hall,new Vector3(x+2,0,43),1.35f,100+(int)x);}
        }
        Window(hall,new Vector3(9,factory?4.8f:5.2f,53.6f),new Vector2(4.5f,2.3f),5);
        Window(hall,new Vector3(31,factory?11:5.2f,53.6f),new Vector2(3.3f,1.7f),4);
        if(factory)Window(hall,new Vector3(23,17,53.6f),new Vector2(4,1.6f),5);
        if(warehouse)Warehouse(w,root);
    }
    static void Crate(Transform p,Vector3 pos,Vector3 size,int n)
    {
        var t=Group("Packing crate",p,pos);Box("Timber body",t,Vector3.zero,size,n%3==0?"Cargo ochre":"Cargo dark timber");
        for(int side=-1;side<=1;side+=2)
        {
            Box("Steel packing band",t,new Vector3(side*size.x*.32f,0,-size.z*.5f-.015f),new Vector3(.065f,size.y+.02f,.025f),"Near silhouette");
            Box("Cross-depth packing band",t,new Vector3(side*size.x*.32f,size.y*.5f+.012f,0),new Vector3(.065f,.025f,size.z),"Near silhouette");
        }
    }
    static void Warehouse(RecoilPrototype w,Transform root)
    {
        var collapsed=w.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Steel rack - collapsed toward camera - editable");
        var continuation=Group("Collapsed cargo - from cutaway front to rear",collapsed,-collapsed.position);float x=collapsed.position.x;
        // Keep the original 13 collision boxes. The room is cut away at Z=-3; cargo stops there, in front of the camera.
        int n=0;for(float z=-1.1f;z<=52;z+=3.5f)
        {
            var row=Group("Cargo row at depth "+z,continuation,new Vector3(0,0,0));
            for(int tier=0;tier<3;tier++)for(int k=0;k<4-tier;k++)
            {
                float xx=x-2.05f+k*1.36f+tier*.48f;
                float h=tier==2?1.1f:1.22f;var size=new Vector3(1.38f,h,3.65f);
                var tpos=new Vector3(xx,.61f+tier*1.18f,z);Crate(row,tpos,size,n++);
            }
            Rod("Fallen transverse steel upright",row,new Vector3(x-2.6f,.1f,z),new Vector3(x-1.5f,1.25f,z+1.8f),.07f);
        }
        foreach(var rack in w.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Steel rack - pallets and freight").ToArray())
        {
            var extension=Group("Rack continues into rear work hall",rack,Vector3.zero);float rx=rack.position.x;
            foreach(float side in new[]{-1.45f,1.45f})for(float z=3;z<=48;z+=5)
                Box("Deep rack upright",extension,new Vector3(rx+side,2.5f,z)-rack.position,new Vector3(.12f,5,.13f));
            for(int level=0;level<3;level++)
            {
                float y=.2f+level*1.55f;
                Box("Continuous deep shelf",extension,new Vector3(rx,y,25)-rack.position,new Vector3(3.1f,.13f,46),"Flaking oxidised metal");
                for(float z=5;z<48;z+=6)for(int k=0;k<3;k++)Crate(extension,new Vector3(rx-1+k,y+.6f,z)-rack.position,new Vector3(.86f,1.05f,3.5f),k+level);
            }
        }
        // The original boxes are shallow paintings; give their native bodies useful depth without changing colliders.
        foreach(var v in collapsed.GetComponentsInChildren<Recoil3DSpriteVisual>(true))if(v.source && v.source.name=="Crate body"){v.depth=3.5f;v.depthOffset=0;v.Sync();}
    }
    static void Building(Transform p,Bounds b,float front,float depth,string name)
    {
        var g=Group(name,p,Vector3.zero);float back=front+depth;
        Box("Rear masonry",g,new Vector3(b.center.x,b.center.y,back),new Vector3(b.size.x,b.size.y,.65f),"Pitted concrete");
        foreach(float x in new[]{b.min.x,b.max.x})
        {
            Box("Side return wall",g,new Vector3(x,b.center.y,(front+back)/2),new Vector3(.65f,b.size.y,depth),"Pitted concrete");
            for(float z=front+2;z<back;z+=4){Box("Side wall vertical pier",g,new Vector3(x,b.center.y,z),new Vector3(.85f,b.size.y,.25f));if(b.size.y>5)Window(g,new Vector3(x+(x==b.min.x?-.35f:.35f),b.size.y*.65f,z),new Vector2(.12f,1.5f),1);}
        }
        Box("Solid flat roof",g,new Vector3(b.center.x,b.max.y+.05f,(front+back)/2),new Vector3(b.size.x+.5f,.3f,depth+.5f),"Near silhouette");
        for(float z=front;z<=back;z+=2)Box("Roof seam",g,new Vector3(b.center.x,b.max.y+.23f,z),new Vector3(b.size.x,.025f,.04f));
        foreach(float x in new[]{b.min.x+.8f,b.max.x-.8f}){Rod("Roof vent",g,new Vector3(x,b.max.y,back-2),new Vector3(x,b.max.y+1.3f,back-2),.18f);Box("Vent rain cover",g,new Vector3(x,b.max.y+1.35f,back-2),new Vector3(.7f,.1f,.7f));}
    }
    static void Outdoor(RecoilPrototype w,Transform root)
    {
        var all=w.GetComponentsInChildren<Transform>(true);
        foreach(var t in all)if(t.name=="Left perimeter wall foreground to background" || t.name=="Left perimeter coping")
        {var p=t.position;p.z=-8;t.position=p;var s=t.localScale;s.z=84;t.localScale=s;}
        var old=all.First(t=>t.name=="Heavy barred factory main gate - chained");old.gameObject.SetActive(false);
        float gx=old.position.x;
        var gate=Group("Continuous iron gate - locked across yard depth",root,new Vector3(gx,0,0));
        for(float z=-48;z<=34;z+=.65f){float xx=z*.065f;Rod("Forged gate bar",gate,new Vector3(xx,.2f,z),new Vector3(xx,6.8f,z),.075f);Box("Spear cap",gate,new Vector3(xx,6.95f,z),new Vector3(.17f,.35f,.17f));}
        foreach(float h in new[]{.5f,3.5f,6.4f})Rod("Continuous gate rail",gate,new Vector3(-48*.065f,h,-48),new Vector3(34*.065f,h,34),.11f);
        for(float z=-48;z<=34;z+=13.5f)Box("Massive gate post",gate,new Vector3(z*.065f,3.8f,z),new Vector3(.65f,7.6f,.65f),"Pitted concrete");
        var lockRoot=Group("Heavy chain and lock at play plane",gate,new Vector3(-.2f,2.3f,-.1f));
        for(int n=0;n<18;n++)
        {var link=Group("Welded chain link",lockRoot,new Vector3(-.02f,.17f*Mathf.Sin(n*.4f),-2.1f+n*.25f));for(int k=0;k<10;k++){float a=k*Mathf.PI/5,b=(k+1)*Mathf.PI/5;Rod("Thick chain steel",link,new Vector3(Mathf.Cos(a)*.10f,Mathf.Sin(a)*.16f,0),new Vector3(Mathf.Cos(b)*.10f,Mathf.Sin(b)*.16f,0),.03f,"Flaking oxidised metal");}link.localRotation=Quaternion.Euler(0,n%2*80,0);}
        Box("Heavy rectangular padlock",lockRoot,new Vector3(-.08f,-.32f,0),new Vector3(.3f,.5f,.4f));
        foreach(var t in all.Where(t=>t.name=="Factory left return wall into Z"||t.name=="Factory right return wall into Z"||t.name=="Factory roof with depth"))t.gameObject.SetActive(false);
        foreach(var sr in w.GetComponentsInChildren<SpriteRenderer>(true).Where(r=>r.name=="Factory main facade"||r.name=="Store facade"||r.name=="Warehouse wall"||r.name=="Control hut").ToArray())
        {
            // Replacement art can be a child of a disabled legacy sprite; measure the live surface.
            var live=sr.GetComponentsInChildren<Recoil3DSpriteVisual>(true).FirstOrDefault(v=>v.source && v.source.enabled && !v.suppressed && v.meshRenderer && v.gameObject.activeInHierarchy);
            if(!live)throw new Exception("No active exterior surface: "+sr.name);
            var bounds=live.meshRenderer.bounds;float front=bounds.min.z-.04f;
            Building(root,bounds,front,sr.name=="Factory main facade"?26:sr.name=="Warehouse wall"?24:12,sr.name+" - volumetric building");
            var shell=root.Find(sr.name+" - volumetric building");shell.SetParent(sr.transform,true);
        }
        foreach(var v in w.GetComponentsInChildren<Recoil3DSpriteVisual>(true).Where(v=>v.source && (v.source.name=="Canopy"||v.source.name=="Canopy stripe")))
        {v.depth=12;v.depthOffset=7;v.Sync();}
        Truck(w,root);
        Steelworks(root);
    }
    static void Truck(RecoilPrototype w,Transform root)
    {
        var truck=w.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Truck - soft canvas roof");var old=truck.Find("3D military canvas truck");if(old)old.gameObject.SetActive(false);
        var g=Group("Long closed cargo truck - reference orientation",truck,Vector3.zero);
        // Long axis is depth, cab faces foreground. Cargo traverses the whole visible road width in Z.
        Box("Long load chassis",g,new Vector3(-.5f,.52f,2.15f),new Vector3(5.8f,.35f,14.3f),"Near silhouette");
        Box("Enclosed rectangular cargo body",g,new Vector3(-.5f,1.91f,2.15f),new Vector3(5.8f,2.12f,14.3f),"Truck faded cargo");
        Box("Flat cargo roof",g,new Vector3(-.5f,2.99f,2.15f),new Vector3(5.9f,.08f,14.35f),"Truck faded cargo");
        for(float z=-4;z<9.2f;z+=1.7f)foreach(float side in new[]{-3.43f,2.43f})Box("Cargo side vertical rib",g,new Vector3(side,1.95f,z),new Vector3(.035f,1.9f,.06f));
        Box("Cab main shell",g,new Vector3(-.5f,1.55f,-7),new Vector3(5.7f,2.4f,3.7f),"Truck aged cab");
        Box("Engine bonnet",g,new Vector3(-.5f,1.12f,-9.7f),new Vector3(4.6f,1.05f,1.8f),"Truck aged cab");
        Box("Split windshield",g,new Vector3(-.5f,2.2f,-8.88f),new Vector3(4.9f,.85f,.06f),"Soot covered glass");
        Box("Windshield divider",g,new Vector3(-.5f,2.2f,-8.93f),new Vector3(.09f,.95f,.08f));
        Box("Front grille",g,new Vector3(-.5f,1,-10.65f),new Vector3(2.7f,.65f,.07f),"Near silhouette");
        for(int i=0;i<9;i++)Box("Vertical grille slat",g,new Vector3(-1.6f+i*.28f,1,-10.7f),new Vector3(.055f,.6f,.035f));
        Box("Wide steel bumper",g,new Vector3(-.5f,.4f,-10.85f),new Vector3(6,.24f,.3f));
        foreach(float side in new[]{-3.5f,2.5f})
        {
            Box("Cab side window",g,new Vector3(side>0?2.38f:-3.38f,2.18f,-7.1f),new Vector3(.05f,.85f,2.25f),"Soot covered glass");
            Rod("Wing mirror arm",g,new Vector3(side>0?2.3f:-3.3f,2.5f,-8),new Vector3(side,2.5f,-8),.05f);Box("Side mirror",g,new Vector3(side,2.5f,-8),new Vector3(.15f,.5f,.4f));
            foreach(float z in new[]{-7.5f,4f,6.2f}){var tyre=Box("Heavy truck tyre",g,new Vector3(side,.55f,z),new Vector3(1.2f,.22f,1.2f),"Near silhouette",PrimitiveType.Cylinder);tyre.transform.localRotation=Quaternion.Euler(0,0,90);var hub=Box("Wheel hub",g,new Vector3(side+(side>0?.25f:-.25f),.55f,z),new Vector3(.55f,.04f,.55f),"Worn industrial steel",PrimitiveType.Cylinder);hub.transform.localRotation=Quaternion.Euler(0,0,90);}
        }
        foreach(float x in new[]{-2.25f,1.25f})Box("Unlit headlamp",g,new Vector3(x,1.18f,-10.65f),new Vector3(.6f,.4f,.08f),"Soot covered glass");
        // A truck nose must not imply a walk-around route: loading barricades continue from its bumper behind the camera.
        var barrier=Group("Loading lane closed beside truck nose",truck,Vector3.zero);
        for(float z=-46;z<-10;z+=2.8f){Box("Continuous concrete loading barrier",barrier,new Vector3(-.5f,.8f,z),new Vector3(5.8f,1.6f,2.85f),"Pitted concrete");Box("Weathered striped cap",barrier,new Vector3(-.5f,1.62f,z),new Vector3(5.85f,.08f,2.85f),"Cargo ochre");}
        Box("Rear loading obstruction joins enclosure",barrier,new Vector3(-.5f,1.45f,9.2f),new Vector3(5.8f,2.9f,.3f),"Truck faded cargo");
        // Match the existing playable roof and remove the old side-on cab collision silhouette.
        var cab=truck.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="Truck solid cab");if(cab){var c=cab.GetComponent<BoxCollider2D>();if(c)c.enabled=false;}
        var cargo=truck.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="Truck solid cargo body");if(cargo){cargo.position=new Vector3(truck.position.x-.5f,1.37f,0);cargo.localScale=new Vector3(5.8f,2.74f,2.6f);}
    }
    static void Steelworks(Transform p)
    {
        var t=Group("Distant steelworks - furnace stoves conveyors chimneys",p,Vector3.zero);
        for(int n=0;n<3;n++)
        {
            float x=30+n*38,z=66+n%2*14;
            Box("Casting shed",t,new Vector3(x,5,z),new Vector3(23,10,18),"Pitted concrete");
            Box("Blast furnace shaft",t,new Vector3(x-3,18,z+5),new Vector3(7,11,7),"Worn industrial steel",PrimitiveType.Cylinder);
            for(int tier=0;tier<5;tier++){Box("Furnace service gallery",t,new Vector3(x-3,9+tier*4,z+5),new Vector3(10,.25f,10));foreach(float side in new[]{-7f,1f})Box("Furnace support",t,new Vector3(x+side,17,z+5),new Vector3(.4f,34,.4f));}
            for(int k=0;k<3;k++){float sx=x+8+k*4;Box("Hot blast stove",t,new Vector3(sx,12,z+10),new Vector3(3.3f,12,3.3f),"Flaking oxidised metal",PrimitiveType.Cylinder);Box("Stove dome",t,new Vector3(sx,24,z+10),new Vector3(3.3f,2.5f,3.3f),"Worn industrial steel",PrimitiveType.Sphere);Rod("Hot blast main",t,new Vector3(sx,5,z+8),new Vector3(x,5,z+2),.35f);}
            Box("Tall masonry stack",t,new Vector3(x+19,24,z+14),new Vector3(2.1f,24,2.1f),"Pitted concrete",PrimitiveType.Cylinder);
            for(int k=0;k<4;k++)Box("Chimney stiffening band",t,new Vector3(x+19,12+k*9,z+14),new Vector3(2.2f,.13f,2.2f),"Near silhouette",PrimitiveType.Cylinder);
            Rod("Ore conveyor enclosed gallery",t,new Vector3(x-25,4,z+3),new Vector3(x-3,34,z+5),.85f);
            Window(t,new Vector3(x,7,z-9.15f),new Vector2(3,1.5f),4);
        }
        Box("Distant power house",t,new Vector3(-7,8,60),new Vector3(17,16,16),"Pitted concrete");Window(t,new Vector3(-7,10,51.8f),new Vector2(5,2),6);
        Box("Gas holder",t,new Vector3(13,12,95),new Vector3(17,12,17),"Worn industrial steel",PrimitiveType.Cylinder);
    }
    [MenuItem("Recoil/3D/Apply Continuous Depth And Selective Lighting")]
    public static string Apply()
    {
        if(EditorApplication.isPlaying)throw new Exception("Exit Play before editing.");
        for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new Exception("Save open edits before applying art pass.");
        Custom("Distant window glow",new Color(.18f,.22f,.23f),new Color(.28f,.35f,.37f));Custom("Operating indicator",new Color(.14f,.3f,.17f),new Color(.12f,.45f,.23f));
        Custom("Cargo dark timber",new Color(.23f,.235f,.18f),Color.black);Custom("Cargo ochre",new Color(.31f,.27f,.16f),Color.black);
        Custom("Truck faded cargo",new Color(.23f,.245f,.22f),Color.black);Custom("Truck aged cab",new Color(.18f,.23f,.21f),Color.black);
        foreach(string guid in AssetDatabase.FindAssets("t:Material",new[]{"Assets/Recoil3D/Materials"}))
        {var m=AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));if(m.HasProperty("_HazeDensity")){m.SetFloat("_HazeDensity",0);m.SetColor("_DepthHazeColour",new Color(.012f,.016f,.018f));EditorUtility.SetDirty(m);}}
        var previous=SceneManager.GetActiveScene();int count=0;
        foreach(var entry in EditorBuildSettings.scenes.Where(e=>e.enabled && e.path.StartsWith(Recoil3DScene.Folder)))
        {
            var s=SceneManager.GetSceneByPath(entry.path);bool added=!s.isLoaded;if(added)s=EditorSceneManager.OpenScene(entry.path,OpenSceneMode.Additive);SceneManager.SetActiveScene(s);
            try
            {
                var w=s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<RecoilPrototype>(true)).Single();
                if(w.transform.Find(Pass))continue; // Never overwrite hand edits or apply multipliers twice.
                bool outdoor=entry.path.EndsWith("/RecoilPrototype.unity");var root=Group(Pass,w.transform,Vector3.zero);
                AdjustCameraAndLights(w,outdoor);ContinueHardWalls(w);
                if(outdoor)Outdoor(w,root);else Interior(w,root,entry.path.EndsWith("/RecoilFactoryInterior.unity"),entry.path.EndsWith("/RecoilFactoryWarehouse.unity"));
                EditorSceneManager.MarkSceneDirty(s);if(!EditorSceneManager.SaveScene(s))throw new Exception("Could not save "+entry.path);count++;
            }
            finally{if(added)EditorSceneManager.CloseScene(s,true);}
        }
        AssetDatabase.SaveAssets();SceneManager.SetActiveScene(previous);return "Revised "+count+" 3D scenes. Originals preserved.";
    }
}
