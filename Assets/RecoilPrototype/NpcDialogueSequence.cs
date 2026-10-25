using UnityEngine;

[CreateAssetMenu(menuName="Recoil/NPC Dialogue",fileName="Dialogue")]
public sealed class NpcDialogueSequence : ScriptableObject
{
    [System.Serializable] public struct Line
    {
        public string speaker;
        [TextArea(2,6)] public string text;
    }
    public Line[] lines=new Line[0];
}
