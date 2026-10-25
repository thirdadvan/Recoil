using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
public static class AutomaticStepVerification
{
    static void Set(object o,string f,object v){o.GetType().GetField(f,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(o,v);}
    static void Check(bool ok,string error){if(!ok)throw new Exception("Automatic step: "+error);}
    [MenuItem("Recoil/Tests/Verify Low Platform Stepping")]
    public static void Verify()
    {
        var scene=EditorSceneManager.NewPreviewScene();
        try
        {
            var root=new GameObject("Temporary low step test");SceneManager.MoveGameObjectToScene(root,scene);var world=root.AddComponent<RecoilPrototype>();
            var playerGo=new GameObject("Dawn test");playerGo.transform.SetParent(root.transform);var p=playerGo.AddComponent<RecoilPlayerController>();
            Func<string,Transform> part=n=>{var t=new GameObject(n).transform;t.SetParent(playerGo.transform);return t;};
            p.Configure(world,null,part("coat"),part("head"),part("hat"),part("gun"));Set(world,"player",p);
            var depth=playerGo.AddComponent<PlayerDepthPlane>();depth.Initialize(world,p);p.InitializeDepthPlane(world);
            var blocks=(IList)typeof(RecoilPrototype).GetField("blocks",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(world);
            var blockType=typeof(RecoilPrototype).GetNestedType("Block",BindingFlags.NonPublic);
            Func<string,Vector2,Vector2,BoxCollider2D> box=(n,pos,size)=>{var g=new GameObject(n);g.transform.SetParent(root.transform);g.transform.position=pos;var c=g.AddComponent<BoxCollider2D>();c.size=size;return c;};
            Action<BoxCollider2D> solid=c=>{var b=Activator.CreateInstance(blockType,true);blockType.GetField("collider").SetValue(b,c);blocks.Add(b);};
            solid(box("Floor",new Vector2(0,-.5f),new Vector2(20,1)));
            var deck=box("Low soft deck",new Vector2(.5f,.12f),new Vector2(1,.24f));deck.isTrigger=true;var roof=deck.gameObject.AddComponent<SoftRoofPlatform>();Set(world,"softRoofs",new[]{roof});Physics2D.SyncTransforms();
            var move=typeof(RecoilPlayerController).GetMethod("Move",BindingFlags.Instance|BindingFlags.NonPublic);
            Action reset=()=>{p.ResetController(new Vector2(-.7f,p.BodyHeight*.5f));Set(p,"grounded",true);};
            Action tick=()=>{Set(world,"simTime",world.Clock+.02f);move.Invoke(p,new object[]{new Vector2(.025f,-.015f)});depth.Simulate(p.Position,new Vector2(1.25f,-.75f),p.Grounded,false,false,false,.02f);Check(Mathf.Abs(depth.CurrentZ-depth.defaultZ)<.001f,"Low step changed walking depth x="+p.Position.x+" feet="+(p.Position.y-p.BodyHeight*.5f)+" z="+depth.CurrentZ+" state="+depth.State);};
            reset();bool reached=false;for(int i=0;i<100;i++){tick();float feet=p.Position.y-p.BodyHeight*.5f;if(feet>.22f)reached=true;if(p.Position.x>.05f&&p.Position.x<.95f)Check(Mathf.Abs(feet-.24f)<.002f,"Feet penetrate low soft deck");}
            Check(reached&&p.Position.x>1.5f&&Mathf.Abs(p.Position.y-p.BodyHeight*.5f)<.002f,"Walk up and off low deck failed");
            p.ResetController(new Vector2(-.31f,p.BodyHeight*.5f+.05f));Set(p,"grounded",false);Set(p,"flying",true);move.Invoke(p,new object[]{new Vector2(.025f,0)});Check(Mathf.Abs(p.Position.y-p.BodyHeight*.5f-.05f)<.002f,"Flight must not auto-step");
            deck.enabled=false;Set(world,"softRoofs",new SoftRoofPlatform[0]);
            var wall=box("Tall wall",new Vector2(.5f,.6f),new Vector2(1,1.2f));solid(wall);Physics2D.SyncTransforms();reset();for(int i=0;i<100;i++)tick();
            Check(p.Position.x<0&&p.Position.y-p.BodyHeight*.5f<.01f,"Tall wall incorrectly became a step");
            wall.size=new Vector2(1,.24f);wall.transform.position=new Vector2(.5f,.12f);Physics2D.SyncTransforms();reset();reached=false;for(int i=0;i<100;i++){tick();if(p.Position.y-p.BodyHeight*.5f>.22f)reached=true;}Check(reached,"Hard low step must also work");
            Debug.Log("AUTOMATIC_LOW_STEP_CHECK_PASS: soft deck up/off, no penetration, flight exclusion, tall wall exclusion, hard step.");
        }
        finally{EditorSceneManager.ClosePreviewScene(scene);}
    }
}
