using UnityEngine;
[CreateAssetMenu(menuName="Recoil/Information/Archive Person Record")]
public sealed class ArchivePersonRecord : ScriptableObject
{
    public string recordId,title;
    [TextArea] public string[] fields;
    [TextArea] public string dialogueSummary;
    public string ProgressFlag {get{return "information_"+recordId;}}
    public string Transcript {get{return string.Join("\n",fields??new string[0]);}}
}
