using UnityEngine;

[DisallowMultipleComponent,AddComponentMenu("Recoil/Electrical Panel Interaction")]
public sealed class ElectricalPanelInteraction : MonoBehaviour
{
 public string interactionPrompt="E / INSPECT DISTRIBUTION BOX";
 [Min(.1f)] public float interactionRange=1.8f;
 string notice="Lighting is connected. The Freight supply is burned out.";
 float lightingUnplugAt=float.NegativeInfinity,freightUnplugAt=float.NegativeInfinity;
 GUIStyle label,title,note;
 static readonly Rect LightingTerminalHit=new Rect(260,243,95,95),FreightTerminalHit=new Rect(420,243,95,95),CloseHit=new Rect(990,60,75,32);
 public bool InRange(RecoilPrototype w){return isActiveAndEnabled && w && w.Player && Vector2.Distance(w.Player.Position,transform.position)<=interactionRange && w.Clear(w.Player.Position,transform.position);}
 void Styles(){if(label!=null)return;label=new GUIStyle(GUI.skin.label){fontSize=18,normal={textColor=new Color(.8f,.79f,.68f)}};title=new GUIStyle(label){fontSize=26,fontStyle=FontStyle.Bold};note=new GUIStyle(label){fontSize=15,wordWrap=true};}
 static void Plate(Rect r,int color){GUI.color=RecoilPrototype.Hex(color);GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=Color.white;}
 void Text(Rect r,string s,GUIStyle style=null){GUI.Label(r,s,style??label);}
 static Rect LightingCableHit(WarehousePowerState s){return new Rect(s.FreightConnected?256:284,s.LightingCut?392:338,s.FreightConnected?36:48,s.LightingCut?166:195);}
 static Rect FreightCableHit(WarehousePowerState s){return s.FreightConnected?new Rect(289,333,36,158):new Rect(446,s.FreightCut?392:338,48,s.FreightCut?166:195);}
 static bool FreightCableContains(WarehousePowerState s,Vector2 p){return FreightCableHit(s).Contains(p)||(s.FreightConnected&&(new Rect(300,467,176,36).Contains(p)||new Rect(450,479,38,54).Contains(p)));}
 // A held cable accepts a compatible free terminal; every other click puts it down.
 public void ClickAt(RecoilPrototype w,Vector2 pointer)
 {
  var state=WarehousePowerState.Read(w);
  if(state.HoldingCable)
  {
   if(LightingTerminalHit.Contains(pointer) && state.LightingTerminalFree)notice=state.ConnectLightingTerminal();
   else if(FreightTerminalHit.Contains(pointer)&&state.FreightHeld&&state.FreightCut)notice=state.ConnectFreightTerminal();
   else notice=state.ReturnCable();
   state.Save(w);if(CloseHit.Contains(pointer))w.CloseElectricalPanel();return;
  }
  if(CloseHit.Contains(pointer)){w.CloseElectricalPanel();return;}
  // The bypass overlaps the disconnected Lighting cable: prefer the bypass above its elbow.
  if(state.FreightConnected&&FreightCableContains(state,pointer)){freightUnplugAt=Time.unscaledTime;notice=state.FreightCable();}
  else if(LightingCableHit(state).Contains(pointer)){if(!state.LightingCut)lightingUnplugAt=Time.unscaledTime;notice=state.LightingCable();}
  else if(FreightCableContains(state,pointer)){if(!state.FreightCut)freightUnplugAt=Time.unscaledTime;notice=state.FreightCable();}
  else return;
  state.Save(w);
 }
 static float PullOffset(bool unplugged,float at){return unplugged?54*Mathf.SmoothStep(0,1,Mathf.Clamp01((Time.unscaledTime-at)/.22f)):0;}
 static void CableSegment(Vector2 a,Vector2 b,int color,float thickness=9)
 {
  var old=GUI.matrix;GUI.matrix=old*Matrix4x4.TRS(a,Quaternion.Euler(0,0,Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg),Vector3.one);Plate(new Rect(0,-thickness/2,Vector2.Distance(a,b),thickness),color);GUI.matrix=old;
 }
 static void Connector(Vector2 p,int color){Plate(new Rect(p.x-10,p.y,20,16),color);Plate(new Rect(p.x-5,p.y-8,10,8),0xceac64);}
 public void DrawPage(RecoilPrototype w)
 {
  Styles();var matrix=GUI.matrix;Vector2 screenPointer=Event.current.mousePosition;GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,new Vector3(Screen.width/1280f,Screen.height/720f,1));
  Vector2 pointer=GUI.matrix.inverse.MultiplyPoint(screenPointer);
  if(Event.current.type==EventType.MouseDown && Event.current.button==0){ClickAt(w,pointer);Event.current.Use();}
  var state=WarehousePowerState.Read(w);
  Plate(new Rect(0,0,1280,720),0x070e10);Plate(new Rect(180,42,920,635),0x4a5149);Plate(new Rect(203,106,874,495),0x222e2b);
  Text(new Rect(224,59,680,38),"WAREHOUSE / DISTRIBUTION BOARD",title);
  Plate(CloseHit,CloseHit.Contains(pointer)?0x778574:0x596758);Text(new Rect(1000,64,65,28),"CLOSE",note);
  Plate(new Rect(265,132,750,12),0xa48e60);Text(new Rect(267,151,700,24),"INCOMING SUPPLY   /   L       N       PE",note);
  Plate(new Rect(235,565,805,7),0x738079);Text(new Rect(241,574,800,24),"NEUTRAL BAR                               PROTECTIVE EARTH",note);
  for(int i=0;i<5;i++){float x=275+i*160;bool powered=i==0&&state.LightingTerminalPowered;Plate(new Rect(x-2,189,82,15),0x283830);Plate(new Rect(x,191,78,11),powered?0x36f177:0x36443c);}
  Text(new Rect(255,206,160,28),"Lighting");Text(new Rect(417,206,160,28),"Freight");Text(new Rect(579,206,150,28),"AC");Text(new Rect(739,206,150,28),"Gate");Text(new Rect(899,206,160,28),"Ventilation");
  bool ready=state.HoldingCable&&state.LightingTerminalFree;
  bool freightReady=state.FreightHeld&&state.FreightCut;
  Plate(LightingTerminalHit,ready?0x638977:0x8b8d7a);Plate(FreightTerminalHit,freightReady?0x705c41:0x3b3029);
  Text(new Rect(277,270,90,28),"SUPPLY",note);Text(new Rect(430,270,90,28),"BURNED",note);
  // These blocks are terminals, with no switch or trip behavior.
  Plate(new Rect(297,330,22,10),ready?0x98f5ae:0x172720);Plate(new Rect(458,330,22,10),freightReady?0xffd88d:0x172720);
  for(int i=0;i<3;i++){float x=580+i*160;Plate(new Rect(x,243,95,95),0x39342e);Text(new Rect(x+9,268,90,50),"DAMAGED",note);Plate(new Rect(x+36,370,8,45),0x5a5550);Plate(new Rect(x+24,430,8,72),0x514a43);}
  int lightingColor=state.LightingCut?0x729a93:state.LightingPowered?0x4eddab:0x55b7bd;
  if(!state.HoldingCable&&LightingCableHit(state).Contains(pointer))lightingColor=0xb4ffdd;
  if(state.LightingHeld)
  {
   CableSegment(new Vector2(308,550),new Vector2(308,473),lightingColor);CableSegment(new Vector2(308,473),new Vector2(249,440),lightingColor);CableSegment(new Vector2(249,440),new Vector2(249,371),lightingColor);Connector(new Vector2(249,371),0xb4ffdd);
  }
  else
  {
   // Keep the unplugged Lighting cable beside the bypass, so both remain selectable.
   float x=state.FreightConnected?280:308;
   float lightTop=338+PullOffset(state.LightingCut,lightingUnplugAt);
   CableSegment(new Vector2(x,lightTop),new Vector2(x,Mathf.Min(lightTop+184,558)),lightingColor);Connector(new Vector2(x,lightTop),lightingColor);
  }
  int freightColor=!state.HoldingCable&&FreightCableContains(state,pointer)?0xffdf92:0xe5a65b;
  if(state.FreightConnected)
  {
   CableSegment(new Vector2(469,524),new Vector2(469,485),freightColor);CableSegment(new Vector2(469,485),new Vector2(308,485),freightColor);CableSegment(new Vector2(308,485),new Vector2(308,338),freightColor);Connector(new Vector2(308,338),freightColor);
  }
  else if(state.FreightHeld)
  {
   // Fixed suspended pose; it never follows the cursor.
   CableSegment(new Vector2(469,524),new Vector2(469,473),freightColor);CableSegment(new Vector2(469,473),new Vector2(388,440),freightColor);CableSegment(new Vector2(388,440),new Vector2(388,371),freightColor);Connector(new Vector2(388,371),0xffd88d);
  }
  else
  {
   float top=338+PullOffset(state.FreightCut,freightUnplugAt);CableSegment(new Vector2(469,top),new Vector2(469,Mathf.Min(top+184,558)),freightColor);Connector(new Vector2(469,top),freightColor);
  }
  Text(new Rect(226,611,805,44),notice,note);Text(new Rect(226,655,800,24),"Click cables to unplug / pick up; click a free terminal to connect.   E / Esc: close",note);GUI.matrix=matrix;
 }
}
