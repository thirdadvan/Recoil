using UnityEngine;

// Deliberately independent of game input: confined to the art test scene.
[ExecuteAlways]
public sealed class RampagerMorphologyDemo : MonoBehaviour
{
    public RampagerTentacleVisual specimen;
    public bool animateInSceneView=true;
    GUIStyle title,body,button;
#if UNITY_EDITOR
    double lastTime;
    void OnEnable(){lastTime=UnityEditor.EditorApplication.timeSinceStartup;UnityEditor.EditorApplication.update+=EditorTick;}
    void OnDisable(){UnityEditor.EditorApplication.update-=EditorTick;}
    void EditorTick()
    {
        if(Application.isPlaying || !animateInSceneView || !specimen)return;
        double now=UnityEditor.EditorApplication.timeSinceStartup;if(now-lastTime<1.0/24)return;
        specimen.PreviewTick(Mathf.Min(.1f,(float)(now-lastTime)));lastTime=now;UnityEditor.SceneView.RepaintAll();
    }
#endif
    void Update()
    {
        if(!Application.isPlaying || !specimen)return;
        for(int i=0;i<8;i++)if(Input.GetKeyDown(KeyCode.Alpha1+i))specimen.SetPreview((RampagerTentacleVisual.Form)i);
        if(Input.GetKeyDown(KeyCode.Space))specimen.autoCycle=!specimen.autoCycle;
    }
    void OnGUI()
    {
        if(!specimen)return;
        if(title==null){title=new GUIStyle(GUI.skin.label){fontSize=22};body=new GUIStyle(GUI.skin.label){fontSize=14,wordWrap=true};button=new GUIStyle(GUI.skin.button){fontSize=13};}
        GUI.Box(new Rect(16,16,250,357),GUIContent.none);
        GUI.Label(new Rect(28,25,225,32),"TENTACLE WORKER",title);
        GUI.Label(new Rect(28,58,225,25),specimen.CurrentForm.ToString(),body);
        for(int i=0;i<8;i++)if(GUI.Button(new Rect(28,90+i*27,225,25),(i+1)+"  "+((RampagerTentacleVisual.Form)i),button))specimen.SetPreview((RampagerTentacleVisual.Form)i);
        specimen.autoCycle=GUI.Toggle(new Rect(28,311,220,25),specimen.autoCycle,"Auto cycle / SPACE");
        if(specimen.previewForm==RampagerTentacleVisual.Form.Fusion){GUI.Label(new Rect(28,337,65,20),"Fusion",body);specimen.fusionProgress=GUI.HorizontalSlider(new Rect(95,343,145,20),specimen.fusionProgress,0,1);}
    }
}
