using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class CoalerBroadcastBehaviourVerification
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Set(object o,string key,object value){o.GetType().GetField(key,Private).SetValue(o,value);}
    static object Call(object o,string key,params object[] args){return o.GetType().GetMethod(key,Private).Invoke(o,args);}
    static void Check(bool ok,string why){if(!ok)throw new Exception("Broadcast behaviour: "+why);}
    public static string Verify()
    {
        var source=SceneManager.GetSceneByPath("Assets/Scenes/3D/RecoilPrototype.unity");var original=SceneManager.GetActiveScene();var preview=EditorSceneManager.NewPreviewScene();
        try
        {
            foreach(var root in source.GetRootGameObjects()){var clone=UnityEngine.Object.Instantiate(root);SceneManager.MoveGameObjectToScene(clone,preview);}
            var roots=preview.GetRootGameObjects();var world=roots.SelectMany(g=>g.GetComponentsInChildren<RecoilPrototype>(true)).Single();Set(world,"cam",roots.SelectMany(g=>g.GetComponentsInChildren<Camera>(true)).First(c=>c.CompareTag("MainCamera")));Call(world,"BindScene");Call(world,"ResetRun",true);Set(world,"started",true);world.GetComponentInChildren<SoundWaveManager>(true).Initialize(world);
            var enemy=world.GetComponentsInChildren<RampagerController>(true).First();var player=world.Player;
            Action reset=()=>{enemy.ResetController();enemy.ReceiveBuildingWarning(3);enemy.ReceiveBuildingWave(1,1,3,1);Set(player,"<MovementBlocked>k__BackingField",false);};
            Action<float,float,float,float,bool> slice=(intent,reaction,move,hold,sees)=>{Set(player,"<HorizontalIntent>k__BackingField",intent);Call(enemy,"CheckWaveBehaviour",new RampagerPerception.Observation{seesPlayer=sees},reaction,move,hold);};
            reset();slice(1,.5f,0,0,true);Check(!enemy.PreparingAttack,"Reaction must permit early input");slice(1,0,.7f,0,true);Check(!enemy.PreparingAttack,"Following command must be safe");
            reset();slice(0,0,.74f,0,true);Check(!enemy.PreparingAttack,"Stop tolerance shorter than .75s");slice(0,0,.02f,0,true);Check(enemy.PreparingAttack,"Stopped too long during move should anger");
            reset();slice(-1,0,.74f,0,true);Check(!enemy.PreparingAttack,"Reverse tolerance shorter than .75s");slice(-1,0,.02f,0,true);Check(enemy.PreparingAttack,"Reverse during movement must anger");
            reset();slice(0,0,0,.7f,true);Check(!enemy.PreparingAttack,"Standing still during hold must be safe");slice(1,0,0,.74f,true);Check(!enemy.PreparingAttack,"Hold movement tolerance shorter than .75s");slice(1,0,0,.02f,true);Check(enemy.PreparingAttack,"Moving too long in hold must anger");
            reset();slice(0,0,.4f,0,true);slice(0,0,0,.2f,true);Check(!enemy.PreparingAttack,"Move-stop timer carried into hold");slice(1,0,0,.74f,true);Check(!enemy.PreparingAttack,"Stage boundary retained prior violation");
            reset();slice(-1,0,.4f,0,true);slice(-1,0,.1f,0,false);slice(-1,0,.74f,0,true);Check(!enemy.PreparingAttack,"Loss of sight must reset continuous timer");
            reset();Set(player,"<MovementBlocked>k__BackingField",true);slice(0,0,1,0,true);Check(!enemy.PreparingAttack,"Blocked player should not accrue stopped violation");
            Debug.Log("COALER_BROADCAST_BEHAVIOUR_PASS: reaction grace, matching movement, stopped/reversed .75s, stationary hold, moving hold .75s, stage-boundary reset, lost-sight reset and blocked-player exemption verified.");
            return "Broadcast direction checks correspond to actual command slices; .75s thresholds and stage resets pass.";
        }
        finally{foreach(var root in preview.GetRootGameObjects())UnityEngine.Object.DestroyImmediate(root);EditorSceneManager.ClosePreviewScene(preview);if(original.IsValid()&&original.isLoaded)SceneManager.SetActiveScene(original);}
    }
}
