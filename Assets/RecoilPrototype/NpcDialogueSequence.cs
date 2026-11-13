using UnityEngine;

[CreateAssetMenu(menuName="Recoil/NPC Dialogue",fileName="Dialogue")]
public sealed class NpcDialogueSequence : ScriptableObject
{
    public enum SpeakerRole { Unspecified, Npc, Player, Narrator }
    [System.Serializable] public struct ResponseLine
    {
        public SpeakerRole role;
        public string speaker;
        [TextArea(2,6)] public string text;
        public bool offerPhoto;
        public bool slidePhotoUnderDoor;
    }
    [System.Serializable] public sealed class Choice
    {
        public string id;
        [Tooltip("Short thought-menu summary, ideally two or three words.")] public string label;
        [TextArea(2,6),Tooltip("Complete Dawn line shown after choosing the short summary.")] public string text;
        public bool offerPhoto;
        public bool slidePhotoUnderDoor;
        public NpcDialogueSequence continuation;
        public ResponseLine[] response=new ResponseLine[0];
    }
    [System.Serializable] public struct Line
    {
        public SpeakerRole role;
        public string speaker;
        [TextArea(2,6),Tooltip("Leave empty when there is no spoken line: choices then appear immediately as Dawn thought bubbles.")] public string text;
        public bool offerPhoto;
        public bool slidePhotoUnderDoor;
        public string progressFlag;
        public Choice[] choices;
    }
    public Line[] lines=new Line[0];
}
