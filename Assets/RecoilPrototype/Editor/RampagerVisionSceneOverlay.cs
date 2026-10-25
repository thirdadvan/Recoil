using UnityEditor;
using UnityEngine;

// Scene-view callback deliberately keeps the overlay out of Game, even with Game Gizmos enabled.
[InitializeOnLoad]
public static class RampagerVisionSceneOverlay
{
    static double nextRepaint;

    static RampagerVisionSceneOverlay()
    {
        SceneView.duringSceneGui += DrawVision;
        EditorApplication.update += RefreshDuringPlay;
    }

    static void RefreshDuringPlay()
    {
        if (!EditorApplication.isPlaying || EditorApplication.timeSinceStartup < nextRepaint) return;
        nextRepaint = EditorApplication.timeSinceStartup + .1;
        SceneView.RepaintAll();
    }

    static void DrawVision(SceneView sceneView)
    {
        if (Event.current.type != EventType.Repaint || !sceneView.drawGizmos) return;
        foreach (var perception in Object.FindObjectsOfType<RampagerPerception>())
        {
            if (!perception.isActiveAndEnabled) continue;
            var movement = perception.GetComponent<RampagerMovement>();
            var controller = perception.GetComponent<RampagerController>();
            if(controller && (!Application.isPlaying || !controller.WaveActive))
            {
                using(new Handles.DrawingScope(new Color(.65f,.4f,.9f,.7f)))
                    Handles.DrawWireDisc(controller.transform.position,Vector3.up,controller.CloseSenseRadius);
                continue;
            }
            float facing = Application.isPlaying ? (movement && movement.Facing > 0 ? 1 : -1) : (movement ? movement.InitialFacingSign : -1);
            var motor = perception.GetComponent<RampagerMovement>();
            float eyeOffset = motor ? 1.25f * motor.bodyScale - .8f : .45f;
            Vector3 eye = perception.transform.position + Vector3.up * eyeOffset;
            float radius = perception.SightRange;
            float halfAngle = perception.fieldOfView * .5f;
            float pitch = controller ? controller.SightPitch : -25;
            float centerAngle = facing > 0 ? pitch : 180-pitch;
            const int segments = 48;
            var outline = new Vector3[segments + 3];
            outline[0] = eye;
            for (int i = 0; i <= segments; i++)
            {
                float angle = (centerAngle - halfAngle + perception.fieldOfView * i / segments) * Mathf.Deg2Rad;
                outline[i + 1] = eye + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * radius;
            }
            outline[outline.Length - 1] = eye;
            using (new Handles.DrawingScope(controller && controller.State==RampagerController.Behaviour.Sleeping && Application.isPlaying ? new Color(.5f,.5f,.5f,.45f) : new Color(1f, .7f, .15f, .9f)))
                Handles.DrawAAPolyLine(2f, outline);
        }
    }
}
