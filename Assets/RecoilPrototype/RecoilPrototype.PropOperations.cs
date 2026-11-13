using UnityEngine;
public sealed partial class RecoilPrototype
{
    RecoilPropOperation propOperation;
    RecoilInformationJournal informationJournal;
    public bool PropOperationActive {get{return propOperation&&propOperation.Active;}}
    public bool PropOperationBlocksConversation {get{return propOperation&&propOperation.BlocksConversation;}}
    public RecoilPropOperation PropOperation
    {get{if(!propOperation){propOperation=GetComponent<RecoilPropOperation>();if(!propOperation)propOperation=gameObject.AddComponent<RecoilPropOperation>();propOperation.Initialize(this);}return propOperation;}}
    public bool InformationOpen {get{return informationJournal&&informationJournal.IsOpen;}}
    public RecoilInformationJournal InformationJournal
    {get{if(!informationJournal){informationJournal=GetComponent<RecoilInformationJournal>();if(!informationJournal)informationJournal=gameObject.AddComponent<RecoilInformationJournal>();informationJournal.Initialize(this);}return informationJournal;}}
    public void OpenInformationJournal(){InformationJournal.Open();}
    bool HandleInformationInput()
    {
        if(InformationOpen){InformationJournal.Tick();FrameDelta=0;ReaderInputConsumed=true;return true;}
        if(RecoilInput.FilesPressed&&PlayerControlsAvailable){OpenInformationJournal();ReaderInputConsumed=true;return true;}return false;
    }
    void ResetPropOperation(){if(propOperation)propOperation.Cancel();if(informationJournal)informationJournal.Close();}
}
