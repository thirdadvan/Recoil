using UnityEngine;
using System.Linq;

// Information records use progress flags; they never enter the physical item grid.
public sealed class RecoilInformationJournal : MonoBehaviour
{
    public bool IsOpen {get;private set;}
    public ArchivePersonRecord ActiveRecord {get;private set;}
    RecoilPrototype world;ArchivePersonRecord[] records;float oldScale;int openedFrame;
    GUIStyle text,mono,title,small;Font journalFont;bool armed;
    public void Initialize(RecoilPrototype owner){world=owner;if(records==null)records=Resources.LoadAll<ArchivePersonRecord>("RecoilInformation").OrderBy(r=>r.recordId).ToArray();}
    public ArchivePersonRecord[] KnownRecords {get{return (records??new ArchivePersonRecord[0]).Where(r=>world&&world.HasItem(r.ProgressFlag)).ToArray();}}
    public void Acquire(ArchivePersonRecord record){if(!record||!world)return;world.RememberProgress(record.ProgressFlag);Open(record);}
    public void Open(ArchivePersonRecord record=null)
    {
        if(!world)return;if(world.InventoryOpen)world.CloseInventory();if(IsOpen){ActiveRecord=record;return;}
        ActiveRecord=record?record:KnownRecords.FirstOrDefault();oldScale=Time.timeScale;Time.timeScale=0;IsOpen=true;armed=false;openedFrame=Time.frameCount;world.Player.StopForConversation();
    }
    public void Close(){if(!IsOpen)return;IsOpen=false;Time.timeScale=oldScale;}
    public void Tick()
    {
        bool held=RecoilInput.InteractHeld||RecoilInput.ConfirmHeld||Input.GetKey(KeyCode.Return)||Input.GetKey(KeyCode.Space)||Input.GetMouseButton(0);
        if(Time.frameCount>openedFrame&&!held)armed=true;
        if(!armed)return;
        if(RecoilInput.PausePressed||RecoilInput.InteractPressed||RecoilInput.InventoryPressed||RecoilInput.FilesPressed){Close();return;}
        var known=KnownRecords;if(known.Length==0)return;int index=System.Array.IndexOf(known,ActiveRecord);
        if(RecoilInput.UpPressed)ActiveRecord=known[(index-1+known.Length)%known.Length];
        if(RecoilInput.DownPressed)ActiveRecord=known[(index+1)%known.Length];
    }
    void Styles()
    {
        if(text!=null)return;var font=journalFont=Font.CreateDynamicFontFromOSFont(new[]{"Menlo","Consolas","Courier New"},22);
        text=new GUIStyle{fontSize=23,wordWrap=true};text.normal.textColor=new Color(.83f,.84f,.80f);
        mono=new GUIStyle(text){font=font,fontSize=25};mono.normal.textColor=new Color(.45f,.90f,.54f);
        title=new GUIStyle(text){fontSize=34,fontStyle=FontStyle.Bold};small=new GUIStyle(text){fontSize=17};
    }
    static void Fill(Rect rect,Color color){var old=GUI.color;GUI.color=color;GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=old;}
    public void Draw()
    {
        if(!IsOpen)return;Styles();var matrix=GUI.matrix;var color=GUI.color;GUI.matrix=Matrix4x4.identity;GUI.color=Color.white;
        try{
            Fill(new Rect(0,0,Screen.width,Screen.height),new Color(0,0,0,.83f));float scale=Mathf.Min(Screen.width/1600f,Screen.height/900f);
            GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-1600*scale)*.5f,(Screen.height-900*scale)*.5f,0),Quaternion.identity,Vector3.one*scale);
            GUI.Label(new Rect(70,35,1100,50),"FILES / ARCHIVE INFORMATION",title);
            int i=0;foreach(var record in KnownRecords){var button=new Rect(65,165+i++*58,210,45);Fill(button,ActiveRecord==record?new Color(.08f,.18f,.12f):new Color(.025f,.05f,.035f));GUI.Label(new Rect(button.x+12,button.y+10,button.width-20,button.height),record.title,small);if(GUI.Button(button,GUIContent.none,GUIStyle.none))ActiveRecord=record;}
            Fill(new Rect(310,120,1190,505),new Color(.20f,.21f,.18f));Fill(new Rect(335,145,1140,445),new Color(.012f,.037f,.022f));
            GUI.Label(new Rect(375,170,1040,42),"FACTORY RECORDS TERMINAL / 1978",mono);
            if(ActiveRecord){GUI.Label(new Rect(375,225,1040,45),"> FIND "+ActiveRecord.title,mono);GUI.Label(new Rect(375,288,1040,275),ActiveRecord.Transcript,mono);}
            else GUI.Label(new Rect(375,280,1000,90),"NO INFORMATION RECORDED",mono);
            for(int y=148;y<590;y+=4)Fill(new Rect(337,y,1136,1),new Color(0,0,0,.09f));
            GUI.Label(new Rect(335,650,1050,32),"TRANSCRIPT",small);
            GUI.Label(new Rect(335,690,1120,140),ActiveRecord?ActiveRecord.Transcript:"Information discovered during exploration will appear here.",text);
            GUI.Label(new Rect(65,850,1150,30),RecoilInput.UsingGamepad?"UP / DOWN · SELECT     CIRCLE / R1 · CLOSE":"UP / DOWN · SELECT     ESC / J / TAB · CLOSE",small);
        }finally{GUI.matrix=matrix;GUI.color=color;}
    }
    void OnDisable(){Close();}
    void OnDestroy(){if(journalFont)Destroy(journalFont);}
}
