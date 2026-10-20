using UnityEngine;

[DisallowMultipleComponent,AddComponentMenu("Recoil/Warehouse Lighting Power")]
public sealed class WarehouseLightingPower : MonoBehaviour
{
 RecoilPrototype world;RecoilLight2D[] lamps;bool[] authoredEnabled;bool applied,previous;
 void Start(){world=GetComponent<RecoilPrototype>();lamps=GetComponentsInChildren<RecoilLight2D>(true);authoredEnabled=new bool[lamps.Length];for(int i=0;i<lamps.Length;i++)authoredEnabled[i]=lamps[i].enabled;Apply();}
 void Update(){Apply();}
 void Apply(){if(!world||lamps==null)return;bool on=!world.HasItem(WarehousePowerState.Prefix+"lighting_cut") && !world.HasItem(WarehousePowerState.Prefix+"freight_connected");if(applied&&on==previous)return;applied=true;previous=on;for(int i=0;i<lamps.Length;i++)if(lamps[i] && !lamps[i].GetComponentInParent<FreightEquipmentLighting>())lamps[i].enabled=on&&authoredEnabled[i];}
}
