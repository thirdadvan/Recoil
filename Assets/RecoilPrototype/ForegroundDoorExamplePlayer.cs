using UnityEngine;

// Only used by the isolated presentation scene, not the game's player controller.
public sealed class ForegroundDoorExamplePlayer : MonoBehaviour
{
    public float speed=3;
    void Update()
    {
        float direction=(Input.GetKey(KeyCode.D)||Input.GetKey(KeyCode.RightArrow)?1:0)-(Input.GetKey(KeyCode.A)||Input.GetKey(KeyCode.LeftArrow)?1:0);
        var position=transform.position;position.x=Mathf.Clamp(position.x+direction*speed*Time.deltaTime,-6,6);transform.position=position;
    }
    void OnGUI()
    {
        var style=new GUIStyle(GUI.skin.label){fontSize=20,alignment=TextAnchor.MiddleCenter};
        GUI.Label(new Rect(0,20,Screen.width,35),"FOREGROUND DOOR / A D or arrow keys to walk",style);
        GUI.Label(new Rect(0,Screen.height-65,Screen.width,40),"Door stays in front. Overlap fades it to 30%; leaving restores it.",style);
    }
}
