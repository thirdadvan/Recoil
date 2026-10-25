using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class Recoil3DObstacleVerification
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Set(object o,string name,object value){o.GetType().GetField(name,F).SetValue(o,value);}
    static object Call(object o,string name,params object[] args){return o.GetType().GetMethod(name,F).Invoke(o,args);}
    public static string Run()
    {
        if(UnityEditor.EditorApplication.isPlaying)throw new Exception("Exit Play first.");
        var original=SceneManager.GetActiveScene();var source=SceneManager.GetSceneByPath("Assets/Scenes/3D/RecoilPrototype.unity");bool opened=!source.isLoaded;if(opened)source=EditorSceneManager.OpenScene("Assets/Scenes/3D/RecoilPrototype.unity",OpenSceneMode.Additive);
        var preview=EditorSceneManager.NewPreviewScene();
        try
        {
            foreach(var root in source.GetRootGameObjects()){var clone=UnityEngine.Object.Instantiate(root);SceneManager.MoveGameObjectToScene(clone,preview);}
            var all=preview.GetRootGameObjects();var w=all.SelectMany(r=>r.GetComponentsInChildren<RecoilPrototype>()).Single();var camera=all.SelectMany(r=>r.GetComponentsInChildren<Camera>()).Single();Set(w,"cam",camera);Call(w,"BindScene");Call(w,"ResetRun",true);Set(w,"started",true);w.Player.Noise.enabled=false;
            var truck=w.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Truck - soft canvas roof");float centre=truck.position.x;
            var player=w.Player;
            player.ResetController(new Vector2(centre-7.5f,.825f));player.Noise.enabled=false;
            for(int i=0;i<250;i++){Set(w,"simTime",w.Clock+.02f);player.Simulate(1,0,false,false,.02f);}
            if(player.Position.x>centre-4)throw new Exception("Truck/pallet route did not block normal ground walking.");
            float maxFeet=0;
            for(int side=-1;side<=1;side+=2)
            {
                player.ResetController(new Vector2(centre+side*7.5f,.825f));player.Noise.enabled=false;
                for(int i=0;i<1300;i++)
                {
                    Set(w,"simTime",w.Clock+.02f);player.Simulate(-side,0,false,player.Grounded,.02f);
                    maxFeet=Mathf.Max(maxFeet,player.Position.y-player.BodyHeight*.5f);
                    if(side<0 && player.Position.x>centre+7 || side>0 && player.Position.x<centre-7)break;
                }
                if(side<0 && player.Position.x<centre+7 || side>0 && player.Position.x>centre-7)throw new Exception("Bunny-hop route could not cross the truck from side "+side+"; stopped at "+player.Position);
            }
            if(maxFeet<2.9f)throw new Exception("Jump route never reached canvas roof.");
            var gate=w.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Continuous iron gate - locked across yard depth");
            if(gate.GetComponentsInChildren<Renderer>().Length<100)throw new Exception("Gate chain/bar geometry missing.");
            return "Ground traversal blocked; unassisted jumps cross truck in both directions; roof reached. Heavy gate/chain geometry present.";
        }
        finally{foreach(var root in preview.GetRootGameObjects())UnityEngine.Object.DestroyImmediate(root);EditorSceneManager.ClosePreviewScene(preview);if(opened)EditorSceneManager.CloseScene(source,true);SceneManager.SetActiveScene(original);}
    }
}
