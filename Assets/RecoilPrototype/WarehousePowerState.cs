// Persistent cable-routing model. Power follows the connected cable, without switches.
public sealed class WarehousePowerState
{
 public const string Prefix="warehouse_power_",FreightPower=Prefix+"freight_on";
 public bool LightingCut,LightingHeld,FreightCut,FreightHeld,FreightConnected;
 public bool HoldingCable {get{return LightingHeld||FreightHeld;}}
 public bool LightingPowered {get{return !LightingCut&&!FreightConnected;}}
 public bool FreightPowered {get{return FreightConnected;}}
 public bool LightingTerminalPowered {get{return LightingPowered||FreightPowered;}}
 public bool LightingTerminalFree {get{return LightingCut&&!FreightConnected;}}
 public static WarehousePowerState Read(RecoilPrototype w){return new WarehousePowerState{LightingCut=w.HasItem(Prefix+"lighting_cut"),LightingHeld=w.HasItem(Prefix+"lighting_held"),FreightCut=w.HasItem(Prefix+"freight_cut"),FreightHeld=w.HasItem(Prefix+"freight_held"),FreightConnected=w.HasItem(Prefix+"freight_connected")};}
 public void Save(RecoilPrototype w){w.SetProgressFlag(Prefix+"lighting_off",false);w.SetProgressFlag(Prefix+"lighting_cut",LightingCut);w.SetProgressFlag(Prefix+"lighting_held",LightingHeld);w.SetProgressFlag(Prefix+"freight_cut",FreightCut);w.SetProgressFlag(Prefix+"freight_held",FreightHeld);w.SetProgressFlag(Prefix+"freight_connected",FreightConnected);w.SetProgressFlag(FreightPower,FreightPowered);}
 public string ReturnCable(){LightingHeld=false;FreightHeld=false;return "Cable returned to its disconnected position.";}
 public string ConnectLightingTerminal()
 {
  if(!HoldingCable)return "Select a disconnected cable first.";
  if(!LightingTerminalFree)return ReturnCable();
  if(LightingHeld){LightingCut=false;LightingHeld=false;return "Lighting cable reconnected. Warehouse lighting restored.";}
  FreightConnected=true;FreightCut=true;FreightHeld=false;return "Freight bypass connected through Lighting. Freight supply restored.";
 }
 public string ConnectFreightTerminal()
 {
  if(!FreightHeld||!FreightCut)return ReturnCable();
  FreightCut=false;FreightHeld=false;FreightConnected=false;return "Freight cable reconnected to its burned supply. No power.";
 }
 public string LightingCable()
 {
  if(!LightingCut){LightingCut=true;return "Lighting cable unplugged. The Lighting terminal is free.";}
  FreightHeld=false;LightingHeld=true;return "Holding Lighting cable. Select the free Lighting terminal.";
 }
 public string FreightCable()
 {
  if(FreightConnected){FreightConnected=false;FreightCut=true;FreightHeld=false;return "Freight bypass unplugged.";}
  if(!FreightCut){FreightCut=true;return "Freight cable unplugged from its burned supply.";}
  LightingHeld=false;FreightHeld=true;return "Holding Freight cable. Select a free terminal.";
 }
}
